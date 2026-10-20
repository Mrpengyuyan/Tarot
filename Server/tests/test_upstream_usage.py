import asyncio
import json

import httpx
import pytest
from sqlalchemy import select

from app.models.budget import AIBudgetReservation
from app.services.budget_store import DatabaseBudgetStore
from app.services.coze_service import CozeBudgetExceededError, CozeService
from app.services.tarot_service import tarot_interpretation_service


CANARY = "PRIVATE_USAGE_CANARY"
ANSWER = '{"overall_interpretation":"Usable answer","summary":"Usable summary"}'
EMPTY_USAGE = {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}


@pytest.fixture()
def upstream(monkeypatch, db_session_factory):
    state = {"usage": {"prompt_tokens": 20, "completion_tokens": 10}, "posts": [], "timeout": False}
    original_client = httpx.AsyncClient

    def respond(request):
        assert request.url.host == "127.0.0.1" and request.url.port == 9
        state["posts"].append(request)
        if state["timeout"]:
            raise httpx.ReadTimeout("Simulated lost response", request=request)
        body = state.get("raw_body") or json.dumps({
            "choices": [{"message": {"content": ANSWER}}], "usage": state["usage"],
        }).encode("ascii")
        return httpx.Response(200, content=body, headers={"Content-Type": "application/json"})

    def mock_client(**kwargs):
        return original_client(transport=httpx.MockTransport(respond), **kwargs)

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", mock_client)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "isolated-test-key-not-real"
    service.base_url = "http://127.0.0.1:9"
    service.daily_budget_usd = service.monthly_budget_usd = 1
    service.chat_input_cost_per_m = service.chat_output_cost_per_m = 1
    service.max_output_tokens = 100
    return state, service, store


@pytest.mark.parametrize("field", ["prompt_tokens", "completion_tokens", "total_tokens", "input_tokens", "output_tokens"])
@pytest.mark.parametrize("invalid", [
    CANARY, {}, [], [1], True, -1, 1.5, float("nan"), float("inf"), float("-inf"), 10**400, 2**63, "",
], ids=["text", "object", "empty-list", "list", "boolean", "negative", "fraction", "nan", "inf", "negative-inf",
        "overflow", "over-integer-bound", "empty-string"])
def test_bad_usage_keeps_the_answer_and_entire_reservation(upstream, db_session, caplog, field, invalid):
    state, service, store = upstream
    state["usage"][field] = invalid
    result = asyncio.run(service.send_messages_and_wait(messages=[], single_attempt=True, expect_json=True))
    assert result["text"] == ANSWER
    assert result["usage"] == EMPTY_USAGE
    assert result["cost_usd"] == 0
    assert len(state["posts"]) == 1
    assert result["fallback_used"] is False
    reservation = db_session.scalars(select(AIBudgetReservation)).one()
    assert reservation.actual_micros is None
    assert reservation.estimated_micros == 1126
    assert store.snapshot() == {
        "daily_reserved_micros": 1126, "monthly_reserved_micros": 1126,
        "daily_spent_micros": 0, "monthly_spent_micros": 0,
    }
    assert CANARY not in caplog.text
    assert CANARY not in json.dumps(result, allow_nan=False)


@pytest.mark.parametrize("explicit_total", [False, True])
def test_overflowing_combined_count_keeps_the_entire_reservation(upstream, db_session, explicit_total):
    state, service, store = upstream
    state["usage"] = {"prompt_tokens": 2**63 - 1, "completion_tokens": 1}
    if explicit_total:
        state["usage"]["total_tokens"] = 1
    result = asyncio.run(service.send_messages_and_wait(messages=[], single_attempt=True))
    assert result["text"] == ANSWER and result["usage"] == EMPTY_USAGE
    assert len(state["posts"]) == 1
    assert db_session.scalars(select(AIBudgetReservation)).one().actual_micros is None
    assert store.snapshot()["daily_reserved_micros"] == 1126
    assert store.snapshot()["daily_spent_micros"] == 0


@pytest.mark.parametrize("field", ["prompt_tokens", "completion_tokens", "total_tokens", "input_tokens", "output_tokens"])
@pytest.mark.parametrize("sign", ["", "-"])
def test_oversized_json_integer_does_not_discard_the_answer(upstream, db_session, caplog, field, sign):
    state, service, store = upstream
    state["usage"][field] = "OVERSIZED_INTEGER"
    body = json.dumps({"choices": [{"message": {"content": ANSWER}}], "usage": state["usage"]}).encode("ascii")
    literal = (sign + "9" * 5000).encode("ascii")
    state["raw_body"] = body.replace(b'"OVERSIZED_INTEGER"', literal)
    result = asyncio.run(service.send_messages_and_wait(messages=[], single_attempt=True))
    assert result["text"] == ANSWER and result["usage"] == EMPTY_USAGE
    assert result["cost_usd"] == 0 and len(state["posts"]) == 1
    assert db_session.scalars(select(AIBudgetReservation)).one().actual_micros is None
    assert store.snapshot()["daily_reserved_micros"] == store.snapshot()["monthly_reserved_micros"] == 1126
    assert store.snapshot()["daily_spent_micros"] == store.snapshot()["monthly_spent_micros"] == 0
    assert "9" * 100 not in caplog.text


