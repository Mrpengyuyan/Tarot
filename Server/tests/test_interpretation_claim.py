from __future__ import annotations

from datetime import datetime, timedelta, timezone

from app.core.config import Settings
from app.crud import prediction as prediction_crud
from app.models.record import Interpretation, Prediction, PredictionStatus, QuestionType
from app.models.user import User

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
