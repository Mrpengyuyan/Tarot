from __future__ import annotations

import sqlite3
import traceback
from contextlib import contextmanager
from pathlib import Path

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import event
from sqlalchemy.engine import Engine

from app.core.config import settings


PREVIOUS = "0006_guest_session_rate_limit"
REVISION = "0007_email_identity"
VALID_EMAILS = [
    (101, "Mixed.Case+Tag@Example.COM", b"mixed.case+tag@example.com"),
    (107, "\u7528\u6237@\u4f8b\u5b50.\u516c\u53f8", "\u7528\u6237@\u4f8b\u5b50.\u516c\u53f8".encode("utf-8")),
    (113, "Idn@xn--bcher-kva.de", "idn@b\u00fccher.de".encode("utf-8")),
    (127, "Cafe\u0301@Example.COM", "caf\u00e9@example.com".encode("utf-8")),
    (131, "\u0130pek@Example.COM", "i\u0307pek@example.com".encode("utf-8")),
    (149, "Stra\u00dfe@Example.COM", "stra\u00dfe@example.com".encode("utf-8")),
    (151, "Strasse@Example.COM", b"strasse@example.com"),
    (163, "Reader Name <Named@Example.COM>", b"named@example.com"),
]


@pytest.fixture()
def migration_db(tmp_path, monkeypatch):
    database = tmp_path / "email_identity.db"
    monkeypatch.setattr(settings, "DATABASE_URL", f"sqlite:///{database}")
    config = Config()
    config.set_main_option(
        "script_location", str(Path(__file__).resolve().parents[1] / "alembic")
    )
    return database, config


def _legacy_schema(database, config):
    command.upgrade(config, PREVIOUS)
    with sqlite3.connect(database) as conn:
        # 0001 uses current metadata, not the historical users definition.
        columns = {row[1] for row in conn.execute("PRAGMA table_info(users)")}
        if "email_key" in columns:
            conn.execute("DROP INDEX ix_users_email_key")
            conn.execute("ALTER TABLE users DROP COLUMN email_key")


def _insert_user(conn, user_id, email, *, key=None):
    columns = (
        "id, username, email, hashed_password, nickname, avatar_url, birth_date, "
        "zodiac_sign, bio, is_active, is_superuser, is_guest, reading_quota_day, "
        "reading_quota_count, created_at, updated_at, last_login, prediction_count"
    )
    values = [
        user_id, f"legacy_{user_id}", email, f"private-password-hash-{user_id}",
        f"nickname-{user_id}", "https://example.com/avatar.png", "1990-01-02 03:04:05",
        "aries", "private profile", 1, 0, 0, "2026-01-02", 2,
        "2025-02-03 04:05:06.123456", "2025-06-07 08:09:10.654321",
        "2025-09-10 11:12:13.456789", 3,
    ]
    if key is not None:
        columns += ", email_key"
        values.append(key)
    conn.execute(
        f"INSERT INTO users ({columns}) VALUES ({', '.join('?' for _ in values)})",
        values,
    )


def _seed_history(database):
    with sqlite3.connect(database) as conn:
        conn.execute("PRAGMA foreign_keys=ON")
        conn.execute(
            "CREATE INDEX ix_users_history_probe ON users (last_login, created_at) "
            "WHERE is_active = 1"
        )
        conn.execute(
            "CREATE TABLE legacy_sessions (id INTEGER PRIMARY KEY, "
            "user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE, "
            "token_hash TEXT NOT NULL, expires_at TEXT NOT NULL)"
        )
        conn.execute(
            "INSERT INTO legacy_sessions VALUES "
            "(9001, 101, 'private-session-hash', '2027-01-01 00:00:00')"
        )
        conn.execute(
            "INSERT INTO spread_types (id, name, description, card_count, difficulty_level, "
            "positions, is_active, is_beginner_friendly, suitable_for_love, "
            "suitable_for_career, suitable_for_finance, suitable_for_health, "
            "suitable_for_general, usage_count) "
            "VALUES (701, 'Legacy spread', 'preserve', 1, 1, '[]', 1, 1, 1, 1, 1, 1, 1, 9)"
        )
        conn.execute(
            "INSERT INTO predictions (id, user_id, spread_type_id, question, question_type, "
            "status, created_at, completed_at, interpretation_attempts, ai_run_state, "
            "ai_requests_started, is_favorite, user_notes) VALUES "
            "(801, 101, 701, 'private history', 'general', 'completed', "
            "'2025-01-02 03:04:05', '2025-01-02 03:05:06', 1, 'completed', 1, 1, 'keep notes')"
        )
        conn.execute(
            "INSERT INTO interpretations (id, prediction_id, overall_interpretation, "
            "generated_at) VALUES (901, 801, 'private answer', '2025-01-02 03:05:06')"
        )


