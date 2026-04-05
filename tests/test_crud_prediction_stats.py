"""Unit tests for CRUD prediction: stats, ownership, delete, and counter integrity."""
from __future__ import annotations

import pytest
from sqlalchemy.orm import Session

from app.crud.prediction import (
    create_interpretation,
    create_prediction_with_stats,
    delete_prediction,
    get_prediction_by_id,
    get_prediction_interpretation,
    get_total_user_predictions_count,
    get_user_prediction_stats,
    update_prediction_status,
    validate_prediction_ownership,
)
from app.crud.user import create_user
from app.models.record import PredictionStatus
from app.models.spread import SpreadType
from app.schemas.prediction import (
    InterpretationCreate,
    PredictionCreate,
    QuestionTypeEnum,
)
from app.schemas.user import UserCreate


@pytest.fixture()
def user_and_spread(db_session: Session):
    """Create a test user and spread."""
    user = create_user(
        db_session,
        UserCreate(
            username="stats_user",
            email="stats_user@example.com",
            password="password123",
        ),
    )
    spread = SpreadType(
        name="Stats Test Spread",
        name_en="Stats Test Spread",
        description="Spread for stats tests",
        card_count=3,
        positions=[
            {"position": 1, "name": "P1", "meaning": "M1"},
            {"position": 2, "name": "P2", "meaning": "M2"},
            {"position": 3, "name": "P3", "meaning": "M3"},
        ],
        is_active=True,
        usage_count=0,
    )
    db_session.add(spread)
    db_session.commit()
    db_session.refresh(spread)
    return user, spread


def _create_prediction(db_session, user_id, spread_id, question_type="general", question="Test?"):
    return create_prediction_with_stats(
        db_session,
        user_id=user_id,
        prediction_create=PredictionCreate(
            spread_type_id=spread_id,
            question=question,
            question_type=QuestionTypeEnum(question_type),
        ),
    )


# ── validate_prediction_ownership ────────────────────────────────────────

class TestValidatePredictionOwnership:
    def test_correct_owner_returns_true(self, db_session, user_and_spread):
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)
        assert validate_prediction_ownership(db_session, prediction.id, user.id) is True

    def test_wrong_owner_returns_false(self, db_session, user_and_spread):
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)
        assert validate_prediction_ownership(db_session, prediction.id, user.id + 999) is False

    def test_nonexistent_prediction_returns_false(self, db_session, user_and_spread):
        user, _ = user_and_spread
        assert validate_prediction_ownership(db_session, 99999, user.id) is False


# ── delete_prediction ────────────────────────────────────────────────────

class TestDeletePrediction:
    def test_delete_existing_prediction(self, db_session, user_and_spread):
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)
        pid = prediction.id
        assert delete_prediction(db_session, pid) is True
        assert get_prediction_by_id(db_session, pid) is None

    def test_delete_nonexistent_returns_false(self, db_session):
        assert delete_prediction(db_session, 99999) is False

    def test_delete_with_interpretation_cascades(self, db_session, user_and_spread):
        """Deleting a prediction should also remove its interpretation (cascade)."""
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)
        create_interpretation(
            db_session,
            prediction_id=prediction.id,
            interpretation_create=InterpretationCreate(
                overall_interpretation="Test interpretation",
                model_used="test_model",
            ),
        )
        pid = prediction.id
        assert delete_prediction(db_session, pid) is True
        assert get_prediction_interpretation(db_session, pid) is None


# ── update_prediction_status ─────────────────────────────────────────────

class TestUpdatePredictionStatus:
    def test_update_to_completed_sets_timestamp(self, db_session, user_and_spread):
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)
        assert prediction.completed_at is None

        result = update_prediction_status(db_session, prediction.id, PredictionStatus.COMPLETED)
        assert result is True

        db_session.refresh(prediction)
        assert prediction.status == PredictionStatus.COMPLETED
        assert prediction.completed_at is not None

    def test_update_to_failed_does_not_set_completed_at(self, db_session, user_and_spread):
        user, spread = user_and_spread
        prediction = _create_prediction(db_session, user.id, spread.id)

        update_prediction_status(db_session, prediction.id, PredictionStatus.FAILED)
        db_session.refresh(prediction)
        assert prediction.status == PredictionStatus.FAILED
        assert prediction.completed_at is None

    def test_update_nonexistent_returns_false(self, db_session):
        assert update_prediction_status(db_session, 99999, PredictionStatus.COMPLETED) is False


# ── get_user_prediction_stats ────────────────────────────────────────────

class TestGetUserPredictionStats:
    def test_empty_stats_for_new_user(self, db_session, user_and_spread):
        user, _ = user_and_spread
        stats = get_user_prediction_stats(db_session, user.id)
        assert stats.total_predictions == 0
        assert stats.completed_predictions == 0
        assert stats.favorite_predictions == 0
        assert stats.most_used_question_type is None
        assert stats.average_rating is None

    def test_stats_reflect_predictions(self, db_session, user_and_spread):
        user, spread = user_and_spread

        # Create 3 predictions: 2 career, 1 love
        p1 = _create_prediction(db_session, user.id, spread.id, "career", "Career Q1")
        p2 = _create_prediction(db_session, user.id, spread.id, "career", "Career Q2")
        p3 = _create_prediction(db_session, user.id, spread.id, "love", "Love Q1")

        # Complete 2
        update_prediction_status(db_session, p1.id, PredictionStatus.COMPLETED)
        update_prediction_status(db_session, p2.id, PredictionStatus.COMPLETED)

        # Favorite 1
        p1.is_favorite = True
        db_session.commit()

        # Rate 1
        p1.user_rating = 4
        db_session.commit()

        stats = get_user_prediction_stats(db_session, user.id)
        assert stats.total_predictions == 3
        assert stats.completed_predictions == 2
        assert stats.favorite_predictions == 1
        assert stats.most_used_question_type is not None
        assert stats.average_rating == pytest.approx(4.0)


# ── get_total_user_predictions_count ─────────────────────────────────────

class TestGetTotalUserPredictionsCount:
    def test_counts_only_for_specified_user(self, db_session, user_and_spread):
        user, spread = user_and_spread
        _create_prediction(db_session, user.id, spread.id)
        _create_prediction(db_session, user.id, spread.id)

        # Create another user with 1 prediction
        other_user = create_user(
            db_session,
            UserCreate(
                username="other_count_user",
                email="other_count@example.com",
                password="password123",
            ),
        )
        _create_prediction(db_session, other_user.id, spread.id)

        assert get_total_user_predictions_count(db_session, user.id) == 2
        assert get_total_user_predictions_count(db_session, other_user.id) == 1
