import json

import pytest
from fastapi.testclient import TestClient
from pydantic import ValidationError
from sqlalchemy import select

from app.core.security import verify_token
from app.db.base_class import Base
from app.main import app, get_application
from app.models.user import User
from app.schemas.card import TarotCard, TarotCardCreate, TarotCardUpdate
from app.schemas.prediction import Interpretation, InterpretationCreate, InterpretationUpdate, Prediction, PredictionUpdate
from app.schemas.spread import SpreadType, SpreadTypeCreate, SpreadTypeUpdate


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
    # Exercise nested error inputs as well as invalid scalar text.
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


def _card_payload():
    return {"name_en": "New card", "name_zh": "Card", "card_number": 99,
            "card_type": "major_arcana", "upright_meaning": "Up", "reversed_meaning": "Down"}


def _spread_payload(name="New spread", text="Position"):
    return {"name": name, "description": "Spread", "card_count": 1,
            "positions": [{"position": 1, "name": text, "meaning": text}]}


@pytest.mark.parametrize("index", range(3, 15), ids=[
    "notes", "interpretation-create", "interpretation-update", "force-ai", "card-create", "card-batch",
    "card-update", "spread-create-position", "spread-update-position", "spread-create-name",
    "spread-create-meaning", "spread-batch",
])
@pytest.mark.parametrize("invalid", ["bad\ud800text", "bad\udffftext"], ids=["high", "low"])
def test_raw_invalid_unicode_is_rejected_without_writes_or_poisoned_lists(
    api, db_session, reading, seeded_spread_and_cards, index, invalid,
):
    record_id, headers = reading
    if index == 5:
        assert api.post(f"/api/v1/records/{record_id}/interpret", headers=headers,
                        json={"overall_interpretation": "Retained text", "summary": "Retained summary"}).status_code == 200
    cases = _requests(invalid, record_id, seeded_spread_and_cards["spread_id"], seeded_spread_and_cards["card_ids"][0])
    meaning_spread = _spread_payload()
    meaning_spread["positions"][0]["meaning"] = invalid
    cases.extend([
        ("POST", "/api/v1/spreads/", _spread_payload(name=invalid), []),
        ("POST", "/api/v1/spreads/", meaning_spread, []),
        ("POST", "/api/v1/spreads/batch", [_spread_payload("Valid first"), _spread_payload("Invalid second", invalid)], []),
    ])
    before = _snapshot(db_session)
    cards_before = api.get("/api/v1/cards/").json()
    spreads_before = api.get("/api/v1/spreads/").json()
    method, path, payload, _ = cases[index]
    response = api.request(method, path, headers={**headers, "Content-Type": "application/json"},
                           content=json.dumps(payload).encode("ascii"))
    assert response.status_code == 422
    assert _response_json(response)["detail"][0]["loc"][0] == "body"
    assert _snapshot(db_session) == before
    for url, expected in [("/api/v1/cards/", cards_before), ("/api/v1/spreads/", spreads_before)]:
        saved = api.get(url)
        assert saved.status_code == 200
        assert _response_json(saved) == expected


CARD_TEXT_FIELDS = (
    "name_en", "name_zh", "image_url", "upright_meaning", "upright_love", "upright_career", "upright_finance",
    "upright_health", "reversed_meaning", "reversed_love", "reversed_career", "reversed_finance", "reversed_health",
    "description", "keywords_upright", "keywords_reversed", "element", "zodiac", "planet",
)
INTERPRETATION_TEXT_FIELDS = (
    "overall_interpretation", "card_analysis", "relationship_analysis", "advice", "warning", "summary",
    "key_themes", "model_used", "model_version",
)
TEXT_FIELDS = [
    *[(model, field, _card_payload() if model is TarotCardCreate else {})
      for model in (TarotCardCreate, TarotCardUpdate) for field in CARD_TEXT_FIELDS],
    *[(model, field, {"overall_interpretation": "Retained text"} if model is InterpretationCreate else {})
      for model in (InterpretationCreate, InterpretationUpdate) for field in INTERPRETATION_TEXT_FIELDS],
    *[(model, field, _spread_payload() if model is SpreadTypeCreate else {})
      for model in (SpreadTypeCreate, SpreadTypeUpdate) for field in ("name", "name_en", "description", "layout_image_url")],
    (PredictionUpdate, "user_notes", {}),
]


