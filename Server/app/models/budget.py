from __future__ import annotations

from datetime import datetime, timezone

from sqlalchemy import BigInteger, DateTime, String
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base_class import Base


class AIBudgetWindow(Base):
    __tablename__ = "ai_budget_windows"

    period_key: Mapped[str] = mapped_column(String(32), primary_key=True)
    spent_micros: Mapped[int] = mapped_column(BigInteger, nullable=False, server_default="0")
    reserved_micros: Mapped[int] = mapped_column(BigInteger, nullable=False, server_default="0")


class AIBudgetReservation(Base):
    __tablename__ = "ai_budget_reservations"

    id: Mapped[str] = mapped_column(String(36), primary_key=True)
    day_key: Mapped[str] = mapped_column(String(32), nullable=False)
    month_key: Mapped[str] = mapped_column(String(32), nullable=False)
    model: Mapped[str] = mapped_column(String(100), nullable=False)
    estimated_micros: Mapped[int] = mapped_column(BigInteger, nullable=False)
    actual_micros: Mapped[int | None] = mapped_column(BigInteger, nullable=True)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False,
    )