@pytest.mark.parametrize("usage,expected,spent", [
    # Preserve the existing float-based cost calculation and upward rounding.
    ({"prompt_tokens": 20, "completion_tokens": 10}, {"prompt_tokens": 20, "completion_tokens": 10, "total_tokens": 30}, 31),
    ({"input_tokens": "20", "output_tokens": 10.0, "total_tokens": "30"}, {"prompt_tokens": 20, "completion_tokens": 10, "total_tokens": 30}, 31),
    ({"prompt_tokens": 0, "input_tokens": 20, "completion_tokens": 10}, {"prompt_tokens": 20, "completion_tokens": 10, "total_tokens": 30}, 31),
    ({"prompt_tokens": "0", "input_tokens": 20, "completion_tokens": 10}, {"prompt_tokens": 0, "completion_tokens": 10, "total_tokens": 10}, 10),
    ({}, EMPTY_USAGE, 0), (None, EMPTY_USAGE, 0), ([], EMPTY_USAGE, 0),
    ({"prompt_tokens": None, "completion_tokens": None}, EMPTY_USAGE, 0),
    ({"prompt_tokens": 0, "completion_tokens": 0}, EMPTY_USAGE, 0),
])
def test_normal_usage_settlement_and_missing_usage_policy_are_unchanged(upstream, db_session, usage, expected, spent):
    state, service, store = upstream
    state["usage"] = usage
    result = asyncio.run(service.send_messages_and_wait(messages=[], single_attempt=True))
    assert result["text"] == ANSWER and result["usage"] == expected
    assert len(state["posts"]) == 1
    reservation = db_session.scalars(select(AIBudgetReservation)).one()
    assert reservation.actual_micros == (spent if spent else None)
    assert store.snapshot() == {
        "daily_reserved_micros": 0 if spent else 1126, "monthly_reserved_micros": 0 if spent else 1126,
        "daily_spent_micros": spent, "monthly_spent_micros": spent,
    }


def test_unknown_usage_cannot_reuse_reserved_budget_after_service_recreation(upstream, db_session_factory):
    state, service, store = upstream
    state["usage"]["total_tokens"] = CANARY
    service.daily_budget_usd = 0.0015
    first = asyncio.run(service.send_messages_and_wait(messages=[], single_attempt=True))
    assert first["text"] == ANSWER
    replacement = CozeService(budget_store=DatabaseBudgetStore(db_session_factory))
    for name in ("api_key", "base_url", "daily_budget_usd", "monthly_budget_usd", "chat_input_cost_per_m",
                 "chat_output_cost_per_m", "max_output_tokens"):
        setattr(replacement, name, getattr(service, name))
    with pytest.raises(CozeBudgetExceededError):
        asyncio.run(replacement.send_messages_and_wait(messages=[], single_attempt=True))
    assert len(state["posts"]) == 1
    assert store.snapshot()["daily_reserved_micros"] == 1126


@pytest.mark.parametrize("mode", ["sync", "async", "manual-retry"])
@pytest.mark.parametrize("invalid", [CANARY, {"private": CANARY}, float("inf"), 10**400],
                         ids=["text", "object", "inf", "overflow"])
def test_usable_interpretation_is_saved_without_extra_requests_or_private_logs(
    client, seeded_spread_and_cards, upstream, db_session, caplog, monkeypatch, mode, invalid,
):
    state, service, store = upstream
    monkeypatch.setattr(tarot_interpretation_service, "ai_service", service)
    guest = client.post("/api/v1/guest-session")
    assert guest.status_code == 200
    headers = {"Authorization": "Bearer " + guest.json()["access_token"]}
    created = client.post("/api/v1/records/", headers=headers, json={
        "question": "Reading usage acceptance", "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    record_id = created.json()["id"]
    path = f"/api/v1/records/{record_id}"
    assert client.post(path + "/draw", headers=headers).status_code == 200
    if mode == "manual-retry":
        state["timeout"] = True
        first = client.post(path + "/interpret", headers=headers)
        assert first.status_code == 504
        assert len(state["posts"]) == 1
        state["timeout"] = False
    state["usage"]["total_tokens"] = invalid
    suffix = {"sync": "/interpret", "async": "/interpret/async", "manual-retry": "/interpret/retry"}[mode]
    response = client.post(path + suffix, headers=headers)
    assert response.status_code == (200 if mode == "sync" else 202)
    detail = client.get(path, headers=headers)
    assert detail.status_code == 200
    payload = detail.json()
    assert payload["status"] == "completed"
    assert payload["interpretation"]["overall_interpretation"] == "Usable answer"
    assert payload["interpretation"]["summary"] == "Usable summary"
    assert payload["interpretation_run"]["state"] == "completed"
    assert payload["interpretation_run"]["can_retry"] is False
    expected_requests = 2 if mode == "manual-retry" else 1
    assert payload["interpretation_run"]["requests_started"] == len(state["posts"]) == expected_requests
    reservations = db_session.scalars(select(AIBudgetReservation)).all()
    assert len(reservations) == expected_requests
    assert all(item.actual_micros is None for item in reservations)
    assert store.snapshot()["daily_reserved_micros"] == sum(item.estimated_micros for item in reservations)
    assert store.snapshot()["daily_spent_micros"] == 0
    assert CANARY not in caplog.text and CANARY not in detail.text
    assert client.post(path + "/interpret/async", headers=headers).status_code == 200
    assert client.post(path + "/interpret/retry", headers=headers).status_code == 200
    assert len(state["posts"]) == expected_requests
