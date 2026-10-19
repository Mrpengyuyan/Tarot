import json

import pytest
from fastapi.testclient import TestClient
from sqlalchemy import select

from app.api.v1.endpoints import records as records_endpoint
from app.db.base_class import Base
from app.main import app, get_application


INVALID_NUMBERS = [
    '"Infinity"', '"-Infinity"', '"NaN"', '"1e309"',
    "1e309", "-1e309", "Infinity", "-Infinity", "NaN",
]


@pytest.fixture()
def api(client):
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


@pytest.fixture()
def reading(api, seeded_spread_and_cards):
    guest = api.post("/api/v1/guest-session")
    assert guest.status_code == 200
    headers = {"Authorization": f"Bearer {guest.json()['access_token']}"}
    created = api.post("/api/v1/records/", headers=headers, json={
        "question": "Confidence validation acceptance", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    record_id = created.json()["id"]
    assert api.post(f"/api/v1/records/{record_id}/draw", headers=headers).status_code == 200
    return record_id, headers


def _create_interpretation(api, reading, score=0.8):
    record_id, headers = reading
    response = api.post(f"/api/v1/records/{record_id}/interpret", headers=headers, json={
        "overall_interpretation": "Retained interpretation", "summary": "Retained summary", "confidence_score": score,
    })
    assert response.status_code == 200
    return response


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _reject_json_constant(value):
    raise AssertionError(f"Response contains non-standard JSON number: {value}")


def _strict_json(response):
    return json.loads(response.text, parse_constant=_reject_json_constant)


@pytest.mark.parametrize("mode", ["create", "update"])
@pytest.mark.parametrize("token", INVALID_NUMBERS)
def test_invalid_confidence_is_rejected_before_any_write(api, db_session, reading, mode, token):
    record_id, headers = reading
    if mode == "update":
        _create_interpretation(api, reading)
    before = _snapshot(db_session)
    detail_before = api.get(f"/api/v1/records/{record_id}", headers=headers).json()
    body = ('{"overall_interpretation":"Must not replace text","summary":"Must not replace summary",'
            '"confidence_score":' + token + '}')
    response = api.request(
        "POST" if mode == "create" else "PUT",
        f"/api/v1/records/{record_id}/" + ("interpret" if mode == "create" else "interpretation"),
        headers={**headers, "Content-Type": "application/json"}, content=body,
    )
    assert response.status_code == 422
    assert _strict_json(response)["detail"][0]["loc"] == ["body", "confidence_score"]
    assert _snapshot(db_session) == before
    detail_after = api.get(f"/api/v1/records/{record_id}", headers=headers)
    assert detail_after.status_code == 200
    assert _strict_json(detail_after) == detail_before


@pytest.mark.parametrize("mode", ["create", "update"])
@pytest.mark.parametrize("score", [0.0, 1.0, 0.8, -0.5, 1.5, 1.7976931348623157e308, "0.75", None])
def test_finite_values_and_explicit_null_keep_existing_behavior(api, reading, mode, score):
    record_id, headers = reading
    if mode == "create":
        response = _create_interpretation(api, reading, score)
    else:
        _create_interpretation(api, reading)
        response = api.put(f"/api/v1/records/{record_id}/interpretation", headers=headers, json={
            "confidence_score": score,
        })
    assert response.status_code == 200
    expected = float(score) if score is not None else None
    assert _strict_json(response)["confidence_score"] == expected
    detail = api.get(f"/api/v1/records/{record_id}", headers=headers)
    assert detail.status_code == 200
    assert _strict_json(detail)["interpretation"]["confidence_score"] == expected


def test_omitted_confidence_does_not_replace_saved_score(api, reading):
    record_id, headers = reading
    _create_interpretation(api, reading)
    for payload in [{}, {"summary": "Updated summary"}]:
        response = api.put(f"/api/v1/records/{record_id}/interpretation", headers=headers, json=payload)
        assert response.status_code == 200
        assert response.json()["confidence_score"] == 0.8
        assert response.json()["overall_interpretation"] == "Retained interpretation"


def test_invalid_confidence_cannot_trigger_forced_ai_or_change_run_state(
    api, db_session, reading, monkeypatch,
):
    calls = []

    async def fake_ai(*args, **kwargs):
        calls.append(True)
        return {"overall_interpretation": "Must not be generated", "confidence_score": 0.8}

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", fake_ai)
    record_id, headers = reading
    before = _snapshot(db_session)
    response = api.post(f"/api/v1/records/{record_id}/interpret?force_ai=true", headers=headers, json={
        "overall_interpretation": "Must not be saved", "confidence_score": "Infinity",
    })
    assert response.status_code == 422
    assert calls == []
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("fresh_app", [False, True])
def test_validation_errors_remain_json_safe_for_nested_nonfinite_inputs(api, db_session, fresh_app):
    before = _snapshot(db_session)
    target = TestClient(get_application(), raise_server_exceptions=False) if fresh_app else api
    try:
        response = target.post("/api/v1/register", json={
            "username": {"nested": [float("inf"), {"value": float("nan")}]},
            "email": "nested-input@example.com", "password": "password123",
        })
        assert response.status_code == 422
        error = _strict_json(response)["detail"][0]
        assert error["loc"] == ["body", "username"]
        assert error["type"] == "string_type"
    finally:
        if fresh_app:
            target.close()
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("fresh_app", [False, True])
def test_ordinary_validation_error_shape_is_unchanged(api, fresh_app):
    target = TestClient(get_application(), raise_server_exceptions=False) if fresh_app else api
    try:
        response = target.post("/api/v1/register", json={})
        assert response.status_code == 422
        errors = _strict_json(response)["detail"]
        assert {tuple(error["loc"]) for error in errors} == {
            ("body", "username"), ("body", "email"), ("body", "password"),
        }
        assert all(error["type"] == "missing" and error["msg"] == "Field required" for error in errors)
    finally:
        if fresh_app:
            target.close()
