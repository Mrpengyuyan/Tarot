from __future__ import annotations

import sqlite3
from pathlib import Path

from alembic import command
from alembic.config import Config

from app.core.config import settings


def test_upgrade_creates_guest_session_rate_limit_window(tmp_path, monkeypatch):
    database = tmp_path / "guest_rate_limit.db"
    monkeypatch.setattr(settings, "DATABASE_URL", f"sqlite:///{database}")
    config = Config()
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))

    command.upgrade(config, "0005_ai_cost_priority")
    with sqlite3.connect(database) as conn:
        conn.execute("DROP TABLE guest_session_windows")

    command.upgrade(config, "head")

    with sqlite3.connect(database) as conn:
        columns = {
            row[1] for row in conn.execute("PRAGMA table_info(guest_session_windows)")
        }
    assert columns == {"window_key", "source_hash", "issued"}
