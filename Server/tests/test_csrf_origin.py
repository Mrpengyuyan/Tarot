import pytest
from fastapi.testclient import TestClient
from sqlalchemy import event, select
from starlette.requests import Request

from app.api.deps import _ensure_cookie_csrf_protection, get_current_user_optional
from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.core.security import create_access_token, create_refresh_token, verify_token
from app.db.base_class import Base
from app.main import app
from app.models.user import User


BAD_REFERERS = ["http://[", "https://[PRIVATE_REFERER]/private?value=secret", "http://[::gg]/"]


@pytest.fixture()
def api(client, monkeypatch):
    monkeypatch.setattr(settings, "CSRF_PROTECTION_ENABLED", True)
    monkeypatch.setattr(settings, "CORS_ORIGINS", "http://localhost:3000,http://[::1]:3000")
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


@pytest.fixture()
def session(api, db_session, seeded_spread_and_cards):
    user = User(username="origin_owner", email="origin_owner@example.com", hashed_password="unused",
                nickname="Retained nickname")
    db_session.add(user)
    db_session.commit()
    access = create_access_token({"sub": str(user.id), "sub_type": "user_id"})
    refresh = create_refresh_token({"sub": str(user.id), "sub_type": "user_id"})
    bearer = {"Authorization": f"Bearer {access}"}
    created = api.post("/api/v1/records/", headers=bearer, json={
        "question": "Retained question", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    record_id = created.json()["id"]
    assert api.post(f"/api/v1/records/{record_id}/draw", headers=bearer).status_code == 200
    csrf = "isolated-csrf-token"
    return {
        "id": user.id, "record_id": record_id, "access": access,
        "headers": {
            "Cookie": (f"{settings.AUTH_COOKIE_NAME}={access}; {settings.REFRESH_COOKIE_NAME}={refresh}; "
                       f"{settings.CSRF_COOKIE_NAME}={csrf}"),
            settings.CSRF_HEADER_NAME: csrf,
        },
    }


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _request_cases(record_id, spread_id):
    return [
        ("POST", "/api/v1/refresh", None),
        ("PUT", "/api/v1/users/me", {"username": "must_not_change"}),
        ("POST", "/api/v1/records/", {"question": "Must not create", "question_type": "general", "spread_type_id": spread_id}),
        ("PUT", f"/api/v1/records/{record_id}", {"user_notes": "Must not change"}),
        ("POST", f"/api/v1/records/{record_id}/interpret?force_ai=true", None),
        ("POST", f"/api/v1/records/{record_id}/interpret/async", None),
        ("POST", f"/api/v1/records/{record_id}/interpret/retry", None),
        ("DELETE", f"/api/v1/records/{record_id}", None),
    ]


@pytest.mark.parametrize("referer", BAD_REFERERS, ids=["unclosed-ipv6", "private-invalid-host", "invalid-ipv6"])
@pytest.mark.parametrize("case", range(8), ids=["refresh", "profile", "create", "notes", "sync-ai", "async-ai", "retry-ai", "delete"])
def test_malformed_referer_is_rejected_without_writes_or_ai(
    api, db_session, session, seeded_spread_and_cards, monkeypatch, caplog, referer, case,
):
    calls = []

    async def unexpected_ai(*args, **kwargs):
        calls.append(True)
        raise AssertionError("Rejected request must not call AI")

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", unexpected_ai)
    method, path, payload = _request_cases(session["record_id"], seeded_spread_and_cards["spread_id"])[case]
    before = _snapshot(db_session)
    kwargs = {"json": payload} if payload is not None else {}
    response = api.request(method, path, headers={**session["headers"], "Referer": referer}, **kwargs)
    assert response.status_code == 403
    assert response.json() == {"detail": "Origin not allowed"}
    assert "set-cookie" not in response.headers
    assert referer not in response.text and "PRIVATE_REFERER" not in caplog.text
    assert _snapshot(db_session) == before
    assert calls == []


@pytest.mark.parametrize("path,method", [("/api/v1/refresh", "POST"), ("/api/v1/users/me", "PUT")])
def test_invalid_credentials_do_not_turn_malformed_referer_into_500(api, path, method):
    response = api.request(method, path, headers={
        "Cookie": (f"{settings.AUTH_COOKIE_NAME}=untrusted; {settings.REFRESH_COOKIE_NAME}=untrusted; "
                   f"{settings.CSRF_COOKIE_NAME}=probe"),
        settings.CSRF_HEADER_NAME: "probe", "Referer": BAD_REFERERS[0],
    }, **({"json": {"username": "must_not_change"}} if method == "PUT" else {}))
    assert response.status_code == 403
    assert response.json() == {"detail": "Origin not allowed"}


@pytest.mark.parametrize("source", [
    {}, {"Referer": ""}, {"Referer": "/relative/path"},
    {"Referer": "http://localhost:3000/game?value=ok#section"},
    {"Referer": "http://[::1]:3000/game"},
    {"Origin": "http://localhost:3000", "Referer": BAD_REFERERS[0]},
], ids=["absent", "empty", "relative-legacy", "allowed-referer", "allowed-ipv6", "origin-precedence"])
def test_supported_sources_preserve_cookie_update_and_refresh(api, session, source):
    headers = {**session["headers"], **source}
    updated = api.put("/api/v1/users/me", headers=headers, json={"username": "updated_origin_owner"})
    assert updated.status_code == 200
    assert updated.json()["id"] == session["id"]
    assert updated.json()["username"] == "updated_origin_owner"
    refreshed = api.post("/api/v1/refresh", headers=headers)
    assert refreshed.status_code == 200
    assert verify_token(refreshed.json()["access_token"]) == session["id"]


@pytest.mark.parametrize("source", [
    {"Origin": "https://untrusted.example", "Referer": "http://localhost:3000/game"},
    {"Referer": "https://untrusted.example/game"},
], ids=["disallowed-origin-precedence", "disallowed-referer"])
def test_disallowed_sources_remain_blocked(api, db_session, session, source):
    before = _snapshot(db_session)
    response = api.put("/api/v1/users/me", headers={**session["headers"], **source},
                       json={"username": "must_not_change"})
    assert response.status_code == 403
    assert response.json() == {"detail": "Origin not allowed"}
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("csrf", [None, "wrong-token"])
def test_csrf_token_is_still_checked_before_source_parsing(api, session, csrf):
    headers = {**session["headers"], "Referer": BAD_REFERERS[0]}
    if csrf is None:
        headers.pop(settings.CSRF_HEADER_NAME)
    else:
        headers[settings.CSRF_HEADER_NAME] = csrf
    response = api.put("/api/v1/users/me", headers=headers, json={"username": "must_not_change"})
    assert response.status_code == 403
    assert response.json() == {"detail": "CSRF validation failed"}


def test_bearer_auth_still_takes_precedence_over_cookie_source_checks(api, session):
    headers = {**session["headers"], "Authorization": f"Bearer {session['access']}",
               "Referer": BAD_REFERERS[0], settings.CSRF_HEADER_NAME: "wrong-token"}
    response = api.put("/api/v1/users/me", headers=headers, json={"username": "bearer_updated_owner"})
    assert response.status_code == 200 and response.json()["id"] == session["id"]
    assert response.json()["username"] == "bearer_updated_owner"
    headers["Authorization"] = "Bearer untrusted"
    assert api.put("/api/v1/users/me", headers=headers, json={"username": "must_not_change"}).status_code == 401


def test_cookie_get_keeps_existing_safe_method_behavior(api, session):
    headers = {**session["headers"], "Referer": BAD_REFERERS[0]}
    headers.pop(settings.CSRF_HEADER_NAME)
    response = api.get("/api/v1/users/me", headers=headers)
    assert response.status_code == 200 and response.json()["id"] == session["id"]


@pytest.mark.parametrize("method", ["GET", "HEAD", "OPTIONS"])
def test_safe_methods_do_not_parse_referer(method):
    request = Request({"type": "http", "method": method,
                       "headers": [(b"referer", BAD_REFERERS[0].encode("ascii"))]})
    _ensure_cookie_csrf_protection(request)


def test_optional_cookie_auth_treats_malformed_source_as_unauthenticated(db_session, session):
    headers = {**session["headers"], "Referer": BAD_REFERERS[0]}
    request = Request({"type": "http", "method": "POST",
                       "headers": [(key.lower().encode("ascii"), value.encode("ascii")) for key, value in headers.items()]})
    statements = []

    def capture(connection, cursor, statement, parameters, context, executemany):
        statements.append(statement)

    engine = db_session.get_bind()
    event.listen(engine, "before_cursor_execute", capture)
    try:
        assert get_current_user_optional(request, db_session, credentials=None) is None
    finally:
        event.remove(engine, "before_cursor_execute", capture)
    assert statements == []
