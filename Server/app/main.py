import logging
from math import isfinite

from fastapi import FastAPI, Request
from fastapi.encoders import jsonable_encoder
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from sqlalchemy import text

from app.api.v1.api import api_router
from app.core.config import settings
from app.db import base  # noqa: F401
from app.db.base_class import Base
from app.db.bootstrap import ensure_reference_data
from app.db.session import SessionLocal, engine

logger = logging.getLogger(__name__)


def _json_safe_float(value: float) -> float | str:
    return value if isfinite(value) else str(value)


def _json_safe_string(value: str) -> str:
    return value.encode("utf-8", errors="backslashreplace").decode("utf-8")


async def validation_error_handler(_request: Request, exc: RequestValidationError) -> JSONResponse:
    # Error details echo untrusted input, including invalid numbers and Unicode.
    detail = jsonable_encoder(
        exc.errors(), custom_encoder={float: _json_safe_float, str: _json_safe_string},
    )
    return JSONResponse(status_code=422, content={"detail": detail})


def create_tables():
    """Create database tables and required uniqueness indexes."""
    Base.metadata.create_all(bind=engine)

    with engine.begin() as conn:
        conn.execute(
            text(
                "CREATE UNIQUE INDEX IF NOT EXISTS uq_card_draw_prediction_position "
                "ON card_draws (prediction_id, position)"
            )
        )
        conn.execute(
            text(
                "CREATE UNIQUE INDEX IF NOT EXISTS uq_interpretation_prediction "
                "ON interpretations (prediction_id)"
            )
        )


def create_start_app_handler(app: FastAPI):
    async def start_app() -> None:
        if settings.AUTO_CREATE_TABLES_ON_STARTUP:
            create_tables()
            logger.info("Database tables created successfully")
        else:
            logger.info("Skipping auto table creation (AUTO_CREATE_TABLES_ON_STARTUP=false)")

        if settings.AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP:
            with SessionLocal() as db:
                reference_data = ensure_reference_data(db)

            logger.info(
                "Reference data ready. cards=%s spreads=%s imported_cards=%s imported_spreads=%s repaired_questions=%s",
                reference_data["cards_after"],
                reference_data["spreads_after"],
                reference_data["cards_imported"],
                reference_data["spreads_imported"],
                reference_data["questions_repaired"],
            )
        else:
            logger.info("Skipping reference-data bootstrap (AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP=false)")
        logger.info("Tip: Run 'python -m app.scripts.init_demo_data' to initialize demo users")

    return start_app


def get_application():
    environment = str(settings.ENVIRONMENT).strip().lower()
    enable_docs = settings.DEBUG or environment in {"dev", "development", "local"}
    docs_url = "/docs" if enable_docs else None
    redoc_url = "/redoc" if enable_docs else None

    app = FastAPI(
        title=settings.PROJECT_NAME,
        version=settings.PROJECT_VERSION,
        description="Tarot game backend API",
        docs_url=docs_url,
        redoc_url=redoc_url,
    )
    app.add_exception_handler(RequestValidationError, validation_error_handler)

    allowed_origins = settings.cors_origins_list
    allow_credentials = settings.CORS_ALLOW_CREDENTIALS and "*" not in allowed_origins

    app.add_middleware(
        CORSMiddleware,
        allow_origins=allowed_origins,
        allow_credentials=allow_credentials,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    logger.info(
        "CORS configured with origins=%s credentials=%s",
        allowed_origins,
        allow_credentials,
    )

    app.add_event_handler("startup", create_start_app_handler(app))
    app.include_router(api_router, prefix=settings.API_V1_STR)
    return app


app = get_application()
