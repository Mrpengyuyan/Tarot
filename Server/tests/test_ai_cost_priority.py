from __future__ import annotations

import asyncio
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timedelta, timezone

import httpx
import pytest

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.crud import prediction as prediction_crud
from app.models.record import PredictionStatus
from app.services.budget_store import DatabaseBudgetStore
from app.services.coze_service import CozeBudgetExceededError
from app.services.coze_service import CozeService, CozeTimeoutError


class _NoopBudget:
    def reserve(self, **kwargs):
        return "reservation"

    def settle(self, reservation_id, *, actual_micros):
        return None


def _login_and_draw(client, spread_id: int, username: str) -> int:
    assert client.post(
        "/api/v1/register",
        json={"username": username, "email": f"{username}@example.com", "password": "password123"},
    ).status_code == 200
    response = client.post("/api/v1/login", data={"username": username, "password": "password123"})
    assert response.status_code == 200
    csrf = response.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(settings.CSRF_COOKIE_NAME)
    if csrf:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf})
    response = client.post(
        "/api/v1/records/",
        json={"spread_type_id": spread_id, "question": "接下来做什么？", "question_type": "general"},
    )
    assert response.status_code == 200
    prediction_id = response.json()["id"]
    assert client.post(f"/api/v1/records/{prediction_id}/draw").status_code == 200
    return prediction_id


def test_single_attempt_timeout_does_not_try_fallback_endpoint(monkeypatch):
    posts = []

    class FakeClient:
        def __init__(self, **kwargs):
            assert kwargs["follow_redirects"] is False

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, url, **kwargs):
            posts.append(url)
            raise httpx.ReadTimeout("lost response")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=_NoopBudget())
    service.api_key = "test-key"
    service.budget_guard_enabled = True
    service.max_retries = 2

    with pytest.raises(CozeTimeoutError):
        asyncio.run(service.send_messages_and_wait(
            messages=[{"role": "user", "content": "hello"}], single_attempt=True,
        ))
    assert len(posts) == 1


def test_single_attempt_works_without_python_311_timeout(monkeypatch):
    service = CozeService(budget_store=_NoopBudget())
    service.api_key = "test-key"

    async def chat_once(**kwargs):
        return {"text": "answer", "usage": {}, "cost_usd": 0.0}

    monkeypatch.setattr(service, "_chat_once", chat_once)
    monkeypatch.delattr(asyncio, "timeout", raising=False)

    result = asyncio.run(service.send_messages_and_wait(
        messages=[{"role": "user", "content": "hello"}], single_attempt=True,
    ))
    assert result["text"] == "answer"


def test_single_attempt_wall_clock_timeout_is_reported(monkeypatch):
    service = CozeService(budget_store=_NoopBudget())
    service.api_key = "test-key"

    async def chat_once(**kwargs):
        return {"text": "answer", "usage": {}, "cost_usd": 0.0}

    async def expire(awaitable, *, timeout):
        assert timeout == 90
        awaitable.close()
        raise asyncio.TimeoutError

    monkeypatch.setattr(service, "_chat_once", chat_once)
    monkeypatch.setattr(asyncio, "wait_for", expire)

    with pytest.raises(CozeTimeoutError, match="90-second wall-clock limit"):
        asyncio.run(service.send_messages_and_wait(
            messages=[{"role": "user", "content": "hello"}], single_attempt=True,
        ))


def test_single_attempt_rejects_redirect_without_following(monkeypatch):
    posts = []

    class FakeClient:
        def __init__(self, **kwargs):
            assert kwargs["follow_redirects"] is False

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, url, **kwargs):
            posts.append(url)
            return httpx.Response(302, headers={"location": "https://another.example/chat"})

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=_NoopBudget())
    service.api_key = "test-key"
    service.budget_guard_enabled = True

    with pytest.raises(Exception, match="HTTP 302"):
        asyncio.run(service.send_messages_and_wait(
            messages=[{"role": "user", "content": "hello"}], single_attempt=True,
        ))
    assert len(posts) == 1


def test_single_attempt_fails_closed_when_budget_guard_disabled(monkeypatch):
    class FakeClient:
        def __init__(self, **kwargs):
            raise AssertionError("paid request must not reach network")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=_NoopBudget())
    service.api_key = "test-key"
    service.budget_guard_enabled = False
    with pytest.raises(CozeBudgetExceededError):
        asyncio.run(service._request_chat_completion(
            payload={"model": service.chat_model, "messages": [], "max_tokens": 100},
            single_attempt=True,
        ))


