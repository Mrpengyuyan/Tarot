from datetime import datetime, timedelta, timezone

import pytest
from jose import jwt
from sqlalchemy import select

from app.api.v1.endpoints import health
from app.core.config import settings
from app.core.security import verify_token
from app.models.user import User


def _sign_in(client, username="session_owner", password="password123"):
    registered = client.post("/api/v1/register", json={
        "username": username, "email": f"{username}@example.com", "password": password,
    })
    assert registered.status_code == 200
    response = client.post("/api/v1/login", data={"username": username, "password": password})
    assert response.status_code == 200
    return {
        "id": registered.json()["id"],
        "access": response.json()["access_token"],
        "refresh": client.cookies.get(settings.REFRESH_COOKIE_NAME),
        "csrf": client.cookies.get(settings.CSRF_COOKIE_NAME),
    }


def _headers(session, cookie=False):
    if cookie:
        return {"Cookie": f"{settings.AUTH_COOKIE_NAME}={session['access']}"}
    return {"Authorization": f"Bearer {session['access']}"}


def _refresh_headers(session, token=None):
    return {
        "Cookie": (f"{settings.REFRESH_COOKIE_NAME}={token or session['refresh']}; "
                   f"{settings.CSRF_COOKIE_NAME}={session['csrf']}"),
        settings.CSRF_HEADER_NAME: session["csrf"],
    }


def _record(client, session, spread_id):
    response = client.post("/api/v1/records/", headers=_headers(session), json={
        "question": "Private session identity test", "question_type": "general", "spread_type_id": spread_id,
    })
    assert response.status_code == 200
    return response.json()["id"]


@pytest.fixture()
def signed_in(client):
    return _sign_in(client)


@pytest.fixture()
def reused_username(client, seeded_spread_and_cards):
    original = _sign_in(client)
    original["record_id"] = _record(client, original, seeded_spread_and_cards["spread_id"])
    renamed = client.put("/api/v1/users/me", headers=_headers(original), json={"username": "renamed_owner"})
    assert renamed.status_code == 200
    replacement = client.post("/api/v1/register", json={
        "username": "session_owner", "email": "replacement@example.com", "password": "different123",
    })
    assert replacement.status_code == 200
    login = client.post("/api/v1/login", data={"username": "session_owner", "password": "different123"})
    assert login.status_code == 200
    new_owner = {"id": replacement.json()["id"], "access": login.json()["access_token"]}
    new_owner["record_id"] = _record(client, new_owner, seeded_spread_and_cards["spread_id"])
    assert original["id"] != new_owner["id"]
    return original, new_owner


@pytest.mark.parametrize("cookie", [False, True])
def test_username_reuse_does_not_change_access_token_owner(client, reused_username, cookie):
    original, replacement = reused_username
    headers = _headers(original, cookie)
    profile = client.get("/api/v1/users/me", headers=headers)
    assert profile.status_code == 200
    assert profile.json()["id"] == original["id"]
    assert profile.json()["username"] == "renamed_owner"
    assert client.get(f"/api/v1/records/{original['record_id']}", headers=headers).status_code == 200
    assert client.get(f"/api/v1/records/{replacement['record_id']}", headers=headers).status_code == 404
    assert client.put(f"/api/v1/records/{replacement['record_id']}", headers=headers,
                      json={"user_notes": "Must not change another account"}).status_code in {403, 404}


def test_username_reuse_does_not_change_refresh_token_owner(client, reused_username):
    original, replacement = reused_username
    refreshed = client.post("/api/v1/refresh", headers=_refresh_headers(original))
    assert refreshed.status_code == 200
    new_refresh = refreshed.cookies.get(settings.REFRESH_COOKIE_NAME)
    assert new_refresh
    assert verify_token(new_refresh, expected_type="refresh") == original["id"]
    refreshed_session = {"access": refreshed.json()["access_token"]}
    profile = client.get("/api/v1/users/me", headers=_headers(refreshed_session))
    assert profile.status_code == 200
    assert profile.json()["id"] == original["id"]
    assert profile.json()["username"] == "renamed_owner"
    assert client.get(f"/api/v1/records/{replacement['record_id']}",
                      headers=_headers(refreshed_session)).status_code == 404


@pytest.mark.parametrize("identifier", ["session_owner", "session_owner@example.com"])
def test_login_tokens_use_fixed_user_id_and_explicit_subject_type(client, signed_in, identifier):
    response = client.post("/api/v1/login", data={"username": identifier, "password": "password123"})
    assert response.status_code == 200
    for token, expected_type in ((response.json()["access_token"], "access"),
                                 (client.cookies.get(settings.REFRESH_COOKIE_NAME), "refresh")):
        claims = jwt.decode(token, settings.SECRET_KEY, algorithms=[settings.ALGORITHM])
        assert claims["sub"] == str(signed_in["id"])
        assert claims["sub_type"] == "user_id"
        assert claims["type"] == expected_type
        assert verify_token(token, expected_type=expected_type) == signed_in["id"]


def test_guest_access_and_refresh_tokens_use_fixed_user_id(client):
    response = client.post("/api/v1/guest-session")
    assert response.status_code == 200
    profile = client.get("/api/v1/users/me")
    assert profile.status_code == 200
    user_id = profile.json()["id"]
    assert verify_token(response.json()["access_token"]) == user_id
    assert verify_token(client.cookies.get(settings.REFRESH_COOKIE_NAME), expected_type="refresh") == user_id


