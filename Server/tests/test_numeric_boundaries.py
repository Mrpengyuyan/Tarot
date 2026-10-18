import pytest
from fastapi.testclient import TestClient
from sqlalchemy import select

from app.core.config import settings
from app.core.security import verify_token
from app.db.base_class import Base
from app.main import app
from app.models.user import User


MIN_INTEGER = -(2**63)
MAX_INTEGER = 2**63 - 1
PATH_CASES = [
    ("GET", "/cards/{id}"), ("GET", "/cards/{id}/meaning"),
    ("PUT", "/cards/{id}"), ("DELETE", "/cards/{id}"),
    ("GET", "/spreads/{id}"), ("GET", "/spreads/{id}/suitability"),
    ("PUT", "/spreads/{id}"), ("DELETE", "/spreads/{id}"), ("POST", "/spreads/{id}/use"),
    ("GET", "/users/{id}"),
    ("GET", "/records/{id}"), ("PUT", "/records/{id}"), ("DELETE", "/records/{id}"),
    ("POST", "/records/{id}/draw"), ("GET", "/records/{id}/cards"),
    ("POST", "/records/{id}/interpret"), ("POST", "/records/{id}/interpret/async"),
    ("POST", "/records/{id}/interpret/retry"), ("GET", "/records/{id}/interpretation"),
    ("PUT", "/records/{id}/interpretation"),
]
PAGINATION_URLS = [
    "/cards/", "/cards/search?q=Card", "/spreads/", "/spreads/beginner",
    "/spreads/by-question-type/general", "/spreads/search?q=Test", "/records/", "/records/admin/all",
]


@pytest.fixture()
def api(client):
    # Reuse the database overrides, but observe a real 500 response before the fix.
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


def _guest(api):
    response = api.post("/api/v1/guest-session")
    assert response.status_code == 200
    token = response.json()["access_token"]
    return verify_token(token), {"Authorization": f"Bearer {token}"}


@pytest.fixture()
def admin_headers(api, db_session):
    user_id, headers = _guest(api)
    db_session.get(User, user_id).is_superuser = True
    db_session.commit()
    return headers


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


@pytest.mark.parametrize("method,path", PATH_CASES)
@pytest.mark.parametrize("value", [MAX_INTEGER + 1, 10**100], ids=["over-limit", "huge"])
def test_overflowing_path_ids_are_rejected_without_writes(api, db_session, admin_headers, method, path, value):
    before = _snapshot(db_session)
    kwargs = {"json": {}} if method == "PUT" else {}
    response = api.request(method, "/api/v1" + path.format(id=value), headers=admin_headers, **kwargs)
    assert response.status_code == 422
    assert response.json()["detail"][0]["loc"][0] == "path"
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("path", ["/cards/{id}", "/cards/{id}/meaning", "/spreads/{id}",
                                 "/spreads/{id}/suitability", "/users/{id}"])
def test_negative_overflow_is_rejected_by_signed_path_routes(api, admin_headers, path):
    response = api.get("/api/v1" + path.format(id=MIN_INTEGER - 1), headers=admin_headers)
    assert response.status_code == 422


@pytest.mark.parametrize("method,path", [(method, path) for method, path in PATH_CASES if path.startswith("/records/")])
def test_record_router_does_not_convert_extremely_long_ids_before_validation(api, admin_headers, method, path):
    kwargs = {"json": {}} if method == "PUT" else {}
    response = api.request(method, "/api/v1" + path.format(id="9" * 5000), headers=admin_headers, **kwargs)
    assert response.status_code == 422
    assert response.json()["detail"][0]["loc"] == ["path", "prediction_id"]


def test_record_router_preserves_numeric_matching_and_url_generation(api, admin_headers):
    for value in ["not-an-id", "-1", "+1", "1.5"]:
        assert api.get(f"/api/v1/records/{value}", headers=admin_headers).status_code == 404
    assert str(app.url_path_for("get_prediction_detail", prediction_id=1)) == "/api/v1/records/1"


@pytest.mark.parametrize("url", PAGINATION_URLS)
@pytest.mark.parametrize("skip", [MAX_INTEGER + 1, 10**100], ids=["over-limit", "huge"])
def test_overflowing_offsets_are_rejected(api, admin_headers, url, skip):
    response = api.get("/api/v1" + url, headers=admin_headers, params={"skip": skip})
    assert response.status_code == 422
    assert response.json()["detail"][0]["loc"] == ["query", "skip"]


