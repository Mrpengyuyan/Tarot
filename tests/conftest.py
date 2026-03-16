from __future__ import annotations

from collections.abc import Generator

import pytest
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import Session, sessionmaker

import app.db.base  # noqa: F401 - ensure model metadata is loaded
from app.db.base_class import Base
from app.db.session import get_db
from app.main import app
from app.models.spread import SpreadType
from app.models.tarot_card import CardType, TarotCard


@pytest.fixture()
def db_session_factory(tmp_path):
    db_path = tmp_path / "unit_test.db"
    engine = create_engine(
        f"sqlite:///{db_path}",
        connect_args={"check_same_thread": False},
    )
    Base.metadata.create_all(bind=engine)

    factory = sessionmaker(autocommit=False, autoflush=False, bind=engine)
    try:
        yield factory
    finally:
        engine.dispose()


@pytest.fixture()
def db_session(db_session_factory) -> Generator[Session, None, None]:
    session = db_session_factory()
    try:
        yield session
    finally:
        session.close()


@pytest.fixture()
def client(monkeypatch, db_session_factory) -> Generator[TestClient, None, None]:
    # Avoid creating tables on the default runtime database during startup.
    monkeypatch.setattr("app.main.create_tables", lambda: None)

    def override_get_db():
        db = db_session_factory()
        try:
            yield db
        finally:
            db.close()

    app.dependency_overrides[get_db] = override_get_db
    with TestClient(app) as test_client:
        yield test_client
    app.dependency_overrides.clear()


@pytest.fixture()
def seeded_spread_and_cards(db_session: Session):
    spread = SpreadType(
        name="Test Three Card Spread",
        name_en="Test Three Card Spread",
        description="Minimal spread for tests",
        card_count=3,
        positions=[
            {"position": 1, "name": "Past", "meaning": "Past influence"},
            {"position": 2, "name": "Present", "meaning": "Current energy"},
            {"position": 3, "name": "Future", "meaning": "Potential outcome"},
        ],
        is_active=True,
        is_beginner_friendly=True,
    )
    db_session.add(spread)

    cards = []
    for idx in range(1, 5):
        cards.append(
            TarotCard(
                name_en=f"Card {idx}",
                name_zh=f"CardZH{idx}",
                card_number=idx,
                card_type=CardType.MAJOR_ARCANA,
                upright_meaning="Upright meaning",
                reversed_meaning="Reversed meaning",
                upright_love="Love upright",
                reversed_love="Love reversed",
                upright_career="Career upright",
                reversed_career="Career reversed",
                upright_finance="Finance upright",
                reversed_finance="Finance reversed",
                upright_health="Health upright",
                reversed_health="Health reversed",
                keywords_upright="focus,action",
                keywords_reversed="delay,conflict",
                description="Test card",
            )
        )
    db_session.add_all(cards)
    db_session.commit()
    db_session.refresh(spread)

    return {
        "spread_id": spread.id,
        "card_ids": [card.id for card in cards],
    }
