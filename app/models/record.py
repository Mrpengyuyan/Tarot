from __future__ import annotations

import enum
from datetime import datetime
from typing import TYPE_CHECKING

from sqlalchemy import Boolean, DateTime, Enum as SAEnum, Float, ForeignKey, Integer, String, Text, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column, relationship
from sqlalchemy.sql import func

from app.db.base_class import Base

if TYPE_CHECKING:
    from app.models.spread import SpreadType
    from app.models.tarot_card import TarotCard
    from app.models.user import User


class QuestionType(enum.Enum):
    LOVE = "love"
    CAREER = "career"
    FINANCE = "finance"
    HEALTH = "health"
    GENERAL = "general"


class PredictionStatus(enum.Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


def enum_values(enum_cls: type[enum.Enum]) -> list[str]:
    return [member.value for member in enum_cls]


class Prediction(Base):
    __tablename__ = "predictions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    user_id: Mapped[int] = mapped_column(Integer, ForeignKey("users.id"), nullable=False, comment="用户ID")
    spread_type_id: Mapped[int] = mapped_column(
        Integer,
        ForeignKey("spread_types.id"),
        nullable=False,
        comment="牌阵类型ID",
    )

    question: Mapped[str] = mapped_column(Text, nullable=False, comment="用户问题")
    question_type: Mapped[QuestionType] = mapped_column(
        SAEnum(QuestionType, values_callable=enum_values),
        nullable=False,
        comment="问题类型",
    )

    status: Mapped[PredictionStatus] = mapped_column(
        SAEnum(PredictionStatus, values_callable=enum_values),
        default=PredictionStatus.PENDING,
        nullable=False,
        comment="预测状态",
    )
    created_at: Mapped[datetime] = mapped_column(DateTime, default=func.now(), nullable=False, comment="创建时间")
    completed_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True, comment="完成时间")

    is_favorite: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False, comment="是否收藏")
    user_rating: Mapped[int | None] = mapped_column(Integer, nullable=True, comment="用户评分（1-5）")
    user_notes: Mapped[str | None] = mapped_column(Text, nullable=True, comment="用户备注")

    user: Mapped["User"] = relationship("User", back_populates="predictions")
    spread_type: Mapped["SpreadType"] = relationship("SpreadType")
    card_draws: Mapped[list["CardDraw"]] = relationship(
        "CardDraw",
        back_populates="prediction",
        cascade="all, delete-orphan",
    )
    interpretation: Mapped["Interpretation | None"] = relationship(
        "Interpretation",
        back_populates="prediction",
        uselist=False,
        cascade="all, delete-orphan",
    )

    def __repr__(self) -> str:
        return f"<Prediction(id={self.id}, user_id={self.user_id}, question='{self.question[:50]}...')>"


class CardDraw(Base):
    __tablename__ = "card_draws"
    __table_args__ = (
        UniqueConstraint("prediction_id", "position", name="uq_card_draw_prediction_position"),
    )

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    prediction_id: Mapped[int] = mapped_column(
        Integer,
        ForeignKey("predictions.id"),
        nullable=False,
        comment="预测记录ID",
    )
    tarot_card_id: Mapped[int] = mapped_column(
        Integer,
        ForeignKey("tarot_cards.id"),
        nullable=False,
        comment="塔罗牌ID",
    )
    position: Mapped[int] = mapped_column(Integer, nullable=False, comment="牌位位置")
    is_reversed: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False, comment="是否逆位")
    drawn_at: Mapped[datetime] = mapped_column(DateTime, default=func.now(), nullable=False, comment="抽牌时间")

    prediction: Mapped["Prediction"] = relationship("Prediction", back_populates="card_draws")
    tarot_card: Mapped["TarotCard"] = relationship("TarotCard")

    def get_card_meaning(self, aspect: str = "general") -> str | None:
        return self.tarot_card.get_meaning(self.is_reversed, aspect)

    def get_position_name(self) -> str:
        return self.prediction.spread_type.get_position_name(self.position)

    def get_position_meaning(self) -> str:
        return self.prediction.spread_type.get_position_meaning(self.position)

    def __repr__(self) -> str:
        reversed_text = "逆位" if self.is_reversed else "正位"
        return f"<CardDraw(id={self.id}, card='{self.tarot_card.name_zh}', position={self.position}, {reversed_text})>"


class Interpretation(Base):
    __tablename__ = "interpretations"
    __table_args__ = (
        UniqueConstraint("prediction_id", name="uq_interpretation_prediction"),
    )

    id: Mapped[int] = mapped_column(Integer, primary_key=True, index=True)
    prediction_id: Mapped[int] = mapped_column(
        Integer,
        ForeignKey("predictions.id"),
        nullable=False,
        comment="预测记录ID",
    )

    overall_interpretation: Mapped[str] = mapped_column(Text, nullable=False, comment="整体解读")
    card_analysis: Mapped[str | None] = mapped_column(Text, nullable=True, comment="单牌分析")
    relationship_analysis: Mapped[str | None] = mapped_column(Text, nullable=True, comment="牌间关系分析")
    advice: Mapped[str | None] = mapped_column(Text, nullable=True, comment="建议")
    warning: Mapped[str | None] = mapped_column(Text, nullable=True, comment="警告或注意事项")
    summary: Mapped[str | None] = mapped_column(Text, nullable=True, comment="预测概要")
    key_themes: Mapped[str | None] = mapped_column(String(500), nullable=True, comment="关键主题（逗号分隔）")

    model_used: Mapped[str | None] = mapped_column(String(100), nullable=True, comment="使用的AI模型")
    model_version: Mapped[str | None] = mapped_column(String(50), nullable=True, comment="模型版本")
    confidence_score: Mapped[float | None] = mapped_column(Float, nullable=True, comment="置信度分数")
    generated_at: Mapped[datetime] = mapped_column(DateTime, default=func.now(), nullable=False, comment="生成时间")

    prediction: Mapped["Prediction"] = relationship("Prediction", back_populates="interpretation")

    def get_key_themes_list(self) -> list[str]:
        return self.key_themes.split(",") if self.key_themes else []

    def __repr__(self) -> str:
        return f"<Interpretation(id={self.id}, prediction_id={self.prediction_id}, model='{self.model_used}')>"