@pytest.mark.parametrize("url", PAGINATION_URLS)
def test_maximum_offset_remains_valid_and_negative_offset_is_rejected(
    api, admin_headers, seeded_spread_and_cards, url,
):
    response = api.get("/api/v1" + url, headers=admin_headers, params={"skip": MAX_INTEGER})
    assert response.status_code == 200
    assert response.json() == []
    assert api.get("/api/v1" + url, headers=admin_headers, params={"skip": -1}).status_code == 422


@pytest.mark.parametrize("value", [MIN_INTEGER - 1, MAX_INTEGER + 1, 10**100])
def test_each_excluded_card_id_is_checked(api, admin_headers, seeded_spread_and_cards, value):
    response = api.get("/api/v1/cards/draw", headers=admin_headers, params=[
        ("exclude_ids", seeded_spread_and_cards["card_ids"][0]), ("exclude_ids", value),
    ])
    assert response.status_code == 422
    assert response.json()["detail"][0]["loc"] == ["query", "exclude_ids", 1]


def test_invalid_spread_ids_do_not_write_or_consume_reading_quota(
    api, db_session, seeded_spread_and_cards, monkeypatch,
):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 3)
    user_id, headers = _guest(api)
    before = _snapshot(db_session)
    payload = {"question": "Numeric boundary acceptance", "question_type": "general"}
    for value in [MIN_INTEGER - 1, MAX_INTEGER + 1, 10**100]:
        response = api.post("/api/v1/records/", headers=headers, json={**payload, "spread_type_id": value})
        assert response.status_code == 422
        assert response.json()["detail"][0]["loc"] == ["body", "spread_type_id"]
        assert _snapshot(db_session) == before
    for _ in range(3):
        response = api.post("/api/v1/records/", headers=headers, json={
            **payload, "spread_type_id": seeded_spread_and_cards["spread_id"],
        })
        assert response.status_code == 200
    db_session.expire_all()
    assert db_session.get(User, user_id).reading_quota_count == 3
    assert api.post("/api/v1/records/", headers=headers, json={
        **payload, "spread_type_id": seeded_spread_and_cards["spread_id"],
    }).status_code == 429


@pytest.mark.parametrize("value", [MIN_INTEGER, -1, 0, MAX_INTEGER])
def test_representable_missing_ids_keep_existing_not_found_behavior(api, admin_headers, value):
    for path in ["/cards/{id}", "/spreads/{id}", "/users/{id}", "/records/{id}"]:
        assert api.get("/api/v1" + path.format(id=value), headers=admin_headers).status_code == 404
    assert api.post("/api/v1/records/", headers=admin_headers, json={
        "question": "Unknown spread", "question_type": "general", "spread_type_id": value,
    }).status_code == 400


def test_valid_excluded_ids_and_arbitrarily_large_random_seed_remain_supported(
    api, admin_headers, seeded_spread_and_cards,
):
    excluded = seeded_spread_and_cards["card_ids"][0]
    params = [("count", 2), ("seed", 10**100), ("exclude_ids", MIN_INTEGER),
              ("exclude_ids", MAX_INTEGER), ("exclude_ids", excluded)]
    first = api.get("/api/v1/cards/draw", headers=admin_headers, params=params)
    second = api.get("/api/v1/cards/draw", headers=admin_headers, params=params)
    assert first.status_code == second.status_code == 200
    assert first.json() == second.json()
    ids = [card["id"] for card in first.json()]
    assert len(ids) == len(set(ids)) == 2
    assert excluded not in ids


def test_valid_ids_still_require_existing_authentication_and_permissions(api, seeded_spread_and_cards):
    user_id, headers = _guest(api)
    assert api.put(f"/api/v1/cards/{seeded_spread_and_cards['card_ids'][0]}",
                   headers=headers, json={}).status_code == 403
    assert api.delete(f"/api/v1/spreads/{seeded_spread_and_cards['spread_id']}", headers=headers).status_code == 403
    assert api.get(f"/api/v1/users/{user_id + 1}", headers=headers).status_code == 403
    assert api.get("/api/v1/records/1", headers=headers).status_code == 404
    api.cookies.clear()
    assert api.get(f"/api/v1/records/{MAX_INTEGER + 1}").status_code == 401
