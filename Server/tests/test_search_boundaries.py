import pytest
from fastapi.testclient import TestClient
from sqlalchemy import event, select

from app.core.security import create_access_token
from app.db.base_class import Base
from app.main import app
from app.models.record import Prediction, QuestionType
from app.models.spread import SpreadType
from app.models.tarot_card import TarotCard
from app.models.user import User


MAX_SEARCH_BYTES = 49_998
SEARCH_ROUTES = [
    ("/api/v1/cards/", "search", "cards"),
    ("/api/v1/cards/search", "q", "cards"),
    ("/api/v1/spreads/", "search", "spreads"),
    ("/api/v1/spreads/search", "q", "spreads"),
    ("/api/v1/records/", "search", "records"),
]
SEARCH_TEXT = "Beacon \u4e2d\U0001f52e literal \\ud800"


@pytest.fixture()
def api(client):
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client


@pytest.fixture()
def search_data(db_session, seeded_spread_and_cards):
    owner = User(username="search_owner", email="owner@example.com", hashed_password="unused")
    other = User(username="search_other", email="other@example.com", hashed_password="unused")
    db_session.add_all([owner, other])
    db_session.flush()
    spread_id = seeded_spread_and_cards["spread_id"]
    card_id = seeded_spread_and_cards["card_ids"][0]
    db_session.get(TarotCard, card_id).name_en = SEARCH_TEXT
    db_session.get(SpreadType, spread_id).name = SEARCH_TEXT
    owned = Prediction(user_id=owner.id, spread_type_id=spread_id,
                       question=SEARCH_TEXT, question_type=QuestionType.GENERAL)
    db_session.add_all([
        owned,
        Prediction(user_id=other.id, spread_type_id=spread_id,
                   question=SEARCH_TEXT, question_type=QuestionType.GENERAL),
    ])
    db_session.commit()
    token = create_access_token({"sub": str(owner.id), "sub_type": "user_id"})
    return {
        "headers": {"Authorization": f"Bearer {token}"},
        "target_ids": {"cards": card_id, "spreads": spread_id, "records": owned.id},
        "all_ids": {"cards": seeded_spread_and_cards["card_ids"],
                    "spreads": [spread_id], "records": [owned.id]},
    }


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


def _term(byte_count, tail):
    return "x" * (byte_count - len(tail.encode("utf-8"))) + tail


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
@pytest.mark.parametrize("tail", ["", "\u4e2d", "\U0001f52e"], ids=["ascii", "chinese", "emoji"])
def test_overlong_search_is_rejected_before_matching_without_writes(
    api, db_session, search_data, url, key, resource, tail,
):
    before = _snapshot(db_session)
    statements = []

    def capture(connection, cursor, statement, parameters, context, executemany):
        statements.append(statement)

    engine = db_session.get_bind()
    event.listen(engine, "before_cursor_execute", capture)
    try:
        response = api.get(url, params={key: _term(MAX_SEARCH_BYTES + 1, tail)},
                           headers=search_data["headers"])
    finally:
        event.remove(engine, "before_cursor_execute", capture)
    assert response.status_code == 422
    assert response.json()["detail"][0]["loc"] == ["query", key]
    assert not any(" like " in statement.lower() for statement in statements)
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
@pytest.mark.parametrize("tail", ["", "\u4e2d", "\U0001f52e"], ids=["ascii", "chinese", "emoji"])
def test_search_at_utf8_byte_limit_reaches_database_unchanged(
    api, db_session, search_data, url, key, resource, tail,
):
    term = _term(MAX_SEARCH_BYTES, tail)
    patterns = []

    def capture(connection, cursor, statement, parameters, context, executemany):
        if " like " in statement.lower():
            patterns.extend(parameters)

    engine = db_session.get_bind()
    event.listen(engine, "before_cursor_execute", capture)
    try:
        response = api.get(url, params={key: term}, headers=search_data["headers"])
    finally:
        event.remove(engine, "before_cursor_execute", capture)
    assert response.status_code == 200
    assert response.json() == []
    assert f"%{term}%" in patterns


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
@pytest.mark.parametrize("term", ["bEaCoN", "\u4e2d", "\U0001f52e", "\\ud800", "B_acon%", "B%on", "Beacon "])
def test_normal_search_text_and_wildcards_keep_existing_matching_and_ownership(
    api, search_data, url, key, resource, term,
):
    response = api.get(url, params={key: term}, headers=search_data["headers"])
    assert response.status_code == 200
    assert [row["id"] for row in response.json()] == [search_data["target_ids"][resource]]


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
@pytest.mark.parametrize("term", [" Beacon", "missing-token"])
def test_search_does_not_trim_or_replace_nonmatching_terms(api, search_data, url, key, resource, term):
    response = api.get(url, params={key: term}, headers=search_data["headers"])
    assert response.status_code == 200
    assert response.json() == []


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
@pytest.mark.parametrize("term", ["", "%", "_"])
def test_empty_and_match_all_search_preserve_existing_results(api, search_data, url, key, resource, term):
    response = api.get(url, params={key: term}, headers=search_data["headers"])
    assert response.status_code == 200
    assert sorted(row["id"] for row in response.json()) == sorted(search_data["all_ids"][resource])


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
def test_omitted_search_keeps_optional_and_required_query_behavior(api, search_data, url, key, resource):
    response = api.get(url, headers=search_data["headers"])
    if key == "q":
        assert response.status_code == 422
        assert response.json()["detail"][0]["loc"] == ["query", "q"]
    else:
        assert response.status_code == 200
        assert sorted(row["id"] for row in response.json()) == sorted(search_data["all_ids"][resource])


@pytest.mark.parametrize("url,key,resource", SEARCH_ROUTES)
def test_search_keeps_public_and_authenticated_route_access(api, search_data, url, key, resource):
    for term in ("Beacon", "x" * (MAX_SEARCH_BYTES + 1)):
        response = api.get(url, params={key: term})
        expected = 401 if resource == "records" else (422 if len(term) > MAX_SEARCH_BYTES else 200)
        assert response.status_code == expected
