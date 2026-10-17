import asyncio
from datetime import datetime, timedelta, timezone
from pathlib import Path

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import delete, engine_from_config, event, select, text
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.crud import prediction as prediction_crud
from app.crud import spread as spread_crud
from app.db.session import engine
from app.models.record import CardDraw, Interpretation, Prediction, PredictionStatus, QuestionType
from app.models.spread import SpreadType
from app.models.tarot_card import TarotCard
from app.models.user import User
from app.schemas.prediction import CardDrawCreate, InterpretationCreate
from app.services.budget_store import DatabaseBudgetStore


@pytest.fixture()
def owner_headers(client):
    response = client.post("/api/v1/guest-session")
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


@pytest.fixture()
def admin_headers(owner_headers, db_session):
    user = db_session.scalars(select(User)).one()
    user.is_superuser = True
    db_session.commit()
    return owner_headers


def _create_record(client, headers, spread_id):
    response = client.post("/api/v1/records/", headers=headers, json={
        "question": "Will my saved reading remain available?",
        "question_type": "general",
        "spread_type_id": spread_id,
    })
    assert response.status_code == 200
    return response.json()["id"]


@pytest.fixture()
def saved_reading(client, owner_headers, seeded_spread_and_cards):
    record_id = _create_record(client, owner_headers, seeded_spread_and_cards["spread_id"])
    drawn = client.post(f"/api/v1/records/{record_id}/draw", headers=owner_headers)
    assert drawn.status_code == 200
    interpreted = client.post(f"/api/v1/records/{record_id}/interpret", headers=owner_headers, json={
        "overall_interpretation": "Saved answer",
        "summary": "Saved summary",
    })
    assert interpreted.status_code == 200
    return record_id


def _reading_snapshot(client, headers, record_id):
    snapshot = []
    for suffix in ("", "/cards", "/interpretation"):
        response = client.get(f"/api/v1/records/{record_id}{suffix}", headers=headers)
        assert response.status_code == 200
        snapshot.append(response.json())
    return snapshot


def test_runtime_sqlite_enforces_foreign_keys_on_new_connections():
    if engine.dialect.name != "sqlite":
        pytest.skip("SQLite connection setting")
    for _ in range(2):
        with engine.connect() as connection:
            assert connection.execute(text("PRAGMA foreign_keys")).scalar_one() == 1
        engine.dispose()


@pytest.mark.parametrize("missing_parent", ["card", "draw_record", "interpretation_record", "user", "spread"])
def test_database_rejects_orphan_writes(
    client, owner_headers, db_session, seeded_spread_and_cards, missing_parent,
):
    record_id = _create_record(client, owner_headers, seeded_spread_and_cards["spread_id"])
    user_id = db_session.scalars(select(User.id)).one()
    if missing_parent in ("card", "draw_record"):
        orphan = CardDraw(
            prediction_id=999999 if missing_parent == "draw_record" else record_id,
            tarot_card_id=999999 if missing_parent == "card" else seeded_spread_and_cards["card_ids"][0],
            position=1,
        )
    elif missing_parent == "interpretation_record":
        orphan = Interpretation(prediction_id=999999, overall_interpretation="Orphan")
    else:
        orphan = Prediction(
            user_id=999999 if missing_parent == "user" else user_id,
            spread_type_id=999999 if missing_parent == "spread" else seeded_spread_and_cards["spread_id"],
            question="Orphan record",
            question_type=QuestionType.GENERAL,
        )
    db_session.add(orphan)
    with pytest.raises(IntegrityError):
        db_session.commit()
    db_session.rollback()
    assert db_session.get(Prediction, record_id) is not None


@pytest.mark.parametrize("target", ["card", "spread"])
def test_referenced_data_cannot_be_deleted_outside_api(
    client, owner_headers, db_session, saved_reading, seeded_spread_and_cards, target,
):
    if target == "card":
        model = TarotCard
        target_id = db_session.scalars(select(CardDraw.tarot_card_id)).first()
    else:
        model = SpreadType
        target_id = seeded_spread_and_cards["spread_id"]
    with pytest.raises(IntegrityError):
        db_session.execute(delete(model).where(model.id == target_id))
        db_session.commit()
    db_session.rollback()
    assert db_session.get(model, target_id) is not None
    assert _reading_snapshot(client, owner_headers, saved_reading)[2]["summary"] == "Saved summary"


