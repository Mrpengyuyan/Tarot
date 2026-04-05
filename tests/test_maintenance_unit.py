"""Unit tests for app.db.maintenance — data quality guard for training/evaluation."""
from __future__ import annotations

import pytest

from app.crud.prediction import create_prediction_with_stats
from app.crud.user import create_user
from app.db.maintenance import (
    build_replacement,
    is_corrupted_question,
    is_synthetic_question,
    repair_prediction_questions,
)
from app.models.record import CardDraw, Prediction
from app.models.spread import SpreadType
from app.models.tarot_card import CardType, TarotCard
from app.schemas.prediction import PredictionCreate, QuestionTypeEnum
from app.schemas.user import UserCreate


# ── is_corrupted_question ────────────────────────────────────────────────

class TestIsCorruptedQuestion:
    @pytest.mark.parametrize(
        "value",
        [
            None,
            "",
            "   ",
            "???",
            "锛燂拷锛燂拷",
            "\uFFFD\uFFFD\uFFFD",
            "??锛燂拷??锛燂拷",
        ],
    )
    def test_detects_corrupted_patterns(self, value):
        assert is_corrupted_question(value) is True

    @pytest.mark.parametrize(
        "value",
        [
            "我明天会顺利吗？",
            "How will my career develop?",
            "这段关系接下来会如何发展？",
            "Will things improve?",
        ],
    )
    def test_accepts_valid_questions(self, value):
        assert is_corrupted_question(value) is False

    def test_mixed_corruption_below_threshold_passes(self):
        # Only 1 corrupted char in 10 total → 10% < 50% → not corrupted
        assert is_corrupted_question("正常文本?正常") is False

    def test_mixed_corruption_above_threshold_fails(self):
        # "??" is 2 / 3 → 66% > 50% → corrupted
        assert is_corrupted_question("??好") is True


# ── is_synthetic_question ────────────────────────────────────────────────

class TestIsSyntheticQuestion:
    @pytest.mark.parametrize(
        "value",
        [
            "关于感情关系的占卜",
            "关于事业发展的占卜",
            "关于财务规划的占卜",
            "关于身心状态的占卜",
            "关于当前状态的占卜",
            "关于人生课题的占卜",
            "关于感情关系的占卜（月亮逆位）",
            "关于当前状态的占卜：明天出行好吗",
        ],
    )
    def test_detects_synthetic_patterns(self, value):
        assert is_synthetic_question(value) is True

    @pytest.mark.parametrize(
        "value",
        [
            "我的感情关系会如何发展？",
            "关于感情关系的深度探讨",
            "",
            None,
        ],
    )
    def test_rejects_non_synthetic(self, value):
        assert is_synthetic_question(value) is False


# ── build_replacement ────────────────────────────────────────────────────

class TestBuildReplacement:
    def test_love_with_card(self):
        result = build_replacement("love", "三张牌", "Three Card", "愚者", False)
        assert "愚者" in result
        assert "正位" in result
        assert "关系" in result

    def test_love_without_card(self):
        result = build_replacement("love", None, None, None, None)
        assert "关系" in result

    def test_career_with_reversed_card(self):
        result = build_replacement("career", "单张牌", "Single Card", "魔术师", True)
        assert "魔术师" in result
        assert "逆位" in result
        assert "工作" in result

    def test_finance_type(self):
        result = build_replacement("finance", None, None, None, None)
        assert "财务" in result

    def test_health_type(self):
        result = build_replacement("health", None, None, None, None)
        assert "状态" in result

    def test_celtic_cross_spread(self):
        result = build_replacement("general", "凯尔特十字", "Celtic Cross", None, None)
        assert "课题" in result

    def test_past_present_future_spread(self):
        result = build_replacement("general", "过去现在未来", "Past-Present-Future", None, None)
        assert "变化" in result

    def test_fallback_with_card(self):
        result = build_replacement("general", "自定义牌阵", "Custom Spread", "星星", False)
        assert "星星" in result
        assert "正位" in result

    def test_fallback_without_card(self):
        result = build_replacement("general", "自定义牌阵", "Custom Spread", None, None)
        assert "关注" in result


# ── repair_prediction_questions (integration with DB) ────────────────────

class TestRepairPredictionQuestions:
    def _seed_prediction(self, db_session, question: str, question_type: str = "general"):
        """Create a user + spread + prediction with the given question."""
        user = create_user(
            db_session,
            UserCreate(
                username=f"repair_user_{id(question)}_{hash(question) & 0xFFF:03x}",
                email=f"repair_{hash(question) & 0xFFF:03x}@example.com",
                password="password123",
            ),
        )
        spread = SpreadType(
            name="Repair Test Spread",
            name_en="Repair Test Spread",
            description="Spread for repair tests",
            card_count=1,
            positions=[{"position": 1, "name": "Now", "meaning": "Current"}],
            is_active=True,
        )
        db_session.add(spread)
        db_session.commit()
        db_session.refresh(spread)

        prediction = create_prediction_with_stats(
            db_session,
            user_id=user.id,
            prediction_create=PredictionCreate(
                spread_type_id=spread.id,
                question=question,
                question_type=QuestionTypeEnum(question_type),
            ),
        )
        # Manually set the question (since PredictionCreate will have validated it)
        prediction.question = question
        db_session.commit()
        db_session.refresh(prediction)
        return prediction

    def test_repairs_corrupted_question(self, db_session):
        prediction = self._seed_prediction(db_session, "锛燂拷锛燂拷")
        result = repair_prediction_questions(db_session, commit=True)
        assert result["repaired"] >= 1
        db_session.refresh(prediction)
        assert "锛燂拷" not in prediction.question
        assert len(prediction.question) > 0

    def test_repairs_synthetic_question(self, db_session):
        prediction = self._seed_prediction(db_session, "关于感情关系的占卜", question_type="love")
        result = repair_prediction_questions(db_session, include_synthetic=True, commit=True)
        assert result["repaired"] >= 1
        db_session.refresh(prediction)
        assert prediction.question != "关于感情关系的占卜"

    def test_skips_synthetic_when_disabled(self, db_session):
        prediction = self._seed_prediction(db_session, "关于事业发展的占卜", question_type="career")
        result = repair_prediction_questions(db_session, include_synthetic=False, commit=True)
        assert result["repaired"] == 0
        db_session.refresh(prediction)
        assert prediction.question == "关于事业发展的占卜"

    def test_leaves_valid_questions_untouched(self, db_session):
        original = "我明天会顺利吗？"
        prediction = self._seed_prediction(db_session, original)
        result = repair_prediction_questions(db_session, commit=True)
        db_session.refresh(prediction)
        assert prediction.question == original

    def test_commit_false_rolls_back(self, db_session):
        prediction = self._seed_prediction(db_session, "???")
        result = repair_prediction_questions(db_session, commit=False)
        assert result["repaired"] >= 1
        # After rollback, original corrupted question should be restored
        db_session.refresh(prediction)
        assert prediction.question == "???"