def _quote(name):
    return '"' + name.replace('"', '""') + '"'


def _data(database):
    with sqlite3.connect(database) as conn:
        tables = conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table' "
            "AND name NOT LIKE 'sqlite_%' AND name != 'alembic_version' ORDER BY name"
        ).fetchall()
        result = {}
        for (table,) in tables:
            columns = [
                row[1] for row in conn.execute(f"PRAGMA table_info({_quote(table)})")
                if row[1] != "email_key"
            ]
            result[table] = conn.execute(
                f"SELECT {', '.join(map(_quote, columns))} FROM {_quote(table)} ORDER BY rowid"
            ).fetchall()
        return result


def _schema(database):
    with sqlite3.connect(database) as conn:
        tables = conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table' "
            "AND name NOT LIKE 'sqlite_%' ORDER BY name"
        ).fetchall()
        result = {}
        for (table,) in tables:
            indexes = []
            for index in conn.execute(f"PRAGMA index_list({_quote(table)})").fetchall():
                columns = tuple(
                    row[2] for row in conn.execute(f"PRAGMA index_info({_quote(index[1])})")
                )
                indexes.append((index[1], index[2], index[3], index[4], columns))
            result[table] = {
                "columns": tuple(
                    row[1:] for row in conn.execute(f"PRAGMA table_info({_quote(table)})")
                ),
                "indexes": tuple(sorted(indexes)),
                "foreign_keys": tuple(
                    conn.execute(f"PRAGMA foreign_key_list({_quote(table)})")
                ),
            }
        return result


def _dump(database):
    with sqlite3.connect(database) as conn:
        return tuple(conn.iterdump())


def _version(database):
    with sqlite3.connect(database) as conn:
        return conn.execute("SELECT version_num FROM alembic_version").fetchone()[0]


def _assert_key_schema(database):
    schema = _schema(database)["users"]
    column = next((col for col in schema["columns"] if col[0] == "email_key"), None)
    assert column is not None, "0007 must add the legacy email_key column"
    assert column[1:3] == ("BLOB", 1)
    indexes = [index for index in schema["indexes"] if index[-1] == ("email_key",)]
    assert indexes == [("ix_users_email_key", 1, "c", 0, ("email_key",))]


def _assert_children_intact(database):
    with sqlite3.connect(database) as conn:
        conn.execute("PRAGMA foreign_keys=ON")
        assert conn.execute("PRAGMA foreign_key_check").fetchall() == []
        with pytest.raises(sqlite3.IntegrityError, match="FOREIGN KEY"):
            conn.execute(
                "INSERT INTO legacy_sessions VALUES (9002, -1, 'invalid', '2027-01-01')"
            )


@contextmanager
def _record_writes():
    writes = []

    def record(conn, cursor, statement, parameters, context, executemany):
        if statement.lstrip().split(None, 1)[0].upper() in {
            "CREATE", "ALTER", "DROP", "INSERT", "UPDATE", "DELETE", "REPLACE",
        }:
            writes.append(statement)

    event.listen(Engine, "before_cursor_execute", record)
    try:
        yield writes
    finally:
        event.remove(Engine, "before_cursor_execute", record)


def test_legacy_backfill_preserves_original_values_children_and_indexes(migration_db):
    database, config = migration_db
    _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        for user_id, email, _ in VALID_EMAILS:
            _insert_user(conn, user_id, email)
    _seed_history(database)
    original_data = _data(database)
    original_schema = _schema(database)

    command.upgrade(config, "head")

    _assert_key_schema(database)
    assert _version(database) == REVISION
    assert _data(database) == original_data
    with sqlite3.connect(database) as conn:
        assert conn.execute(
            "SELECT id, email, email_key, typeof(email_key) FROM users ORDER BY id"
        ).fetchall() == [(uid, email, key, "blob") for uid, email, key in VALID_EMAILS]
        assert conn.execute(
            "SELECT sql FROM sqlite_master WHERE name='ix_users_history_probe'"
        ).fetchone()[0].endswith("WHERE is_active = 1")
    new_schema = _schema(database)
    users = new_schema["users"]
    users["columns"] = tuple(col for col in users["columns"] if col[0] != "email_key")
    users["indexes"] = tuple(
        index for index in users["indexes"] if index[0] != "ix_users_email_key"
    )
    assert new_schema == original_schema
    _assert_children_intact(database)