@pytest.mark.parametrize("target", ["card", "spread"])
def test_referenced_data_delete_returns_conflict_and_preserves_history(
    client, admin_headers, db_session, saved_reading, seeded_spread_and_cards, target,
):
    before = _reading_snapshot(client, admin_headers, saved_reading)
    if target == "card":
        card_id = db_session.scalars(select(CardDraw.tarot_card_id)).first()
        url = f"/api/v1/cards/{card_id}"
    else:
        url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}?hard_delete=true"
    response = client.delete(url, headers=admin_headers)
    assert response.status_code == 409
    assert _reading_snapshot(client, admin_headers, saved_reading) == before


def test_pending_reading_also_protects_spread(client, admin_headers, seeded_spread_and_cards):
    record_id = _create_record(client, admin_headers, seeded_spread_and_cards["spread_id"])
    response = client.delete(
        f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}?hard_delete=true", headers=admin_headers,
    )
    assert response.status_code == 409
    assert client.post(f"/api/v1/records/{record_id}/draw", headers=admin_headers).status_code == 200


@pytest.mark.parametrize("target", ["card", "spread"])
def test_unused_data_can_still_be_deleted(client, admin_headers, seeded_spread_and_cards, target):
    if target == "card":
        url = f"/api/v1/cards/{seeded_spread_and_cards['card_ids'][0]}"
    else:
        url = f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}"
    response = client.delete(url, params={"hard_delete": True}, headers=admin_headers)
    assert response.status_code == 200
    assert client.get(url).status_code == 404
    assert client.delete(url, params={"hard_delete": True}, headers=admin_headers).status_code == 404


@pytest.mark.parametrize("target,hard_delete", [("cards", False), ("spreads", False), ("spreads", True)])
def test_only_admin_can_delete_reference_data(
    client, owner_headers, seeded_spread_and_cards, target, hard_delete,
):
    target_id = (seeded_spread_and_cards["card_ids"][0] if target == "cards"
                 else seeded_spread_and_cards["spread_id"])
    url = f"/api/v1/{target}/{target_id}"
    before = client.get(url).json()
    assert client.delete(url, params={"hard_delete": hard_delete}, headers=owner_headers).status_code == 403
    assert client.get(url).json() == before


@pytest.mark.parametrize("target,hard_delete", [("cards", False), ("spreads", False), ("spreads", True)])
def test_deleting_missing_reference_data_returns_not_found(client, admin_headers, target, hard_delete):
    response = client.delete(f"/api/v1/{target}/999999", params={"hard_delete": hard_delete}, headers=admin_headers)
    assert response.status_code == 404


def test_deactivating_spread_preserves_history_but_blocks_new_readings(
    client, admin_headers, saved_reading, seeded_spread_and_cards,
):
    spread_id = seeded_spread_and_cards["spread_id"]
    before = _reading_snapshot(client, admin_headers, saved_reading)
    assert client.delete(f"/api/v1/spreads/{spread_id}", headers=admin_headers).status_code == 200
    after = _reading_snapshot(client, admin_headers, saved_reading)
    assert client.get(f"/api/v1/spreads/{spread_id}").json()["is_active"] is False
    assert after == before
    assert client.get("/api/v1/spreads/").json() == []
    assert client.get("/api/v1/spreads/", params={"active_only": False}).json()[0]["id"] == spread_id
    response = client.post("/api/v1/records/", headers=admin_headers, json={
        "question": "New reading", "question_type": "general", "spread_type_id": spread_id,
    })
    assert response.status_code == 400


def test_started_reading_can_finish_after_spread_deactivation(client, admin_headers, seeded_spread_and_cards):
    spread_id = seeded_spread_and_cards["spread_id"]
    record_id = _create_record(client, admin_headers, spread_id)
    assert client.delete(f"/api/v1/spreads/{spread_id}", headers=admin_headers).status_code == 200
    assert client.post(f"/api/v1/records/{record_id}/draw", headers=admin_headers).status_code == 200
    response = client.post(f"/api/v1/records/{record_id}/interpret", headers=admin_headers, json={
        "overall_interpretation": "Finished after deactivation",
    })
    assert response.status_code == 200
    assert client.get(f"/api/v1/records/{record_id}", headers=admin_headers).json()["status"] == "completed"


