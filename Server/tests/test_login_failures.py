import pytest
from sqlalchemy import select

from app.core import security
from app.models.user import User


@pytest.fixture(scope="module")
def valid_hash():
    return security.get_password_hash("valid-password123")


@pytest.fixture()
def account(client, db_session):
    response = client.post("/api/v1/register", json={
        "username": "login_guard", "email": "login-guard@example.com", "password": "valid-password123",
    })
    assert response.status_code == 200
    user = db_session.get(User, response.json()["id"])
    return {"id": user.id, "username": user.username, "email": user.email, "hash": user.hashed_password}


@pytest.mark.parametrize("password", ["x" * 5000, "password\x00suffix", "password\ud800"],
                         ids=["oversized", "null-byte", "invalid-unicode"])
def test_invalid_password_input_fails_closed(valid_hash, password):
    assert security.verify_password(password, valid_hash) is False


@pytest.mark.parametrize("stored_hash", ["", "not-a-password-hash", "$2b$12$bad", "$unsupported$hash"])
def test_invalid_stored_hash_fails_closed(stored_hash):
    assert security.verify_password("valid-password123", stored_hash) is False


@pytest.mark.parametrize("password,expected", [("valid-password123", True), ("wrong-password123", False)])
def test_valid_bcrypt_behavior_is_unchanged(valid_hash, password, expected):
    assert security.verify_password(password, valid_hash) is expected


@pytest.mark.parametrize("error_type", [RuntimeError, TypeError])
def test_unexpected_backend_failures_are_not_hidden(monkeypatch, error_type):
    def unavailable(*args, **kwargs):
        raise error_type("Authentication backend unavailable")

    monkeypatch.setattr(security.pwd_context, "verify", unavailable)
    with pytest.raises(error_type, match="Authentication backend unavailable"):
        security.verify_password("password123", "stored-hash")


@pytest.mark.parametrize("identifier", ["username", "email"])
@pytest.mark.parametrize("case", ["oversized", "null_byte", "unknown_hash", "empty_hash", "malformed_bcrypt", "unsupported_hash"])
def test_login_rejects_invalid_credentials_without_tokens_or_data_changes(
    client, db_session, account, identifier, case,
):
    password = "valid-password123"
    stored_hash = account["hash"]
    if case == "oversized":
        password = "x" * 5000
    elif case == "null_byte":
        password = "valid-password123\x00suffix"
    else:
        stored_hash = {
            "unknown_hash": "not-a-password-hash", "empty_hash": "",
            "malformed_bcrypt": "$2b$12$bad", "unsupported_hash": "$unsupported$hash",
        }[case]
        db_session.get(User, account["id"]).hashed_password = stored_hash
        db_session.commit()
    response = client.post("/api/v1/login", data={"username": account[identifier], "password": password})
    assert response.status_code == 401
    assert response.json() == {"detail": "Incorrect username or password"}
    assert response.headers["www-authenticate"] == "Bearer"
    assert "set-cookie" not in response.headers
    db_session.expire_all()
    user = db_session.scalars(select(User)).one()
    assert (user.id, user.username, user.email, user.hashed_password) == (
        account["id"], account["username"], account["email"], stored_hash,
    )
    assert user.is_active is True


@pytest.mark.parametrize("identifier", ["username", "email"])
def test_normal_login_still_issues_valid_tokens_without_rehashing(client, db_session, account, identifier):
    response = client.post("/api/v1/login", data={
        "username": account[identifier], "password": "valid-password123",
    })
    assert response.status_code == 200
    assert security.verify_token(response.json()["access_token"]) == account["id"]
    db_session.expire_all()
    assert db_session.get(User, account["id"]).hashed_password == account["hash"]