def test_repeated_head_upgrade_is_a_no_op(migration_db):
    database, config = migration_db
    _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        _insert_user(conn, *VALID_EMAILS[0][:2])
    command.upgrade(config, "head")
    assert _version(database) == REVISION
    before = _dump(database)

    with _record_writes() as writes:
        command.upgrade(config, "head")

    assert writes == []
    assert _dump(database) == before


CONFLICTS = [
    pytest.param(["Private@Example.COM", "private@example.com"], id="ascii-case"),
    pytest.param(["\u7528\u6237@Example.COM", "\u7528\u6237@example.com"], id="unicode"),
    pytest.param(["Idn@xn--bcher-kva.de", "idn@b\u00fccher.de"], id="idn"),
    pytest.param(["Cafe\u0301@Example.COM", "Caf\u00e9@example.com"], id="nfc"),
    pytest.param(["\u0130pek@example.com", "i\u0307pek@example.com"], id="unicode-lower"),
    pytest.param(["private-address-without-at"], id="missing-at"),
    pytest.param(["private@invalid..example.com"], id="invalid-domain"),
]


@pytest.mark.parametrize("emails", CONFLICTS)
@pytest.mark.parametrize("current_metadata", [False, True], ids=["legacy", "current-metadata"])
def test_preflight_rejects_without_writes_or_private_diagnostics(
    migration_db, emails, current_metadata, capsys, caplog,
):
    database, config = migration_db
    if current_metadata:
        command.upgrade(config, PREVIOUS)
    else:
        _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        _insert_user(conn, 101, "safe@example.com", key=b"safe" if current_metadata else None)
        for user_id, email in enumerate(emails, 201):
            key = f"old-key-{user_id}".encode() if current_metadata else None
            _insert_user(conn, user_id, email, key=key)
    _seed_history(database)
    before = _dump(database)
    before_schema = _schema(database)
    capsys.readouterr()
    caplog.clear()

    with _record_writes() as writes:
        with pytest.raises(RuntimeError) as error:
            command.upgrade(config, "head")

    assert writes == []
    assert _dump(database) == before
    assert _schema(database) == before_schema
    assert _version(database) == PREVIOUS
    assert error.value.__cause__ is None
    assert error.value.__context__ is None
    message = str(error.value)
    assert "user" in message.lower() and "id" in message.lower()
    for user_id in range(201, 201 + len(emails)):
        assert str(user_id) in message
    assert "101" not in message
    captured = capsys.readouterr()
    diagnostics = message + captured.out + captured.err + caplog.text + "".join(
        traceback.format_exception(type(error.value), error.value, error.value.__traceback__)
    )
    for private in [*emails, "old-key-", "private-password-hash", "private-session-hash"]:
        assert private not in diagnostics
    assert "@" not in message
    assert "input_value" not in diagnostics
    assert "pydantic_core" not in diagnostics
    assert "email-validator" not in diagnostics
    _assert_children_intact(database)


def test_preflight_checks_all_rows_before_reporting_all_conflicting_ids(migration_db):
    database, config = migration_db
    _legacy_schema(database, config)
    emails = [
        "first-private-invalid", "Clash@Example.COM", "clash@example.com",
        "last-private-invalid", "Other@Example.COM", "other@example.com",
        "CLASH@example.com",
    ]
    with sqlite3.connect(database) as conn:
        for user_id, email in enumerate(emails, 201):
            _insert_user(conn, user_id, email)
    before = _dump(database)

    with _record_writes() as writes:
        with pytest.raises(RuntimeError) as error:
            command.upgrade(config, "head")

    assert writes == []
    assert _dump(database) == before
    message = str(error.value)
    for user_id in range(201, 208):
        assert str(user_id) in message
    assert "@" not in message
    assert error.value.__context__ is None


