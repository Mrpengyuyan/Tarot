from __future__ import annotations

from app.api.v1.endpoints import health as health_endpoint
from app.crud.prediction import increment_user_prediction_count
from app.crud.spread import increment_spread_usage
from app.crud.user import create_user
from app.models.spread import SpreadType
from app.schemas.user import UserCreate


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


def test_ai_health_endpoint_redacts_internal_details(client, monkeypatch):
    async def fake_health_check():
        return {
            "service_name": "tarot-interpretation-service",
            "status": "healthy",
            "is_healthy": True,
            "provider": "deepseek",
            "message": "ok",
            "details": {
                "model_used": "deepseek-chat",
                "fallback_used": False,
                "sample": "pong",
                "budget": {"daily_spend_usd": 3.2},
            },
        }

    monkeypatch.setattr(
        health_endpoint.tarot_interpretation_service,
        "health_check",
        fake_health_check,
    )

    resp = client.get("/api/v1/health/ai")
    assert resp.status_code == 200
    body = resp.json()

    assert body["status"] == "healthy"
    assert body["is_healthy"] is True
    assert body["provider"] == "deepseek"
    assert body["details"] == {
        "model_used": "deepseek-chat",
        "fallback_used": False,
    }
    assert "budget" not in body["details"]
    assert "sample" not in body["details"]


def test_system_status_exposes_public_ai_fields_only(client, monkeypatch):
    async def fake_health_check():
        return {
            "service_name": "tarot-interpretation-service",
            "status": "degraded",
            "is_healthy": False,
            "provider": "deepseek",
            "message": "upstream timeout",
            "details": {"sample": "internal-detail"},
            "coze_configured": True,
            "coze_healthy": False,
        }

    monkeypatch.setattr(
        health_endpoint.tarot_interpretation_service,
        "health_check",
        fake_health_check,
    )

    resp = client.get("/api/v1/health/status")
    assert resp.status_code == 200
    ai_component = resp.json()["components"]["ai_service"]

    assert set(ai_component.keys()) == {"status", "is_healthy", "provider", "message"}
    assert ai_component["status"] == "degraded"
    assert ai_component["provider"] == "deepseek"


def test_increment_counters_handle_zero_values(db_session):
    user = create_user(
        db_session,
        UserCreate(
            username="null_counter_user",
            email="null_counter_user@example.com",
            password="password123",
        ),
    )
    user.prediction_count = 0
    db_session.commit()

    spread = SpreadType(
        name="Null Counter Spread",
        name_en="Null Counter Spread",
        description="Spread used for null counter regression checks",
        card_count=1,
        positions=[{"position": 1, "name": "Now", "meaning": "Current state"}],
        is_active=True,
        usage_count=0,
    )
    db_session.add(spread)
    db_session.commit()
    db_session.refresh(spread)

    assert increment_user_prediction_count(db_session, user.id) is True
    assert increment_spread_usage(db_session, spread.id) is True

    db_session.refresh(user)
    db_session.refresh(spread)
    assert user.prediction_count == 1
    assert spread.usage_count == 1


def test_smoke_seeded_draw_with_exclude_ids_is_deterministic(client, seeded_spread_and_cards):
    _register_and_login(client, username="smoke_draw_exclude_user")

    excluded_id = seeded_spread_and_cards["card_ids"][0]
    draw_params = {
        "count": 2,
        "seed": 20260329,
        "exclude_ids": [excluded_id],
    }

    draw_a = client.get("/api/v1/cards/draw", params=draw_params)
    draw_b = client.get("/api/v1/cards/draw", params=draw_params)
    assert draw_a.status_code == 200
    assert draw_b.status_code == 200

    body_a = draw_a.json()
    body_b = draw_b.json()
    ids_a = [item["id"] for item in body_a]
    ids_b = [item["id"] for item in body_b]

    assert ids_a == ids_b
    assert excluded_id not in ids_a
