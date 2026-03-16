from __future__ import annotations

from types import SimpleNamespace

from fastapi.security import HTTPAuthorizationCredentials

from app.api.deps import _extract_token
from app.core.config import settings


def _register_and_login(client, username: str = "tester"):
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
    return login_resp


def test_auth_cookie_refresh_logout_flow(client):
    login_resp = _register_and_login(client, username="cookie_user")
    assert "set-cookie" in login_resp.headers
    assert login_resp.json()["access_token"]

    me_resp = client.get("/api/v1/users/me")
    assert me_resp.status_code == 200
    assert me_resp.json()["username"] == "cookie_user"

    refresh_resp = client.post("/api/v1/refresh")
    assert refresh_resp.status_code == 200
    assert refresh_resp.json()["access_token"]

    logout_resp = client.post("/api/v1/logout")
    assert logout_resp.status_code == 200

    unauthenticated_resp = client.get("/api/v1/users/me")
    assert unauthenticated_resp.status_code == 401


def test_extract_token_precedence_and_cookie_formats():
    credentials = HTTPAuthorizationCredentials(
        scheme="Bearer",
        credentials="header-token",
    )
    request = SimpleNamespace(cookies={settings.AUTH_COOKIE_NAME: "cookie-token"})
    assert _extract_token(credentials, request) == "header-token"

    bearer_cookie_request = SimpleNamespace(cookies={settings.AUTH_COOKIE_NAME: "Bearer cookie-token"})
    assert _extract_token(None, bearer_cookie_request) == "cookie-token"

    raw_cookie_request = SimpleNamespace(cookies={settings.AUTH_COOKIE_NAME: "raw-cookie-token"})
    assert _extract_token(None, raw_cookie_request) == "raw-cookie-token"

    empty_cookie_request = SimpleNamespace(cookies={})
    assert _extract_token(None, empty_cookie_request) is None

