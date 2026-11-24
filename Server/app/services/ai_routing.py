from __future__ import annotations

import json
from typing import Any, Dict, List, Optional, Sequence

from app.schemas.limits import MAX_DATABASE_INTEGER
from app.services.ai_errors import CozeHttpStatusError, CozeRequestError, CozeTimeoutError


def extract_usage(result: Dict[str, Any]) -> Dict[str, int]:
    unknown = {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}
    usage = result.get("usage")
    if not isinstance(usage, dict):
        return unknown
    try:
        # Any invalid count makes usage unknown; keep the full budget reservation.
        counts = {}
        for field in ("prompt_tokens", "input_tokens", "completion_tokens", "output_tokens", "total_tokens"):
            value = usage.get(field)
            if value is None:
                continue
            if isinstance(value, bool) or not isinstance(value, (int, float, str)):
                return unknown
            if isinstance(value, float) and not value.is_integer():
                return unknown
            if not 0 <= int(value) <= MAX_DATABASE_INTEGER:
                return unknown
            counts[field] = int(value)
        # Both sides must be known; aliases must agree, including explicit zeroes.
        breakdown = []
        for fields in (("prompt_tokens", "input_tokens"), ("completion_tokens", "output_tokens")):
            values = {counts[field] for field in fields if field in counts}
            if len(values) != 1:
                return unknown
            breakdown.append(values.pop())
        prompt_tokens, completion_tokens = breakdown
        if prompt_tokens + completion_tokens > MAX_DATABASE_INTEGER:
            return unknown
        total_tokens = counts.get("total_tokens", prompt_tokens + completion_tokens)
        if total_tokens != prompt_tokens + completion_tokens:
            return unknown
    except (TypeError, ValueError, OverflowError):
        return unknown
    return {
        "prompt_tokens": prompt_tokens,
        "completion_tokens": completion_tokens,
        "total_tokens": total_tokens,
    }


def estimate_cost_usd(
    *,
    model: str,
    usage: Dict[str, int],
    reasoner_model: str,
    chat_input_cost_per_m: float,
    chat_output_cost_per_m: float,
    reasoner_input_cost_per_m: float,
    reasoner_output_cost_per_m: float,
) -> float:
    if model == reasoner_model:
        input_cost = reasoner_input_cost_per_m
        output_cost = reasoner_output_cost_per_m
    else:
        input_cost = chat_input_cost_per_m
        output_cost = chat_output_cost_per_m

    prompt_tokens = int(usage.get("prompt_tokens") or 0)
    completion_tokens = int(usage.get("completion_tokens") or 0)

    return (prompt_tokens / 1_000_000) * input_cost + (completion_tokens / 1_000_000) * output_cost


def is_retryable_exception(
    exc: Exception,
    *,
    retry_on_timeout: bool,
    retry_on_429: bool,
    retry_on_5xx: bool,
) -> bool:
    if isinstance(exc, CozeTimeoutError):
        return retry_on_timeout
    if isinstance(exc, CozeHttpStatusError):
        if exc.status_code == 429:
            return retry_on_429
        if exc.status_code >= 500:
            return retry_on_5xx
        return False
    if isinstance(exc, CozeRequestError):
        return True
    return False


def retry_delay_seconds(
    attempt_index: int,
    *,
    retry_backoff_ms: int,
    retry_backoff_factor: float,
    retry_max_backoff_ms: int,
) -> float:
    if retry_backoff_ms <= 0:
        return 0.0
    factor = max(retry_backoff_factor, 1.0)
    delay_ms = retry_backoff_ms * (factor**attempt_index)
    delay_ms = min(delay_ms, max(retry_max_backoff_ms, retry_backoff_ms))
    return float(delay_ms) / 1000.0


def normalize_content(content: Any) -> str:
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


def extract_answer_text(result: Dict[str, Any]) -> str:
    choices = result.get("choices")
    if not isinstance(choices, list) or not choices:
        return ""
    first = choices[0] if isinstance(choices[0], dict) else {}
    message = first.get("message") if isinstance(first, dict) else {}
    if not isinstance(message, dict):
        return ""

    content = normalize_content(message.get("content"))
    if content:
        return content
    return normalize_content(first.get("text"))


def looks_like_json(text: str) -> bool:
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


def is_complex_request(
    *,
    question: Optional[str],
    user_context: Optional[str],
    messages: List[Dict[str, Any]],
    trigger_chars: int,
    trigger_keywords: Sequence[str],
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
            if len(" ".join(text_parts)) >= trigger_chars:
                break

    merged = " ".join(text_parts).lower().strip()
    if not merged:
        return False
    if len(merged) >= max(trigger_chars, 1):
        return True
    return any(keyword in merged for keyword in trigger_keywords)


def should_try_reasoner(
    *,
    chat_text: str,
    expect_json: bool,
    is_complex: bool,
    chat_failed: bool,
    reasoner_allowed: bool,
    enable_reasoner_fallback: bool,
    chat_model: str,
    reasoner_model: str,
) -> bool:
    if not enable_reasoner_fallback or not reasoner_allowed:
        return False
    if not reasoner_model or reasoner_model == chat_model:
        return False
    if chat_failed:
        return True
    if not chat_text.strip():
        return True
    if expect_json and not looks_like_json(chat_text):
        return True
    if is_complex and len(chat_text.strip()) < 240:
        return True
    return False
