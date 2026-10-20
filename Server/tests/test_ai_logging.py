import json
import logging

import httpx
import pytest
from sqlalchemy import event, select
from sqlalchemy.exc import StatementError

from app.db.session import get_session_factory
from app.main import app
from app.models.budget import AIBudgetReservation
from app.services.budget_store import DatabaseBudgetStore
from app.services.coze_service import CozeBudgetExceededError, CozeService
from app.services.tarot_service import tarot_interpretation_service


QUESTION = "PRIVATE_QUESTION"
ANSWER = "PRIVATE_READING"
PRIVATE = QUESTION + " " + ANSWER + " PRIVATE_CREDENTIAL"


def _private_failure(*args, **kwargs):
    try:
        raise ValueError(PRIVATE)
    except ValueError as exc:
        raise StatementError("Private database failure", "SELECT private_value", {"value": PRIVATE}, exc) from exc


def _assert_safe_logs(caplog):
    entries = [entry for entry in caplog.records if entry.name.startswith("app.")]
    assert entries
    for entry in entries:
        assert entry.exc_info is None and entry.stack_info is None
    for value in (QUESTION, ANSWER, "PRIVATE_CREDENTIAL"):
        assert value not in caplog.text


@pytest.fixture()
def upstream(monkeypatch, db_session_factory, caplog):
    caplog.set_level(logging.DEBUG)
    state = {"answer": {"overall_interpretation": ANSWER, "summary": "Valid summary"},
             "usage": {"prompt_tokens": 20, "completion_tokens": 10}, "posts": [], "timeout": False}
    original_client = httpx.AsyncClient

    def respond(request):
        assert request.url.host == "127.0.0.1" and request.url.port == 9
        state["posts"].append(request)
        if state["timeout"]:
            raise httpx.ReadTimeout(PRIVATE, request=request)
        return httpx.Response(200, json={
            "choices": [{"message": {"content": json.dumps(state["answer"])}}], "usage": state["usage"],
        })

    def mock_client(**kwargs):
        return original_client(transport=httpx.MockTransport(respond), **kwargs)

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", mock_client)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "PRIVATE_CREDENTIAL"
    service.base_url = "http://127.0.0.1:9"
    service.daily_budget_usd = service.monthly_budget_usd = 1
    service.chat_input_cost_per_m = service.chat_output_cost_per_m = 1
    service.max_output_tokens = 100
    monkeypatch.setattr(tarot_interpretation_service, "ai_service", service)
    return state, service, store


