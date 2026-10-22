import pytest
from sqlalchemy import select

from app.crud.card import draw_random_cards
from app.db.base_class import Base
from app.models.spread import SpreadType
from app.models.tarot_card import TarotCard
from app.scripts import init_tarot_data


@pytest.fixture()
def seed_command(monkeypatch, db_session_factory):
    monkeypatch.setattr(init_tarot_data, "SessionLocal", db_session_factory)
    return init_tarot_data.main


def _snapshot(db, tables=None):
    db.expire_all()
    return {table.name: db.execute(select(table).order_by(*table.primary_key)).all()
            for table in (tables if tables is not None else Base.metadata.sorted_tables)}


def test_empty_database_imports_complete_reference_data(seed_command, db_session):
    seed_command()
    assert db_session.query(TarotCard).count() == 78
    assert db_session.query(SpreadType).count() == 6
    before = _snapshot(db_session)
    seed_command()
    assert _snapshot(db_session) == before


def test_repeated_seed_preserves_customized_reading_history(seed_command, client, db_session):
    seed_command()
    spread = db_session.query(SpreadType).filter(SpreadType.card_count == 3).first()
    assert spread is not None
    card = draw_random_cards(db_session, count=3, seed=0)[0]
    card.name_en, card.name_zh = "Customized card", "Customized card name"
    card.upright_meaning = "Customized upright meaning"
    spread.positions = [
        {**position, "name": f"Custom position {index}", "meaning": f"Custom meaning {index}"}
        for index, position in enumerate(spread.positions, 1)
    ]
    spread.description = "Customized spread description"
    db_session.commit()

    guest = client.post("/api/v1/guest-session")
    assert guest.status_code == 200
    headers = {"Authorization": "Bearer " + guest.json()["access_token"]}
    created = client.post("/api/v1/records/", headers=headers, json={
        "question": "Keep the original reading", "question_type": "general", "spread_type_id": spread.id,
    })
    assert created.status_code == 200
    path = f"/api/v1/records/{created.json()['id']}"
    drawn = client.post(path + "/draw?seed=0", headers=headers)
    assert drawn.status_code == 200
    assert card.id in {item["tarot_card_id"] for item in drawn.json()["card_draws"]}
    spread.is_active, spread.usage_count = False, 123
    db_session.commit()
    before = _snapshot(db_session)
    before_detail = client.get(path, headers=headers).json()
    before_cards = client.get(path + "/cards", headers=headers).json()

    seed_command()
    seed_command()

    assert _snapshot(db_session) == before
    assert client.get(path, headers=headers).json() == before_detail
    assert client.get(path + "/cards", headers=headers).json() == before_cards


@pytest.mark.parametrize("occupied", [TarotCard, SpreadType], ids=["cards", "inactive-spreads"])
def test_only_empty_group_is_imported(seed_command, db_session, seeded_spread_and_cards, occupied):
    empty, expected = (SpreadType, 6) if occupied is TarotCard else (TarotCard, 78)
    db_session.query(empty).delete()
    if occupied is SpreadType:
        db_session.query(SpreadType).update({"name": "Renamed spread", "is_active": False, "usage_count": 123})
    else:
        db_session.query(TarotCard).update({"card_number": TarotCard.card_number + 100})
    db_session.commit()
    before = _snapshot(db_session, [occupied.__table__])

    seed_command()

    assert _snapshot(db_session, [occupied.__table__]) == before
    assert db_session.query(empty).count() == expected


@pytest.mark.parametrize("missing_directory", [False, True], ids=["missing-files", "missing-directory"])
def test_existing_groups_do_not_require_seed_sources(
    seed_command, db_session, seeded_spread_and_cards, monkeypatch, tmp_path, missing_directory,
):
    monkeypatch.setattr(init_tarot_data, "DATA_DIR", tmp_path / "missing" if missing_directory else tmp_path)
    before = _snapshot(db_session)
    seed_command()
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("model,initialize", [
    (TarotCard, init_tarot_data.init_tarot_cards), (SpreadType, init_tarot_data.init_spreads),
], ids=["cards", "spreads"])
def test_existing_group_is_skipped_without_loading_seed_files(
    db_session, seeded_spread_and_cards, monkeypatch, model, initialize,
):
    before = _snapshot(db_session)
    count = db_session.query(model).count()
    monkeypatch.setattr(init_tarot_data, "load_json_data", lambda filename: pytest.fail("Must skip existing group"))
    assert initialize(db_session) == count
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("initialize,model", [
    (init_tarot_data.init_tarot_cards, TarotCard), (init_tarot_data.init_spreads, SpreadType),
], ids=["cards", "spreads"])
def test_failed_group_commit_rolls_back_new_rows(db_session, monkeypatch, initialize, model):
    def fail_commit():
        raise RuntimeError("Simulated initialization commit failure")

    monkeypatch.setattr(db_session, "commit", fail_commit)
    assert initialize(db_session) == 0
    assert db_session.query(model).count() == 0
    assert not db_session.new
