import pytest
from fastapi.testclient import TestClient
from sqlalchemy import select

from app.core.security import verify_token
from app.db.base_class import Base
from app.main import app
from app.models.tarot_card import TarotCard
from app.models.user import User


MIN_INTEGER = -(2**63)
MAX_INTEGER = 2**63 - 1
MODES = ["create", "batch", "update"]


@pytest.fixture()
def api(client):
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


@pytest.fixture()
def guest_headers(api):
    response = api.post("/api/v1/guest-session")
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


@pytest.fixture()
def admin_headers(guest_headers, db_session):
    token = guest_headers["Authorization"].removeprefix("Bearer ")
    db_session.get(User, verify_token(token)).is_superuser = True
    db_session.commit()
    return guest_headers


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _card(number):
    return {
        "name_en": "Boundary card", "name_zh": "Boundary card",
        "card_number": number, "card_type": "major_arcana",
        "upright_meaning": "Upright", "reversed_meaning": "Reversed",
    }


def _write(api, mode, headers, card_id, fields):
    if mode == "update":
        return api.put(f"/api/v1/cards/{card_id}", headers=headers, json=fields)
    payload = {**_card(99), **fields}
    if mode == "batch":
        return api.post("/api/v1/cards/batch", headers=headers,
                        json=[_card(1000), payload, _card(1001)])
    return api.post("/api/v1/cards/", headers=headers, json=payload)


@pytest.mark.parametrize("mode", MODES)
@pytest.mark.parametrize("number", [
    MIN_INTEGER - 1, MAX_INTEGER + 1, -(10**100), 10**100,
    str(MIN_INTEGER - 1), str(MAX_INTEGER + 1),
], ids=["below-min", "above-max", "huge-negative", "huge-positive", "string-below", "string-above"])
def test_overflowing_card_numbers_reject_the_whole_write(
    api, db_session, admin_headers, seeded_spread_and_cards, mode, number,
):
    before = _snapshot(db_session)
    response = _write(api, mode, admin_headers, seeded_spread_and_cards["card_ids"][0],
                      {"card_number": number, "name_en": "Must not be written"})
    assert response.status_code == 422
    location = ["body", 1, "card_number"] if mode == "batch" else ["body", "card_number"]
    assert response.json()["detail"][0]["loc"] == location
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("mode", MODES)
@pytest.mark.parametrize("number", [MIN_INTEGER, -1, 0, MAX_INTEGER, "99", 99.0])
def test_representable_card_numbers_keep_existing_write_and_read_behavior(
    api, db_session, admin_headers, seeded_spread_and_cards, mode, number,
):
    response = _write(api, mode, admin_headers, seeded_spread_and_cards["card_ids"][0],
                      {"card_number": number})
    assert response.status_code == 200
    card = response.json()[1] if mode == "batch" else response.json()
    assert card["card_number"] == int(number)
    db_session.expire_all()
    assert db_session.get(TarotCard, card["id"]).card_number == int(number)
    assert api.get(f"/api/v1/cards/{card['id']}").json()["card_number"] == int(number)
    assert db_session.query(TarotCard).count() == {"create": 5, "batch": 7, "update": 4}[mode]


@pytest.mark.parametrize("mode", MODES)
def test_null_card_numbers_remain_rejected_without_writes(
    api, db_session, admin_headers, seeded_spread_and_cards, mode,
):
    before = _snapshot(db_session)
    response = _write(api, mode, admin_headers, seeded_spread_and_cards["card_ids"][0],
                      {"card_number": None, "name_en": "Must not be written"})
    assert response.status_code == 422
    assert _snapshot(db_session) == before


def test_omitting_card_number_on_update_keeps_the_existing_number(
    api, admin_headers, seeded_spread_and_cards,
):
    card_id = seeded_spread_and_cards["card_ids"][0]
    response = _write(api, "update", admin_headers, card_id, {"name_en": "Updated name"})
    assert response.status_code == 200
    assert response.json()["card_number"] == 1
    saved = api.get(f"/api/v1/cards/{card_id}").json()
    assert saved["card_number"] == 1
    assert saved["name_en"] == "Updated name"


@pytest.mark.parametrize("mode", MODES)
@pytest.mark.parametrize("authenticated,status", [(True, 403), (False, 401)])
def test_card_number_writes_keep_admin_permission_requirements(
    api, db_session, guest_headers, seeded_spread_and_cards, mode, authenticated, status,
):
    before = _snapshot(db_session)
    api.cookies.clear()
    response = _write(api, mode, guest_headers if authenticated else {},
                      seeded_spread_and_cards["card_ids"][0], {"card_number": 99})
    assert response.status_code == status
    assert _snapshot(db_session) == before
