from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor
import asyncio

import pytest

from app.core.config import Settings
from app.services.budget_store import BudgetLimitExceeded, DatabaseBudgetStore
from app.services.coze_service import CozeBudgetExceededError, CozeHttpStatusError, CozeService


def test_paid_ai_budget_has_safe_defaults_without_env_file():
    assert Settings.model_fields["AI_BUDGET_GUARD_ENABLED"].default is True
    assert Settings.model_fields["AI_DAILY_BUDGET_USD"].default == 5.0
    assert Settings.model_fields["AI_MONTHLY_BUDGET_USD"].default == 120.0


def test_paid_request_fails_closed_without_persistent_budget_store(monkeypatch):
    class FakeClient:
        def __init__(self, **kwargs):
            raise AssertionError("The network must not be reached")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService()
    service.api_key = "test-key"
    service.budget_guard_enabled = True

    with pytest.raises(CozeBudgetExceededError):
        asyncio.run(
            service._request_chat_completion(
                payload={
                    "model": service.chat_model,
                    "messages": [{"role": "user", "content": "hello"}],
                    "max_tokens": 900,
                }
            )
        )


def test_reservation_survives_new_service_instance(db_session_factory):
    first = DatabaseBudgetStore(db_session_factory)
    reservation_id = first.reserve(
        model="deepseek-chat",
        estimated_micros=800,
        daily_limit_micros=1000,
        monthly_limit_micros=2000,
    )

    second = DatabaseBudgetStore(db_session_factory)
    with pytest.raises(BudgetLimitExceeded):
        second.reserve(
            model="deepseek-chat",
            estimated_micros=300,
            daily_limit_micros=1000,
            monthly_limit_micros=2000,
        )

    second.settle(reservation_id, actual_micros=200)
    assert second.snapshot()["daily_reserved_micros"] == 0
    assert second.snapshot()["daily_spent_micros"] == 200
    assert second.reserve(
        model="deepseek-chat",
        estimated_micros=800,
        daily_limit_micros=1000,
        monthly_limit_micros=2000,
    )


def test_concurrent_reservations_cannot_both_take_remaining_budget(db_session_factory):
    store = DatabaseBudgetStore(db_session_factory)

    def reserve_once():
        try:
            return store.reserve(
                model="deepseek-chat",
                estimated_micros=700,
                daily_limit_micros=1000,
                monthly_limit_micros=2000,
            )
        except BudgetLimitExceeded:
            return None

    with ThreadPoolExecutor(max_workers=2) as pool:
        results = list(pool.map(lambda _: reserve_once(), range(2)))

    assert sum(result is not None for result in results) == 1
    assert store.snapshot()["daily_reserved_micros"] == 700


def test_provider_request_is_blocked_before_network_when_budget_is_insufficient(
    db_session_factory, monkeypatch
):
    calls = []

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, *args, **kwargs):
            calls.append(args)
            raise AssertionError("Network request should not be sent")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=DatabaseBudgetStore(db_session_factory))
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 0.000001
    service.monthly_budget_usd = 0.000001

    with pytest.raises(CozeBudgetExceededError):
        asyncio.run(
            service._request_chat_completion(
                payload={
                    "model": service.chat_model,
                    "messages": [{"role": "user", "content": "hello"}],
                    "max_tokens": 900,
                }
            )
        )
    assert calls == []


def test_send_messages_preserves_budget_denial_without_fallback(db_session_factory, monkeypatch):
    class FakeClient:
        def __init__(self, **kwargs):
            raise AssertionError("The network must not be reached")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=DatabaseBudgetStore(db_session_factory))
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 0.000001
    service.monthly_budget_usd = 0.000001

    with pytest.raises(CozeBudgetExceededError):
        asyncio.run(
            service.send_messages_and_wait(messages=[{"role": "user", "content": "hello"}])
        )


def test_provider_response_settles_persistent_reservation(db_session_factory, monkeypatch):
    class FakeResponse:
        status_code = 200

        def json(self):
            return {
                "choices": [{"message": {"content": "answer"}}],
                "usage": {"prompt_tokens": 20, "completion_tokens": 10},
            }

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, *args, **kwargs):
            return FakeResponse()

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 1
    service.monthly_budget_usd = 1

    asyncio.run(
        service._request_chat_completion(
            payload={
                "model": service.chat_model,
                "messages": [{"role": "user", "content": "hello"}],
                "max_tokens": 900,
            }
        )
    )

    snapshot = store.snapshot()
    assert snapshot["daily_reserved_micros"] == 0
    assert snapshot["daily_spent_micros"] > 0


@pytest.mark.parametrize("reported_usage", [{}, {"total_tokens": 30}])
def test_response_without_token_usage_keeps_reservation(db_session_factory, monkeypatch, reported_usage):
    class FakeResponse:
        status_code = 200

        def json(self):
            return {"choices": [{"message": {"content": "answer"}}], "usage": reported_usage}

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, *args, **kwargs):
            return FakeResponse()

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 1
    service.monthly_budget_usd = 1

    asyncio.run(
        service._request_chat_completion(
            payload={
                "model": service.chat_model,
                "messages": [{"role": "user", "content": "hello"}],
                "max_tokens": 900,
            }
        )
    )

    assert store.snapshot()["daily_reserved_micros"] > 0


def test_rejected_provider_request_releases_reservation(db_session_factory, monkeypatch):
    class FakeResponse:
        status_code = 401
        text = "Unauthorized"

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, *args, **kwargs):
            return FakeResponse()

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "invalid-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 1
    service.monthly_budget_usd = 1

    with pytest.raises(CozeHttpStatusError):
        asyncio.run(
            service._request_chat_completion(
                payload={
                    "model": service.chat_model,
                    "messages": [{"role": "user", "content": "hello"}],
                    "max_tokens": 900,
                }
            )
        )

    assert store.snapshot()["daily_reserved_micros"] == 0
    assert store.snapshot()["daily_spent_micros"] == 0


def test_budget_denial_after_uncertain_upstream_attempt_is_marked_as_started(
    db_session_factory, monkeypatch
):
    class FakeResponse:
        status_code = 503
        text = "Service unavailable"

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, *args, **kwargs):
            return FakeResponse()

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.daily_budget_usd = 0.001
    service.monthly_budget_usd = 1

    with pytest.raises(CozeBudgetExceededError) as denied:
        asyncio.run(
            service._request_chat_completion(
                payload={
                    "model": service.chat_model,
                    "messages": [{"role": "user", "content": "hello"}],
                    "max_tokens": 900,
                }
            )
        )

    assert denied.value.request_started is True
