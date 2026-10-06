import asyncio
import json
import logging
import math
import threading
from datetime import datetime, timezone
from typing import Any, Callable, Dict, List, Optional

import httpx

from app.core.config import settings
from app.db.session import SessionLocal
from app.services.budget_store import BudgetLimitExceeded, DatabaseBudgetStore

logger = logging.getLogger(__name__)


class CozeError(Exception):
    """Compatibility error type retained for existing imports."""


class CozeTimeoutError(CozeError):
    """Timeout error from upstream model provider."""


class CozeRequestError(CozeError):
    """Network/request-level upstream error."""


class CozeHttpStatusError(CozeError):
    """HTTP status error from upstream provider."""

    def __init__(self, status_code: int, detail: str):
        super().__init__(detail)
        self.status_code = int(status_code)


class CozeBudgetExceededError(CozeError):
    """Raised when budget guard blocks a request."""

    def __init__(self, message: str, *, request_started: bool = False) -> None:
        super().__init__(message)
        self.request_started = request_started


class CozeService:
    """
    Backward-compatible service wrapper.

    Historical name: CozeService
    Current implementation: DeepSeek Chat Completions API

    Strategy:
    - Primary route: deepseek-chat
    - Conditional fallback: deepseek-reasoner
    """

    def __init__(self, budget_store: Optional[DatabaseBudgetStore] = None) -> None:
        self.budget_store = budget_store
        self.provider_name = "deepseek"
        # Prefer DeepSeek vars; allow legacy COZE_API_KEY as a temporary fallback.
        self.api_key = settings.DEEPSEEK_API_KEY or settings.COZE_API_KEY
        self.base_url = (settings.DEEPSEEK_BASE_URL or "https://api.deepseek.com").rstrip("/")
        self.timeout = float(settings.DEEPSEEK_TIMEOUT or settings.COZE_TIMEOUT or 65.0)
        self.chat_endpoint = (settings.DEEPSEEK_CHAT_ENDPOINT or "chat/completions").strip("/")
        self.chat_model = (settings.DEEPSEEK_CHAT_MODEL or "deepseek-chat").strip()
        self.reasoner_model = (settings.DEEPSEEK_REASONER_MODEL or "deepseek-reasoner").strip()
        self.trust_env_proxy = bool(settings.DEEPSEEK_TRUST_ENV_PROXY)
        self.enable_reasoner_fallback = bool(settings.DEEPSEEK_ENABLE_REASONER_FALLBACK)
        self.reasoner_trigger_chars = int(settings.DEEPSEEK_REASONER_TRIGGER_CHARS or 120)
        self.reasoner_keywords = settings.deepseek_reasoner_keywords
        self.force_json_output = bool(settings.DEEPSEEK_FORCE_JSON_OUTPUT)
        self.max_output_tokens = max(0, int(settings.AI_MAX_OUTPUT_TOKENS or 0))
        self.reproducible_mode = bool(settings.AI_REPRODUCIBLE_MODE)
        self.chat_temperature = max(0.0, min(2.0, float(settings.AI_CHAT_TEMPERATURE or 0.0)))
        self.reasoner_temperature = max(0.0, min(2.0, float(settings.AI_REASONER_TEMPERATURE or 0.0)))

        if self.reproducible_mode:
            self.enable_reasoner_fallback = False
            self.chat_temperature = 0.0
            self.reasoner_temperature = 0.0

        # Retry policy
        self.max_retries = max(0, int(settings.AI_MAX_RETRIES or 0))
        self.retry_backoff_ms = max(0, int(settings.AI_RETRY_BACKOFF_MS or 0))
        self.retry_backoff_factor = float(settings.AI_RETRY_BACKOFF_FACTOR or 1.0)
        self.retry_max_backoff_ms = max(0, int(settings.AI_RETRY_MAX_BACKOFF_MS or 0))
        self.retry_on_timeout = bool(settings.AI_RETRY_ON_TIMEOUT)
        self.retry_on_429 = bool(settings.AI_RETRY_ON_429)
        self.retry_on_5xx = bool(settings.AI_RETRY_ON_5XX)

        # Budget and cost guard
        self.budget_guard_enabled = bool(settings.AI_BUDGET_GUARD_ENABLED)
        self.daily_budget_usd = max(0.0, float(settings.AI_DAILY_BUDGET_USD or 0.0))
        self.monthly_budget_usd = max(0.0, float(settings.AI_MONTHLY_BUDGET_USD or 0.0))
        self.request_soft_cap_usd = max(0.0, float(settings.AI_REQUEST_SOFT_CAP_USD or 0.0))
        self.reasoner_max_percent = max(0.0, min(1.0, float(settings.AI_REASONER_MAX_PERCENT or 0.0)))
        self.reasoner_ratio_warmup_calls = max(0, int(settings.AI_REASONER_RATIO_WARMUP_CALLS or 0))
        self.disable_reasoner_when_budget_high = bool(settings.AI_DISABLE_REASONER_WHEN_BUDGET_HIGH)
        self.budget_alert_threshold = max(0.0, min(1.0, float(settings.AI_BUDGET_ALERT_THRESHOLD or 0.85)))

        # Cost table (USD / 1M tokens)
        self.chat_input_cost_per_m = max(0.0, float(settings.AI_COST_CHAT_INPUT_PER_M or 0.0))
        self.chat_output_cost_per_m = max(0.0, float(settings.AI_COST_CHAT_OUTPUT_PER_M or 0.0))
        self.reasoner_input_cost_per_m = max(0.0, float(settings.AI_COST_REASONER_INPUT_PER_M or 0.0))
        self.reasoner_output_cost_per_m = max(0.0, float(settings.AI_COST_REASONER_OUTPUT_PER_M or 0.0))

        self._budget_lock = threading.Lock()
        self._budget_state: Dict[str, Any] = {
            "day": "",
            "month": "",
            "daily_spend_usd": 0.0,
            "monthly_spend_usd": 0.0,
            "total_calls": 0,
            "reasoner_calls": 0,
            "last_model_used": None,
            "last_request_cost_usd": 0.0,
        }
        if self.budget_guard_enabled and self.budget_store is None:
            logger.warning(
                "AI budget guard is enabled with process-local counters. "
                "Pass a database budget store before making paid requests."
            )

    def is_configured(self) -> bool:
        return bool(self.api_key)

    def _headers(self) -> Dict[str, str]:
        return {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

    @staticmethod
    def _exception_text(exc: Exception) -> str:
        message = str(exc).strip()
        if message:
            return message
        return repr(exc)

    def _candidate_chat_endpoints(self) -> List[str]:
        endpoints: List[str] = []
        for endpoint in [self.chat_endpoint, "chat/completions", "v1/chat/completions"]:
            normalized = endpoint.strip("/")
            if normalized and normalized not in endpoints:
                endpoints.append(normalized)
        return endpoints

    @staticmethod
    def _now_utc() -> datetime:
        return datetime.now(timezone.utc)

    def _reset_budget_windows_if_needed_locked(self) -> None:
        now = self._now_utc()
        day_key = now.strftime("%Y-%m-%d")
        month_key = now.strftime("%Y-%m")

        if self._budget_state["day"] != day_key:
            self._budget_state["day"] = day_key
            self._budget_state["daily_spend_usd"] = 0.0

        if self._budget_state["month"] != month_key:
            self._budget_state["month"] = month_key
            self._budget_state["monthly_spend_usd"] = 0.0
            self._budget_state["total_calls"] = 0
            self._budget_state["reasoner_calls"] = 0

    def _budget_snapshot_locked(self) -> Dict[str, Any]:
        total_calls = int(self._budget_state["total_calls"])
        reasoner_calls = int(self._budget_state["reasoner_calls"])
        ratio = (reasoner_calls / total_calls) if total_calls else 0.0

        daily_spend = float(self._budget_state["daily_spend_usd"])
        monthly_spend = float(self._budget_state["monthly_spend_usd"])

        daily_remaining = (self.daily_budget_usd - daily_spend) if self.daily_budget_usd > 0 else None
        monthly_remaining = (self.monthly_budget_usd - monthly_spend) if self.monthly_budget_usd > 0 else None

        return {
            "enabled": self.budget_guard_enabled,
            "day": self._budget_state["day"],
            "month": self._budget_state["month"],
            "daily_spend_usd": daily_spend,
            "monthly_spend_usd": monthly_spend,
            "daily_budget_usd": self.daily_budget_usd if self.daily_budget_usd > 0 else None,
            "monthly_budget_usd": self.monthly_budget_usd if self.monthly_budget_usd > 0 else None,
            "daily_remaining_usd": daily_remaining,
            "monthly_remaining_usd": monthly_remaining,
            "total_calls": total_calls,
            "reasoner_calls": reasoner_calls,
            "reasoner_ratio": ratio,
            "reasoner_ratio_limit": self.reasoner_max_percent,
            "last_model_used": self._budget_state.get("last_model_used"),
            "last_request_cost_usd": float(self._budget_state.get("last_request_cost_usd", 0.0)),
        }

    def budget_status(self) -> Dict[str, Any]:
        with self._budget_lock:
            self._reset_budget_windows_if_needed_locked()
            status = self._budget_snapshot_locked()
        if self.budget_guard_enabled and self.budget_store is not None:
            persisted = self.budget_store.snapshot()
            daily_used = (persisted["daily_spent_micros"] + persisted["daily_reserved_micros"]) / 1_000_000
            monthly_used = (persisted["monthly_spent_micros"] + persisted["monthly_reserved_micros"]) / 1_000_000
            status["daily_spend_usd"] = persisted["daily_spent_micros"] / 1_000_000
            status["monthly_spend_usd"] = persisted["monthly_spent_micros"] / 1_000_000
            status["daily_reserved_usd"] = persisted["daily_reserved_micros"] / 1_000_000
            status["monthly_reserved_usd"] = persisted["monthly_reserved_micros"] / 1_000_000
            status["daily_remaining_usd"] = max(0.0, self.daily_budget_usd - daily_used) if self.daily_budget_usd else None
            status["monthly_remaining_usd"] = max(0.0, self.monthly_budget_usd - monthly_used) if self.monthly_budget_usd else None
        return status

    def _estimate_max_request_cost_micros(self, payload: Dict[str, Any]) -> int:
        output_cap = int(payload.get("max_tokens") or 0)
        if output_cap <= 0:
            raise CozeBudgetExceededError("AI_MAX_OUTPUT_TOKENS must be positive when budget guard is enabled")
        model = str(payload.get("model") or "")
        if model == self.reasoner_model:
            input_price = self.reasoner_input_cost_per_m
            output_price = self.reasoner_output_cost_per_m
        else:
            input_price = self.chat_input_cost_per_m
            output_price = self.chat_output_cost_per_m
        if input_price <= 0 or output_price <= 0:
            raise CozeBudgetExceededError("AI cost prices must be positive when budget guard is enabled")
        input_bound = len(json.dumps(payload.get("messages") or [], ensure_ascii=False).encode("utf-8")) + 1024
        return max(1, math.ceil(input_bound * input_price + output_cap * output_price))

    def _is_hard_budget_exhausted_locked(self) -> Optional[str]:
        if self.daily_budget_usd > 0 and float(self._budget_state["daily_spend_usd"]) >= self.daily_budget_usd:
            return "daily"
        if self.monthly_budget_usd > 0 and float(self._budget_state["monthly_spend_usd"]) >= self.monthly_budget_usd:
            return "monthly"
        return None

    def _projected_reasoner_ratio_locked(self) -> float:
        projected_total = int(self._budget_state["total_calls"]) + 1
        projected_reasoner = int(self._budget_state["reasoner_calls"]) + 1
        return projected_reasoner / projected_total

    def _ensure_budget_allows_request(self, model: str) -> None:
        if not self.budget_guard_enabled:
            return

        with self._budget_lock:
            self._reset_budget_windows_if_needed_locked()
            exhausted_scope = self._is_hard_budget_exhausted_locked()
            if exhausted_scope:
                raise CozeBudgetExceededError(f"AI {exhausted_scope} budget exhausted.")

            if model == self.reasoner_model:
                total_calls = int(self._budget_state["total_calls"])
                if self.reasoner_max_percent > 0 and total_calls >= self.reasoner_ratio_warmup_calls:
                    projected_ratio = self._projected_reasoner_ratio_locked()
                    if projected_ratio > self.reasoner_max_percent:
                        raise CozeBudgetExceededError(
                            f"Reasoner ratio limit reached ({projected_ratio:.2%} > {self.reasoner_max_percent:.2%})."
                        )

                if self.disable_reasoner_when_budget_high:
                    daily_spend = float(self._budget_state["daily_spend_usd"])
                    monthly_spend = float(self._budget_state["monthly_spend_usd"])
                    daily_ratio = (daily_spend / self.daily_budget_usd) if self.daily_budget_usd > 0 else 0.0
                    monthly_ratio = (monthly_spend / self.monthly_budget_usd) if self.monthly_budget_usd > 0 else 0.0
                    if max(daily_ratio, monthly_ratio) >= self.budget_alert_threshold:
                        raise CozeBudgetExceededError(
                            "Reasoner fallback disabled due to high budget usage."
                        )

    def _register_usage_and_cost(self, *, model: str, usage: Dict[str, int], cost_usd: float) -> None:
        with self._budget_lock:
            self._reset_budget_windows_if_needed_locked()
            self._budget_state["total_calls"] = int(self._budget_state["total_calls"]) + 1
            if model == self.reasoner_model:
                self._budget_state["reasoner_calls"] = int(self._budget_state["reasoner_calls"]) + 1
            self._budget_state["daily_spend_usd"] = float(self._budget_state["daily_spend_usd"]) + float(cost_usd)
            self._budget_state["monthly_spend_usd"] = float(self._budget_state["monthly_spend_usd"]) + float(cost_usd)
            self._budget_state["last_model_used"] = model
            self._budget_state["last_request_cost_usd"] = float(cost_usd)

            if self.budget_guard_enabled:
                if self.request_soft_cap_usd > 0 and float(cost_usd) > self.request_soft_cap_usd:
                    raise CozeBudgetExceededError(
                        f"Request soft cap exceeded: {cost_usd:.6f} > {self.request_soft_cap_usd:.6f} USD."
                    )
                exhausted_scope = self._is_hard_budget_exhausted_locked()
                if exhausted_scope:
                    raise CozeBudgetExceededError(
                        f"AI {exhausted_scope} budget exceeded after request."
                    )

    def _extract_usage(self, result: Dict[str, Any]) -> Dict[str, int]:
        usage = result.get("usage")
        if not isinstance(usage, dict):
            usage = {}
        prompt_tokens = int(usage.get("prompt_tokens") or usage.get("input_tokens") or 0)
        completion_tokens = int(usage.get("completion_tokens") or usage.get("output_tokens") or 0)
        total_tokens = int(usage.get("total_tokens") or (prompt_tokens + completion_tokens))
        if prompt_tokens < 0:
            prompt_tokens = 0
        if completion_tokens < 0:
            completion_tokens = 0
        if total_tokens < 0:
            total_tokens = prompt_tokens + completion_tokens
        return {
            "prompt_tokens": prompt_tokens,
            "completion_tokens": completion_tokens,
            "total_tokens": total_tokens,
        }

    def _estimate_cost_usd(self, *, model: str, usage: Dict[str, int]) -> float:
        if model == self.reasoner_model:
            input_cost = self.reasoner_input_cost_per_m
            output_cost = self.reasoner_output_cost_per_m
        else:
            input_cost = self.chat_input_cost_per_m
            output_cost = self.chat_output_cost_per_m

        prompt_tokens = int(usage.get("prompt_tokens") or 0)
        completion_tokens = int(usage.get("completion_tokens") or 0)

        return (prompt_tokens / 1_000_000) * input_cost + (completion_tokens / 1_000_000) * output_cost

    def _is_retryable_exception(self, exc: Exception) -> bool:
        if isinstance(exc, CozeTimeoutError):
            return self.retry_on_timeout
        if isinstance(exc, CozeHttpStatusError):
            if exc.status_code == 429:
                return self.retry_on_429
            if exc.status_code >= 500:
                return self.retry_on_5xx
            return False
        if isinstance(exc, CozeRequestError):
            return True
        return False

    def _retry_delay_seconds(self, attempt_index: int) -> float:
        if self.retry_backoff_ms <= 0:
            return 0.0
        factor = max(self.retry_backoff_factor, 1.0)
        delay_ms = self.retry_backoff_ms * (factor**attempt_index)
        delay_ms = min(delay_ms, max(self.retry_max_backoff_ms, self.retry_backoff_ms))
        return float(delay_ms) / 1000.0

    async def _request_chat_completion(
        self,
        *,
        payload: Dict[str, Any],
        timeout: Optional[float] = None,
        single_attempt: bool = False,
        before_send: Optional[Callable[[], None]] = None,
    ) -> Dict[str, Any]:
        if not self.is_configured():
            raise CozeError("DeepSeek is not configured. Please set DEEPSEEK_API_KEY.")
        if single_attempt and not self.budget_guard_enabled:
            raise CozeBudgetExceededError("Persistent AI budget guard is required for paid interpretations")
        if self.budget_guard_enabled and self.budget_store is None:
            raise CozeBudgetExceededError("Persistent AI budget store is required for paid requests")

        request_timeout = float(timeout) if timeout else self.timeout
        last_error: Optional[Exception] = None
        request_started = False

        endpoints = [self.chat_endpoint] if single_attempt else self._candidate_chat_endpoints()
        for endpoint in endpoints:
            url = f"{self.base_url}/{endpoint}"
            reservation_id = None
            if self.budget_guard_enabled and self.budget_store is not None:
                try:
                    reservation_id = self.budget_store.reserve(
                        model=str(payload.get("model") or ""),
                        estimated_micros=self._estimate_max_request_cost_micros(payload),
                        daily_limit_micros=math.floor(self.daily_budget_usd * 1_000_000),
                        monthly_limit_micros=math.floor(self.monthly_budget_usd * 1_000_000),
                    )
                except BudgetLimitExceeded as exc:
                    raise CozeBudgetExceededError(str(exc), request_started=request_started) from exc
            try:
                if before_send is not None:
                    before_send()
                async with httpx.AsyncClient(
                    timeout=request_timeout,
                    follow_redirects=not single_attempt,
                    trust_env=self.trust_env_proxy,
                ) as client:
                    request_started = True
                    response = await client.post(url, headers=self._headers(), json=payload)
            except Exception as exc:
                if not isinstance(exc, httpx.HTTPError):
                    if reservation_id is not None:
                        self.budget_store.settle(reservation_id, actual_micros=0)
                    raise
                if isinstance(exc, httpx.TimeoutException):
                    last_error = CozeTimeoutError(
                        f"DeepSeek request timeout ({request_timeout}s) on endpoint '{endpoint}'"
                    )
                elif isinstance(exc, httpx.RequestError):
                    detail = self._exception_text(exc)
                    last_error = CozeRequestError(
                        f"DeepSeek request error on endpoint '{endpoint}' ({type(exc).__name__}): {detail}"
                    )
                else:
                    raise
                if single_attempt:
                    raise last_error from exc
                continue

            if response.status_code >= 400 or (single_attempt and response.status_code >= 300):
                if reservation_id is not None and response.status_code < 500:
                    self.budget_store.settle(reservation_id, actual_micros=0)
                detail = response.text[:500]
                last_error = CozeHttpStatusError(
                    int(response.status_code),
                    f"DeepSeek HTTP {response.status_code} on endpoint '{endpoint}': {detail}",
                )
                if single_attempt:
                    raise last_error
                continue

            try:
                result = response.json()
                if reservation_id is not None and isinstance(result, dict) and isinstance(result.get("usage"), dict):
                    usage = self._extract_usage(result)
                    if usage["prompt_tokens"] + usage["completion_tokens"] > 0:
                        actual_cost = self._estimate_cost_usd(model=str(payload.get("model") or ""), usage=usage)
                        try:
                            self.budget_store.settle(
                                reservation_id,
                                actual_micros=math.ceil(actual_cost * 1_000_000),
                            )
                        except Exception:
                            logger.exception("AI budget settlement failed; retaining the reserved amount")
                return result
            except json.JSONDecodeError:
                last_error = CozeError(
                    f"DeepSeek returned non-JSON response on endpoint '{endpoint}': {response.text[:200]}"
                )
                if single_attempt:
                    raise last_error
                continue

        if last_error:
            raise last_error
        raise CozeError("DeepSeek request failed for unknown reasons.")

    @staticmethod
    def _normalize_content(content: Any) -> str:
        if content is None:
            return ""
        if isinstance(content, str):
            return content.strip()
        if isinstance(content, list):
            chunks: List[str] = []
            for item in content:
                if isinstance(item, dict):
                    text = item.get("text")
                    if isinstance(text, str) and text.strip():
                        chunks.append(text.strip())
                elif isinstance(item, str) and item.strip():
                    chunks.append(item.strip())
            return "\n".join(chunks).strip()
        return str(content).strip()

    def _extract_answer_text(self, result: Dict[str, Any]) -> str:
        choices = result.get("choices")
        if not isinstance(choices, list) or not choices:
            return ""
        first = choices[0] if isinstance(choices[0], dict) else {}
        message = first.get("message") if isinstance(first, dict) else {}
        if not isinstance(message, dict):
            return ""

        content = self._normalize_content(message.get("content"))
        if content:
            return content
        return self._normalize_content(first.get("text"))

    @staticmethod
    def _looks_like_json(text: str) -> bool:
        raw = (text or "").strip()
        if not raw:
            return False
        if not (raw.startswith("{") and raw.endswith("}")):
            return False
        try:
            parsed = json.loads(raw)
            return isinstance(parsed, dict)
        except Exception:
            return False

    def _is_complex_request(
        self,
        *,
        question: Optional[str],
        user_context: Optional[str],
        messages: List[Dict[str, Any]],
    ) -> bool:
        text_parts: List[str] = []
        if question:
            text_parts.append(question)
        if user_context:
            text_parts.append(user_context)
        if not text_parts:
            for msg in messages:
                if not isinstance(msg, dict):
                    continue
                role = str(msg.get("role", "")).lower()
                if role != "user":
                    continue
                content = msg.get("content")
                if isinstance(content, str):
                    text_parts.append(content)
                if len(" ".join(text_parts)) >= self.reasoner_trigger_chars:
                    break

        merged = " ".join(text_parts).lower().strip()
        if not merged:
            return False
        if len(merged) >= max(self.reasoner_trigger_chars, 1):
            return True
        return any(keyword in merged for keyword in self.reasoner_keywords)

    def _should_try_reasoner(
        self,
        *,
        chat_text: str,
        expect_json: bool,
        is_complex: bool,
        chat_failed: bool,
        reasoner_allowed: bool,
    ) -> bool:
        if not self.enable_reasoner_fallback or not reasoner_allowed:
            return False
        if not self.reasoner_model or self.reasoner_model == self.chat_model:
            return False
        if chat_failed:
            return True
        if not chat_text.strip():
            return True
        if expect_json and not self._looks_like_json(chat_text):
            return True
        if is_complex and len(chat_text.strip()) < 240:
            return True
        return False

    async def _chat_once(
        self,
        *,
        model: str,
        messages: List[Dict[str, Any]],
        max_wait_time: int,
        expect_json: bool,
        single_attempt: bool = False,
        before_send: Optional[Callable[[], None]] = None,
    ) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "model": model,
            "messages": messages,
            "stream": False,
            "temperature": self.chat_temperature if model == self.chat_model else self.reasoner_temperature,
        }
        if self.max_output_tokens > 0:
            payload["max_tokens"] = self.max_output_tokens
        if expect_json and self.force_json_output:
            payload["response_format"] = {"type": "json_object"}

        max_attempts = 1 if single_attempt else self.max_retries + 1
        last_error: Optional[Exception] = None

        for attempt in range(max_attempts):
            try:
                request_kwargs = {"payload": payload, "timeout": max_wait_time + 5}
                if single_attempt:
                    request_kwargs.update(single_attempt=True, before_send=before_send)
                result = await self._request_chat_completion(**request_kwargs)
                usage = self._extract_usage(result)
                cost_usd = self._estimate_cost_usd(model=model, usage=usage)
                text = self._extract_answer_text(result)
                return {
                    "text": text,
                    "raw": result,
                    "usage": usage,
                    "cost_usd": cost_usd,
                }
            except CozeError as exc:
                if isinstance(exc, CozeBudgetExceededError) and last_error is not None:
                    exc.request_started = True
                if not single_attempt and expect_json and "response_format" in str(exc).lower() and "response_format" in payload:
                    payload.pop("response_format", None)
                    continue

                last_error = exc
                if attempt >= self.max_retries or not self._is_retryable_exception(exc):
                    break

                delay = self._retry_delay_seconds(attempt)
                if delay > 0:
                    logger.warning(
                        "Retrying model request (model=%s, attempt=%s/%s, delay=%.2fs): %s",
                        model,
                        attempt + 1,
                        max_attempts,
                        delay,
                        exc,
                    )
                    await asyncio.sleep(delay)
                else:
                    logger.warning(
                        "Retrying model request (model=%s, attempt=%s/%s): %s",
                        model,
                        attempt + 1,
                        max_attempts,
                        exc,
                    )
                continue

        if last_error:
            raise last_error
        raise CozeError("DeepSeek request failed before any response was received.")

    def _reasoner_allowed(self) -> tuple[bool, Optional[str]]:
        try:
            self._ensure_budget_allows_request(self.reasoner_model)
            return True, None
        except CozeBudgetExceededError as exc:
            return False, str(exc)

    async def send_messages_and_wait(
        self,
        *,
        messages: List[Dict[str, Any]],
        user_id: str = "user",
        max_wait_time: int = 60,
        question: Optional[str] = None,
        user_context: Optional[str] = None,
        expect_json: bool = False,
        single_attempt: bool = False,
        before_send: Optional[Callable[[], None]] = None,
    ) -> Dict[str, Any]:
        del user_id  # reserved for compatibility and future trace fields

        if not self.is_configured():
            raise CozeError("DeepSeek is not configured. Please set DEEPSEEK_API_KEY.")

        self._ensure_budget_allows_request(self.chat_model)
        if single_attempt:
            try:
                async with asyncio.timeout(90):
                    result = await self._chat_once(
                        model=self.chat_model,
                        messages=messages,
                        max_wait_time=85,
                        expect_json=expect_json,
                        single_attempt=True,
                        before_send=before_send,
                    )
            except TimeoutError as exc:
                raise CozeTimeoutError("DeepSeek request exceeded the 90-second wall-clock limit") from exc
            try:
                self._register_usage_and_cost(
                    model=self.chat_model,
                    usage=result.get("usage") or {},
                    cost_usd=float(result.get("cost_usd") or 0.0),
                )
            except CozeBudgetExceededError as exc:
                logger.warning("Budget threshold exceeded after paid response; preserving result: %s", exc)
            if not result["text"]:
                raise CozeError("DeepSeek primary request returned empty content.")
            try:
                budget = self.budget_status()
            except Exception:
                logger.exception("AI budget snapshot unavailable after paid response")
                budget = {}
            return {
                "text": result["text"],
                "model_used": self.chat_model,
                "model_version": None,
                "fallback_used": False,
                "usage": result["usage"],
                "cost_usd": result["cost_usd"],
                "budget": budget,
            }
        is_complex = self._is_complex_request(
            question=question,
            user_context=user_context,
            messages=messages,
        )
        reasoner_allowed = self.enable_reasoner_fallback
        reasoner_block_reason: Optional[str] = None

        chat_text = ""
        chat_result: Optional[Dict[str, Any]] = None
        chat_error: Optional[Exception] = None
        try:
            chat_result = await self._chat_once(
                model=self.chat_model,
                messages=messages,
                max_wait_time=max_wait_time,
                expect_json=expect_json,
            )
            chat_text = str(chat_result.get("text", ""))
        except CozeBudgetExceededError:
            raise
        except Exception as exc:
            chat_error = exc
            logger.warning("DeepSeek primary model '%s' failed: %s", self.chat_model, exc)
        else:
            try:
                self._register_usage_and_cost(
                    model=self.chat_model,
                    usage=chat_result.get("usage") or {},
                    cost_usd=float(chat_result.get("cost_usd") or 0.0),
                )
            except CozeBudgetExceededError as exc:
                logger.warning(
                    "Budget threshold exceeded after primary response; returning response anyway: %s",
                    exc,
                )

        reasoner_allowed, reasoner_block_reason = self._reasoner_allowed()

        if not self._should_try_reasoner(
            chat_text=chat_text,
            expect_json=expect_json,
            is_complex=is_complex,
            chat_failed=chat_error is not None,
            reasoner_allowed=reasoner_allowed,
        ):
            if chat_text:
                return {
                    "text": chat_text,
                    "model_used": self.chat_model,
                    "model_version": None,
                    "fallback_used": False,
                    "usage": (chat_result or {}).get("usage", {}),
                    "cost_usd": float((chat_result or {}).get("cost_usd") or 0.0),
                    "budget": self.budget_status(),
                }
            if chat_error:
                if reasoner_block_reason:
                    raise CozeError(
                        f"DeepSeek primary failed and reasoner fallback blocked: {reasoner_block_reason}; "
                        f"primary={chat_error}"
                    ) from chat_error
                raise CozeError(f"DeepSeek primary request failed: {chat_error}") from chat_error
            raise CozeError("DeepSeek primary request returned empty content.")

        logger.info(
            "Falling back to '%s' (complex=%s, expect_json=%s, primary_failed=%s)",
            self.reasoner_model,
            is_complex,
            expect_json,
            chat_error is not None,
        )

        try:
            self._ensure_budget_allows_request(self.reasoner_model)
        except CozeBudgetExceededError as exc:
            logger.warning("Reasoner fallback blocked by budget guard: %s", exc)
            if chat_text:
                return {
                    "text": chat_text,
                    "model_used": self.chat_model,
                    "model_version": None,
                    "fallback_used": False,
                    "usage": (chat_result or {}).get("usage", {}),
                    "cost_usd": float((chat_result or {}).get("cost_usd") or 0.0),
                    "budget": self.budget_status(),
                }
            if chat_error:
                raise CozeError(
                    f"DeepSeek primary failed and reasoner fallback blocked: {exc}; primary={chat_error}"
                ) from chat_error
            raise CozeError(f"Reasoner fallback blocked: {exc}") from exc

        try:
            reasoner_result = await self._chat_once(
                model=self.reasoner_model,
                messages=messages,
                max_wait_time=max_wait_time,
                expect_json=expect_json,
            )
            reasoner_text = str(reasoner_result.get("text", "")).strip()
            if reasoner_text:
                try:
                    self._register_usage_and_cost(
                        model=self.reasoner_model,
                        usage=reasoner_result.get("usage") or {},
                        cost_usd=float(reasoner_result.get("cost_usd") or 0.0),
                    )
                except CozeBudgetExceededError as exc:
                    logger.warning(
                        "Budget threshold exceeded after reasoner response; returning response anyway: %s",
                        exc,
                    )
                return {
                    "text": reasoner_text,
                    "model_used": self.reasoner_model,
                    "model_version": None,
                    "fallback_used": True,
                    "usage": reasoner_result.get("usage", {}),
                    "cost_usd": float(reasoner_result.get("cost_usd") or 0.0),
                    "budget": self.budget_status(),
                }
            logger.warning("DeepSeek reasoner fallback returned empty content; falling back to primary output.")
        except Exception as exc:
            logger.warning("DeepSeek reasoner fallback '%s' failed: %s", self.reasoner_model, exc)
            if chat_text:
                return {
                    "text": chat_text,
                    "model_used": self.chat_model,
                    "model_version": None,
                    "fallback_used": False,
                    "usage": (chat_result or {}).get("usage", {}),
                    "cost_usd": float((chat_result or {}).get("cost_usd") or 0.0),
                    "budget": self.budget_status(),
                }
            if chat_error:
                raise CozeError(
                    f"DeepSeek primary and fallback failed. primary={chat_error}; fallback={exc}"
                ) from exc
            raise CozeError(f"DeepSeek fallback request failed: {exc}") from exc

        if chat_text:
            return {
                "text": chat_text,
                "model_used": self.chat_model,
                "model_version": None,
                "fallback_used": False,
                "usage": (chat_result or {}).get("usage", {}),
                "cost_usd": float((chat_result or {}).get("cost_usd") or 0.0),
                "budget": self.budget_status(),
            }
        raise CozeError("DeepSeek fallback completed but returned empty content.")

    async def send_message_and_wait(
        self,
        message: str,
        user_id: str = "user",
        max_wait_time: int = 60,
    ) -> str:
        response = await self.send_messages_and_wait(
            messages=[{"role": "user", "content": message}],
            user_id=user_id,
            max_wait_time=max_wait_time,
            question=message,
            expect_json=False,
        )
        return str(response.get("text", "")).strip()

    async def health_check(self, *, deep: bool = False) -> Dict[str, Any]:
        budget = self.budget_status()
        if not self.is_configured():
            return {
                "status": "not_configured",
                "message": "DEEPSEEK_API_KEY is missing.",
                "is_healthy": False,
                "provider": self.provider_name,
                "details": {
                    "probe": "config_only",
                    "model_used": self.chat_model,
                    "fallback_used": False,
                },
                "budget": budget,
            }

        if not deep:
            return {
                "status": "healthy",
                "message": "DeepSeek credentials configured.",
                "is_healthy": True,
                "provider": self.provider_name,
                "details": {
                    "probe": "config_only",
                    "model_used": self.chat_model,
                    "fallback_used": False,
                },
                "budget": budget,
            }

        try:
            result = await self.send_messages_and_wait(
                messages=[
                    {"role": "system", "content": "You are a concise assistant."},
                    {"role": "user", "content": "Reply with one word: pong"},
                ],
                user_id="health_check",
                max_wait_time=15,
                expect_json=False,
            )
            sample = str(result.get("text", ""))[:120]
            return {
                "status": "healthy",
                "message": "DeepSeek is reachable.",
                "is_healthy": bool(sample),
                "provider": self.provider_name,
                "details": {
                    "sample": sample,
                    "model_used": result.get("model_used"),
                    "fallback_used": result.get("fallback_used", False),
                },
                "budget": self.budget_status(),
            }
        except Exception as exc:
            return {
                "status": "unhealthy",
                "message": "Provider probe failed",
                "is_healthy": False,
                "provider": self.provider_name,
                "details": {
                    "probe": "live",
                    "error_type": type(exc).__name__,
                },
                "budget": self.budget_status(),
            }


coze_service = CozeService(budget_store=DatabaseBudgetStore(SessionLocal))
