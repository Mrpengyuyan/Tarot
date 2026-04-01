from __future__ import annotations

import asyncio

from fastapi import FastAPI

import app.main as main_module
from app.api.deps import get_current_superuser
from app.api.v1.endpoints import health as health_endpoint
from app.core.config import settings
from app.main import app
from app.services.coze_service import CozeService


def test_ai_deep_health_requires_superuser(client):
    resp = client.get("/api/v1/health/ai/deep")
    assert resp.status_code == 401


def test_ai_deep_health_uses_deep_probe_for_superuser(client, monkeypatch):
    captured: dict[str, bool] = {"deep": False}

    async def fake_health_check(*, deep: bool = False):
        captured["deep"] = deep
        return {
            "service_name": "tarot-interpretation-service",
            "status": "healthy",
            "is_healthy": True,
            "provider": "deepseek",
            "message": "ok",
            "details": {
                "probe": "live",
                "model_used": "deepseek-chat",
                "fallback_used": False,
            },
        }

    monkeypatch.setattr(
        health_endpoint.tarot_interpretation_service,
        "health_check",
        fake_health_check,
    )
    app.dependency_overrides[get_current_superuser] = lambda: object()
    try:
        resp = client.get("/api/v1/health/ai/deep")
    finally:
        app.dependency_overrides.pop(get_current_superuser, None)

    assert resp.status_code == 200
    assert resp.json()["status"] == "healthy"
    assert captured["deep"] is True


def test_system_status_sanitizes_unhealthy_ai_message(client, monkeypatch):
    async def fake_health_check(*, deep: bool = False):
        del deep
        return {
            "service_name": "tarot-interpretation-service",
            "status": "unhealthy",
            "is_healthy": False,
            "provider": "deepseek",
            "message": "internal stack trace should not leak",
            "details": {"sample": "sensitive"},
        }

    monkeypatch.setattr(
        health_endpoint.tarot_interpretation_service,
        "health_check",
        fake_health_check,
    )

    resp = client.get("/api/v1/health/status")
    assert resp.status_code == 503
    ai_component = resp.json()["components"]["ai_service"]
    assert ai_component["status"] == "unhealthy"
    assert ai_component["message"] == "AI service unavailable"


def test_status_and_metrics_require_admin_in_production_like_environment(client, monkeypatch):
    monkeypatch.setattr(settings, "ENVIRONMENT", "production")

    status_resp = client.get("/api/v1/health/status")
    metrics_resp = client.get("/api/v1/health/metrics")

    assert status_resp.status_code == 403
    assert metrics_resp.status_code == 403


def test_coze_health_check_light_probe_skips_live_call(monkeypatch):
    service = CozeService()
    service.api_key = "test-key"
    called = {"count": 0}

    async def fake_send_messages_and_wait(**kwargs):  # noqa: ANN003
        del kwargs
        called["count"] += 1
        return {"text": "pong", "model_used": service.chat_model, "fallback_used": False}

    monkeypatch.setattr(service, "send_messages_and_wait", fake_send_messages_and_wait)
    result = asyncio.run(service.health_check(deep=False))

    assert result["status"] == "healthy"
    assert result["is_healthy"] is True
    assert result["details"]["probe"] == "config_only"
    assert called["count"] == 0


def test_coze_health_check_deep_probe_calls_provider(monkeypatch):
    service = CozeService()
    service.api_key = "test-key"
    called = {"count": 0}

    async def fake_send_messages_and_wait(**kwargs):  # noqa: ANN003
        del kwargs
        called["count"] += 1
        return {"text": "pong", "model_used": service.chat_model, "fallback_used": False}

    monkeypatch.setattr(service, "send_messages_and_wait", fake_send_messages_and_wait)
    result = asyncio.run(service.health_check(deep=True))

    assert result["status"] == "healthy"
    assert result["is_healthy"] is True
    assert result["details"]["sample"] == "pong"
    assert result["details"]["model_used"] == service.chat_model
    assert called["count"] == 1


def test_startup_handler_skips_mutations_when_flags_disabled(monkeypatch):
    called = {"tables": 0, "bootstrap": 0}

    def fake_create_tables():
        called["tables"] += 1

    def fake_ensure_reference_data(db):
        del db
        called["bootstrap"] += 1
        return {
            "cards_after": 0,
            "spreads_after": 0,
            "cards_imported": 0,
            "spreads_imported": 0,
            "questions_repaired": 0,
        }

    def fake_session_local():
        raise AssertionError("SessionLocal should not be called when bootstrap is disabled")

    monkeypatch.setattr(settings, "AUTO_CREATE_TABLES_ON_STARTUP", False)
    monkeypatch.setattr(settings, "AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP", False)
    monkeypatch.setattr(main_module, "create_tables", fake_create_tables)
    monkeypatch.setattr(main_module, "ensure_reference_data", fake_ensure_reference_data)
    monkeypatch.setattr(main_module, "SessionLocal", fake_session_local)

    handler = main_module.create_start_app_handler(FastAPI())
    asyncio.run(handler())

    assert called["tables"] == 0
    assert called["bootstrap"] == 0


def test_startup_handler_runs_mutations_when_flags_enabled(monkeypatch):
    called = {"tables": 0, "bootstrap": 0, "session": 0}

    class _DummyDb:
        pass

    class _DummySessionContext:
        def __enter__(self):
            called["session"] += 1
            return _DummyDb()

        def __exit__(self, exc_type, exc, tb):
            del exc_type, exc, tb
            return False

    def fake_create_tables():
        called["tables"] += 1

    def fake_ensure_reference_data(db):
        assert isinstance(db, _DummyDb)
        called["bootstrap"] += 1
        return {
            "cards_after": 1,
            "spreads_after": 1,
            "cards_imported": 1,
            "spreads_imported": 1,
            "questions_repaired": 0,
        }

    def fake_session_local():
        return _DummySessionContext()

    monkeypatch.setattr(settings, "AUTO_CREATE_TABLES_ON_STARTUP", True)
    monkeypatch.setattr(settings, "AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP", True)
    monkeypatch.setattr(main_module, "create_tables", fake_create_tables)
    monkeypatch.setattr(main_module, "ensure_reference_data", fake_ensure_reference_data)
    monkeypatch.setattr(main_module, "SessionLocal", fake_session_local)

    handler = main_module.create_start_app_handler(FastAPI())
    asyncio.run(handler())

    assert called["tables"] == 1
    assert called["bootstrap"] == 1
    assert called["session"] == 1