@pytest.mark.parametrize("username", ["session_owner", "001"])
@pytest.mark.parametrize("token_type", ["access", "refresh"])
def test_legacy_username_tokens_are_rejected_even_for_numeric_names(client, username, token_type):
    session = _sign_in(client, username)
    legacy = jwt.encode({
        "sub": username, "type": token_type,
        "exp": datetime.now(timezone.utc) + timedelta(minutes=5),
    }, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    if token_type == "access":
        response = client.get("/api/v1/users/me", headers={"Authorization": f"Bearer {legacy}"})
    else:
        response = client.post("/api/v1/refresh", headers=_refresh_headers(session, legacy))
    assert response.status_code == 401
    assert "set-cookie" not in response.headers


def test_legacy_numeric_subject_is_not_reinterpreted_as_user_id(client, signed_in):
    legacy = jwt.encode({
        "sub": str(signed_in["id"]), "type": "access",
        "exp": datetime.now(timezone.utc) + timedelta(minutes=5),
    }, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    assert verify_token(legacy) is None
    assert client.get("/api/v1/users/me", headers={"Authorization": f"Bearer {legacy}"}).status_code == 401


@pytest.mark.parametrize("change", [
    {"sub": None}, {"sub": 1}, {"sub": True}, {"sub": ""}, {"sub": "0"}, {"sub": "-1"},
    {"sub": "01"}, {"sub": "1.0"}, {"sub": "alice"}, {"sub": "9" * 5000},
    {"sub": str(2**63)}, {"sub_type": None}, {"sub_type": "username"},
    {"type": None}, {"type": "refresh"}, {"exp": None}, {"exp": "invalid"}, {"exp": 0},
])
def test_malformed_signed_identity_claims_fail_closed(change):
    claims = {
        "sub": "1", "sub_type": "user_id", "type": "access",
        "exp": datetime.now(timezone.utc) + timedelta(minutes=5),
        **change,
    }
    token = jwt.encode(claims, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    assert verify_token(token) is None


@pytest.mark.parametrize("field", ["sub", "sub_type", "type", "exp"])
def test_identity_token_requires_all_security_claims(field):
    claims = {
        "sub": "1", "sub_type": "user_id", "type": "access",
        "exp": datetime.now(timezone.utc) + timedelta(minutes=5),
    }
    del claims[field]
    token = jwt.encode(claims, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    assert verify_token(token) is None


def test_access_and_refresh_tokens_are_not_interchangeable(client, signed_in):
    response = client.get("/api/v1/users/me", headers={"Authorization": f"Bearer {signed_in['refresh']}"})
    assert response.status_code == 401
    assert client.post("/api/v1/refresh", headers=_refresh_headers(signed_in, signed_in["access"])).status_code == 401


def test_disabled_account_cannot_log_in_or_refresh(client, signed_in, db_session):
    user = db_session.get(User, signed_in["id"])
    user.is_active = False
    db_session.commit()
    login = client.post("/api/v1/login", data={"username": "session_owner", "password": "password123"})
    assert login.status_code == 400
    assert "set-cookie" not in login.headers
    refresh = client.post("/api/v1/refresh", headers=_refresh_headers(signed_in))
    assert refresh.status_code == 400
    assert "set-cookie" not in refresh.headers


@pytest.mark.parametrize("path,expected", [
    ("/api/v1/users/me", 400), ("/api/v1/health/ai/deep", 400),
    ("/api/v1/health/status", 403), ("/api/v1/health/metrics", 403),
])
@pytest.mark.parametrize("cookie", [False, True])
def test_disabled_admin_is_blocked_before_health_probe(
    client, signed_in, db_session, monkeypatch, path, expected, cookie,
):
    user = db_session.get(User, signed_in["id"])
    user.is_superuser = True
    user.is_active = False
    db_session.commit()
    monkeypatch.setattr(settings, "HEALTH_ADMIN_ONLY", True)
    calls = []

    async def unexpected_probe(**kwargs):
        calls.append(kwargs)
        return {"status": "healthy", "is_healthy": True}

    monkeypatch.setattr(health.tarot_interpretation_service, "health_check", unexpected_probe)
    assert client.get(path, headers=_headers(signed_in, cookie)).status_code == expected
    assert calls == []


def test_admin_role_is_loaded_from_database_for_each_request(client, signed_in, db_session, monkeypatch):
    user = db_session.get(User, signed_in["id"])
    user.is_superuser = True
    db_session.commit()
    calls = []

    async def fake_probe(*, deep=False):
        calls.append(deep)
        return {"status": "healthy", "is_healthy": True}

    monkeypatch.setattr(health.tarot_interpretation_service, "health_check", fake_probe)
    assert client.get("/api/v1/health/ai/deep", headers=_headers(signed_in)).status_code == 200
    user.is_superuser = False
    db_session.commit()
    assert client.get("/api/v1/health/ai/deep", headers=_headers(signed_in)).status_code == 403
    assert calls == [True]


def test_deleted_account_tokens_cannot_authenticate(client, signed_in, db_session):
    db_session.delete(db_session.get(User, signed_in["id"]))
    db_session.commit()
    assert db_session.scalars(select(User)).all() == []
    assert client.get("/api/v1/users/me", headers=_headers(signed_in)).status_code == 401
    assert client.post("/api/v1/refresh", headers=_refresh_headers(signed_in)).status_code == 401
