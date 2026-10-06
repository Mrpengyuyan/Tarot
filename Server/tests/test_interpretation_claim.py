from __future__ import annotations

import asyncio
from datetime import datetime, timedelta, timezone

from fastapi import HTTPException
import pytest

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import Settings
from app.crud import prediction as prediction_crud
from app.models.record import Interpretation, Prediction, PredictionStatus, QuestionType
from app.models.user import User
from app.schemas.prediction import CardDrawCreate
from app.services.coze_service import CozeTimeoutError

MAX_ATTEMPTS = 3


def _now() -> datetime:
    return datetime.now(timezone.utc)


def _make_prediction(db_session, spread_id: int, username: str = "claim_user") -> Prediction:
    user = User(
        username=username,
        email=f"{username}@example.com",
        hashed_password="not-used-in-this-test",
        nickname="claim",
        is_active=True,
        is_superuser=False,
    )
    db_session.add(user)
    db_session.commit()
    prediction = Prediction(
        user_id=user.id,
        spread_type_id=spread_id,
        question="我接下来该专注什么？",
        question_type=QuestionType.GENERAL,
    )
    db_session.add(prediction)
    db_session.commit()
    db_session.refresh(prediction)
    return prediction


def _claim(db_session, prediction_id: int, now: datetime, stale_seconds: int = 300) -> bool:
    return prediction_crud.claim_interpretation_generation(
        db_session,
        prediction_id=prediction_id,
        now=now,
        stale_before=now - timedelta(seconds=stale_seconds),
        max_attempts=MAX_ATTEMPTS,
    )


def _make_drawn_prediction(db_session, seeded_spread_and_cards, username: str) -> Prediction:
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"], username)
    prediction_crud.batch_create_card_draws(
        db_session,
        prediction.id,
        [
            CardDrawCreate(tarot_card_id=card_id, position=position, is_reversed=False)
            for position, card_id in enumerate(seeded_spread_and_cards["card_ids"][:3], start=1)
        ],
        prediction_status=PredictionStatus.PROCESSING,
    )
    return prediction


def test_generation_tracking_settings_defaults():
    defaults = Settings(_env_file=None)

    assert defaults.AI_INTERPRETATION_STALE_SECONDS == 300
    assert defaults.AI_INTERPRETATION_MAX_ATTEMPTS == 3


def test_first_claim_marks_prediction_processing(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])

    assert _claim(db_session, prediction.id, _now()) is True

    db_session.refresh(prediction)
    assert prediction.status == PredictionStatus.PROCESSING
    assert prediction.interpretation_attempts == 1
    assert prediction.interpretation_started_at is not None


