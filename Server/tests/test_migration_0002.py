from __future__ import annotations

import sqlite3
from pathlib import Path

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import create_engine, inspect

from app.core.config import settings

SERVER_ROOT = Path(__file__).resolve().parents[1]
NEW_COLUMNS = {"interpretation_started_at", "interpretation_attempts"}
HEAD_REVISION = "0002_interpretation_tracking"


def _alembic_config() -> Config:
    config = Config()
    config.set_main_option("script_location", str(SERVER_ROOT / "alembic"))
    return config


def _prediction_columns(database_url: str) -> set[str]:
    engine = create_engine(database_url)
    try:
        return {column["name"] for column in inspect(engine).get_columns("predictions")}
    finally:
        engine.dispose()


def _current_revision(database_url: str) -> str:
    engine = create_engine(database_url)
    try:
        with engine.connect() as connection:
            return connection.exec_driver_sql("SELECT version_num FROM alembic_version").scalar_one()
    finally:
        engine.dispose()


@pytest.fixture()
def migration_db(tmp_path, monkeypatch):
    db_path = tmp_path / "migration.db"
    database_url = f"sqlite:///{db_path}"
    # alembic/env.py always replaces sqlalchemy.url with settings.DATABASE_URL.
    monkeypatch.setattr(settings, "DATABASE_URL", database_url)
    return db_path, database_url


def test_upgrade_head_on_fresh_database_has_generation_tracking(migration_db):
    _, database_url = migration_db

    command.upgrade(_alembic_config(), "head")

    assert NEW_COLUMNS <= _prediction_columns(database_url)
    assert _current_revision(database_url) == HEAD_REVISION
    assert len(HEAD_REVISION) <= 32


def test_upgrade_adds_missing_columns_to_legacy_predictions_table(migration_db):
    db_path, database_url = migration_db
    config = _alembic_config()
    command.upgrade(config, "0001_initial_schema")

    # 0001 builds tables from the current models; drop the new columns to recreate a pre-0002 schema.
    connection = sqlite3.connect(db_path)
    try:
        connection.execute("ALTER TABLE predictions DROP COLUMN interpretation_attempts")
        connection.execute("ALTER TABLE predictions DROP COLUMN interpretation_started_at")
        connection.commit()
    finally:
        connection.close()
    assert not NEW_COLUMNS & _prediction_columns(database_url)

    command.upgrade(config, "head")

    assert NEW_COLUMNS <= _prediction_columns(database_url)
    assert _current_revision(database_url) == HEAD_REVISION
