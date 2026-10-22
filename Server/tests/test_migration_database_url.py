from __future__ import annotations

import sqlite3
from io import StringIO
from pathlib import Path

import pytest
from alembic import command
from alembic.config import Config
from alembic.script import ScriptDirectory
from sqlalchemy import create_engine

from app.core.config import settings


def _alembic_config() -> Config:
    config = Config(output_buffer=StringIO())
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))
    return config


@pytest.mark.parametrize("filename", ["plain.db", "literal%.db", "encoded%25.db", "mixed%2F%%.db", "named%(db)s.db"])
def test_upgrade_preserves_database_url_and_existing_data(tmp_path, monkeypatch, filename):
    database = tmp_path / filename
    url = f"sqlite:///{database}"
    monkeypatch.setattr(settings, "DATABASE_URL", url)
    engine = create_engine(url)
    try:
        with engine.begin() as connection:
            connection.exec_driver_sql("CREATE TABLE audit_marker (value TEXT NOT NULL)")
            connection.exec_driver_sql("INSERT INTO audit_marker VALUES ('preserve-me')")
    finally:
        engine.dispose()

    config = _alembic_config()
    command.upgrade(config, "head")
    command.upgrade(config, "head")

    assert settings.DATABASE_URL == url
    assert config.get_main_option("sqlalchemy.url") == url
    assert config.get_section(config.config_ini_section)["sqlalchemy.url"] == url
    with sqlite3.connect(database) as connection:
        tables = {row[0] for row in connection.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
        assert {"users", "predictions", "ai_budget_windows", "guest_session_windows"} <= tables
        assert connection.execute("SELECT version_num FROM alembic_version").fetchall() == [
            (ScriptDirectory.from_config(config).get_current_head(),)
        ]
        assert connection.execute("SELECT value FROM audit_marker").fetchall() == [("preserve-me",)]
    assert set(tmp_path.iterdir()) == {database}


@pytest.mark.parametrize("url", [
    "sqlite:///{database}",
    "postgresql://audit_user:audit%25%2Fpass@127.0.0.1:9/audit_database",
    "postgresql://audit_user:audit%%(name)s@127.0.0.1:9/audit_database",
])
def test_offline_configuration_preserves_url_without_connecting(tmp_path, monkeypatch, url):
    database = tmp_path / "offline%25.db"
    url = url.format(database=database)
    monkeypatch.setattr(settings, "DATABASE_URL", url)
    config = _alembic_config()

    def reject_live_engine(*args, **kwargs):
        pytest.fail("Offline URL configuration must not create a live database engine")

    monkeypatch.setattr("sqlalchemy.engine_from_config", reject_live_engine)
    # Exercise offline configuration only; schema revisions may require live inspection.
    command.upgrade(config, "base", sql=True)

    assert settings.DATABASE_URL == url
    assert config.get_main_option("sqlalchemy.url") == url
    assert config.get_section(config.config_ini_section)["sqlalchemy.url"] == url
    assert not database.exists()
