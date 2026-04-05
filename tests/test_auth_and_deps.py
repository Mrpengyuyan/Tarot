from __future__ import annotations

from fastapi.security import HTTPAuthorizationCredentials

from app.api.deps import _extract_token
from app.core.config import settings


class _CookieRequest:
    def __init__(self, cookies: dict[str, str]) -> None:
        self.cookies = cookies


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
    csrf_token = login_resp.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(settings.CSRF_COOKIE_NAME)
    if csrf_token:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})
    return login_resp


def test_auth_cookie_refresh_logout_flow(client):
    login_resp = _register_and_login(client, username="cookie_user")
    assert "set-cookie" in login_resp.headers
    assert login_resp.json()["token_type"] == "bearer"
    assert login_resp.json().get("access_token")

    me_resp = client.get("/api/v1/users/me")
    assert me_resp.status_code == 200
    assert me_resp.json()["username"] == "cookie_user"

    refresh_resp = client.post("/api/v1/refresh")
    assert refresh_resp.status_code == 200
    assert refresh_resp.json()["token_type"] == "bearer"
    assert refresh_resp.json().get("access_token")

    logout_resp = client.post("/api/v1/logout")
    assert logout_resp.status_code == 200

    unauthenticated_resp = client.get("/api/v1/users/me")
    assert unauthenticated_resp.status_code == 401


def test_extract_token_precedence_and_cookie_formats():
    credentials = HTTPAuthorizationCredentials(
        scheme="Bearer",
        credentials="header-token",
    )
    request = _CookieRequest(cookies={settings.AUTH_COOKIE_NAME: "cookie-token"})
    assert _extract_token(credentials, request) == "header-token"

    bearer_cookie_request = _CookieRequest(cookies={settings.AUTH_COOKIE_NAME: "Bearer cookie-token"})
    assert _extract_token(None, bearer_cookie_request) == "cookie-token"

    raw_cookie_request = _CookieRequest(cookies={settings.AUTH_COOKIE_NAME: "raw-cookie-token"})
    assert _extract_token(None, raw_cookie_request) == "raw-cookie-token"

    empty_cookie_request = _CookieRequest(cookies={})
    assert _extract_token(None, empty_cookie_request) is None


def test_cookie_auth_requires_csrf_for_unsafe_methods(client):
    _register_and_login(client, username="csrf_user")
    client.headers.pop(settings.CSRF_HEADER_NAME, None)

    blocked_resp = client.post("/api/v1/refresh")
    assert blocked_resp.status_code == 403
    assert "csrf" in blocked_resp.json()["detail"].lower()

    csrf_token = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_token
    client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})

    ok_resp = client.post("/api/v1/refresh")
    assert ok_resp.status_code == 200


def test_refresh_keeps_existing_csrf_cookie_value(client):
    _register_and_login(client, username="csrf_refresh_user")
    csrf_before = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_before

    refresh_resp = client.post("/api/v1/refresh")
    assert refresh_resp.status_code == 200

    csrf_after = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_after == csrf_before


def test_cookie_auth_blocks_disallowed_origin_for_unsafe_methods(client):
    _register_and_login(client, username="csrf_origin_user")
    csrf_token = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_token

    blocked_resp = client.post(
        "/api/v1/refresh",
        headers={
            settings.CSRF_HEADER_NAME: csrf_token,
            "Origin": "http://evil.example.com",
        },
    )
    assert blocked_resp.status_code == 403
    assert "origin" in blocked_resp.json()["detail"].lower()
