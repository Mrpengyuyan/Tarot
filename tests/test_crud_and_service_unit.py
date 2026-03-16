from __future__ import annotations

import asyncio

from app.crud.prediction import create_prediction_with_stats
from app.crud.user import create_user
from app.models.spread import SpreadType
from app.schemas.prediction import PredictionCreate
from app.schemas.user import UserCreate
from app.services.coze_service import CozeBudgetExceededError, CozeService, CozeTimeoutError
from app.services.tarot_service import TarotInterpretationService


def test_create_prediction_with_stats_updates_user_and_spread_counters(db_session):
    user = create_user(
        db_session,
        UserCreate(
            username="counter_user",
            email="counter_user@example.com",
            password="password123",
        ),
    )
    spread = SpreadType(
        name="Counter Spread",
        name_en="Counter Spread",
        description="Counter spread description",
        card_count=1,
        positions=[{"position": 1, "name": "Now", "meaning": "Current state"}],
        is_active=True,
        usage_count=0,
    )
    db_session.add(spread)
    db_session.commit()
    db_session.refresh(spread)

    prediction = create_prediction_with_stats(
        db_session,
        user_id=user.id,
        prediction_create=PredictionCreate(
            spread_type_id=spread.id,
            question="Verify counter update behavior",
            question_type="general",
        ),
    )

    db_session.refresh(user)
    db_session.refresh(spread)

    assert prediction.id is not None
    assert user.prediction_count == 1
    assert spread.usage_count == 1


def test_parse_interpretation_payload_handles_nested_json_and_plain_text():
    service = TarotInterpretationService()

    nested_json_response = """
    ```json
    {
      "interpretation": {
        "overall_interpretation": "Overall trend is positive; proceed step by step.",
        "advice": "Prioritize high-impact tasks first",
        "key_themes": ["pace", "focus"],
        "confidence_score": 1.5
      }
    }
    ```
    """
    parsed_nested = service._parse_interpretation_payload(nested_json_response)
    assert parsed_nested["overall_interpretation"] == "Overall trend is positive; proceed step by step."
    assert parsed_nested["advice"] == "Prioritize high-impact tasks first"
    assert parsed_nested["confidence_score"] == 1.0
    assert parsed_nested["key_themes"] is not None
    assert "pace" in parsed_nested["key_themes"]
    assert "focus" in parsed_nested["key_themes"]

    plain_text_response = "Model output that is not JSON."
    parsed_plain = service._parse_interpretation_payload(plain_text_response)
    assert parsed_plain["overall_interpretation"] == plain_text_response
    assert parsed_plain["confidence_score"] is None


def test_deepseek_primary_route_without_fallback_when_json_is_valid():
    service = CozeService()
    service.api_key = "test-key"
    service.enable_reasoner_fallback = True

    calls = []

    async def fake_chat_once(*, model, messages, max_wait_time, expect_json):  # noqa: ANN001
        del messages, max_wait_time, expect_json
        calls.append(model)
        return {"text": '{"overall_interpretation":"ok"}', "raw": {}}

    service._chat_once = fake_chat_once  # type: ignore[method-assign]

    result = asyncio.run(
        service.send_messages_and_wait(
            messages=[{"role": "user", "content": "Short question"}],
            question="Short question",
            expect_json=True,
        )
    )

    assert calls == [service.chat_model]
    assert result["model_used"] == service.chat_model
    assert result["fallback_used"] is False


def test_deepseek_reasoner_conditional_fallback_on_complex_non_json_output():
    service = CozeService()
    service.api_key = "test-key"
    service.enable_reasoner_fallback = True
    service.reasoner_trigger_chars = 10

    calls = []

    async def fake_chat_once(*, model, messages, max_wait_time, expect_json):  # noqa: ANN001
        del messages, max_wait_time, expect_json
        calls.append(model)
        if model == service.chat_model:
            return {"text": "plain text response", "raw": {}}
        return {"text": '{"overall_interpretation":"reasoner ok"}', "raw": {}}

    service._chat_once = fake_chat_once  # type: ignore[method-assign]

    result = asyncio.run(
        service.send_messages_and_wait(
            messages=[{"role": "user", "content": "Why is this so complex?"}],
            question="Why is this so complex?",
            user_context="Please provide deep analysis and tradeoff discussion.",
            expect_json=True,
        )
    )

    assert calls == [service.chat_model, service.reasoner_model]
    assert result["model_used"] == service.reasoner_model
    assert result["fallback_used"] is True


def test_deepseek_chat_once_retries_on_timeout_then_succeeds():
    service = CozeService()
    service.api_key = "test-key"
    service.max_retries = 1
    service.retry_backoff_ms = 0
    service.retry_on_timeout = True

    calls = {"count": 0}

    async def fake_request_chat_completion(*, payload, timeout):  # noqa: ANN001
        del payload, timeout
        calls["count"] += 1
        if calls["count"] == 1:
            raise CozeTimeoutError("timeout")
        return {
            "choices": [{"message": {"content": "ok"}}],
            "usage": {"prompt_tokens": 10, "completion_tokens": 20, "total_tokens": 30},
        }

    service._request_chat_completion = fake_request_chat_completion  # type: ignore[method-assign]

    result = asyncio.run(
        service._chat_once(
            model=service.chat_model,
            messages=[{"role": "user", "content": "ping"}],
            max_wait_time=10,
            expect_json=False,
        )
    )

    assert calls["count"] == 2
    assert result["text"] == "ok"
    assert result["usage"]["total_tokens"] == 30


def test_budget_guard_blocks_reasoner_when_ratio_exceeds_limit():
    service = CozeService()
    service.budget_guard_enabled = True
    service.reasoner_max_percent = 0.20
    service.reasoner_ratio_warmup_calls = 0
    now = service._now_utc()
    service._budget_state["day"] = now.strftime("%Y-%m-%d")
    service._budget_state["month"] = now.strftime("%Y-%m")
    service._budget_state["total_calls"] = 5
    service._budget_state["reasoner_calls"] = 1

    try:
        service._ensure_budget_allows_request(service.reasoner_model)
        assert False, "Expected CozeBudgetExceededError"
    except CozeBudgetExceededError:
        pass


def test_budget_guard_allows_reasoner_during_warmup_window():
    service = CozeService()
    service.budget_guard_enabled = True
    service.reasoner_max_percent = 0.20
    service.reasoner_ratio_warmup_calls = 20
    now = service._now_utc()
    service._budget_state["day"] = now.strftime("%Y-%m-%d")
    service._budget_state["month"] = now.strftime("%Y-%m")
    service._budget_state["total_calls"] = 0
    service._budget_state["reasoner_calls"] = 0

    service._ensure_budget_allows_request(service.reasoner_model)
