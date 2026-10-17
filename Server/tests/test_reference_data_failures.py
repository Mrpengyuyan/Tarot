import pytest

from app.db.bootstrap import ensure_reference_data
from app.scripts import init_tarot_data


def test_seed_command_fails_when_reference_files_are_missing(tmp_path, db_session_factory, monkeypatch):
    monkeypatch.setattr(init_tarot_data, "DATA_DIR", tmp_path)
    monkeypatch.setattr(init_tarot_data, "SessionLocal", db_session_factory)

    with pytest.raises(RuntimeError, match="Reference data initialization failed"):
        init_tarot_data.main()


def test_auto_bootstrap_fails_when_reference_files_are_missing(tmp_path, db_session, monkeypatch):
    monkeypatch.setattr(init_tarot_data, "DATA_DIR", tmp_path)

    with pytest.raises(RuntimeError, match="Reference data initialization failed"):
        ensure_reference_data(db_session)
