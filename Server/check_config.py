#!/usr/bin/env python3
"""Simple runtime config check for local debugging and production readiness verification."""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path
from typing import List, Optional, Sequence
from urllib.parse import urlsplit, urlunsplit

SERVER_DIR = Path(__file__).resolve().parent
DEFAULT_DESKTOP_CONFIG = (
    SERVER_DIR.parent
    / "UnityClient"
    / "TarotUnity"
    / "Assets"
    / "StreamingAssets"
    / "tarot_desktop_config.json"
)

if str(SERVER_DIR) not in sys.path:
    sys.path.insert(0, str(SERVER_DIR))

WEAK_SECRET_KEYS = {
    "",
    "change-me-in-production",
    "change-this-secret-key-in-production",
    "your-super-secret-key-here-change-in-production",
    "dev-local-secret-key-change-me-before-production",
    "replace-with-a-long-random-secret-at-least-32-chars",
}
LOCALHOST_HOSTS = {"localhost", "127.0.0.1", "0.0.0.0", "::1"}


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


def _is_localhost_url(url: str) -> bool:
    parsed = urlsplit((url or "").strip())
    host = (parsed.hostname or "").strip().lower()
    return host in LOCALHOST_HOSTS


def collect_production_issues(
    cfg,
    desktop_config_path: Optional[Path] = DEFAULT_DESKTOP_CONFIG,
) -> List[str]:
    """Return a list of production-readiness issues for backend settings and optional desktop config."""
    issues: List[str] = []

    secret = str(getattr(cfg, "SECRET_KEY", "") or "")
    if secret in WEAK_SECRET_KEYS or len(secret) < 32:
        issues.append("SECRET_KEY is weak or shorter than 32 characters.")

    if not getattr(cfg, "AUTH_COOKIE_SECURE", False):
        issues.append("AUTH_COOKIE_SECURE must be true for production HTTPS deployments.")

    if getattr(cfg, "DEBUG", False):
        issues.append("DEBUG must be false in production.")

    if getattr(cfg, "ALLOW_MOCK_AI_FALLBACK", False):
        issues.append("ALLOW_MOCK_AI_FALLBACK must be false in production.")

    if not getattr(cfg, "AI_BUDGET_GUARD_ENABLED", True):
        issues.append("AI_BUDGET_GUARD_ENABLED should be true when serving paid AI interpretations.")

    if not getattr(cfg, "DEEPSEEK_API_KEY", None):
        issues.append("DEEPSEEK_API_KEY is not configured.")

    try:
        _ = cfg.trusted_proxy_networks
    except ValueError as exc:
        issues.append(f"TRUSTED_PROXY_IPS is invalid: {exc}")

    origins = list(getattr(cfg, "cors_origins_list", []) or [])
    if not origins:
        issues.append("CORS_ORIGINS must not be empty.")
    for origin in origins:
        if origin == "*":
            issues.append("CORS_ORIGINS must not contain wildcard '*'.")
        elif _is_localhost_url(origin):
            issues.append(f"CORS_ORIGINS still includes localhost origin: {origin}")

    if desktop_config_path is not None and desktop_config_path.exists():
        try:
            payload = json.loads(desktop_config_path.read_text(encoding="utf-8"))
            backend_url = str(payload.get("backendBaseUrl") or "").strip()
            parsed = urlsplit(backend_url)
            if parsed.scheme.lower() != "https":
                issues.append(f"Desktop config backendBaseUrl is not HTTPS: {backend_url or '<empty>'}")
            if _is_localhost_url(backend_url):
                issues.append(f"Desktop config backendBaseUrl still points to localhost: {backend_url}")
        except Exception as exc:
            issues.append(f"Desktop config could not be parsed ({desktop_config_path}): {exc}")

    return issues


def _print_summary(cfg) -> None:
    print("Config check")
    print(f"project_name: {cfg.PROJECT_NAME}")
    print(f"api_prefix: {cfg.API_V1_STR}")
    print(f"database_url: {mask_database_url(cfg.DATABASE_URL)}")
    print(f"environment: {cfg.ENVIRONMENT}")
    print(f"debug: {cfg.DEBUG}")
    docs_enabled = cfg.DEBUG or str(cfg.ENVIRONMENT).strip().lower() in {"dev", "development", "local"}
    print(f"docs_enabled: {docs_enabled}")
    print(f"trusted_proxy_ips: {cfg.TRUSTED_PROXY_IPS or '<none>'}")

    print("\nDeepSeek config:")
    print("api_key: configured" if cfg.DEEPSEEK_API_KEY else "api_key: missing")
    print(f"base_url: {cfg.DEEPSEEK_BASE_URL or 'missing'}")
    print(f"timeout: {cfg.DEEPSEEK_TIMEOUT}")
    print(f"chat_model: {cfg.DEEPSEEK_CHAT_MODEL}")
    print(f"reasoner_model: {cfg.DEEPSEEK_REASONER_MODEL}")
    print(f"reasoner_fallback: {cfg.DEEPSEEK_ENABLE_REASONER_FALLBACK}")
    print(f"deepseek_ready: {bool(cfg.DEEPSEEK_API_KEY)}")
    print("\nAI runtime policy:")
    print(f"max_retries: {cfg.AI_MAX_RETRIES}")
    print(f"retry_backoff_ms: {cfg.AI_RETRY_BACKOFF_MS}")
    print(f"retry_backoff_factor: {cfg.AI_RETRY_BACKOFF_FACTOR}")
    print(f"retry_on_timeout: {cfg.AI_RETRY_ON_TIMEOUT}")
    print(f"retry_on_429: {cfg.AI_RETRY_ON_429}")
    print(f"retry_on_5xx: {cfg.AI_RETRY_ON_5XX}")
    print(f"max_output_tokens: {cfg.AI_MAX_OUTPUT_TOKENS}")
    print(f"budget_guard: {cfg.AI_BUDGET_GUARD_ENABLED}")
    print(f"daily_budget_usd: {cfg.AI_DAILY_BUDGET_USD}")
    print(f"monthly_budget_usd: {cfg.AI_MONTHLY_BUDGET_USD}")
    print(f"request_soft_cap_usd: {cfg.AI_REQUEST_SOFT_CAP_USD}")
    print(f"reasoner_max_percent: {cfg.AI_REASONER_MAX_PERCENT}")
    if cfg.AI_BUDGET_GUARD_ENABLED:
        print("budget_guard_note: database-backed reservations shared by workers; keep enabled for paid interpretations.")


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Inspect Tarot API runtime config and production readiness.")
    parser.add_argument(
        "--production-check",
        action="store_true",
        help="Fail with exit code 1 if any production-readiness check fails.",
    )
    parser.add_argument(
        "--desktop-config",
        type=Path,
        default=DEFAULT_DESKTOP_CONFIG,
        help="Path to Unity tarot_desktop_config.json to verify during --production-check.",
    )
    args = parser.parse_args(argv)

    try:
        from app.core.config import settings
    except ImportError as exc:
        print(f"Import error: {exc}")
        return 1
    except Exception as exc:
        print(f"Config check failed: {exc}")
        return 1

    _print_summary(settings)

    if args.production_check:
        issues = collect_production_issues(settings, desktop_config_path=args.desktop_config)
        print("\nProduction readiness check:")
        if not issues:
            print("PASS: all production readiness checks succeeded.")
            return 0
        for issue in issues:
            print(f"FAIL: {issue}")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