def test_budget_is_reserved_before_send_marker_and_released_if_marker_fails(monkeypatch):
    events = []

    class FakeBudget:
        def reserve(self, **kwargs):
            events.append("reserve")
            return "reservation"

        def settle(self, reservation_id, *, actual_micros):
            events.append(("settle", actual_micros))

    class FakeClient:
        def __init__(self, **kwargs):
            events.append("client")

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, url, **kwargs):
            events.append("post")

    def reject_marker():
        events.append("mark")
        raise RuntimeError("claim lost")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=FakeBudget())
    service.api_key = "test-key"
    with pytest.raises(RuntimeError, match="claim lost"):
        asyncio.run(service._request_chat_completion(
            payload={"model": service.chat_model, "messages": [], "max_tokens": 100},
            single_attempt=True, before_send=reject_marker,
        ))
    assert events == ["reserve", "mark", ("settle", 0)]


def test_successful_paid_response_survives_local_post_response_budget_warning(monkeypatch):
    service = CozeService()
    service.api_key = "test-key"
    service.budget_guard_enabled = False

    async def chat_once(**kwargs):
        return {"text": "answer", "usage": {}, "cost_usd": 0.01}

    monkeypatch.setattr(service, "_chat_once", chat_once)
    monkeypatch.setattr(service, "_register_usage_and_cost", lambda **kwargs: (_ for _ in ()).throw(
        CozeBudgetExceededError("local estimate exceeded")
    ))
    result = asyncio.run(service.send_messages_and_wait(
        messages=[{"role": "user", "content": "hello"}], single_attempt=True,
    ))
    assert result["text"] == "answer"


def test_paid_response_survives_budget_settlement_failure(monkeypatch):
    class BrokenSettlement(_NoopBudget):
        def settle(self, reservation_id, *, actual_micros):
            raise RuntimeError("budget database temporarily unavailable")

    class FakeClient:
        def __init__(self, **kwargs):
            pass

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, url, **kwargs):
            return httpx.Response(200, json={
                "choices": [{"message": {"content": "answer"}}],
                "usage": {"prompt_tokens": 5, "completion_tokens": 8},
            })

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    service = CozeService(budget_store=BrokenSettlement())
    service.api_key = "test-key"
    result = asyncio.run(service._request_chat_completion(
        payload={"model": service.chat_model, "messages": [], "max_tokens": 100},
        single_attempt=True,
    ))
    assert result["choices"][0]["message"]["content"] == "answer"


def test_paid_response_survives_budget_snapshot_failure(monkeypatch):
    class BrokenSnapshot(_NoopBudget):
        def snapshot(self):
            raise RuntimeError("budget database temporarily unavailable")

    service = CozeService(budget_store=BrokenSnapshot())
    service.api_key = "test-key"

    async def chat_once(**kwargs):
        return {"text": "answer", "usage": {}, "cost_usd": 0.01}

    monkeypatch.setattr(service, "_chat_once", chat_once)
    result = asyncio.run(service.send_messages_and_wait(
        messages=[{"role": "user", "content": "hello"}], single_attempt=True,
    ))
    assert result["text"] == "answer"


def test_double_manual_retry_claim_only_one_wins(client, db_session, db_session_factory, seeded_spread_and_cards):
    prediction_id = _login_and_draw(client, seeded_spread_and_cards["spread_id"], "double_retry")
    prediction = prediction_crud.get_prediction_by_id(db_session, prediction_id)
    prediction.status = PredictionStatus.FAILED
    prediction.ai_run_state = "retry_required"
    prediction.ai_requests_started = 1
    db_session.commit()
    now = datetime.now(timezone.utc)

    def claim_once():
        with db_session_factory() as db:
            return prediction_crud.claim_manual_interpretation_retry(db, prediction_id, now)

    with ThreadPoolExecutor(max_workers=2) as executor:
        results = list(executor.map(lambda _: claim_once(), range(2)))
    assert sorted(results) == [False, True]