@pytest.mark.parametrize("settled", [False, True])
def test_deleting_reading_cleans_children_without_refunding_quota_or_budget(
    client, owner_headers, db_session, db_session_factory, saved_reading, seeded_spread_and_cards, settled,
):
    user = db_session.scalars(select(User)).one()
    quota_before = (user.reading_quota_day, user.reading_quota_count)
    assert quota_before[1] == 1
    store = DatabaseBudgetStore(db_session_factory)
    reservation = store.reserve(model="test-model", estimated_micros=100, daily_limit_micros=1000,
                                monthly_limit_micros=2000)
    if settled:
        store.settle(reservation, actual_micros=60)
    budget_before = store.snapshot()
    assert budget_before["daily_spent_micros" if settled else "daily_reserved_micros"] > 0
    assert db_session.scalars(select(CardDraw)).all()
    assert db_session.scalars(select(Interpretation)).one()
    assert client.delete(f"/api/v1/records/{saved_reading}", headers=owner_headers).status_code == 200
    db_session.expire_all()
    assert db_session.get(Prediction, saved_reading) is None
    assert db_session.scalars(select(CardDraw)).all() == []
    assert db_session.scalars(select(Interpretation)).all() == []
    assert db_session.get(SpreadType, seeded_spread_and_cards["spread_id"]) is not None
    assert all(db_session.get(TarotCard, card_id) is not None for card_id in seeded_spread_and_cards["card_ids"])
    assert (user.reading_quota_day, user.reading_quota_count) == quota_before
    assert store.snapshot() == budget_before


def test_other_player_cannot_delete_saved_reading(client, owner_headers, saved_reading):
    before = _reading_snapshot(client, owner_headers, saved_reading)
    second_guest = client.post("/api/v1/guest-session")
    assert second_guest.status_code == 200
    other_headers = {"Authorization": f"Bearer {second_guest.json()['access_token']}"}
    assert client.delete(f"/api/v1/records/{saved_reading}", headers=other_headers).status_code == 404
    assert _reading_snapshot(client, owner_headers, saved_reading) == before


def test_deleted_reading_is_not_processed_by_delayed_ai_job(
    client, owner_headers, db_session, db_session_factory, seeded_spread_and_cards, monkeypatch,
):
    record_id = _create_record(client, owner_headers, seeded_spread_and_cards["spread_id"])
    assert client.post(f"/api/v1/records/{record_id}/draw", headers=owner_headers).status_code == 200
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session, record_id, now, now - timedelta(minutes=5), 3,
    )
    assert client.delete(f"/api/v1/records/{record_id}", headers=owner_headers).status_code == 200

    async def unexpected_ai_call(**kwargs):
        pytest.fail("A deleted reading must not reach the AI provider")

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", unexpected_ai_call)
    asyncio.run(records_endpoint.run_interpretation_job(db_session_factory, record_id, None, now))
    assert db_session.scalars(select(Interpretation)).all() == []


