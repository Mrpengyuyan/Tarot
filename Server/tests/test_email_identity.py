from concurrent.futures import ThreadPoolExecutor
from threading import Barrier

import pytest
from sqlalchemy import select
from sqlalchemy.exc import IntegrityError

from app.core.email_identity import email_identity_key
from app.core.security import create_access_token, get_password_hash, verify_token
from app.db.base_class import Base
from app.models.user import User


PASSWORD = "email-identity-password"
EQUIVALENT_EMAILS = [
    ("\u00dcser@example.com", "\u00fcSER@EXAMPLE.COM"),
    ("\u7528\u6237@\u4f8b\u5b50.\u516c\u53f8", "\u7528\u6237@XN--FSQU00A.XN--55QX5D"),
    ("\u00e9@example.com", "e\u0301@example.com"),
    ("\u0130ser@example.com", "i\u0307ser@example.com"),
]


@pytest.fixture(scope="module")
def password_hash():
    return get_password_hash(PASSWORD)


@pytest.fixture()
def account(db_session, password_hash):
    user = User(username="email_owner", email="original@example.com", hashed_password=password_hash)
    db_session.add(user)
    db_session.commit()
    return user, {"Authorization": "Bearer " + create_access_token({"sub": str(user.id), "sub_type": "user_id"})}


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table).order_by(*table.primary_key)).all()
            for table in Base.metadata.sorted_tables}


@pytest.mark.parametrize("mode", ["register", "change-email"])
@pytest.mark.parametrize("email,identifier", EQUIVALENT_EMAILS)
def test_international_email_is_loginable_without_changing_display(client, db_session, account, mode, email, identifier):
    user, headers = account
    response = (client.post("/api/v1/register", json={
        "username": "international_player", "email": email, "password": PASSWORD, "email_key": "client-controlled",
    }) if mode == "register" else client.put("/api/v1/users/me", headers=headers,
                                            json={"email": email, "email_key": "client-controlled"}))
    assert response.status_code == 200 and response.json()["email"] == email
    user_id = response.json()["id"]
    before = _snapshot(db_session)
    for supplied in (email, identifier, f" \t{identifier}\n "):
        login = client.post("/api/v1/login", data={"username": supplied, "password": PASSWORD})
        assert login.status_code == 200
        assert verify_token(login.json()["access_token"]) == user_id
    assert _snapshot(db_session) == before
    saved = db_session.get(User, user_id)
    assert isinstance(saved.email_key, bytes) and saved.email_key == email_identity_key(email)
    assert "email_key" not in response.json()


@pytest.mark.parametrize("mode", ["register", "change-email"])
@pytest.mark.parametrize("email,duplicate", EQUIVALENT_EMAILS)
def test_equivalent_email_is_rejected_without_partial_writes(client, db_session, account, mode, email, duplicate):
    user, headers = account
    assert client.post("/api/v1/register", json={
        "username": "original_mail_owner", "email": email, "password": PASSWORD,
    }).status_code == 200
    before = _snapshot(db_session)
    payload = {"username": "must_not_save", "email": duplicate, "password": "must-not-save-password"}
    response = (client.post("/api/v1/register", json=payload) if mode == "register"
                else client.put("/api/v1/users/me", headers=headers, json=payload))
    assert response.status_code == 400
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("first,second", [("stra\u00dfe@example.com", "strasse@example.com"),
                                          ("\u00e9@example.com", "e@example.com")])
def test_normalization_does_not_merge_distinct_addresses(client, first, second):
    ids = []
    for index, email in enumerate((first, second)):
        response = client.post("/api/v1/register", json={
            "username": f"distinct_{index}", "email": email, "password": PASSWORD,
        })
        assert response.status_code == 200
        ids.append(response.json()["id"])
        login = client.post("/api/v1/login", data={"username": email, "password": PASSWORD})
        assert login.status_code == 200 and verify_token(login.json()["access_token"]) == ids[-1]
    assert ids[0] != ids[1]


@pytest.mark.parametrize("mode", ["create", "update"])
def test_database_uniqueness_serializes_equivalent_email_claims(db_session_factory, password_hash, mode):
    if mode == "update":
        with db_session_factory() as db:
            db.add_all([User(id=index + 1, username=f"claim_{index}", email=f"original{index}@example.com",
                             hashed_password=password_hash) for index in range(2)])
            db.commit()
    barrier = Barrier(2)

    def claim(index):
        with db_session_factory() as db:
            email = ("\u00dcser@example.com", "\u00fcser@example.com")[index]
            if mode == "create":
                db.add(User(username=f"claim_{index}", email=email, hashed_password=password_hash))
            else:
                db.get(User, index + 1).email = email
            barrier.wait(timeout=10)
            try:
                db.commit()
                return "stored"
            except IntegrityError:
                db.rollback()
                return "duplicate"

    with ThreadPoolExecutor(max_workers=2) as pool:
        results = list(pool.map(claim, range(2)))
    assert sorted(results) == ["duplicate", "stored"]
    with db_session_factory() as db:
        users = db.query(User).all()
        assert len(users) == (1 if mode == "create" else 2)
        assert len({user.email_key for user in users}) == len(users)


@pytest.mark.parametrize("identifier", ["broken@", "name@@example.com", "name@" + "x" * 1000])
def test_invalid_email_login_does_not_write_or_issue_tokens(client, db_session, account, identifier):
    before = _snapshot(db_session)
    response = client.post("/api/v1/login", data={"username": identifier, "password": PASSWORD})
    assert response.status_code == 401
    assert "set-cookie" not in response.headers
    assert _snapshot(db_session) == before