def test_stale_possible_send_requires_manual_retry_and_fences_old_worker(
    client, db_session, seeded_spread_and_cards,
):
    prediction_id = _login_and_draw(client, seeded_spread_and_cards["spread_id"], "stale_send")
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session, prediction_id, now=now, stale_before=now - timedelta(seconds=300), max_attempts=3,
    )
    assert prediction_crud.mark_interpretation_request_sending(db_session, prediction_id, now)
    prediction = prediction_crud.get_prediction_by_id(db_session, prediction_id)
    prediction.ai_request_started_at = now - timedelta(seconds=121)
    db_session.commit()

    detail = client.get(f"/api/v1/records/{prediction_id}").json()["interpretation_run"]
    assert detail["state"] == "retry_required"
    assert detail["requests_started"] == 1
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/async").status_code == 409
    assert not prediction_crud.interpretation_claim_is_current(db_session, prediction_id, now)


def test_possible_send_cannot_be_released_as_unused_budget_claim(
    client, db_session, seeded_spread_and_cards,
):
    prediction_id = _login_and_draw(client, seeded_spread_and_cards["spread_id"], "no_release_after_send")
    now = datetime.now(timezone.utc)
    assert prediction_crud.claim_interpretation_generation(
        db_session, prediction_id, now=now, stale_before=now - timedelta(seconds=300), max_attempts=3,
    )
    assert prediction_crud.mark_interpretation_request_sending(db_session, prediction_id, now)
    assert not prediction_crud.release_interpretation_generation(db_session, prediction_id, now)
    db_session.expire_all()
    prediction = prediction_crud.get_prediction_by_id(db_session, prediction_id)
    assert prediction.ai_run_state == "sending"
    assert prediction.ai_requests_started == 1


def test_failed_reading_needs_explicit_retry_and_is_capped(client, seeded_spread_and_cards, monkeypatch):
    prediction_id = _login_and_draw(client, seeded_spread_and_cards["spread_id"], "paid_retry")
    calls = []

    async def fake_interpretation(db, prediction, cards_data, user_context=None, before_send=None):
        assert before_send is not None
        before_send()
        calls.append(prediction.id)
        raise CozeTimeoutError("lost response")

    monkeypatch.setattr(records_endpoint.tarot_interpretation_service.ai_service, "is_configured", lambda: True)
    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "create_interpretation", fake_interpretation)

    assert client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true").status_code == 504
    detail = client.get(f"/api/v1/records/{prediction_id}").json()["interpretation_run"]
    assert detail["state"] == "retry_required"
    assert detail["requests_started"] == 1
    assert detail["can_retry"] is True
    assert client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true").status_code == 409
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/async").status_code == 409
    assert calls == [prediction_id]

    assert client.post(f"/api/v1/records/{prediction_id}/interpret/retry").status_code == 202
    assert len(calls) == 2
    detail = client.get(f"/api/v1/records/{prediction_id}").json()["interpretation_run"]
    assert detail["state"] == "exhausted"
    assert detail["requests_started"] == 2
    assert detail["can_retry"] is False
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/retry").status_code == 429
    assert len(calls) == 2


def test_real_interpretation_service_timeout_never_auto_sends_again(
    client, db_session_factory, seeded_spread_and_cards, monkeypatch,
):
    prediction_id = _login_and_draw(client, seeded_spread_and_cards["spread_id"], "real_service_timeout")
    posts = []

    class FakeClient:
        def __init__(self, **kwargs):
            assert kwargs["follow_redirects"] is False

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args):
            return None

        async def post(self, url, **kwargs):
            posts.append(url)
            raise httpx.ReadTimeout("response lost")

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", FakeClient)
    ai_service = CozeService(budget_store=DatabaseBudgetStore(db_session_factory))
    ai_service.api_key = "test-key"
    ai_service.budget_guard_enabled = True
    monkeypatch.setattr(records_endpoint.tarot_interpretation_service, "ai_service", ai_service)

    assert client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true").status_code == 504
    assert len(posts) == 1
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/async").status_code == 409
    assert client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true").status_code == 409
    assert len(posts) == 1
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/retry").status_code == 202
    assert len(posts) == 2
    detail = client.get(f"/api/v1/records/{prediction_id}").json()["interpretation_run"]
    assert detail["state"] == "exhausted"
    assert detail["requests_started"] == 2
    assert detail["can_retry"] is False
    assert client.post(f"/api/v1/records/{prediction_id}/interpret/retry").status_code == 429
    assert len(posts) == 2
