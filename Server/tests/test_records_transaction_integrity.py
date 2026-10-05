from __future__ import annotations

import asyncio

import pytest
from sqlalchemy import event

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.crud import prediction as prediction_crud
from app.models.record import Interpretation, Prediction, PredictionStatus


def _create_drawable_record(client, spread_id: int, username: str) -> int:
    register = client.post(
        "/api/v1/register",
        json={"username": username, "email": f"{username}@example.com", "password": "password123"},
    )
    assert register.status_code == 200
    login = client.post("/api/v1/login", data={"username": username, "password": "password123"})
    assert login.status_code == 200
    client.headers.update({settings.CSRF_HEADER_NAME: client.cookies.get(settings.CSRF_COOKIE_NAME)})
    created = client.post(
        "/api/v1/records/",
        json={"spread_type_id": spread_id, "question": "What comes next?", "question_type": "general"},
    )
    assert created.status_code == 200
    return created.json()["id"]


def test_draw_does_not_persist_cards_if_status_update_fails(client, db_session, seeded_spread_and_cards):
    prediction_id = _create_drawable_record(client, seeded_spread_and_cards["spread_id"], "draw_atomic")

    def fail_processing_status(mapper, connection, target):  # noqa: ANN001
        if target.id == prediction_id and target.status == PredictionStatus.PROCESSING:
            raise RuntimeError("status update failed")

    event.listen(Prediction, "before_update", fail_processing_status)
    try:
        with pytest.raises(RuntimeError, match="status update failed"):
            client.post(f"/api/v1/records/{prediction_id}/draw")
    finally:
        event.remove(Prediction, "before_update", fail_processing_status)

    db_session.expire_all()
    assert prediction_crud.get_prediction_card_draws(db_session, prediction_id) == []
    assert prediction_crud.get_prediction_by_id(db_session, prediction_id).status == PredictionStatus.PENDING


def test_background_completion_failure_does_not_leave_saved_interpretation(
    client, db_session, seeded_spread_and_cards, monkeypatch
):
    prediction_id = _create_drawable_record(client, seeded_spread_and_cards["spread_id"], "interpret_atomic")
    assert client.post(f"/api/v1/records/{prediction_id}/draw").status_code == 200

    async def fake_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        return {"overall_interpretation": "A clear answer", "model_used": "test_ai"}

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", fake_interpretation)

    def fail_completion_status(mapper, connection, target):  # noqa: ANN001
        if target.id == prediction_id and target.status == PredictionStatus.COMPLETED:
            raise RuntimeError("completion update failed")

    event.listen(Prediction, "before_update", fail_completion_status)
    try:
        response = client.post(f"/api/v1/records/{prediction_id}/interpret/async")
    finally:
        event.remove(Prediction, "before_update", fail_completion_status)

    assert response.status_code == 202
    db_session.expire_all()
    assert prediction_crud.get_prediction_interpretation(db_session, prediction_id) is None
    assert prediction_crud.get_prediction_by_id(db_session, prediction_id).status == PredictionStatus.FAILED


def test_background_session_open_failure_does_not_escape(caplog):
    def unavailable_session():
        raise RuntimeError("database unavailable")

    asyncio.run(records_endpoint.run_interpretation_job(unavailable_session, 42, None))

    assert "Background interpretation for record 42 failed" in caplog.text
    assert "database unavailable" in caplog.text


def test_background_failure_status_write_error_waits_for_stale_retry(
    client, db_session, seeded_spread_and_cards, monkeypatch
):
    prediction_id = _create_drawable_record(client, seeded_spread_and_cards["spread_id"], "failed_status")
    assert client.post(f"/api/v1/records/{prediction_id}/draw").status_code == 200

    async def failing_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        raise RuntimeError("AI unavailable")

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", failing_interpretation)

    def fail_status_write(mapper, connection, target):  # noqa: ANN001
        if target.id == prediction_id and target.status == PredictionStatus.FAILED:
            raise RuntimeError("status write unavailable")

    event.listen(Prediction, "before_update", fail_status_write)
    try:
        response = client.post(f"/api/v1/records/{prediction_id}/interpret/async")
    finally:
        event.remove(Prediction, "before_update", fail_status_write)

    assert response.status_code == 202
    db_session.expire_all()
    assert prediction_crud.get_prediction_interpretation(db_session, prediction_id) is None
    assert prediction_crud.get_prediction_by_id(db_session, prediction_id).status == PredictionStatus.PROCESSING


def test_async_existing_interpretation_repairs_incomplete_status(client, db_session, seeded_spread_and_cards):
    prediction_id = _create_drawable_record(client, seeded_spread_and_cards["spread_id"], "repair_status")
    assert client.post(f"/api/v1/records/{prediction_id}/draw").status_code == 200
    db_session.add(Interpretation(prediction_id=prediction_id, overall_interpretation="Already saved"))
    db_session.commit()

    response = client.post(f"/api/v1/records/{prediction_id}/interpret/async")

    assert response.status_code == 200
    db_session.expire_all()
    prediction = prediction_crud.get_prediction_by_id(db_session, prediction_id)
    assert prediction.status == PredictionStatus.COMPLETED
    assert prediction.completed_at is not None
