from __future__ import annotations

from app.models.spread import SpreadType
from app.models.tarot_card import CardType, Suit, TarotCard


def test_cards_list_combines_card_type_and_search_filters(client, db_session, seeded_spread_and_cards):
    del seeded_spread_and_cards
    db_session.add(
        TarotCard(
            name_en="Special Minor Card",
            name_zh="SpecialMinorZH",
            card_number=99,
            card_type=CardType.MINOR_ARCANA,
            suit=Suit.WANDS,
            upright_meaning="Upright meaning",
            reversed_meaning="Reversed meaning",
            keywords_upright="specialtoken,focus",
            keywords_reversed="delay,block",
        )
    )
    db_session.commit()

    # If filters are combined correctly, this should exclude the minor arcana card.
    resp = client.get("/api/v1/cards/?card_type=major_arcana&search=specialtoken")
    assert resp.status_code == 200
    assert resp.json() == []


def test_spreads_list_combines_question_type_search_and_difficulty_filters(client, db_session, seeded_spread_and_cards):
    del seeded_spread_and_cards
    matching_spread = SpreadType(
        name="Career Combo Target",
        name_en="Career Combo Target",
        description="match-all-filters",
        card_count=3,
        difficulty_level=3,
        positions=[
            {"position": 1, "name": "A", "meaning": "A"},
            {"position": 2, "name": "B", "meaning": "B"},
            {"position": 3, "name": "C", "meaning": "C"},
        ],
        suitable_for_career=True,
        is_active=True,
    )
    search_only_spread = SpreadType(
        name="Career Combo Target",
        name_en="Career Combo Target",
        description="should-be-filtered-out-by-question-type",
        card_count=3,
        difficulty_level=3,
        positions=[
            {"position": 1, "name": "A", "meaning": "A"},
            {"position": 2, "name": "B", "meaning": "B"},
            {"position": 3, "name": "C", "meaning": "C"},
        ],
        suitable_for_career=False,
        is_active=True,
    )
    db_session.add_all([matching_spread, search_only_spread])
    db_session.commit()
    db_session.refresh(matching_spread)

    resp = client.get(
        "/api/v1/spreads/?difficulty=3&question_type=career&search=Combo%20Target"
    )
    assert resp.status_code == 200
    body = resp.json()
    assert [item["id"] for item in body] == [matching_spread.id]


def test_spreads_list_rejects_invalid_question_type(client):
    resp = client.get("/api/v1/spreads/?question_type=invalid_type")
    assert resp.status_code == 400
    assert "question_type must be one of" in resp.json()["detail"]
