import pytest
from sqlalchemy import select

from app.core.security import get_password_hash, verify_token
from app.models.user import User


def _register(client, username, email="new-player@example.com", password="password123"):
    return client.post("/api/v1/register", json={"username": username, "email": email, "password": password})


def _snapshot(db):
    db.expire_all()
    return db.execute(select(User.id, User.username, User.email, User.hashed_password).order_by(User.id)).all()


@pytest.fixture()
def account(client):
    registered = _register(client, "original_player", "original@example.com")
    assert registered.status_code == 200
    login = client.post("/api/v1/login", data={"username": "original_player", "password": "password123"})
    assert login.status_code == 200
    return {"id": registered.json()["id"], "headers": {"Authorization": f"Bearer {login.json()['access_token']}"}}


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("raw,expected", [
    (" \t Alice \n", "Alice"), ("  aBc \u73a9\u5bb6  ", "aBc \u73a9\u5bb6"),
    (" " + "x" * 50 + " ", "x" * 50), (" inner space ", "inner space"),
    ("\u3000player\u3000", "player"),
], ids=["ascii", "unicode", "max-length", "internal-space", "wide-padding"])
def test_username_is_trimmed_before_length_validation_and_remains_loginable(
    client, db_session, account, mode, raw, expected,
):
    response = (_register(client, raw) if mode == "register"
                else client.put("/api/v1/users/me", headers=account["headers"], json={"username": raw}))
    assert response.status_code == 200
    user_id = response.json()["id"]
    assert response.json()["username"] == expected
    db_session.expire_all()
    assert db_session.get(User, user_id).username == expected
    login = client.post("/api/v1/login", data={"username": raw, "password": "password123"})
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == user_id


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("raw", ["   ", " \t\n ", " aa ", " \u3000\u3000", " " + "x" * 51 + " "],
                         ids=["spaces", "tabs-newlines", "too-short", "wide-spaces", "too-long"])
def test_invalid_trimmed_username_is_rejected_without_writing(client, db_session, account, mode, raw):
    before = _snapshot(db_session)
    response = (_register(client, raw) if mode == "register"
                else client.put("/api/v1/users/me", headers=account["headers"], json={"username": raw}))
    assert response.status_code == 422
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("mode", ["register", "update"])
def test_padding_cannot_bypass_duplicate_username_check(client, db_session, account, mode):
    if mode == "register":
        target = "original_player"
    else:
        target = "another_player"
        assert _register(client, target).status_code == 200
    before = _snapshot(db_session)
    response = (_register(client, f" {target} ") if mode == "register"
                else client.put("/api/v1/users/me", headers=account["headers"], json={"username": f" {target} "}))
    assert response.status_code == 400
    assert _snapshot(db_session) == before


def _email(length):
    return "A" * 64 + "@" + "b" * (length - 69) + ".com"


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("padded", [False, True])
def test_email_at_database_length_limit_is_accepted(client, account, mode, padded):
    email = _email(100)
    supplied = f"  {email}\t " if padded else email
    response = (_register(client, "email_boundary", supplied) if mode == "register"
                else client.put("/api/v1/users/me", headers=account["headers"], json={"email": supplied}))
    assert response.status_code == 200
    assert response.json()["email"] == email
    login = client.post("/api/v1/login", data={"username": email, "password": "password123"})
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == response.json()["id"]


@pytest.mark.parametrize("mode", ["register", "update"])
@pytest.mark.parametrize("length", [101, 119])
def test_email_over_database_length_limit_is_rejected_without_writing(
    client, db_session, account, mode, length,
):
    before = _snapshot(db_session)
    email = _email(length)
    response = (_register(client, "email_boundary", email) if mode == "register"
                else client.put("/api/v1/users/me", headers=account["headers"], json={"email": email}))
    assert response.status_code == 422
    assert _snapshot(db_session) == before


def test_legacy_account_values_remain_readable_and_are_not_automatically_normalized(client, db_session):
    user = User(username="  legacy_player  ", email=_email(119), hashed_password=get_password_hash("password123"))
    db_session.add(user)
    db_session.commit()
    before = _snapshot(db_session)
    login = client.post("/api/v1/login", data={"username": user.email, "password": "password123"})
    assert login.status_code == 200
    headers = {"Authorization": f"Bearer {login.json()['access_token']}"}
    profile = client.get("/api/v1/users/me", headers=headers)
    assert profile.status_code == 200
    assert profile.json()["username"] == "  legacy_player  "
    assert profile.json()["email"] == _email(119)
    response = client.put("/api/v1/users/me", headers=headers, json={})
    assert response.status_code == 200
    assert response.json() == profile.json()
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("mode", ["register", "update"])
def test_password_whitespace_is_preserved(client, account, mode):
    password = "  password123  "
    if mode == "register":
        response = _register(client, "password_keeper", password=password)
    else:
        response = client.put("/api/v1/users/me", headers=account["headers"], json={"password": password})
    assert response.status_code == 200
    username = response.json()["username"]
    login = client.post("/api/v1/login", data={"username": username, "password": password})
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == response.json()["id"]
    assert client.post("/api/v1/login", data={"username": username, "password": password.strip()}).status_code == 401