@pytest.mark.parametrize("model,field,payload", TEXT_FIELDS, ids=[f"{model.__name__}-{field}" for model, field, _ in TEXT_FIELDS])
@pytest.mark.parametrize("invalid", ["bad\ud800text", "bad\udffftext"], ids=["high", "low"])
def test_every_declared_text_write_field_rejects_invalid_unicode(model, field, payload, invalid):
    with pytest.raises(ValidationError) as rejected:
        model.model_validate({**payload, field: invalid})
    assert rejected.value.errors()[0]["loc"] == (field,)


@pytest.mark.parametrize("model", [SpreadTypeCreate, SpreadTypeUpdate])
@pytest.mark.parametrize("field", ["name", "meaning"])
@pytest.mark.parametrize("invalid", ["bad\ud800text", "bad\udffftext"], ids=["high", "low"])
def test_nested_position_text_is_validated_on_both_spread_write_models(model, field, invalid):
    payload = _spread_payload()
    payload["positions"][0][field] = invalid
    with pytest.raises(ValidationError) as rejected:
        model.model_validate(payload)
    assert rejected.value.errors()[0]["loc"][0] == "positions"


@pytest.mark.parametrize("value", ["Tarot English", "\u5854\u7f57 \U0001f52e e\u0301 \ufffd", "literal \\ud800", ""])
@pytest.mark.parametrize("escaped", [False, True])
def test_valid_text_null_and_omission_keep_their_write_contracts(api, reading, value, escaped):
    record_id, headers = reading

    def request(method, path, payload):
        response = api.request(method, path, headers={**headers, "Content-Type": "application/json"},
                               content=json.dumps(payload, ensure_ascii=escaped).encode("utf-8"))
        assert response.status_code == 200
        return _response_json(response)

    note_url = f"/api/v1/records/{record_id}"
    assert request("PUT", note_url, {"user_notes": value})["user_notes"] == value
    interpretation = request("POST", f"{note_url}/interpret", {"overall_interpretation": value, "advice": value})
    card = request("POST", "/api/v1/cards/", {**_card_payload(), "name_en": value, "description": value})
    spread = request("POST", "/api/v1/spreads/", {**_spread_payload(text=value), "name": value, "name_en": value})
    assert interpretation["overall_interpretation"] == card["name_en"] == spread["name"] == value
    assert spread["positions"] == [{"position": 1, "name": value, "meaning": value}]
    for url, field in [(note_url, "user_notes"), (f"{note_url}/interpretation", "advice"),
                       (f"/api/v1/cards/{card['id']}", "description"), (f"/api/v1/spreads/{spread['id']}", "name_en")]:
        saved = api.get(url, headers=headers)
        assert saved.status_code == 200 and _response_json(saved)[field] == value
        assert request("PUT", url, {})[field] == value
        assert request("PUT", url, {field: None})[field] is None


def test_unknown_fields_remain_ignored_instead_of_being_scanned(api, reading):
    record_id, headers = reading
    response = api.put(f"/api/v1/records/{record_id}", headers={**headers, "Content-Type": "application/json"},
                       content=json.dumps({"user_notes": "Retained note", "unused": "bad\ud800text"}).encode("ascii"))
    assert response.status_code == 200
    assert _response_json(response)["user_notes"] == "Retained note"
    assert "unused" not in response.json()


@pytest.mark.parametrize("invalid", ["bad\ud800text", "bad\udffftext"], ids=["high", "low"])
def test_response_models_do_not_gain_new_historical_text_restrictions(invalid):
    card = TarotCard.model_validate({**_card_payload(), "id": 1, "description": invalid})
    spread = SpreadType.model_validate({**_spread_payload(text=invalid), "id": 1})
    interpretation = Interpretation.model_validate({
        "id": 1, "prediction_id": 1, "overall_interpretation": invalid, "generated_at": "2026-10-20T00:00:00Z",
    })
    prediction = Prediction.model_validate({
        "id": 1, "user_id": 1, "spread_type_id": 1, "question": "Legacy question", "question_type": "general",
        "status": "pending", "created_at": "2026-10-20T00:00:00Z", "user_notes": invalid,
    })
    assert card.description == interpretation.overall_interpretation == prediction.user_notes == invalid
    assert spread.positions[0].name == spread.positions[0].meaning == invalid
