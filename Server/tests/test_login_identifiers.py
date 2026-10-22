import pytest
from sqlalchemy import select

from app.core.config import settings
from app.core.security import create_access_token, get_password_hash, verify_token
from app.db.base_class import Base
from app.models.user import User


OWNER_PASSWORD = "owner-password123"
OTHER_PASSWORD = "other-password123"


@pytest.fixture(scope="module")
def password_hashes():
    return {password: get_password_hash(password) for password in (OWNER_PASSWORD, OTHER_PASSWORD)}


@pytest.fixture()
def accounts(db_session, password_hashes):
    def create(username, email, password=OWNER_PASSWORD, **kwargs):
        user = User(username=username, email=email, hashed_password=password_hashes[password], **kwargs)
        db_session.add(user)
        db_session.commit()
        return user
    return create


def _headers(user):
    return {"Authorization": "Bearer " + create_access_token({"sub": str(user.id), "sub_type": "user_id"})}


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table).order_by(*table.primary_key)).all()
            for table in Base.metadata.sorted_tables}


def _login(client, identifier, password=OWNER_PASSWORD):
    return client.post("/api/v1/login", data={"username": identifier, "password": password})


@pytest.mark.parametrize("mode", ["register", "rename"])
@pytest.mark.parametrize("username", [
    "owner@example.com", "user@tag", "@player", "player@", "  owner@example.com\t ",
    "\u73a9\u5bb6@\u6807\u7b7e",
])
def test_at_sign_in_new_username_rejects_entire_write(client, db_session, accounts, mode, username):
    owner = accounts("original_player", "owner@example.com")
    payload = {"username": username, "email": "new@example.com", "password": OTHER_PASSWORD}
    before = _snapshot(db_session)
    response = (client.post("/api/v1/register", json=payload) if mode == "register"
                else client.put("/api/v1/users/me", headers=_headers(owner), json=payload))
    assert response.status_code == 422
    assert _snapshot(db_session) == before
    assert "set-cookie" not in response.headers
    login = _login(client, "owner@example.com")
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == owner.id


@pytest.mark.parametrize("shadow_password", [OWNER_PASSWORD, OTHER_PASSWORD], ids=["same-password", "different-password"])
@pytest.mark.parametrize("identifier", ["owner@example.com", " OWNER@EXAMPLE.COM ", "\towner@example.com\n"])
def test_email_login_resolves_email_owner_not_legacy_username(
    client, db_session, accounts, shadow_password, identifier,
):
    owner = accounts("original_player", "owner@example.com")
    shadow = accounts("owner@example.com", "shadow@example.com", shadow_password)
    before = _snapshot(db_session)
    response = _login(client, identifier)
    assert response.status_code == 200
    assert verify_token(response.json()["access_token"]) == owner.id != shadow.id
    assert verify_token(client.cookies.get(settings.REFRESH_COOKIE_NAME), expected_type="refresh") == owner.id
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("active", [False, True], ids=["disabled-owner", "active-owner"])
def test_email_lookup_never_falls_back_to_another_accounts_password(client, accounts, active):
    accounts("original_player", "owner@example.com", is_active=active)
    accounts("owner@example.com", "shadow@example.com", OTHER_PASSWORD)
    response = _login(client, "owner@example.com", OTHER_PASSWORD)
    assert response.status_code == 401
    assert "set-cookie" not in response.headers
    assert _login(client, "owner@example.com").status_code == (200 if active else 400)


def test_legacy_at_username_uses_email_or_existing_session_to_rename(
    client, db_session, accounts, seeded_spread_and_cards,
):
    legacy = accounts("legacy@nickname", "legacy@example.com")
    headers = _headers(legacy)
    before = _snapshot(db_session)
    rejected = _login(client, legacy.username)
    assert rejected.status_code == 401 and "set-cookie" not in rejected.headers
    assert _snapshot(db_session) == before
    assert client.get("/api/v1/users/me", headers=headers).json()["username"] == legacy.username
    assert client.put("/api/v1/users/me", headers=headers, json={}).status_code == 200
    rejected_update = client.put("/api/v1/users/me", headers=headers, json={
        "username": legacy.username, "email": "must-not-save@example.com", "password": OTHER_PASSWORD,
    })
    assert rejected_update.status_code == 422
    assert _snapshot(db_session) == before
    login = _login(client, legacy.email)
    assert login.status_code == 200
    assert verify_token(login.json()["access_token"]) == legacy.id
    refreshed = client.post("/api/v1/refresh", headers={
        settings.CSRF_HEADER_NAME: client.cookies.get(settings.CSRF_COOKIE_NAME),
    })
    assert refreshed.status_code == 200
    assert verify_token(refreshed.json()["access_token"]) == legacy.id

    created = client.post("/api/v1/records/", headers=headers, json={
        "question": "Keep the original account history", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    renamed = client.put("/api/v1/users/me", headers=headers, json={"username": "renamed_player"})
    assert renamed.status_code == 200 and renamed.json()["id"] == legacy.id
    assert _login(client, "renamed_player").status_code == 200
    assert client.get("/api/v1/users/me", headers=headers).json()["username"] == "renamed_player"
    assert client.get(f"/api/v1/records/{created.json()['id']}", headers=headers).status_code == 200


def test_legacy_username_identical_to_own_email_remains_readable(client, db_session, accounts):
    user = accounts("same@example.com", "same@example.com")
    before = _snapshot(db_session)
    response = _login(client, user.email)
    assert response.status_code == 200
    assert verify_token(response.json()["access_token"]) == user.id
    assert client.get("/api/v1/users/me", headers=_headers(user)).json()["username"] == user.username
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("mode", ["register", "change-email"])
def test_registering_or_changing_email_cannot_redirect_login_to_legacy_username(client, accounts, mode):
    shadow = accounts("future@example.com", "shadow@example.com", OTHER_PASSWORD)
    if mode == "register":
        response = client.post("/api/v1/register", json={
            "username": "future_player", "email": "future@example.com", "password": OWNER_PASSWORD,
        })
    else:
        owner = accounts("future_player", "original@example.com")
        response = client.put("/api/v1/users/me", headers=_headers(owner), json={"email": "future@example.com"})
    assert response.status_code == 200
    owner_id = response.json()["id"]
    assert owner_id != shadow.id
    login = _login(client, "future@example.com")
    assert login.status_code == 200 and verify_token(login.json()["access_token"]) == owner_id
    assert _login(client, "future@example.com", OTHER_PASSWORD).status_code == 401
