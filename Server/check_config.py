#!/usr/bin/env python3
"""Simple runtime config check for local debugging."""

import os
import sys
from urllib.parse import urlsplit, urlunsplit

sys.path.insert(0, os.path.dirname(__file__))

try:
    from app.core.config import settings

    def mask_database_url(url: str) -> str:
        try:
            parsed = urlsplit(url)
            if not parsed.netloc or "@" not in parsed.netloc:
                return url
            credentials, host = parsed.netloc.rsplit("@", 1)
            username = credentials.split(":", 1)[0]
            safe_netloc = f"{username}:***@{host}" if username else f"***@{host}"
            return urlunsplit((parsed.scheme, safe_netloc, parsed.path, parsed.query, parsed.fragment))
        except Exception:
            return "<invalid_database_url>"

    print("Config check")
    print(f"project_name: {settings.PROJECT_NAME}")
    print(f"api_prefix: {settings.API_V1_STR}")
    print(f"database_url: {mask_database_url(settings.DATABASE_URL)}")
    print(f"environment: {settings.ENVIRONMENT}")
    print(f"debug: {settings.DEBUG}")
    docs_enabled = settings.DEBUG or str(settings.ENVIRONMENT).strip().lower() in {"dev", "development", "local"}
    print(f"docs_enabled: {docs_enabled}")

    print("\nDeepSeek config:")
    print("api_key: configured" if settings.DEEPSEEK_API_KEY else "api_key: missing")
    print(f"base_url: {settings.DEEPSEEK_BASE_URL or 'missing'}")
    print(f"timeout: {settings.DEEPSEEK_TIMEOUT}")
    print(f"chat_model: {settings.DEEPSEEK_CHAT_MODEL}")
    print(f"reasoner_model: {settings.DEEPSEEK_REASONER_MODEL}")
    print(f"reasoner_fallback: {settings.DEEPSEEK_ENABLE_REASONER_FALLBACK}")
    print(f"deepseek_ready: {bool(settings.DEEPSEEK_API_KEY)}")
    print("\nAI runtime policy:")
    print(f"max_retries: {settings.AI_MAX_RETRIES}")
    print(f"retry_backoff_ms: {settings.AI_RETRY_BACKOFF_MS}")
    print(f"retry_backoff_factor: {settings.AI_RETRY_BACKOFF_FACTOR}")
    print(f"retry_on_timeout: {settings.AI_RETRY_ON_TIMEOUT}")
    print(f"retry_on_429: {settings.AI_RETRY_ON_429}")
    print(f"retry_on_5xx: {settings.AI_RETRY_ON_5XX}")
    print(f"max_output_tokens: {settings.AI_MAX_OUTPUT_TOKENS}")
    print(f"budget_guard: {settings.AI_BUDGET_GUARD_ENABLED}")
    print(f"daily_budget_usd: {settings.AI_DAILY_BUDGET_USD}")
    print(f"monthly_budget_usd: {settings.AI_MONTHLY_BUDGET_USD}")
    print(f"request_soft_cap_usd: {settings.AI_REQUEST_SOFT_CAP_USD}")
    print(f"reasoner_max_percent: {settings.AI_REASONER_MAX_PERCENT}")
    if settings.AI_BUDGET_GUARD_ENABLED:
        print("budget_guard_note: process-local counters; keep disabled for multi-instance deployment.")
except ImportError as exc:
    print(f"Import error: {exc}")
except Exception as exc:
    print(f"Config check failed: {exc}")
