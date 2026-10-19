import json

import pytest
from fastapi.testclient import TestClient
from sqlalchemy import select

from app.core.security import verify_token
from app.db.base_class import Base
from app.main import app, get_application
from app.models.user import User


@pytest.fixture()
def api(client):
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


@pytest.fixture()
def reading(api, db_session, seeded_spread_and_cards):
    guest = api.post("/api/v1/guest-session")
    assert guest.status_code == 200
    token = guest.json()["access_token"]
    db_session.get(User, verify_token(token)).is_superuser = True
    db_session.commit()
    headers = {"Authorization": f"Bearer {token}"}
    created = api.post("/api/v1/records/", headers=headers, json={
        "question": "Retained question", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    record_id = created.json()["id"]
    assert api.post(f"/api/v1/records/{record_id}/draw", headers=headers).status_code == 200
    return record_id, headers


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _response_json(response):
    response.content.decode("utf-8", errors="strict")
    result = json.loads(response.content)
    # Decoded keys and values must also be safe, not just the response bytes.
    json.dumps(result, ensure_ascii=False, allow_nan=False).encode("utf-8")
    return result


def _requests(value, record_id, spread_id, card_id):
    card = {"name_en": value, "name_zh": "Card", "card_number": 99,
            "card_type": "major_arcana", "upright_meaning": "Upright", "reversed_meaning": "Reversed"}
    spread = {"name": "New spread", "description": "Spread", "card_count": 1,
              "positions": [{"position": 1, "name": value, "meaning": "Meaning"}]}
    return [
        ("POST", "/api/v1/register", {"username": value, "email": "invalid@example.com", "password": "password123"}, ["username"]),
        ("PUT", "/api/v1/users/me", {"username": value}, ["username"]),
        ("POST", "/api/v1/records/", {"question": value, "question_type": "general", "spread_type_id": spread_id}, ["question"]),
        ("PUT", f"/api/v1/records/{record_id}", {"user_notes": value, "is_favorite": True}, ["user_notes"]),
        ("POST", f"/api/v1/records/{record_id}/interpret", {"overall_interpretation": value}, ["overall_interpretation"]),
        ("PUT", f"/api/v1/records/{record_id}/interpretation", {"overall_interpretation": value, "summary": "Must not change"}, ["overall_interpretation"]),
        ("POST", f"/api/v1/records/{record_id}/interpret?force_ai=true", {"overall_interpretation": value}, ["overall_interpretation"]),
        ("POST", "/api/v1/cards/", card, ["name_en"]),
        ("POST", "/api/v1/cards/batch", [{**card, "name_en": "Valid first card"}, card], [1, "name_en"]),
        ("PUT", f"/api/v1/cards/{card_id}", {"name_en": value, "description": "Must not change"}, ["name_en"]),
        ("POST", "/api/v1/spreads/", spread, ["positions", 0, "name"]),
        ("PUT", f"/api/v1/spreads/{spread_id}", spread, ["positions", 0, "name"]),
    ]


@pytest.mark.parametrize("index", range(12))
@pytest.mark.parametrize("invalid,safe", [("bad\ud800text", "bad\\ud800text"), ("bad\udffftext", "bad\\udffftext")],
                         ids=["unpaired-high", "unpaired-low"])
def test_invalid_unicode_returns_422_before_any_write(
    api, db_session, reading, seeded_spread_and_cards, index, invalid, safe,
):
    record_id, headers = reading
    if index == 5:
        assert api.post(f"/api/v1/records/{record_id}/interpret", headers=headers,
                        json={"overall_interpretation": "Retained text", "summary": "Retained summary"}).status_code == 200
    # Unconstrained text fields accept raw surrogates; use a rejected type there.
    value = invalid if index in (0, 1, 2) else [invalid]
    method, path, payload, location = _requests(
        value, record_id, seeded_spread_and_cards["spread_id"], seeded_spread_and_cards["card_ids"][0],
    )[index]
    before = _snapshot(db_session)
    response = api.request(method, path, headers={**headers, "Content-Type": "application/json"},
                           content=json.dumps(payload).encode("ascii"))
    assert response.status_code == 422
    error = _response_json(response)["detail"][0]
    assert error["loc"] == ["body", *location]
    assert error["type"] == ("string_unicode" if index in (0, 1, 2) else "string_type")
    assert error["input"] == (safe if index in (0, 1, 2) else [safe])
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("fresh_app", [False, True])
@pytest.mark.parametrize("whole_body", [False, True])
def test_nested_invalid_unicode_keys_values_and_nonfinite_numbers_are_safe(api, db_session, fresh_app, whole_body):
    nested = {"bad\ud800key": ["bad\udfffvalue", float("inf"), float("nan")]}
    payload = [nested] if whole_body else {"username": nested, "email": "nested@example.com", "password": "password123"}
    expected = {"bad\\ud800key": ["bad\\udfffvalue", "inf", "nan"]}
    before = _snapshot(db_session)
    target = TestClient(get_application(), raise_server_exceptions=False) if fresh_app else api
    try:
        response = target.post("/api/v1/register", content=json.dumps(payload).encode("ascii"),
                               headers={"Content-Type": "application/json"})
        assert response.status_code == 422
        error = _response_json(response)["detail"][0]
        assert error["loc"] == (["body"] if whole_body else ["body", "username"])
        assert error["input"] == ([expected] if whole_body else expected)
    finally:
        if fresh_app:
            target.close()
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("text", ["Tarot English", "\u5854\u7f57\u4e2d\u6587\U0001f52e e\u0301 \ufffd", "literal \\ud800"])
@pytest.mark.parametrize("escaped", [False, True])
def test_valid_unicode_survives_normal_writes_and_validation_error_echoes(api, reading, text, escaped):
    record_id, headers = reading
    response = api.put(f"/api/v1/records/{record_id}", headers={**headers, "Content-Type": "application/json"},
                       content=json.dumps({"user_notes": text}, ensure_ascii=escaped).encode("utf-8"))
    assert response.status_code == 200
    assert _response_json(response)["user_notes"] == text
    detail = api.get(f"/api/v1/records/{record_id}", headers=headers)
    assert detail.status_code == 200
    assert _response_json(detail)["user_notes"] == text
    response = api.post("/api/v1/register", headers={"Content-Type": "application/json"},
                        content=json.dumps({"username": [text], "email": "valid@example.com", "password": "password123"},
                                           ensure_ascii=escaped).encode("utf-8"))
    assert response.status_code == 422
    error = _response_json(response)["detail"][0]
    assert error["loc"] == ["body", "username"]
    assert error["type"] == "string_type"
    assert error["input"] == [text]
