from __future__ import annotations

import sqlite3
from pathlib import Path

from alembic import command
from alembic.config import Config

from app.core.config import settings


def test_upgrade_adds_budget_tables_to_existing_database(tmp_path, monkeypatch):
    database = tmp_path / "budget_migration.db"
    monkeypatch.setattr(settings, "DATABASE_URL", f"sqlite:///{database}")
    config = Config()
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))
    command.upgrade(config, "0003_reading_quota")

    with sqlite3.connect(database) as conn:
        conn.execute("DROP TABLE ai_budget_reservations")
        conn.execute("DROP TABLE ai_budget_windows")

    command.upgrade(config, "head")

    with sqlite3.connect(database) as conn:
        names = {row[0] for row in conn.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    assert {"ai_budget_reservations", "ai_budget_windows"} <= names