def test_second_claim_while_fresh_processing_is_rejected(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True

    assert _claim(db_session, prediction.id, started + timedelta(seconds=10)) is False

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == 1


def test_stale_processing_can_be_reclaimed(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True

    assert _claim(db_session, prediction.id, started + timedelta(seconds=301)) is True

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == 2


def test_failed_prediction_can_be_reclaimed(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    assert _claim(db_session, prediction.id, started) is True
    prediction_crud.update_prediction_status(db_session, prediction_id=prediction.id, status=PredictionStatus.FAILED)

    assert _claim(db_session, prediction.id, started + timedelta(seconds=5)) is True


def test_claim_rejected_after_max_attempts(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    started = _now()
    for attempt in range(MAX_ATTEMPTS):
        assert _claim(db_session, prediction.id, started + timedelta(seconds=attempt)) is True
        prediction_crud.update_prediction_status(
            db_session,
            prediction_id=prediction.id,
            status=PredictionStatus.FAILED,
        )

    assert _claim(db_session, prediction.id, started + timedelta(seconds=60)) is False

    db_session.refresh(prediction)
    assert prediction.interpretation_attempts == MAX_ATTEMPTS


def test_claim_rejected_when_interpretation_exists(db_session, seeded_spread_and_cards):
    prediction = _make_prediction(db_session, seeded_spread_and_cards["spread_id"])
    db_session.add(Interpretation(prediction_id=prediction.id, overall_interpretation="已经生成"))
    db_session.commit()

    assert _claim(db_session, prediction.id, _now()) is False


def test_claim_unknown_prediction_returns_false(db_session):
    assert _claim(db_session, 999999, _now()) is False


def test_stale_worker_failure_does_not_fail_newer_claim(
    db_session, db_session_factory, seeded_spread_and_cards, monkeypatch
):
    prediction = _make_drawn_prediction(db_session, seeded_spread_and_cards, "old_failure")
    first_start = _now() - timedelta(seconds=360)
    assert _claim(db_session, prediction.id, first_start)
    db_session.refresh(prediction)
    db_session.expunge(prediction)

    async def failing_ai(db, prediction, cards_data, user_context=None, before_send=None):  # noqa: ANN001
        assert _claim(db_session, prediction.id, _now())
        raise CozeTimeoutError("old request timed out")

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", failing_ai)
    with db_session_factory() as worker_db:
        with pytest.raises(HTTPException) as error:
            asyncio.run(records_endpoint._build_ai_interpretation_create(worker_db, prediction, None))

    assert error.value.status_code == 504
    db_session.expire_all()
    current = prediction_crud.get_prediction_by_id(db_session, prediction.id)
    assert current.status == PredictionStatus.PROCESSING
    assert current.interpretation_attempts == 2


def test_stale_worker_success_does_not_store_outdated_answer(
    db_session, db_session_factory, seeded_spread_and_cards, monkeypatch
):
    prediction = _make_drawn_prediction(db_session, seeded_spread_and_cards, "old_success")
    first_start = _now() - timedelta(seconds=360)
    assert _claim(db_session, prediction.id, first_start)
    db_session.refresh(prediction)
    db_session.expunge(prediction)

    async def successful_ai(db, prediction, cards_data, user_context=None, before_send=None):  # noqa: ANN001
        assert _claim(db_session, prediction.id, _now())
        return {"overall_interpretation": "Outdated answer", "model_used": "test_ai"}

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", successful_ai)
    with db_session_factory() as worker_db:
        with pytest.raises(HTTPException) as error:
            asyncio.run(records_endpoint.generate_and_store_interpretation(worker_db, prediction, None))

    assert error.value.status_code == 409
    db_session.expire_all()
    assert prediction_crud.get_prediction_interpretation(db_session, prediction.id) is None
    assert prediction_crud.get_prediction_by_id(db_session, prediction.id).status == PredictionStatus.PROCESSING


def test_delayed_background_job_skips_when_claim_was_replaced(
    db_session, db_session_factory, seeded_spread_and_cards, monkeypatch
):
    prediction = _make_drawn_prediction(db_session, seeded_spread_and_cards, "delayed_worker")
    first_start = _now() - timedelta(seconds=360)
    assert _claim(db_session, prediction.id, first_start)
    assert _claim(db_session, prediction.id, _now())
    calls = 0

    async def unexpected_generation(db, prediction, user_context):  # noqa: ANN001
        nonlocal calls
        calls += 1

    monkeypatch.setattr(records_endpoint, "generate_and_store_interpretation", unexpected_generation)
    asyncio.run(records_endpoint.run_interpretation_job(db_session_factory, prediction.id, None, first_start))

    assert calls == 0
    db_session.expire_all()
    assert prediction_crud.get_prediction_by_id(db_session, prediction.id).status == PredictionStatus.PROCESSING


def test_worker_rechecks_claim_before_calling_ai(
    db_session, db_session_factory, seeded_spread_and_cards, monkeypatch
):
    prediction = _make_drawn_prediction(db_session, seeded_spread_and_cards, "pre_ai_takeover")
    first_start = _now() - timedelta(seconds=360)
    assert _claim(db_session, prediction.id, first_start)
    real_get_draws = prediction_crud.get_prediction_card_draws
    calls = 0

    def takeover_after_loading_draws(db, prediction_id):  # noqa: ANN001
        draws = real_get_draws(db, prediction_id=prediction_id)
        assert _claim(db_session, prediction_id, _now())
        return draws

    async def unexpected_ai(db, prediction, cards_data, user_context=None, before_send=None):  # noqa: ANN001
        nonlocal calls
        calls += 1
        return {"overall_interpretation": "Outdated answer"}

    monkeypatch.setattr(prediction_crud, "get_prediction_card_draws", takeover_after_loading_draws)
    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", unexpected_ai)
    with db_session_factory() as worker_db:
        worker_prediction = prediction_crud.get_prediction_by_id(worker_db, prediction.id)
        with pytest.raises(HTTPException) as error:
            asyncio.run(records_endpoint.generate_and_store_interpretation(worker_db, worker_prediction, None))

    assert error.value.status_code == 409
    assert calls == 0
    db_session.expire_all()
    assert prediction_crud.get_prediction_by_id(db_session, prediction.id).status == PredictionStatus.PROCESSING
