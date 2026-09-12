"""Regression tests for CozeService budget guard — especially H-4 (soft cap discarding valid responses)."""
from __future__ import annotations

import asyncio
import threading

import pytest

from app.services.coze_service import (
    CozeBudgetExceededError,
    CozeService,
)


class TestBudgetWindowReset:
    """Verify daily/monthly budget windows reset correctly."""

    def test_daily_window_resets_on_new_day(self):
        service = CozeService()
        service.budget_guard_enabled = True
        service.daily_budget_usd = 10.0

        # Simulate yesterday's spend
        service._budget_state["day"] = "2025-01-01"
        service._budget_state["daily_spend_usd"] = 9.99

        # Access budget_status → triggers reset
        status = service.budget_status()
        assert status["daily_spend_usd"] == 0.0
        assert status["day"] != "2025-01-01"

    def test_monthly_window_resets_on_new_month(self):
        service = CozeService()
        service.budget_guard_enabled = True
        service.monthly_budget_usd = 100.0

        service._budget_state["month"] = "2025-01"
        service._budget_state["monthly_spend_usd"] = 99.0
        service._budget_state["total_calls"] = 50
        service._budget_state["reasoner_calls"] = 5

        status = service.budget_status()
        assert status["monthly_spend_usd"] == 0.0
        assert status["total_calls"] == 0
        assert status["reasoner_calls"] == 0

    def test_same_day_does_not_reset(self):
        service = CozeService()
        service.budget_guard_enabled = True
        now = service._now_utc()
        day_key = now.strftime("%Y-%m-%d")

        service._budget_state["day"] = day_key
        service._budget_state["daily_spend_usd"] = 5.0

        status = service.budget_status()
        assert status["daily_spend_usd"] == 5.0


class TestBudgetSoftCapRegression:
    """H-4 regression: _register_usage_and_cost should record usage even when soft cap is exceeded."""

    def test_soft_cap_records_usage_before_raising(self):
        """After soft cap raise, usage counters must still reflect the call."""
        service = CozeService()
        service.budget_guard_enabled = True
        service.request_soft_cap_usd = 0.0001
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")

        # This should raise CozeBudgetExceededError due to soft cap
        with pytest.raises(CozeBudgetExceededError):
            service._register_usage_and_cost(
                model=service.chat_model,
                usage={"prompt_tokens": 1000, "completion_tokens": 1000, "total_tokens": 2000},
                cost_usd=0.01,  # way above 0.0001 cap
            )

        # But the usage should still be recorded
        status = service.budget_status()
        assert status["total_calls"] >= 1
        assert status["daily_spend_usd"] >= 0.01
        assert status["last_request_cost_usd"] == 0.01

    def test_daily_budget_exhaustion_recorded_before_raising(self):
        """When daily budget is exhausted by a call, usage is still recorded."""
        service = CozeService()
        service.budget_guard_enabled = True
        service.daily_budget_usd = 0.005
        service.request_soft_cap_usd = 0  # Disable soft cap
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")

        with pytest.raises(CozeBudgetExceededError, match="daily"):
            service._register_usage_and_cost(
                model=service.chat_model,
                usage={"prompt_tokens": 500, "completion_tokens": 500, "total_tokens": 1000},
                cost_usd=0.01,
            )

        status = service.budget_status()
        assert status["total_calls"] == 1
        assert status["daily_spend_usd"] == 0.01

    def test_below_soft_cap_does_not_raise(self):
        """Costs below soft cap should not raise."""
        service = CozeService()
        service.budget_guard_enabled = True
        service.request_soft_cap_usd = 1.0
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")

        # Should not raise
        service._register_usage_and_cost(
            model=service.chat_model,
            usage={"prompt_tokens": 100, "completion_tokens": 100, "total_tokens": 200},
            cost_usd=0.0001,
        )

        status = service.budget_status()
        assert status["total_calls"] == 1
        assert status["daily_spend_usd"] == pytest.approx(0.0001, abs=1e-6)


class TestBudgetGuardExhaustionBlocking:
    """Pre-request budget guard should block when limits are already exceeded."""

    def test_blocks_when_daily_budget_exhausted(self):
        service = CozeService()
        service.budget_guard_enabled = True
        service.daily_budget_usd = 5.0
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")
        service._budget_state["daily_spend_usd"] = 5.0  # exactly at limit

        with pytest.raises(CozeBudgetExceededError, match="daily"):
            service._ensure_budget_allows_request(service.chat_model)

    def test_blocks_when_monthly_budget_exhausted(self):
        service = CozeService()
        service.budget_guard_enabled = True
        service.monthly_budget_usd = 50.0
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")
        service._budget_state["monthly_spend_usd"] = 50.0

        with pytest.raises(CozeBudgetExceededError, match="monthly"):
            service._ensure_budget_allows_request(service.chat_model)

    def test_allows_when_under_budget(self):
        service = CozeService()
        service.budget_guard_enabled = True
        service.daily_budget_usd = 5.0
        service.monthly_budget_usd = 50.0
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")
        service._budget_state["daily_spend_usd"] = 1.0
        service._budget_state["monthly_spend_usd"] = 10.0

        # Should not raise
        service._ensure_budget_allows_request(service.chat_model)


class TestCostEstimation:
    """Verify cost estimation math for chat and reasoner models."""

    def test_chat_model_cost_estimation(self):
        service = CozeService()
        service.chat_input_cost_per_m = 1.0   # $1 per 1M input tokens
        service.chat_output_cost_per_m = 2.0  # $2 per 1M output tokens

        usage = {"prompt_tokens": 1000, "completion_tokens": 500}
        cost = service._estimate_cost_usd(model=service.chat_model, usage=usage)
        expected = (1000 / 1_000_000) * 1.0 + (500 / 1_000_000) * 2.0
        assert cost == pytest.approx(expected, abs=1e-9)

    def test_reasoner_model_cost_estimation(self):
        service = CozeService()
        service.reasoner_input_cost_per_m = 4.0
        service.reasoner_output_cost_per_m = 8.0

        usage = {"prompt_tokens": 2000, "completion_tokens": 1000}
        cost = service._estimate_cost_usd(model=service.reasoner_model, usage=usage)
        expected = (2000 / 1_000_000) * 4.0 + (1000 / 1_000_000) * 8.0
        assert cost == pytest.approx(expected, abs=1e-9)

    def test_zero_tokens_cost_is_zero(self):
        service = CozeService()
        usage = {"prompt_tokens": 0, "completion_tokens": 0}
        cost = service._estimate_cost_usd(model=service.chat_model, usage=usage)
        assert cost == 0.0


class TestBudgetThreadSafety:
    """Basic thread safety verification for budget state mutations."""

    def test_concurrent_register_usage_does_not_crash(self):
        service = CozeService()
        service.budget_guard_enabled = False  # Disable guards to avoid CozeBudgetExceededError
        now = service._now_utc()
        service._budget_state["day"] = now.strftime("%Y-%m-%d")
        service._budget_state["month"] = now.strftime("%Y-%m")

        errors = []

        def register_once():
            try:
                service._register_usage_and_cost(
                    model=service.chat_model,
                    usage={"prompt_tokens": 10, "completion_tokens": 10, "total_tokens": 20},
                    cost_usd=0.0001,
                )
            except Exception as exc:
                errors.append(exc)

        threads = [threading.Thread(target=register_once) for _ in range(20)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()

        assert len(errors) == 0
        status = service.budget_status()
        assert status["total_calls"] == 20
