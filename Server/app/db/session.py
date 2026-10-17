import logging
from typing import Callable, Generator

from sqlalchemy import create_engine, event, text
from sqlalchemy.orm import Session, sessionmaker
from sqlalchemy.pool import StaticPool

from app.core.config import settings

logger = logging.getLogger(__name__)


def enable_sqlite_foreign_keys(dbapi_connection, _connection_record) -> None:
    cursor = dbapi_connection.cursor()
    try:
        cursor.execute("PRAGMA foreign_keys=ON")
    finally:
        cursor.close()


# Database configuration from settings.
DATABASE_URL = settings.DATABASE_URL

# Build engine.
if DATABASE_URL.startswith("sqlite"):
    sqlite_connect_args = {"check_same_thread": False}
    is_memory_sqlite = DATABASE_URL in {"sqlite://", "sqlite:///:memory:"} or DATABASE_URL.endswith(":memory:")

    if is_memory_sqlite:
        engine = create_engine(
            DATABASE_URL,
            poolclass=StaticPool,
            connect_args=sqlite_connect_args,
            echo=False,
        )
    else:
        engine = create_engine(
            DATABASE_URL,
            connect_args=sqlite_connect_args,
            pool_pre_ping=True,
            echo=False,
        )
else:
    engine = create_engine(
        DATABASE_URL,
        pool_pre_ping=True,
        echo=False,
    )

if engine.dialect.name == "sqlite":
    event.listen(engine, "connect", enable_sqlite_foreign_keys)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


def get_db() -> Generator[Session, None, None]:
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def get_session_factory() -> Callable[[], Session]:
    """Session factory for work that outlives the request, such as FastAPI background tasks."""
    return SessionLocal


def get_db_session() -> Session:
    return SessionLocal()


def check_db_connection() -> bool:
    db = SessionLocal()
    try:
        db.execute(text("SELECT 1"))
        return True
    except Exception as exc:
        logger.error("Database connectivity check failed: %s", exc)
        return False
    finally:
        db.close()