def test_spread_deleted_during_record_creation_returns_conflict_without_consuming_quota(
    client, owner_headers, db_session_factory, seeded_spread_and_cards, monkeypatch,
):
    validate = spread_crud.validate_spread_exists

    def validate_then_delete(db, spread_id):
        valid = validate(db, spread_id)
        assert valid
        with db_session_factory() as concurrent:
            assert spread_crud.hard_delete_spread(concurrent, spread_id)
        return valid

    monkeypatch.setattr(spread_crud, "validate_spread_exists", validate_then_delete)
    response = client.post("/api/v1/records/", headers=owner_headers, json={
        "question": "Concurrent reading", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert response.status_code == 409
    with db_session_factory() as check:
        assert check.scalars(select(Prediction)).all() == []
        user = check.scalars(select(User)).one()
        assert user.reading_quota_count == 0
        assert user.prediction_count == 0


@pytest.mark.parametrize("child", ["draw", "interpretation"])
def test_child_committed_during_record_deletion_returns_conflict_without_partial_deletion(
    client, owner_headers, db_session, db_session_factory, seeded_spread_and_cards, child,
):
    record_id = _create_record(client, owner_headers, seeded_spread_and_cards["spread_id"])
    now = datetime.now(timezone.utc)
    if child == "interpretation":
        assert client.post(f"/api/v1/records/{record_id}/draw", headers=owner_headers).status_code == 200
        assert prediction_crud.claim_interpretation_generation(
            db_session, record_id, now, now - timedelta(minutes=5), 3,
        )
    committed = []

    def commit_child_after_cascade_loaded(session, flush_context, instances):
        if not any(isinstance(item, Prediction) and item.id == record_id for item in session.deleted):
            return
        assert not committed
        with db_session_factory() as concurrent:
            if child == "interpretation":
                prediction_crud.create_interpretation(
                    concurrent, record_id, InterpretationCreate(overall_interpretation="Concurrent answer"),
                    prediction_status=PredictionStatus.COMPLETED, expected_started_at=now,
                )
            else:
                prediction_crud.batch_create_card_draws(
                    concurrent, record_id,
                    [CardDrawCreate(tarot_card_id=card_id, position=position)
                     for position, card_id in enumerate(seeded_spread_and_cards["card_ids"][:3], 1)],
                    prediction_status=PredictionStatus.PROCESSING,
                )
        committed.append(True)

    event.listen(Session, "before_flush", commit_child_after_cascade_loaded)
    try:
        response = client.delete(f"/api/v1/records/{record_id}", headers=owner_headers)
    finally:
        event.remove(Session, "before_flush", commit_child_after_cascade_loaded)
    assert committed == [True]
    assert response.status_code == 409
    with db_session_factory() as check:
        assert check.get(Prediction, record_id) is not None
        assert len(check.scalars(select(CardDraw)).all()) == 3
        if child == "interpretation":
            assert check.scalars(select(Interpretation)).one().overall_interpretation == "Concurrent answer"
    assert client.delete(f"/api/v1/records/{record_id}", headers=owner_headers).status_code == 200
    with db_session_factory() as check:
        assert check.get(Prediction, record_id) is None
        assert check.scalars(select(CardDraw)).all() == []
        assert check.scalars(select(Interpretation)).all() == []


def test_inflight_ai_result_cannot_restore_deleted_reading(
    client, owner_headers, db_session, db_session_factory, seeded_spread_and_cards, monkeypatch,
):
    record_id = _create_record(client, owner_headers, seeded_spread_and_cards["spread_id"])
    assert client.post(f"/api/v1/records/{record_id}/draw", headers=owner_headers).status_code == 200
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session, record_id, now, now - timedelta(minutes=5), 3,
    )
    calls = []

    async def complete_after_deletion(**kwargs):
        calls.append(True)
        with db_session_factory() as concurrent:
            assert prediction_crud.delete_prediction(concurrent, record_id)
        return {"overall_interpretation": "Too late", "model_used": "test_ai"}

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", complete_after_deletion)
    asyncio.run(records_endpoint.run_interpretation_job(db_session_factory, record_id, None, now))
    assert calls == [True]
    with db_session_factory() as check:
        assert check.get(Prediction, record_id) is None
        assert check.scalars(select(CardDraw)).all() == []
        assert check.scalars(select(Interpretation)).all() == []


def test_batch_migrations_remain_independent_of_runtime_foreign_key_setting(
    client, owner_headers, db_session, db_session_factory, saved_reading, monkeypatch,
):
    before = _reading_snapshot(client, owner_headers, saved_reading)
    db_session.close()
    monkeypatch.setattr(settings, "DATABASE_URL", str(db_session_factory.kw["bind"].url))
    migration_foreign_keys = []

    def capture_migration_engine(*args, **kwargs):
        migration_engine = engine_from_config(*args, **kwargs)

        def record_setting(connection, record):
            migration_foreign_keys.append(connection.execute("PRAGMA foreign_keys").fetchone()[0])

        event.listen(migration_engine, "connect", record_setting)
        return migration_engine

    monkeypatch.setattr("sqlalchemy.engine_from_config", capture_migration_engine)
    config = Config()
    config.set_main_option("script_location", str(Path(__file__).resolve().parents[1] / "alembic"))
    command.upgrade(config, "head")
    command.downgrade(config, "0004_persistent_ai_budget")
    command.upgrade(config, "head")
    assert migration_foreign_keys == [0, 0, 0]
    assert _reading_snapshot(client, owner_headers, saved_reading) == before
    with db_session_factory() as check:
        assert check.execute(text("PRAGMA foreign_keys")).scalar_one() == 1
        assert check.execute(text("PRAGMA foreign_key_check")).all() == []
