from __future__ import annotations

import sqlite3
from datetime import datetime, timezone
from pathlib import Path

from alembic import command
from alembic.config import Config

from app.core.config import settings


def test_upgrade_treats_historical_generation_attempts_as_possibly_paid(tmp_path, monkeypatch):
    database = tmp_path / "cost_priority.db"
    monkeypatch.setattr(settings, "DATABASE_URL", f"sqlite:///{database}")
    config = Config()
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))
    command.upgrade(config, "0004_persistent_ai_budget")

    with sqlite3.connect(database) as conn:
        for column in (
            "ai_last_error", "ai_budget_retry_at", "ai_request_started_at",
            "ai_requests_started", "ai_run_state",
        ):
            conn.execute(f"ALTER TABLE predictions DROP COLUMN {column}")
        now = datetime.now(timezone.utc).isoformat()
        conn.execute(
            "INSERT INTO users (username, email, hashed_password, is_active, is_superuser, "
            "created_at, updated_at, prediction_count) VALUES ('migration_user', 'migration@example.com', 'x', 1, 0, ?, ?, 0)",
            (now, now),
        )
        conn.execute(
            "INSERT INTO spread_types (name, description, card_count, difficulty_level, positions, "
            "is_active, is_beginner_friendly, suitable_for_love, suitable_for_career, "
            "suitable_for_finance, suitable_for_health, suitable_for_general, usage_count) "
            "VALUES ('Migration', 'test', 1, 1, '[]', 1, 1, 1, 1, 1, 1, 1, 0)"
        )
        user_id = conn.execute("SELECT id FROM users WHERE username = 'migration_user'").fetchone()[0]
        spread_id = conn.execute("SELECT id FROM spread_types WHERE name = 'Migration'").fetchone()[0]
        for attempts in (0, 1, 2, 1):
            conn.execute(
                "INSERT INTO predictions (user_id, spread_type_id, question, question_type, status, "
                "created_at, interpretation_started_at, interpretation_attempts, is_favorite) "
                "VALUES (?, ?, 'question', 'general', 'failed', ?, ?, ?, 0)",
                (user_id, spread_id, now, now, attempts),
            )
        completed_id = conn.execute("SELECT max(id) FROM predictions").fetchone()[0]
        conn.execute(
            "INSERT INTO interpretations (prediction_id, overall_interpretation, generated_at) VALUES (?, 'answer', ?)",
            (completed_id, now),
        )

    command.upgrade(config, "head")
    with sqlite3.connect(database) as conn:
        rows = conn.execute(
            "SELECT ai_run_state, ai_requests_started FROM predictions ORDER BY id"
        ).fetchall()
    assert rows == [
        ("not_started", 0), ("retry_required", 1), ("exhausted", 2), ("completed", 1),
    ]
