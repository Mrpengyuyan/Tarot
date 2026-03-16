from __future__ import annotations

import asyncio

import pytest

from app.services.coze_service import CozeError, CozeService, CozeTimeoutError


def _set_reasoner_blocked_budget(service: CozeService) -> None:
    service.budget_guard_enabled = True
    service.reasoner_max_percent = 0.20
    service.reasoner_ratio_warmup_calls = 0
    now = service._now_utc()
    service._budget_state["day"] = now.strftime("%Y-%m-%d")
    service._budget_state["month"] = now.strftime("%Y-%m")
    service._budget_state["total_calls"] = 5
    service._budget_state["reasoner_calls"] = 1


def test_fallback_blocked_by_budget_returns_primary_when_primary_has_text():
    service = CozeService()
    service.api_key = "test-key"
    service.enable_reasoner_fallback = True
    service.reasoner_trigger_chars = 1
    _set_reasoner_blocked_budget(service)

    calls: list[str] = []

    async def fake_chat_once(*, model, messages, max_wait_time, expect_json):  # noqa: ANN001
        del messages, max_wait_time, expect_json
        calls.append(model)
        return {"text": "non-json-primary-text", "raw": {}}

    service._chat_once = fake_chat_once  # type: ignore[method-assign]

    result = asyncio.run(
        service.send_messages_and_wait(
            messages=[{"role": "user", "content": "Why and how?"}],
            question="Why and how?",
            user_context="Need analysis",
            expect_json=True,
        )
    )

    assert calls == [service.chat_model]
    assert result["model_used"] == service.chat_model
    assert result["fallback_used"] is False
    assert result["text"] == "non-json-primary-text"


def test_primary_failure_and_fallback_blocked_raises_clear_error():
    service = CozeService()
    service.api_key = "test-key"
    service.enable_reasoner_fallback = True
    service.reasoner_trigger_chars = 1
    _set_reasoner_blocked_budget(service)

    async def fake_chat_once(*, model, messages, max_wait_time, expect_json):  # noqa: ANN001
        del model, messages, max_wait_time, expect_json
        raise CozeTimeoutError("primary timeout")

    service._chat_once = fake_chat_once  # type: ignore[method-assign]

    with pytest.raises(CozeError) as exc_info:
        asyncio.run(
            service.send_messages_and_wait(
                messages=[{"role": "user", "content": "Why and how?"}],
                question="Why and how?",
                user_context="Need analysis",
                expect_json=True,
            )
        )

    message = str(exc_info.value).lower()
    assert "fallback blocked" in message
    assert "primary timeout" in message