@pytest.mark.parametrize("key", [None, b"mixed.case+tag@example.com"], ids=["not-null", "unique"])
def test_database_enforces_key_constraints(migration_db, key):
    database, config = migration_db
    _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        for user_id, email, _ in VALID_EMAILS[:2]:
            _insert_user(conn, user_id, email)
    command.upgrade(config, "head")
    _assert_key_schema(database)
    before = _dump(database)

    with sqlite3.connect(database) as conn:
        with pytest.raises(sqlite3.IntegrityError):
            conn.execute("UPDATE users SET email_key=? WHERE id=107", (key,))

    assert _dump(database) == before


def test_fresh_database_upgrades_to_head(migration_db):
    database, config = migration_db

    command.upgrade(config, "head")

    _assert_key_schema(database)
    assert _version(database) == REVISION
    with sqlite3.connect(database) as conn:
        for user_id, email, key in VALID_EMAILS:
            _insert_user(conn, user_id, email, key=key)
        assert conn.execute("PRAGMA foreign_key_check").fetchall() == []


def test_current_metadata_upgrade_does_not_rewrite_keys_or_schema(migration_db):
    database, config = migration_db
    command.upgrade(config, PREVIOUS)
    with sqlite3.connect(database) as conn:
        for user_id, email, key in VALID_EMAILS:
            _insert_user(conn, user_id, email, key=key)
    _seed_history(database)
    before_schema = _schema(database)
    before_data = _data(database)
    with sqlite3.connect(database) as conn:
        before_users = conn.execute("SELECT * FROM users ORDER BY id").fetchall()

    with _record_writes() as writes:
        command.upgrade(config, "head")

    assert _version(database) == REVISION
    assert _schema(database) == before_schema
    assert _data(database) == before_data
    assert writes and all("UPDATE alembic_version" in statement for statement in writes)
    with sqlite3.connect(database) as conn:
        assert conn.execute("SELECT * FROM users ORDER BY id").fetchall() == before_users


def test_empty_legacy_database_upgrades(migration_db):
    database, config = migration_db
    _legacy_schema(database, config)

    command.upgrade(config, "head")

    _assert_key_schema(database)
    assert _version(database) == REVISION


def test_downgrade_drops_only_key_and_index_and_reupgrade_preserves_history(migration_db):
    database, config = migration_db
    _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        for user_id, email, _ in VALID_EMAILS:
            _insert_user(conn, user_id, email)
    _seed_history(database)
    old_data = _data(database)
    old_schema = _schema(database)
    command.upgrade(config, "head")
    _assert_key_schema(database)

    command.downgrade(config, PREVIOUS)

    assert _version(database) == PREVIOUS
    assert _schema(database) == old_schema
    assert _data(database) == old_data
    _assert_children_intact(database)

    command.upgrade(config, "head")

    _assert_key_schema(database)
    assert _version(database) == REVISION
    assert _data(database) == old_data
    with sqlite3.connect(database) as conn:
        assert conn.execute("SELECT id, email_key FROM users ORDER BY id").fetchall() == [
            (user_id, key) for user_id, _, key in VALID_EMAILS
        ]
    _assert_children_intact(database)


@pytest.mark.parametrize("direction", ["upgrade", "downgrade"])
def test_fk_enabled_connection_is_rejected_before_any_writes(migration_db, direction):
    database, config = migration_db
    _legacy_schema(database, config)
    with sqlite3.connect(database) as conn:
        _insert_user(conn, *VALID_EMAILS[0][:2])
        conn.execute(
            "CREATE TABLE cascade_children (id INTEGER PRIMARY KEY, "
            "user_id INTEGER REFERENCES users(id) ON DELETE CASCADE)"
        )
        conn.execute("INSERT INTO cascade_children VALUES (9001, 101)")
    if direction == "downgrade":
        command.upgrade(config, "head")
    before = _dump(database)
    version = _version(database)

    def enable_foreign_keys(dbapi_connection, connection_record):
        dbapi_connection.execute("PRAGMA foreign_keys=ON")

    event.listen(Engine, "connect", enable_foreign_keys)
    try:
        with _record_writes() as writes:
            with pytest.raises(RuntimeError, match="foreign keys"):
                if direction == "upgrade":
                    command.upgrade(config, "head")
                else:
                    command.downgrade(config, PREVIOUS)
    finally:
        event.remove(Engine, "connect", enable_foreign_keys)

    assert writes == []
    assert _dump(database) == before
    assert _version(database) == version
