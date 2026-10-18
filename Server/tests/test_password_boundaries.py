import pytest
from sqlalchemy import select

from app.core import security
from app.core.config import settings
from app.core.security import create_access_token, get_password_hash, verify_token
from app.models.user import User


def _register(client, password):
    return client.post("/api/v1/register", json={
        "username": "password_boundary", "email": "password-boundary@example.com", "password": password,
    })


def _snapshot(db):
    db.expire_all()
    return db.execute(select(User.id, User.username, User.email, User.hashed_password).order_by(User.id)).all()


@pytest.fixture()
def account(client):
    registered = client.post("/api/v1/register", json={
        "username": "password_owner", "email": "password-owner@example.com", "password": "original123",
    })
    assert registered.status_code == 200
    login = client.post("/api/v1/login", data={"username": "password_owner", "password": "original123"})
    assert login.status_code == 200
    return {
        "id": registered.json()["id"],
        "headers": {"Authorization": f"Bearer {login.json()['access_token']}"},
        "refresh": client.cookies.get(settings.REFRESH_COOKIE_NAME),
        "csrf": client.cookies.get(settings.CSRF_COOKIE_NAME),
    }


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("password", [
    "a" * 8, "a" * 71, "a" * 72, "\u5bc6" * 24, "\U0001f31f" * 18,
    "a" * 69 + "\u5bc6", " " + "a" * 70 + " ",
], ids=["minimum", "ascii-71", "ascii-72", "chinese-72", "emoji-72", "mixed-72", "spaces-72"])
def test_password_within_byte_limit_is_preserved_and_loginable(client, account, mode, password):
    assert len(password.encode("utf-8")) <= 72
    response = (_register(client, password) if mode == "register" else
                client.put("/api/v1/users/me", headers=account["headers"], json={"password": password}))
    assert response.status_code == 200
    username = response.json()["username"]
    login = client.post("/api/v1/login", data={"username": username, "password": password})
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == response.json()["id"]
    different = password.strip() if password != password.strip() else "!" + password[1:]
    assert client.post("/api/v1/login", data={"username": username, "password": different}).status_code == 401
    if mode == "update":
        assert client.get("/api/v1/users/me", headers=account["headers"]).status_code == 200
        refreshed = client.post("/api/v1/refresh", headers={
            "Cookie": (f"{settings.REFRESH_COOKIE_NAME}={account['refresh']}; "
                       f"{settings.CSRF_COOKIE_NAME}={account['csrf']}"),
            settings.CSRF_HEADER_NAME: account["csrf"],
        })
        assert refreshed.status_code == 200
        assert verify_token(refreshed.json()["access_token"]) == account["id"]


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("password", [
    "a" * 73, "a" * 128, "\u5bc6" * 25, "\U0001f31f" * 19, "a" * 70 + "\u5bc6",
    "a" * 72 + "different-suffix",
], ids=["ascii-73", "old-character-limit", "chinese-75", "emoji-76", "mixed-73", "ignored-suffix"])
def test_password_over_byte_limit_is_rejected_without_writing(client, db_session, account, mode, password):
    assert len(password.encode("utf-8")) > 72
    before = _snapshot(db_session)
    response = (_register(client, password) if mode == "register" else
                client.put("/api/v1/users/me", headers=account["headers"], json={"password": password}))
    assert response.status_code == 422
    assert any(error["loc"] == ["body", "password"] for error in response.json()["detail"])
    assert "set-cookie" not in response.headers
    assert _snapshot(db_session) == before
    login = client.post("/api/v1/login", data={"username": "password_owner", "password": "original123"})
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == account["id"]


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("password", ["a" * 7, "\U0001f31f" * 7], ids=["ascii", "unicode"])
def test_minimum_password_length_still_counts_characters(client, db_session, account, mode, password):
    before = _snapshot(db_session)
    response = (_register(client, password) if mode == "register" else
                client.put("/api/v1/users/me", headers=account["headers"], json={"password": password}))
    assert response.status_code == 422
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("password", ["a" * 128, "\u5bc6" * 32], ids=["ascii", "unicode"])
def test_legacy_long_password_is_rejected_without_rewriting_account(client, db_session, password):
    # Seed the historical truncating format without using the protected hash helper.
    legacy = User(username="legacy_password", email="legacy-password@example.com",
                  hashed_password=security.pwd_context.hash(password))
    db_session.add(legacy)
    db_session.commit()
    before = _snapshot(db_session)
    login = client.post("/api/v1/login", data={"username": legacy.email, "password": password})
    assert login.status_code == 401
    assert login.json() == {"detail": "Incorrect username or password"}
    assert "set-cookie" not in login.headers
    token = create_access_token({"sub": str(legacy.id), "sub_type": "user_id"})
    headers = {"Authorization": f"Bearer {token}"}
    assert client.put("/api/v1/users/me", headers=headers, json={}).status_code == 200
    assert client.get("/api/v1/users/me", headers=headers).status_code == 200
    assert _snapshot(db_session) == before


def test_omitted_password_and_rejected_null_preserve_existing_hash(client, db_session, account):
    before = _snapshot(db_session)
    assert client.put("/api/v1/users/me", headers=account["headers"], json={}).status_code == 200
    assert client.put("/api/v1/users/me", headers=account["headers"], json={"password": None}).status_code == 422
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("password", [
    "a" * 73, "a" * 128, "\u5bc6" * 25, "\U0001f31f" * 19, "a" * 70 + "\u5bc6",
], ids=["ascii-73", "old-character-limit", "chinese-75", "emoji-76", "mixed-73"])
def test_hash_helper_rejects_overlong_password_instead_of_truncating(password):
    with pytest.raises(ValueError, match="72 UTF-8 bytes"):
        get_password_hash(password)


@pytest.mark.parametrize("prefix", ["a" * 72, "\u5bc6" * 24, "\U0001f31f" * 18],
                         ids=["ascii", "chinese", "emoji"])
def test_verify_helper_rejects_suffix_beyond_bcrypt_boundary(prefix):
    stored = get_password_hash(prefix)
    assert security.verify_password(prefix, stored) is True
    assert security.verify_password(prefix + "different", stored) is False


@pytest.mark.parametrize("identifier", ["username", "email"])
@pytest.mark.parametrize("password", ["a" * 72, "\u5bc6" * 24, "\U0001f31f" * 18],
                         ids=["ascii", "chinese", "emoji"])
def test_login_cannot_accept_appended_suffix_to_valid_boundary_password(
    client, db_session, identifier, password,
):
    registered = _register(client, password)
    assert registered.status_code == 200
    owner = registered.json()
    before = _snapshot(db_session)
    invalid = client.post("/api/v1/login", data={
        "username": owner[identifier], "password": password + "different",
    })
    assert invalid.status_code == 401
    assert invalid.json() == {"detail": "Incorrect username or password"}
    assert invalid.headers["www-authenticate"] == "Bearer"
    assert "set-cookie" not in invalid.headers
    assert _snapshot(db_session) == before
    valid = client.post("/api/v1/login", data={"username": owner[identifier], "password": password})
    assert valid.status_code == 200
    assert verify_token(valid.json()["access_token"]) == owner["id"]
