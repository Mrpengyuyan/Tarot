from __future__ import annotations

from datetime import datetime, timedelta, timezone

import pytest

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.crud import prediction as prediction_crud
from app.services.coze_service import CozeTimeoutError


def _register_and_login(client, username: str) -> None:
    register_resp = client.post(
        "/api/v1/register",
        json={"username": username, "email": f"{username}@example.com", "password": "password123"},
    )
    assert register_resp.status_code == 200
    login_resp = client.post("/api/v1/login", data={"username": username, "password": "password123"})
    assert login_resp.status_code == 200
    csrf_token = login_resp.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(settings.CSRF_COOKIE_NAME)
    if csrf_token:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})


def _create_prediction(client, spread_id: int) -> int:
    create_resp = client.post(
        "/api/v1/records/",
        json={"spread_type_id": spread_id, "question": "我接下来该专注什么？", "question_type": "general"},
    )
    assert create_resp.status_code == 200
    return create_resp.json()["id"]


def _create_drawn_prediction(client, spread_id: int) -> int:
    prediction_id = _create_prediction(client, spread_id)
    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200
    return prediction_id


def _start_async(client, prediction_id: int):
    return client.post(f"/api/v1/records/{prediction_id}/interpret/async")


def _record(client, prediction_id: int) -> dict:
    detail_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert detail_resp.status_code == 200
    return detail_resp.json()


@pytest.fixture()
def fake_ai(monkeypatch):
    state = {"count": 0, "error": None}

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        state["count"] += 1
        if state["error"] is not None:
            raise state["error"]
        return {
            "overall_interpretation": "牌面显示稳步前进。",
            "summary": "保持节奏。",
            "advice": "先完成一件最重要的事。",
            "key_themes": ["节奏", "专注"],
            "model_used": "unit_test_ai",
            "confidence_score": 0.9,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )
    return state


def test_async_interpretation_returns_202_and_completes_in_background(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_happy")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert resp.json() == {"prediction_id": prediction_id, "status": "processing"}
    detail = _record(client, prediction_id)
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "unit_test_ai"
    assert fake_ai["count"] == 1


def test_async_interpretation_returns_existing_interpretation_with_200(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_existing")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    assert _start_async(client, prediction_id).status_code == 202

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 200
    body = resp.json()
    assert body["prediction_id"] == prediction_id
    assert body["model_used"] == "unit_test_ai"
    assert body["overall_interpretation"] == "牌面显示稳步前进。"
    assert fake_ai["count"] == 1


def test_async_interpretation_does_not_start_twice_while_processing(
    client, db_session, seeded_spread_and_cards, fake_ai
):
    _register_and_login(client, "async_processing")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=now,
        stale_before=now - timedelta(seconds=300),
        max_attempts=3,
    )

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert resp.json() == {"prediction_id": prediction_id, "status": "processing"}
    assert fake_ai["count"] == 0
    assert _record(client, prediction_id)["status"] == "processing"


def test_async_interpretation_requires_drawn_cards(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_no_draw")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 400
    assert resp.json()["detail"] == "Cards must be drawn before interpretation"
    assert fake_ai["count"] == 0


def test_async_interpretation_hides_other_users_records(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_owner")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    _register_and_login(client, "async_intruder")

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 404
    assert resp.json()["detail"] == "Record not found"
    assert fake_ai["count"] == 0


def test_async_interpretation_failure_marks_failed_and_retry_succeeds(client, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_retry")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    fake_ai["error"] = CozeTimeoutError("simulated upstream timeout")

    assert _start_async(client, prediction_id).status_code == 202
    assert _record(client, prediction_id)["status"] == "failed"

    fake_ai["error"] = None
    assert _start_async(client, prediction_id).status_code == 202

    detail = _record(client, prediction_id)
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "unit_test_ai"
    assert fake_ai["count"] == 2


def test_async_interpretation_returns_429_after_max_attempts(
    client, seeded_spread_and_cards, fake_ai, monkeypatch
):
    monkeypatch.setattr(settings, "AI_INTERPRETATION_MAX_ATTEMPTS", 2)
    _register_and_login(client, "async_exhausted")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    fake_ai["error"] = RuntimeError("simulated failure")

    assert _start_async(client, prediction_id).status_code == 202
    assert _start_async(client, prediction_id).status_code == 202
    resp = _start_async(client, prediction_id)

    assert resp.status_code == 429
    assert resp.json()["detail"] == "Interpretation attempts exhausted"
    assert fake_ai["count"] == 2


def test_async_interpretation_reclaims_stale_processing(client, db_session, seeded_spread_and_cards, fake_ai):
    _register_and_login(client, "async_stale")
    prediction_id = _create_drawn_prediction(client, seeded_spread_and_cards["spread_id"])
    stale_start = datetime.now(timezone.utc) - timedelta(seconds=settings.AI_INTERPRETATION_STALE_SECONDS + 60)
    assert prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=stale_start,
        stale_before=stale_start - timedelta(seconds=300),
        max_attempts=3,
    )

    resp = _start_async(client, prediction_id)

    assert resp.status_code == 202
    assert _record(client, prediction_id)["status"] == "completed"
    assert fake_ai["count"] == 1
