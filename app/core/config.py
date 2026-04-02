import os
import sys
from pathlib import Path
from typing import Any, List, Literal, Optional, cast

from pydantic import field_validator
from pydantic_settings import BaseSettings


# Force UTF-8 on Windows terminals to avoid garbled logs.
if sys.platform.startswith("win"):
    os.environ.setdefault("PYTHONIOENCODING", "utf-8")
    if hasattr(sys.stdout, "reconfigure"):
        try:
            cast(Any, sys.stdout).reconfigure(encoding="utf-8", errors="ignore")
            cast(Any, sys.stderr).reconfigure(encoding="utf-8", errors="ignore")
        except Exception:
            pass


BASE_DIR = Path(__file__).resolve().parents[2]
DEFAULT_SQLITE_PATH = (BASE_DIR / "tarot_game.db").resolve()
DEFAULT_DATABASE_URL = f"sqlite:///{DEFAULT_SQLITE_PATH.as_posix()}"
DEFAULT_DEV_SECRET_KEY = "dev-local-secret-key-change-me-before-production"


class Settings(BaseSettings):
    PROJECT_NAME: str = "Tarot Game API"
    PROJECT_VERSION: str = "1.0.0"
    API_V1_STR: str = "/api/v1"

    # Database
    POSTGRES_USER: str = "tarot_user"
    POSTGRES_PASSWORD: str = "tarot_password"
    POSTGRES_DB: str = "tarot_game"
    POSTGRES_SERVER: str = "localhost"
    POSTGRES_PORT: int = 5432
    DATABASE_URL: str = DEFAULT_DATABASE_URL

    # Security
    SECRET_KEY: str = DEFAULT_DEV_SECRET_KEY
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 30
    REQUIRE_STRONG_SECRET: bool = False
    AUTH_COOKIE_NAME: str = "access_token"
    AUTH_COOKIE_SECURE: bool = False
    AUTH_COOKIE_SAMESITE: Literal["lax", "strict", "none"] = "lax"
    AUTH_COOKIE_PATH: str = "/"
    CSRF_PROTECTION_ENABLED: bool = True
    CSRF_COOKIE_NAME: str = "csrf_token"
    CSRF_HEADER_NAME: str = "X-CSRF-Token"

    # CORS
    CORS_ORIGINS: str = "http://localhost:3000,http://127.0.0.1:3000"
    CORS_ALLOW_CREDENTIALS: bool = True

    # DeepSeek (primary provider)
    DEEPSEEK_API_KEY: Optional[str] = None
    DEEPSEEK_BASE_URL: str = "https://api.deepseek.com"
    DEEPSEEK_TIMEOUT: float = 65.0
    DEEPSEEK_CHAT_ENDPOINT: str = "chat/completions"
    DEEPSEEK_CHAT_MODEL: str = "deepseek-chat"
    DEEPSEEK_REASONER_MODEL: str = "deepseek-reasoner"
    DEEPSEEK_TRUST_ENV_PROXY: bool = False
    DEEPSEEK_ENABLE_REASONER_FALLBACK: bool = True
    DEEPSEEK_REASONER_TRIGGER_CHARS: int = 120
    DEEPSEEK_REASONER_TRIGGER_KEYWORDS: str = "why,how,analyze,strategy,complex,why not,tradeoff,risk,plan"
    DEEPSEEK_FORCE_JSON_OUTPUT: bool = True

    # AI runtime policy (retry / budget / cost guard)
    AI_MAX_RETRIES: int = 2
    AI_RETRY_BACKOFF_MS: int = 800
    AI_RETRY_BACKOFF_FACTOR: float = 2.0
    AI_RETRY_MAX_BACKOFF_MS: int = 5000
    AI_RETRY_ON_TIMEOUT: bool = True
    AI_RETRY_ON_429: bool = True
    AI_RETRY_ON_5XX: bool = True
    AI_MAX_OUTPUT_TOKENS: int = 900
    AI_REPRODUCIBLE_MODE: bool = False
    AI_CHAT_TEMPERATURE: float = 0.6
    AI_REASONER_TEMPERATURE: float = 0.5

    AI_BUDGET_GUARD_ENABLED: bool = False
    AI_DAILY_BUDGET_USD: float = 0.0
    AI_MONTHLY_BUDGET_USD: float = 0.0
    AI_REQUEST_SOFT_CAP_USD: float = 0.0
    AI_REASONER_MAX_PERCENT: float = 0.2
    AI_REASONER_RATIO_WARMUP_CALLS: int = 20
    AI_DISABLE_REASONER_WHEN_BUDGET_HIGH: bool = True
    AI_BUDGET_ALERT_THRESHOLD: float = 0.85

    # Pricing per 1M tokens (USD), used by budget accounting.
    AI_COST_CHAT_INPUT_PER_M: float = 0.28
    AI_COST_CHAT_OUTPUT_PER_M: float = 0.42
    AI_COST_REASONER_INPUT_PER_M: float = 0.55
    AI_COST_REASONER_OUTPUT_PER_M: float = 2.19

    # Legacy Coze fields kept only for backward-compatible env fallback.
    COZE_API_KEY: Optional[str] = None
    COZE_BOT_ID: Optional[str] = None
    COZE_BASE_URL: str = "https://api.coze.cn"
    COZE_TIMEOUT: float = 65.0
    COZE_CHAT_ENDPOINT: str = "open_api/v2/chat"
    ALLOW_MOCK_AI_FALLBACK: bool = False

    # App behavior
    ENVIRONMENT: str = "development"
    DEBUG: bool = False
    DEFAULT_ENCODING: str = "utf-8"
    LOG_ENCODING: str = "utf-8"
    METRICS_EXPECTED_TAROT_CARDS: int = 0
    METRICS_EXPECTED_SPREADS: int = 0
    HEALTH_ADMIN_ONLY: bool = False
    AUTO_CREATE_TABLES_ON_STARTUP: bool = False
    AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP: bool = False
    AUTO_REPAIR_PREDICTION_QUESTIONS_ON_STARTUP: bool = False

    model_config = {
        "case_sensitive": True,
        "env_file": ".env",
        "extra": "ignore",
    }

    @field_validator(
        "DEBUG",
        "REQUIRE_STRONG_SECRET",
        "CORS_ALLOW_CREDENTIALS",
        "AUTH_COOKIE_SECURE",
        "CSRF_PROTECTION_ENABLED",
        "ALLOW_MOCK_AI_FALLBACK",
        "DEEPSEEK_ENABLE_REASONER_FALLBACK",
        "DEEPSEEK_TRUST_ENV_PROXY",
        "DEEPSEEK_FORCE_JSON_OUTPUT",
        "AI_RETRY_ON_TIMEOUT",
        "AI_RETRY_ON_429",
        "AI_RETRY_ON_5XX",
        "AI_BUDGET_GUARD_ENABLED",
        "AI_DISABLE_REASONER_WHEN_BUDGET_HIGH",
        "AI_REPRODUCIBLE_MODE",
        "HEALTH_ADMIN_ONLY",
        "AUTO_CREATE_TABLES_ON_STARTUP",
        "AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP",
        "AUTO_REPAIR_PREDICTION_QUESTIONS_ON_STARTUP",
        mode="before",
    )
    @classmethod
    def parse_bool_like(cls, value):
        if isinstance(value, bool) or value is None:
            return value
        if isinstance(value, (int, float)):
            return bool(value)
        if isinstance(value, str):
            normalized = value.strip().lower()
            truthy = {"1", "true", "yes", "y", "on", "debug", "dev", "development"}
            falsy = {"0", "false", "no", "n", "off", "release", "prod", "production"}
            if normalized in truthy:
                return True
            if normalized in falsy:
                return False
        return value

    @field_validator("AUTH_COOKIE_SAMESITE", mode="before")
    @classmethod
    def normalize_cookie_samesite(cls, value):
        if value is None:
            return "lax"
        normalized = str(value).strip().lower()
        if normalized not in {"lax", "strict", "none"}:
            return "lax"
        return cast(Literal["lax", "strict", "none"], normalized)

    @property
    def cors_origins_list(self) -> List[str]:
        origins = [item.strip() for item in self.CORS_ORIGINS.split(",") if item.strip()]
        return origins or ["http://localhost:3000"]

    @property
    def deepseek_reasoner_keywords(self) -> List[str]:
        return [
            item.strip().lower()
            for item in self.DEEPSEEK_REASONER_TRIGGER_KEYWORDS.split(",")
            if item.strip()
        ]

    def validate_runtime(self) -> None:
        weak_values = {
            "",
            "change-me-in-production",
            "change-this-secret-key-in-production",
            "your-super-secret-key-here-change-in-production",
            DEFAULT_DEV_SECRET_KEY,
        }
        secret_too_short = len(self.SECRET_KEY or "") < 32
        environment = str(self.ENVIRONMENT).strip().lower()
        production_like = environment in {"prod", "production", "staging"}
        enforce_strong_secret = self.REQUIRE_STRONG_SECRET or production_like

        if enforce_strong_secret and (self.SECRET_KEY in weak_values or secret_too_short):
            raise ValueError(
                "Weak SECRET_KEY detected. Set a strong SECRET_KEY in environment before production startup."
            )

        if production_like and not self.AUTH_COOKIE_SECURE:
            raise ValueError(
                "AUTH_COOKIE_SECURE must be true in production-like environments."
            )


settings = Settings()
settings.validate_runtime()
