"""Database-backed admission control for paid model requests."""

from __future__ import annotations

from collections.abc import Callable
from datetime import datetime, timezone
from uuid import uuid4

from sqlalchemy import update
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.models.budget import AIBudgetReservation, AIBudgetWindow


class BudgetLimitExceeded(Exception):
    pass


class DatabaseBudgetStore:
    def __init__(self, session_factory: Callable[[], Session]) -> None:
        self.session_factory = session_factory

    @staticmethod
    def _keys() -> tuple[str, str]:
        now = datetime.now(timezone.utc)
        return f"day:{now:%Y-%m-%d}", f"month:{now:%Y-%m}"

    def _ensure_windows(self, keys: tuple[str, str]) -> None:
        with self.session_factory() as db:
            for key in keys:
                if db.get(AIBudgetWindow, key) is not None:
                    continue
                db.add(AIBudgetWindow(period_key=key))
                try:
                    db.commit()
                except IntegrityError:
                    db.rollback()

    def reserve(
        self,
        *,
        model: str,
        estimated_micros: int,
        daily_limit_micros: int,
        monthly_limit_micros: int,
    ) -> str:
        if estimated_micros <= 0 or (daily_limit_micros <= 0 and monthly_limit_micros <= 0):
            raise BudgetLimitExceeded("AI budget limits are not configured")

        keys = self._keys()
        self._ensure_windows(keys)
        reservation_id = str(uuid4())
        with self.session_factory() as db:
            for key, limit in zip(keys, (daily_limit_micros, monthly_limit_micros)):
                statement = update(AIBudgetWindow).where(AIBudgetWindow.period_key == key)
                if limit > 0:
                    statement = statement.where(
                        AIBudgetWindow.spent_micros + AIBudgetWindow.reserved_micros + estimated_micros <= limit
                    )
                claimed = db.execute(
                    statement.values(reserved_micros=AIBudgetWindow.reserved_micros + estimated_micros)
                )
                if claimed.rowcount != 1:
                    db.rollback()
                    raise BudgetLimitExceeded(f"AI {key.split(':', 1)[0]} budget exhausted")

            db.add(
                AIBudgetReservation(
                    id=reservation_id,
                    day_key=keys[0],
                    month_key=keys[1],
                    model=model,
                    estimated_micros=estimated_micros,
                )
            )
            db.commit()
        return reservation_id

    def settle(self, reservation_id: str, *, actual_micros: int) -> None:
        if actual_micros < 0:
            raise ValueError("Actual cost cannot be negative")
        with self.session_factory() as db:
            claimed = db.execute(
                update(AIBudgetReservation)
                .where(AIBudgetReservation.id == reservation_id)
                .where(AIBudgetReservation.actual_micros.is_(None))
                .values(actual_micros=actual_micros)
            )
            if claimed.rowcount != 1:
                db.rollback()
                return

            reservation = db.get(AIBudgetReservation, reservation_id)
            for key in (reservation.day_key, reservation.month_key):
                db.execute(
                    update(AIBudgetWindow)
                    .where(AIBudgetWindow.period_key == key)
                    .values(
                        reserved_micros=AIBudgetWindow.reserved_micros - reservation.estimated_micros,
                        spent_micros=AIBudgetWindow.spent_micros + actual_micros,
                    )
                )
            db.commit()

    def snapshot(self) -> dict[str, int]:
        day_key, month_key = self._keys()
        with self.session_factory() as db:
            daily = db.get(AIBudgetWindow, day_key)
            monthly = db.get(AIBudgetWindow, month_key)
            return {
                "daily_spent_micros": daily.spent_micros if daily else 0,
                "daily_reserved_micros": daily.reserved_micros if daily else 0,
                "monthly_spent_micros": monthly.spent_micros if monthly else 0,
                "monthly_reserved_micros": monthly.reserved_micros if monthly else 0,
            }
