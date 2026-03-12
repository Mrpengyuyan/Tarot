import os
import secrets
import sys
from typing import List, Optional

from pydantic import field_validator
from pydantic_settings import BaseSettings


# Force UTF-8 on Windows terminals to avoid garbled logs.
if sys.platform.startswith("win"):
    os.environ.setdefault("PYTHONIOENCODING", "utf-8")
    if hasattr(sys.stdout, "reconfigure"):
        try:
            sys.stdout.reconfigure(encoding="utf-8", errors="ignore")
            sys.stderr.reconfigure(encoding="utf-8", errors="ignore")
        except Exception:
            pass


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
    DATABASE_URL: str = "sqlite:///./tarot_game.db"

    # Security
    SECRET_KEY: str = secrets.token_urlsafe(64)
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 30
    REQUIRE_STRONG_SECRET: bool = False
    AUTH_COOKIE_NAME: str = "access_token"
    AUTH_COOKIE_SECURE: bool = False
    AUTH_COOKIE_SAMESITE: str = "lax"
    AUTH_COOKIE_PATH: str = "/"

    # CORS
    CORS_ORIGINS: str = "http://localhost:3000,http://127.0.0.1:3000"
    CORS_ALLOW_CREDENTIALS: bool = True

    # Coze
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
        return normalized

    @property
    def cors_origins_list(self) -> List[str]:
        origins = [item.strip() for item in self.CORS_ORIGINS.split(",") if item.strip()]
        return origins or ["http://localhost:3000"]

    def validate_runtime(self) -> None:
        weak_values = {
            "",
            "change-this-secret-key-in-production",
            "your-super-secret-key-here-change-in-production",
        }
        env = str(self.ENVIRONMENT).strip().lower()
        is_production = env in {"prod", "production"}
        if self.REQUIRE_STRONG_SECRET and is_production and self.SECRET_KEY in weak_values:
            raise ValueError(
                "Weak SECRET_KEY detected. Set a strong SECRET_KEY in environment before production startup."
            )


settings = Settings()
settings.validate_runtime()
