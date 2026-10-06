"""Database-backed guest session admission shared by all server workers."""

import hmac
from datetime import datetime, timedelta, timezone
from hashlib import sha256
from math import ceil

from sqlalchemy import delete, update
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.core.config import settings
from app.models.guest_session_window import GuestSessionWindow


class GuestSessionLimitReached(Exception):
    def __init__(self, retry_after_seconds: int) -> None:
        self.retry_after_seconds = retry_after_seconds


def claim_guest_session(db: Session, source_host: str) -> None:
    now = datetime.now(timezone.utc)
    window_start = now.replace(minute=0, second=0, microsecond=0)
    window_key = window_start.strftime("%Y-%m-%dT%H")
    source_hash = hmac.new(
        settings.SECRET_KEY.encode("utf-8"),
        (source_host or "unknown").strip().lower().encode("utf-8"),
        sha256,
    ).hexdigest()
    max_sessions = settings.GUEST_SESSIONS_PER_HOUR
    retry_after = max(1, ceil((window_start + timedelta(hours=1) - now).total_seconds()))
    old_window = (window_start - timedelta(days=2)).strftime("%Y-%m-%dT%H")
    identity = {"window_key": window_key, "source_hash": source_hash}

    for _ in range(3):
        claimed = db.execute(
            update(GuestSessionWindow)
            .where(GuestSessionWindow.window_key == window_key)
            .where(GuestSessionWindow.source_hash == source_hash)
            .where(GuestSessionWindow.issued < max_sessions)
            .values(issued=GuestSessionWindow.issued + 1)
        )
        if claimed.rowcount == 1:
            db.execute(delete(GuestSessionWindow).where(GuestSessionWindow.window_key < old_window))
            db.commit()
            return

        db.rollback()
        existing = db.get(GuestSessionWindow, identity)
        if existing is not None:
            at_limit = existing.issued >= max_sessions
            db.rollback()
            if at_limit:
                raise GuestSessionLimitReached(retry_after)
            continue

        db.add(GuestSessionWindow(**identity, issued=1))
        try:
            db.flush()
            db.execute(delete(GuestSessionWindow).where(GuestSessionWindow.window_key < old_window))
            db.commit()
            return
        except IntegrityError:
            db.rollback()

    raise GuestSessionLimitReached(retry_after)
