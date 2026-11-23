from __future__ import annotations

import json
from types import SimpleNamespace

import pytest
from fastapi.testclient import TestClient

import check_config
from app.core.config import Settings, settings
from app.db.session import get_db, get_session_factory
from app.main import app
from app.services.client_ip import resolve_client_host


def _fake_request(host: str | None, x_forwarded_for: str | None = None):
    headers = {}
    if x_forwarded_for is not None:
        headers["x-forwarded-for"] = x_forwarded_for
    client = SimpleNamespace(host=host) if host is not None else None
    return SimpleNamespace(client=client, headers=headers)


def test_default_untrusted_proxy_ignores_spoofed_forwarded_for():
    request = _fake_request("198.51.100.7", "203.0.113.9")
    assert resolve_client_host(request, trusted_proxy_ips="") == "198.51.100.7"


def test_untrusted_direct_peer_cannot_spoof_even_when_trusted_proxies_configured():
    request = _fake_request("198.51.100.7", "203.0.113.9")
    assert resolve_client_host(request, trusted_proxy_ips="127.0.0.1,10.0.0.0/8") == "198.51.100.7"


def test_trusted_proxy_single_ip_extracts_client_ip():
    request = _fake_request("127.0.0.1", "203.0.113.42")
    assert resolve_client_host(request, trusted_proxy_ips="127.0.0.1") == "203.0.113.42"


def test_trusted_proxy_cidr_walks_right_to_left_and_ignores_spoofed_prefix():
    # Attacker prepended 198.51.100.99; real client is 203.0.113.42; ingress proxy is 10.2.3.4; peer is 10.0.0.5
    request = _fake_request("10.0.0.5", "198.51.100.99, 203.0.113.42, 10.2.3.4")
    assert resolve_client_host(request, trusted_proxy_ips="10.0.0.0/8") == "203.0.113.42"


def test_trusted_proxy_ipv6_and_malformed_header_fallback():
    ipv6_req = _fake_request("::1", "2001:db8::15")
    assert resolve_client_host(ipv6_req, trusted_proxy_ips="::1") == "2001:db8::15"

    malformed_req = _fake_request("127.0.0.1", "not-an-ip, 203.0.113.10")
    assert resolve_client_host(malformed_req, trusted_proxy_ips="127.0.0.1") == "203.0.113.10"

    poisoned_tail = _fake_request("127.0.0.1", "203.0.113.10, garbage-hop")
    assert resolve_client_host(poisoned_tail, trusted_proxy_ips="127.0.0.1") == "127.0.0.1"


def test_missing_client_returns_unknown():
    assert resolve_client_host(_fake_request(None, "203.0.113.1"), trusted_proxy_ips="127.0.0.1") == "unknown"


def test_invalid_trusted_proxy_cidr_fails_runtime_validation():
    cfg = Settings(
        SECRET_KEY="s" * 40,
        TRUSTED_PROXY_IPS="not-a-valid-cidr",
        _env_file=None,
    )
    with pytest.raises(ValueError):
        cfg.validate_runtime()


def test_guest_session_rate_limit_isolates_clients_behind_trusted_proxy(monkeypatch, db_session_factory):
    monkeypatch.setattr("app.main.create_tables", lambda: None)
    monkeypatch.setattr("app.main.ensure_reference_data", lambda db: {})
    monkeypatch.setattr(settings, "GUEST_SESSIONS_PER_HOUR", 1)
    monkeypatch.setattr(settings, "TRUSTED_PROXY_IPS", "127.0.0.1")

    def override_get_db():
        db = db_session_factory()
        try:
            yield db
        finally:
            db.close()

    async def proxied_asgi(scope, receive, send):
        if scope["type"] == "http":
            scope = {**scope, "client": ("127.0.0.1", 50000)}
        await app(scope, receive, send)

    app.dependency_overrides[get_db] = override_get_db
    app.dependency_overrides[get_session_factory] = lambda: db_session_factory
    try:
        with TestClient(proxied_asgi) as proxied_client:
            first_a = proxied_client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "203.0.113.10"})
            assert first_a.status_code == 200

            second_a = proxied_client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "203.0.113.10"})
            assert second_a.status_code == 429

            # Different real player behind the same trusted reverse proxy gets an independent bucket.
            first_b = proxied_client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "203.0.113.11"})
            assert first_b.status_code == 200

            # Without trusting the proxy, spoofing X-Forwarded-For does not bypass rate limiting.
            monkeypatch.setattr(settings, "TRUSTED_PROXY_IPS", "")
            direct_1 = proxied_client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "203.0.113.90"})
            assert direct_1.status_code == 200
            direct_2 = proxied_client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "203.0.113.91"})
            assert direct_2.status_code == 429
    finally:
        app.dependency_overrides.clear()


def test_production_check_flags_dev_defaults_and_passes_valid_production_config(tmp_path):
    dev_desktop = tmp_path / "dev_desktop.json"
    dev_desktop.write_text(json.dumps({"backendBaseUrl": "http://localhost:8000/api/v1"}), encoding="utf-8")

    dev_cfg = Settings(
        SECRET_KEY="dev-local-secret-key-change-me-before-production",
        AUTH_COOKIE_SECURE=False,
        DEBUG=True,
        ALLOW_MOCK_AI_FALLBACK=True,
        CORS_ORIGINS="http://localhost:3000,*",
        DEEPSEEK_API_KEY=None,
        _env_file=None,
    )
    issues = check_config.collect_production_issues(dev_cfg, desktop_config_path=dev_desktop)
    assert any("SECRET_KEY" in item for item in issues)
    assert any("AUTH_COOKIE_SECURE" in item for item in issues)
    assert any("DEBUG" in item for item in issues)
    assert any("ALLOW_MOCK_AI_FALLBACK" in item for item in issues)
    assert any("CORS_ORIGINS" in item for item in issues)
    assert any("DEEPSEEK_API_KEY" in item for item in issues)
    assert any("HTTPS" in item for item in issues)

    prod_desktop = tmp_path / "prod_desktop.json"
    prod_desktop.write_text(json.dumps({"backendBaseUrl": "https://api.tarot.example.com/api/v1"}), encoding="utf-8")

    prod_cfg = Settings(
        SECRET_KEY="a" * 48,
        ENVIRONMENT="production",
        REQUIRE_STRONG_SECRET=True,
        AUTH_COOKIE_SECURE=True,
        DEBUG=False,
        ALLOW_MOCK_AI_FALLBACK=False,
        AI_BUDGET_GUARD_ENABLED=True,
        CORS_ORIGINS="https://tarot.example.com",
        DEEPSEEK_API_KEY="sk-live-test-placeholder",
        TRUSTED_PROXY_IPS="127.0.0.1,10.0.0.0/8",
        _env_file=None,
    )
    assert check_config.collect_production_issues(prod_cfg, desktop_config_path=prod_desktop) == []