@pytest.fixture()
def reading(client, seeded_spread_and_cards):
    guest = client.post("/api/v1/guest-session")
    assert guest.status_code == 200
    headers = {"Authorization": "Bearer " + guest.json()["access_token"]}
    created = client.post("/api/v1/records/", headers=headers, json={
        "question": QUESTION, "question_type": "general", "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    path = f"/api/v1/records/{created.json()['id']}"
    assert client.post(path + "/draw", headers=headers).status_code == 200
    return path, headers


def _budget(db, store, requests, spent):
    db.expire_all()
    reservations = db.scalars(select(AIBudgetReservation)).all()
    assert len(reservations) == requests
    assert [item.actual_micros for item in reservations if item.actual_micros is not None] == ([31] if spent else [])
    held = sum(item.estimated_micros for item in reservations if item.actual_micros is None)
    assert store.snapshot() == {"daily_spent_micros": spent, "monthly_spent_micros": spent,
                                "daily_reserved_micros": held, "monthly_reserved_micros": held}


@pytest.mark.parametrize("mode", ["sync", "async", "manual-retry"])
@pytest.mark.parametrize("field", ["overall_interpretation", "summary"])
@pytest.mark.parametrize("invalid", ["\ud800", "\udfff"], ids=["high-surrogate", "low-surrogate"])
@pytest.mark.parametrize("unknown_usage", [False, True])
def test_invalid_ai_content_keeps_existing_failure_and_budget_without_private_logs(
    client, reading, upstream, db_session, caplog, mode, field, invalid, unknown_usage,
):
    state, service, store = upstream
    path, headers = reading
    if unknown_usage:
        state["usage"] = {}
    if mode == "manual-retry":
        state["timeout"] = True
        assert client.post(path + "/interpret", headers=headers).status_code == 504
        state["timeout"] = False
    state["answer"][field] = QUESTION + invalid + ANSWER
    suffix = {"sync": "/interpret", "async": "/interpret/async", "manual-retry": "/interpret/retry"}[mode]
    response = client.post(path + suffix, headers=headers)
    assert response.status_code == (502 if mode == "sync" else 202)
    result = client.get(path, headers=headers).json()
    requests = 2 if mode == "manual-retry" else 1
    assert result["status"] == "failed" and result["interpretation"] is None
    assert result["interpretation_run"]["state"] == ("exhausted" if requests == 2 else "retry_required")
    assert result["interpretation_run"]["requests_started"] == len(state["posts"]) == requests
    assert result["interpretation_run"]["can_retry"] is (requests == 1)
    before = store.snapshot()
    assert client.post(path + "/interpret/async", headers=headers).status_code == (429 if requests == 2 else 409)
    assert len(state["posts"]) == requests and store.snapshot() == before
    if requests == 2:
        assert client.post(path + "/interpret/retry", headers=headers).status_code == 429
    _budget(db_session, store, requests, 0 if unknown_usage else 31)
    for private in (QUESTION, ANSWER):
        assert private not in response.text
    _assert_safe_logs(caplog)
    assert "ValidationError" in caplog.text
    assert f"record={path.rsplit('/', 1)[1]}" in caplog.text


def test_generation_exception_does_not_log_private_message_or_chain(
    client, reading, upstream, db_session, monkeypatch, caplog,
):
    state, service, store = upstream
    monkeypatch.setattr(tarot_interpretation_service, "_parse_interpretation_payload", _private_failure)
    path, headers = reading
    response = client.post(path + "/interpret", headers=headers)
    assert response.status_code == 502
    assert client.get(path, headers=headers).json()["status"] == "failed"
    assert len(state["posts"]) == 1
    _budget(db_session, store, 1, 31)
    _assert_safe_logs(caplog)
    assert "StatementError" in caplog.text


@pytest.mark.parametrize("stage", ["settlement", "snapshot", "post-response-budget"])
def test_post_response_budget_error_preserves_answer_and_safe_diagnostics(
    client, reading, upstream, db_session, monkeypatch, caplog, stage,
):
    state, service, store = upstream
    if stage == "settlement":
        monkeypatch.setattr(store, "settle", _private_failure)
    elif stage == "snapshot":
        monkeypatch.setattr(service, "budget_status", _private_failure)
    else:
        original = service._register_usage_and_cost

        def exceed(**kwargs):
            original(**kwargs)
            raise CozeBudgetExceededError(PRIVATE)

        monkeypatch.setattr(service, "_register_usage_and_cost", exceed)
    path, headers = reading
    response = client.post(path + "/interpret", headers=headers)
    assert response.status_code == 200
    result = client.get(path, headers=headers).json()
    assert result["status"] == "completed" and result["interpretation"]["overall_interpretation"] == ANSWER
    assert result["interpretation_run"]["can_retry"] is False
    assert len(state["posts"]) == result["interpretation_run"]["requests_started"] == 1
    _budget(db_session, store, 1, 0 if stage == "settlement" else 31)
    _assert_safe_logs(caplog)
    assert ("CozeBudgetExceededError" if stage == "post-response-budget" else "StatementError") in caplog.text


@pytest.mark.parametrize("stage", ["store", "mark-failed", "close"])
def test_background_database_failure_does_not_leak_statement_parameters_or_exception_chain(
    client, reading, upstream, db_session, db_session_factory, monkeypatch, caplog, stage,
):
    state, service, store = upstream
    path, headers = reading
    if stage == "mark-failed":
        state["answer"]["summary"] = QUESTION + "\ud800" + ANSWER

    def fail_database(connection, cursor, statement, parameters, context, executemany):
        if (stage == "store" and statement.startswith("INSERT INTO interpretations")) or (
            stage == "mark-failed" and statement.startswith("UPDATE predictions") and "failed" in parameters
        ):
            _private_failure()

    def session_factory():
        db = db_session_factory()
        close = db.close

        def fail_close():
            close()
            _private_failure()

        db.close = fail_close
        return db

    if stage == "close":
        monkeypatch.setitem(app.dependency_overrides, get_session_factory, lambda: session_factory)
    engine = db_session.get_bind()
    event.listen(engine, "before_cursor_execute", fail_database)
    try:
        response = client.post(path + "/interpret/async", headers=headers)
    finally:
        event.remove(engine, "before_cursor_execute", fail_database)
    assert response.status_code == 202
    result = client.get(path, headers=headers).json()
    assert result["status"] == {"store": "failed", "mark-failed": "processing", "close": "completed"}[stage]
    assert (result["interpretation"] is not None) is (stage == "close")
    assert len(state["posts"]) == result["interpretation_run"]["requests_started"] == 1
    _budget(db_session, store, 1, 31)
    _assert_safe_logs(caplog)
    assert "StatementError" in caplog.text


@pytest.mark.parametrize("stage", ["store", "mark-failed"])
def test_synchronous_database_failure_preserves_plain_500_without_escaping_private_exception(
    client, reading, upstream, db_session, caplog, stage,
):
    state, service, store = upstream
    path, headers = reading
    if stage == "mark-failed":
        state["answer"]["summary"] = QUESTION + "\ud800" + ANSWER

    def fail_database(connection, cursor, statement, parameters, context, executemany):
        if (stage == "store" and statement.startswith("INSERT INTO interpretations")) or (
            stage == "mark-failed" and statement.startswith("UPDATE predictions") and "failed" in parameters
        ):
            _private_failure()

    engine = db_session.get_bind()
    event.listen(engine, "before_cursor_execute", fail_database)
    try:
        response = client.post(path + "/interpret", headers=headers)
    finally:
        event.remove(engine, "before_cursor_execute", fail_database)
    assert response.status_code == 500 and response.text == "Internal Server Error"
    assert response.headers["content-type"] == "text/plain; charset=utf-8"
    result = client.get(path, headers=headers).json()
    assert result["status"] == "processing" and result["interpretation"] is None
    assert len(state["posts"]) == result["interpretation_run"]["requests_started"] == 1
    _budget(db_session, store, 1, 31)
    _assert_safe_logs(caplog)
    assert "StatementError" in caplog.text
