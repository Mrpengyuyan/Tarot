from __future__ import annotations

from datetime import datetime
from types import SimpleNamespace

from sqlalchemy.exc import IntegrityError

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings


def _register_and_login(client, username: str):
    register_payload = {
        "username": username,
        "email": f"{username}@example.com",
        "password": "password123",
    }
    register_resp = client.post("/api/v1/register", json=register_payload)
    assert register_resp.status_code == 200

    login_resp = client.post(
        "/api/v1/login",
        data={"username": username, "password": "password123"},
    )
    assert login_resp.status_code == 200
    csrf_token = login_resp.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(settings.CSRF_COOKIE_NAME)
    if csrf_token:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})


def _create_prediction(client, spread_id: int) -> int:
    create_resp = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": "Will this flow be stable?",
            "question_type": "general",
        },
    )
    assert create_resp.status_code == 200
    return create_resp.json()["id"]


def test_draw_cards_returns_409_when_batch_insert_conflicts(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="draw_conflict_user")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    def fake_batch_create_card_draws(db, prediction_id, card_draws_data):  # noqa: ANN001
        del db, prediction_id, card_draws_data
        raise IntegrityError("insert", {}, Exception("duplicate key"))

    monkeypatch.setattr(
        records_endpoint.prediction_crud,
        "batch_create_card_draws",
        fake_batch_create_card_draws,
    )

    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 409
    assert "already drawn" in draw_resp.json()["detail"].lower()


def test_interpretation_conflict_returns_existing_record(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="interpret_conflict_user")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200

    async def fake_ai_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        del db, prediction, cards_data, user_context
        return {
            "overall_interpretation": "AI payload before conflict",
            "card_analysis": None,
            "relationship_analysis": None,
            "advice": "Keep steady pace",
            "warning": None,
            "summary": "Stable",
            "key_themes": ["pace", "stability"],
            "model_used": "unit_test_ai",
            "model_version": "v-test",
            "confidence_score": 0.8,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_ai_create_interpretation,
    )

    fake_existing = SimpleNamespace(
        id=999,
        prediction_id=prediction_id,
        overall_interpretation="Existing interpretation from conflict fallback",
        card_analysis=None,
        relationship_analysis=None,
        advice="Use existing",
        warning=None,
        summary=None,
        key_themes="fallback,existing",
        model_used="unit_test_existing",
        model_version="v-existing",
        confidence_score=0.77,
        generated_at=datetime.utcnow(),
    )

    calls = {"get_interpretation": 0}

    def fake_get_prediction_interpretation(db, prediction_id):  # noqa: ANN001
        del db, prediction_id
        calls["get_interpretation"] += 1
        if calls["get_interpretation"] == 1:
            return None
        return fake_existing

    def fake_create_interpretation(db, prediction_id, interpretation_create):  # noqa: ANN001
        del db, prediction_id, interpretation_create
        raise IntegrityError("insert", {}, Exception("duplicate key"))

    monkeypatch.setattr(
        records_endpoint.prediction_crud,
        "get_prediction_interpretation",
        fake_get_prediction_interpretation,
    )
    monkeypatch.setattr(
        records_endpoint.prediction_crud,
        "create_interpretation",
        fake_create_interpretation,
    )

    interpret_resp = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert interpret_resp.status_code == 200
    body = interpret_resp.json()
    assert body["id"] == 999
    assert body["prediction_id"] == prediction_id
    assert "existing interpretation" in body["overall_interpretation"].lower()


def test_cards_draw_endpoint_seed_is_reproducible(client, seeded_spread_and_cards):
    _register_and_login(client, username="cards_seed_user")
    del seeded_spread_and_cards

    draw_a = client.get("/api/v1/cards/draw?count=3&seed=20260316")
    draw_b = client.get("/api/v1/cards/draw?count=3&seed=20260316")
    assert draw_a.status_code == 200
    assert draw_b.status_code == 200

    ids_a = [item["id"] for item in draw_a.json()]
    ids_b = [item["id"] for item in draw_b.json()]
    assert ids_a == ids_b


def test_cards_draw_endpoint_returns_400_when_count_exceeds_available(client, seeded_spread_and_cards):
    _register_and_login(client, username="cards_overflow_user")
    available_cards = len(seeded_spread_and_cards["card_ids"])

    draw_resp = client.get(f"/api/v1/cards/draw?count={available_cards + 1}")
    assert draw_resp.status_code == 400
    assert "not enough available cards" in draw_resp.json()["detail"].lower()


def test_cards_draw_endpoint_shuffles_full_deck_with_seed(client, seeded_spread_and_cards):
    _register_and_login(client, username="cards_full_deck_user")
    expected_order = seeded_spread_and_cards["card_ids"]
    count = len(expected_order)

    draw_a = client.get(f"/api/v1/cards/draw?count={count}&seed=42")
    draw_b = client.get(f"/api/v1/cards/draw?count={count}&seed=42")
    assert draw_a.status_code == 200
    assert draw_b.status_code == 200

    ids_a = [item["id"] for item in draw_a.json()]
    ids_b = [item["id"] for item in draw_b.json()]
    assert ids_a == ids_b
    assert sorted(ids_a) == sorted(expected_order)
    assert ids_a != expected_order
