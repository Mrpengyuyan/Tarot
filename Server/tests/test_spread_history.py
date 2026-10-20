from copy import deepcopy

import pytest
from sqlalchemy import event, select

from app.core.security import create_access_token, get_password_hash
from app.crud import spread as spread_crud
from app.db.base_class import Base
from app.models.record import Prediction, PredictionStatus, QuestionType
from app.models.spread import SpreadType
from app.models.user import User
from app.schemas.spread import SpreadTypeUpdate


@pytest.fixture(scope="module")
def password_hash():
    return get_password_hash("history-test-password")


@pytest.fixture()
def admin(client, db_session, password_hash):
    user = User(username="history_admin", email="history@example.com",
                hashed_password=password_hash, is_superuser=True)
    db_session.add(user)
    db_session.commit()
    return {
        "id": user.id,
        "headers": {"Authorization": "Bearer " + create_access_token(
            {"sub": str(user.id), "sub_type": "user_id"},
        )},
    }


@pytest.fixture(params=["pending", "drawn", "completed", "failed"])
def reading(request, client, db_session, admin, seeded_spread_and_cards):
    response = client.post("/api/v1/records/", headers=admin["headers"], json={
        "question": "Retained history question", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert response.status_code == 200
    record_id = response.json()["id"]
    if request.param != "pending":
        assert client.post(f"/api/v1/records/{record_id}/draw", headers=admin["headers"]).status_code == 200
    if request.param == "completed":
        response = client.post(f"/api/v1/records/{record_id}/interpret", headers=admin["headers"], json={
            "overall_interpretation": "Original past/present/future interpretation",
            "card_analysis": "Retained analysis", "confidence_score": 0.8,
        })
        assert response.status_code == 200
    elif request.param == "failed":
        db_session.get(Prediction, record_id).status = PredictionStatus.FAILED
        db_session.commit()
    return {"id": record_id, "state": request.param,
            "spread_id": seeded_spread_and_cards["spread_id"]}


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _layout_change(spread, kind):
    positions = deepcopy(spread["positions"])
    payload = {"description": "Must not replace description", "is_active": False}
    if kind == "card_count":
        payload["card_count"] = 2
        positions = positions[:2]
    elif kind == "position":
        positions[0]["position"], positions[1]["position"] = 2, 1
    else:
        positions[0][kind] = "Must not replace original position"
    return {**payload, "positions": positions}


@pytest.mark.parametrize("kind", ["name", "meaning", "position", "card_count"])
def test_referenced_layout_changes_reject_the_entire_update(
    client, db_session, admin, reading, kind,
):
    spread_url = f"/api/v1/spreads/{reading['spread_id']}"
    record_url = f"/api/v1/records/{reading['id']}"
    spread = client.get(spread_url).json()
    before_detail = client.get(record_url, headers=admin["headers"]).json()
    before_cards = client.get(f"{record_url}/cards", headers=admin["headers"]).json()
    before = _snapshot(db_session)

    response = client.put(spread_url, headers=admin["headers"], json=_layout_change(spread, kind))

    assert response.status_code == 409
    assert _snapshot(db_session) == before
    assert client.get(spread_url).json() == spread
    assert client.get(record_url, headers=admin["headers"]).json() == before_detail
    assert client.get(f"{record_url}/cards", headers=admin["headers"]).json() == before_cards
    if reading["state"] == "pending":
        drawn = client.post(f"{record_url}/draw", headers=admin["headers"])
        assert drawn.status_code == 200
        assert len(drawn.json()["card_draws"]) == 3


@pytest.mark.parametrize("mode", ["empty", "layout", "full"])
def test_referenced_spread_accepts_unchanged_layouts(client, db_session, admin, reading, mode):
    url = f"/api/v1/spreads/{reading['spread_id']}"
    spread = client.get(url).json()
    payload = {} if mode == "empty" else {"card_count": 3, "positions": spread["positions"]}
    if mode == "full":
        payload = {key: value for key, value in spread.items() if key not in {"id", "usage_count"}}
    before = _snapshot(db_session)
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    assert response.json() == spread
    assert _snapshot(db_session) == before


def test_referenced_layout_allows_other_settings_to_change(client, admin, reading):
    url = f"/api/v1/spreads/{reading['spread_id']}"
    spread = client.get(url).json()
    payload = {
        "name": "Updated display name", "name_en": None, "description": "Updated description",
        "difficulty_level": 5, "layout_image_url": None, "is_active": False,
        "is_beginner_friendly": False, "suitable_for_love": False, "suitable_for_career": False,
        "suitable_for_finance": False, "suitable_for_health": False, "suitable_for_general": False,
        "card_count": 3, "positions": spread["positions"],
    }
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    saved = client.get(url).json()
    assert all(saved[field] == value for field, value in payload.items())
    assert saved["usage_count"] == spread["usage_count"]
    if reading["state"] == "pending":
        drawn = client.post(f"/api/v1/records/{reading['id']}/draw", headers=admin["headers"])
        assert drawn.status_code == 200
        assert len(drawn.json()["card_draws"]) == 3


def test_direct_crud_cannot_modify_a_referenced_layout(db_session, reading):
    spread = db_session.get(SpreadType, reading["spread_id"])
    positions = deepcopy(spread.positions)
    positions[0]["meaning"] = "Must not overwrite history"
    before = _snapshot(db_session)
    with pytest.raises(ValueError):
        spread_crud.update_spread(db_session, spread, SpreadTypeUpdate(
            positions=positions, description="Must not partially save",
        ))
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("kind", ["meaning", "card_count"])
def test_unreferenced_spreads_remain_editable(
    client, db_session, admin, seeded_spread_and_cards, kind,
):
    url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}"
    spread = client.get(url).json()
    # A usage counter alone is not a history reference.
    db_session.get(SpreadType, spread["id"]).usage_count = 50
    db_session.commit()
    payload = _layout_change(spread, kind)
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    saved = client.get(url).json()
    assert all(saved[field] == value for field, value in payload.items())
    assert saved["usage_count"] == 50


def test_deleting_last_reference_allows_layout_editing(client, admin, reading):
    assert client.delete(f"/api/v1/records/{reading['id']}", headers=admin["headers"]).status_code == 200
    url = f"/api/v1/spreads/{reading['spread_id']}"
    payload = _layout_change(client.get(url).json(), "card_count")
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    assert response.json()["card_count"] == 2


def test_reference_created_immediately_before_update_is_still_protected(
    client, db_session, db_session_factory, admin, seeded_spread_and_cards,
):
    spread_id = seeded_spread_and_cards["spread_id"]
    url = f"/api/v1/spreads/{spread_id}"
    spread = client.get(url).json()
    inserted = []
    engine = db_session.get_bind()

    def insert_reference(_connection, _cursor, statement, _parameters, _context, _executemany):
        if inserted or not statement.lstrip().upper().startswith("UPDATE SPREAD_TYPES"):
            return
        inserted.append(True)
        with db_session_factory() as other:
            other.add(Prediction(user_id=admin["id"], spread_type_id=spread_id,
                                 question="Concurrent reading", question_type=QuestionType.GENERAL))
            other.commit()

    event.listen(engine, "before_cursor_execute", insert_reference)
    try:
        response = client.put(url, headers=admin["headers"], json=_layout_change(spread, "card_count"))
    finally:
        event.remove(engine, "before_cursor_execute", insert_reference)

    assert inserted == [True]
    assert response.status_code == 409
    assert client.get(url).json() == spread
    db_session.expire_all()
    assert db_session.scalars(select(Prediction)).one().spread_type_id == spread_id


def test_reference_guard_is_scoped_to_the_target_spread(client, db_session, admin, reading):
    other = SpreadType(name="Unused other spread", description="Independent layout", card_count=1,
                       positions=[{"position": 1, "name": "Original", "meaning": "Original meaning"}])
    db_session.add(other)
    db_session.commit()
    positions = [{"position": 1, "name": "Changed", "meaning": "Changed meaning"}]
    response = client.put(f"/api/v1/spreads/{other.id}", headers=admin["headers"], json={"positions": positions})
    assert response.status_code == 200
    assert response.json()["positions"] == positions


def test_stale_unchanged_layout_does_not_overwrite_a_newly_referenced_layout(
    client, db_session, admin, seeded_spread_and_cards,
):
    spread_id = seeded_spread_and_cards["spread_id"]
    stale_spread = db_session.get(SpreadType, spread_id)
    unchanged_payload = SpreadTypeUpdate(card_count=3, positions=deepcopy(stale_spread.positions),
                                        description="Updated without replacing layout")
    new_positions = [{"position": 1, "name": "Cause", "meaning": "New cause"},
                     {"position": 2, "name": "Result", "meaning": "New result"}]
    changed = client.put(f"/api/v1/spreads/{spread_id}", headers=admin["headers"],
                         json={"card_count": 2, "positions": new_positions})
    assert changed.status_code == 200
    created = client.post("/api/v1/records/", headers=admin["headers"], json={
        "question": "New layout reading", "question_type": "general", "spread_type_id": spread_id,
    })
    assert created.status_code == 200

    spread_crud.update_spread(db_session, stale_spread, unchanged_payload)

    saved = client.get(f"/api/v1/spreads/{spread_id}").json()
    assert saved["card_count"] == 2
    assert saved["positions"] == new_positions
    assert saved["description"] == "Updated without replacing layout"
    drawn = client.post(f"/api/v1/records/{created.json()['id']}/draw", headers=admin["headers"])
    assert drawn.status_code == 200
    assert len(drawn.json()["card_draws"]) == 2
