import pytest
from pydantic import ValidationError
from sqlalchemy import select, text

from app.models.user import User
from app.schemas.card import TarotCardUpdate
from app.schemas.prediction import InterpretationUpdate, PredictionUpdate
from app.schemas.spread import SpreadTypeUpdate
from app.schemas.user import UserUpdate


@pytest.fixture()
def admin_headers(client, db_session):
    response = client.post("/api/v1/guest-session")
    assert response.status_code == 200
    user = db_session.scalars(select(User)).one()
    user.is_superuser = True
    db_session.commit()
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


@pytest.fixture()
def saved_targets(client, admin_headers, seeded_spread_and_cards):
    record = client.post("/api/v1/records/", headers=admin_headers, json={
        "question": "Write validation acceptance",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert record.status_code == 200
    record_id = record.json()["id"]
    assert client.post(f"/api/v1/records/{record_id}/draw", headers=admin_headers).status_code == 200
    interpretation = client.post(f"/api/v1/records/{record_id}/interpret", headers=admin_headers, json={
        "overall_interpretation": "Retained interpretation",
        "card_analysis": "Original analysis",
        "summary": "Original summary",
    })
    assert interpretation.status_code == 200
    return {
        "user": "/api/v1/users/me",
        "card": f"/api/v1/cards/{seeded_spread_and_cards['card_ids'][0]}",
        "spread": f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}",
        "record": f"/api/v1/records/{record_id}",
        "interpretation": f"/api/v1/records/{record_id}/interpretation",
    }


def _card_payload():
    return {
        "name_en": "Test Wand",
        "name_zh": "TestWand",
        "card_number": 1,
        "card_type": "minor_arcana",
        "suit": "wands",
        "upright_meaning": "Progress",
        "reversed_meaning": "Delay",
    }


def _spread_payload(numbers=(1, 2, 3)):
    return {
        "name": "Write Validation Spread",
        "description": "Test layout",
        "card_count": 3,
        "positions": [{"position": n, "name": f"Position {n}", "meaning": "Meaning"} for n in numbers],
    }


@pytest.mark.parametrize("batch", [False, True])
def test_created_card_enums_can_be_read_back(client, admin_headers, db_session, batch):
    payload = _card_payload()
    response = client.post("/api/v1/cards/batch" if batch else "/api/v1/cards/",
                           headers=admin_headers, json=[payload] if batch else payload)
    assert response.status_code == 200
    card = response.json()[0] if batch else response.json()
    saved = client.get(f"/api/v1/cards/{card['id']}")
    assert saved.status_code == 200
    assert saved.json()["card_type"] == "minor_arcana"
    assert saved.json()["suit"] == "wands"
    assert db_session.execute(text("SELECT card_type, suit FROM tarot_cards WHERE id = :id"),
                              {"id": card["id"]}).one() == ("MINOR_ARCANA", "WANDS")


def test_card_enum_update_and_optional_suit_clear(client, admin_headers, seeded_spread_and_cards):
    url = f"/api/v1/cards/{seeded_spread_and_cards['card_ids'][0]}"
    updated = client.put(url, headers=admin_headers, json={"card_type": "minor_arcana", "suit": "cups"})
    assert updated.status_code == 200
    saved = client.get(url).json()
    assert saved["card_type"] == "minor_arcana"
    assert saved["suit"] == "cups"
    assert client.put(url, headers=admin_headers, json={"suit": None}).status_code == 200
    saved = client.get(url).json()
    assert saved["card_type"] == "minor_arcana"
    assert saved["suit"] is None


@pytest.mark.parametrize("model,field", [
    (UserUpdate, "username"), (UserUpdate, "email"), (UserUpdate, "password"),
    (PredictionUpdate, "is_favorite"), (InterpretationUpdate, "overall_interpretation"),
    *[(TarotCardUpdate, field) for field in (
        "name_en", "name_zh", "card_number", "card_type", "upright_meaning", "reversed_meaning",
    )],
    *[(SpreadTypeUpdate, field) for field in (
        "name", "description", "card_count", "difficulty_level", "positions", "is_active",
        "is_beginner_friendly", "suitable_for_love", "suitable_for_career", "suitable_for_finance",
        "suitable_for_health", "suitable_for_general",
    )],
])
def test_nonnullable_update_field_rejects_explicit_null(model, field):
    with pytest.raises(ValidationError):
        model.model_validate({field: None})


@pytest.mark.parametrize("target,field", [
    ("user", "password"), ("card", "name_zh"), ("spread", "positions"),
    ("record", "is_favorite"), ("interpretation", "overall_interpretation"),
])
def test_invalid_null_update_does_not_change_saved_data(client, admin_headers, saved_targets, target, field):
    url = saved_targets[target]
    before = client.get(url, headers=admin_headers).json()
    response = client.put(url, headers=admin_headers, json={field: None})
    assert response.status_code == 422
    assert client.get(url, headers=admin_headers).json() == before


@pytest.mark.parametrize("target", ["user", "card", "spread", "record", "interpretation"])
def test_omitted_update_fields_keep_saved_values(client, admin_headers, saved_targets, target):
    url = saved_targets[target]
    before = client.get(url, headers=admin_headers).json()
    assert client.put(url, headers=admin_headers, json={}).status_code == 200
    assert client.get(url, headers=admin_headers).json() == before


@pytest.mark.parametrize("target,payload", [
    ("card", {"image_url": None, "description": None}),
    ("spread", {"name_en": None, "layout_image_url": None}),
    ("record", {"user_notes": None, "user_rating": None}),
    ("interpretation", {"card_analysis": None, "summary": None}),
])
def test_nullable_update_fields_can_still_be_cleared(client, admin_headers, saved_targets, target, payload):
    url = saved_targets[target]
    initial = {field: (4 if field == "user_rating" else "Set value") for field in payload}
    assert client.put(url, headers=admin_headers, json=initial).status_code == 200
    populated = client.get(url, headers=admin_headers).json()
    assert {field: populated[field] for field in payload} == initial
    assert client.put(url, headers=admin_headers, json=payload).status_code == 200
    saved = client.get(url, headers=admin_headers).json()
    assert all(saved[field] is None for field in payload)


def test_spread_count_only_update_cannot_leave_old_positions(client, admin_headers, seeded_spread_and_cards):
    url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}"
    before = client.get(url).json()
    response = client.put(url, headers=admin_headers, json={"card_count": 2})
    assert response.status_code == 400
    assert client.get(url).json() == before


