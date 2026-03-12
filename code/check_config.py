#!/usr/bin/env python3
"""Simple runtime config check for local debugging."""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

try:
    from app.core.config import settings

    print("Config check")
    print(f"project_name: {settings.PROJECT_NAME}")
    print(f"api_prefix: {settings.API_V1_STR}")
    print(f"database_url: {settings.DATABASE_URL}")

    print("\nCoze config:")
    print("api_key: configured" if settings.COZE_API_KEY else "api_key: missing")
    print(f"bot_id: {settings.COZE_BOT_ID or 'missing'}")
    print(f"base_url: {settings.COZE_BASE_URL or 'missing'}")
    print(f"timeout: {settings.COZE_TIMEOUT}")
    print(f"coze_ready: {bool(settings.COZE_API_KEY and settings.COZE_BOT_ID)}")
except ImportError as exc:
    print(f"Import error: {exc}")
except Exception as exc:
    print(f"Config check failed: {exc}")
