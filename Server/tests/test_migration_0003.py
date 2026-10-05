from __future__ import annotations

import sqlite3
from datetime import datetime, timezone
from pathlib import Path

from alembic import command
from alembic.config import Config

from app.core.config import settings


def test_upgrade_backfills_guest_identity_and_today_usage(tmp_path, monkeypatch):
    database = tmp_path / "quota_migration.db"
    monkeypatch.setattr(settings, "DATABASE_URL", f"sqlite:///{database}")
    config = Config()
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))
    command.upgrade(config, "0002_interpretation_tracking")

    original_guest = "guest_" + "a" * 32
    changed_email_guest = "guest_" + "b" * 32
    with sqlite3.connect(database) as conn:
        for column in ("reading_quota_count", "reading_quota_day", "is_guest"):
            conn.execute(f"ALTER TABLE users DROP COLUMN {column}")
        now = datetime.now(timezone.utc).isoformat()
        conn.execute(
            "INSERT INTO users (username, email, hashed_password, is_active, is_superuser, "
            "created_at, updated_at, prediction_count) VALUES (?, ?, ?, 1, 0, ?, ?, 0)",
            (original_guest, f"{original_guest}@guest.tarot.game", "unused", now, now),
        )
        user_id = conn.execute("SELECT id FROM users WHERE username = ?", (original_guest,)).fetchone()[0]
        conn.execute(
            "INSERT INTO users (username, email, hashed_password, is_active, is_superuser, "
            "created_at, updated_at, prediction_count) VALUES (?, ?, ?, 1, 0, ?, ?, 0)",
            (changed_email_guest, "changed@example.com", "unused", now, now),
        )
        conn.execute(
            "INSERT INTO users (username, email, hashed_password, is_active, is_superuser, "
            "created_at, updated_at, prediction_count) VALUES (?, ?, ?, 1, 0, ?, ?, 0)",
            ("guest_player", "registered@example.com", "unused", now, now),
        )
        conn.execute(
            "INSERT INTO spread_types (name, description, card_count, difficulty_level, positions, "
            "is_active, is_beginner_friendly, suitable_for_love, suitable_for_career, "
            "suitable_for_finance, suitable_for_health, suitable_for_general, usage_count) "
            "VALUES (?, ?, 1, 1, ?, 1, 1, 1, 1, 1, 1, 1, 0)",
            ("Migration Spread", "Migration test", "[]"),
        )
        spread_id = conn.execute("SELECT id FROM spread_types WHERE name = 'Migration Spread'").fetchone()[0]
        conn.execute(
            "INSERT INTO predictions (user_id, spread_type_id, question, question_type, status, "
            "created_at, interpretation_attempts, is_favorite) "
            "VALUES (?, ?, ?, ?, ?, ?, 0, 0)",
            (user_id, spread_id, "Migration question", "general", "pending", now),
        )

    command.upgrade(config, "head")

    with sqlite3.connect(database) as conn:
        is_guest, quota_day, quota_count = conn.execute(
            "SELECT is_guest, reading_quota_day, reading_quota_count FROM users WHERE id = ?",
            (user_id,),
        ).fetchone()
    assert is_guest == 1
    assert quota_day == datetime.now(timezone.utc).date().isoformat()
    assert quota_count == 1
    with sqlite3.connect(database) as conn:
        changed_email_is_guest = conn.execute(
            "SELECT is_guest FROM users WHERE username = ?", (changed_email_guest,)
        ).fetchone()[0]
    assert changed_email_is_guest == 1
    with sqlite3.connect(database) as conn:
        registered_is_guest = conn.execute(
            "SELECT is_guest FROM users WHERE username = 'guest_player'"
        ).fetchone()[0]
    assert registered_is_guest == 0