@pytest.mark.parametrize("mode", ["create", "batch", "update"])
@pytest.mark.parametrize("numbers", [(1, 1, 3), (1, 2, 4), (0, 2, 3)])
def test_spread_positions_must_cover_each_drawn_card_once(
    client, admin_headers, seeded_spread_and_cards, mode, numbers,
):
    payload = _spread_payload(numbers)
    before = client.get("/api/v1/spreads/").json()
    if mode == "update":
        url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}"
        response = client.put(url, headers=admin_headers, json={"positions": payload["positions"]})
    else:
        response = client.post("/api/v1/spreads/batch" if mode == "batch" else "/api/v1/spreads/",
                               headers=admin_headers, json=[payload] if mode == "batch" else payload)
    assert response.status_code == 400
    assert client.get("/api/v1/spreads/").json() == before


@pytest.mark.parametrize("mode", ["create", "batch", "update"])
@pytest.mark.parametrize("field,value", [("card_count", 0), ("card_count", 79),
                                         ("difficulty_level", 0), ("difficulty_level", 6)])
def test_spread_write_rejects_values_outside_supported_ranges(
    client, admin_headers, seeded_spread_and_cards, mode, field, value,
):
    payload = {**_spread_payload(), field: value}
    if mode == "update":
        response = client.put(f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}",
                              headers=admin_headers, json={field: value})
    else:
        response = client.post("/api/v1/spreads/batch" if mode == "batch" else "/api/v1/spreads/",
                               headers=admin_headers, json=[payload] if mode == "batch" else payload)
    assert response.status_code == 422


def test_spread_count_and_layout_can_change_together(client, admin_headers, seeded_spread_and_cards):
    url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}"
    positions = _spread_payload((2, 1))["positions"]
    response = client.put(url, headers=admin_headers, json={"card_count": 2, "positions": positions})
    assert response.status_code == 200
    saved = client.get(url).json()
    assert saved["card_count"] == 2
    assert saved["positions"] == positions
