"""Unit tests for CRUD card draw logic — seed determinism and boundary conditions."""
from __future__ import annotations

import pytest
from sqlalchemy.orm import Session

from app.crud.card import draw_random_cards, get_total_cards_count
from app.models.tarot_card import CardType, TarotCard


@pytest.fixture()
def seeded_deck(db_session: Session):
    """Insert a 10-card deck for draw tests."""
    cards = []
    for idx in range(1, 11):
        cards.append(
            TarotCard(
                name_en=f"DrawCard {idx}",
                name_zh=f"抽牌卡{idx}",
                card_number=idx,
                card_type=CardType.MAJOR_ARCANA,
                upright_meaning="Up",
                reversed_meaning="Rev",
                keywords_upright="focus",
                keywords_reversed="delay",
            )
        )
    db_session.add_all(cards)
    db_session.commit()
    return [c.id for c in cards]


class TestDrawRandomCards:
    def test_draw_with_seed_is_deterministic(self, db_session, seeded_deck):
        """Same seed → same card selection AND same order."""
        draw_a = draw_random_cards(db_session, count=3, seed=42)
        draw_b = draw_random_cards(db_session, count=3, seed=42)
        assert [c.id for c in draw_a] == [c.id for c in draw_b]
        assert [c.name_en for c in draw_a] == [c.name_en for c in draw_b]

    def test_different_seed_produces_different_result(self, db_session, seeded_deck):
        """Different seeds should (almost certainly) produce different draws."""
        draw_a = draw_random_cards(db_session, count=5, seed=1)
        draw_b = draw_random_cards(db_session, count=5, seed=999999)
        ids_a = [c.id for c in draw_a]
        ids_b = [c.id for c in draw_b]
        # With 10 cards and count=5, different seeds extremely unlikely to match
        assert ids_a != ids_b

    def test_draw_without_seed_is_random(self, db_session, seeded_deck):
        """Without seed, draws should be non-deterministic (best-effort check)."""
        draws = [
            tuple(c.id for c in draw_random_cards(db_session, count=3))
            for _ in range(10)
        ]
        # At least 2 distinct draws out of 10 attempts (extremely likely with 10C3 = 120 combos)
        assert len(set(draws)) >= 2

    def test_draw_count_equals_available(self, db_session, seeded_deck):
        """Drawing exactly as many cards as available should return all cards (shuffled)."""
        total = get_total_cards_count(db_session)
        drawn = draw_random_cards(db_session, count=total, seed=7)
        assert len(drawn) == total
        drawn_ids = sorted(c.id for c in drawn)
        assert drawn_ids == sorted(seeded_deck)

    def test_draw_count_exceeds_available_raises_error(self, db_session, seeded_deck):
        """Requesting more cards than available must raise ValueError."""
        total = get_total_cards_count(db_session)
        with pytest.raises(ValueError, match="Not enough available cards"):
            draw_random_cards(db_session, count=total + 1)

    def test_exclude_ids_reduces_pool(self, db_session, seeded_deck):
        """Excluded card IDs should never appear in the draw."""
        excluded = seeded_deck[:3]
        drawn = draw_random_cards(db_session, count=4, exclude_ids=excluded, seed=42)
        drawn_ids = [c.id for c in drawn]
        for eid in excluded:
            assert eid not in drawn_ids

    def test_exclude_too_many_raises_error(self, db_session, seeded_deck):
        """Excluding enough cards so that remaining < count should raise ValueError."""
        # Exclude 8 out of 10, then try to draw 4
        excluded = seeded_deck[:8]
        with pytest.raises(ValueError, match="Not enough available cards"):
            draw_random_cards(db_session, count=4, exclude_ids=excluded)

    def test_draw_single_card(self, db_session, seeded_deck):
        """Drawing 1 card should return a list of exactly 1."""
        drawn = draw_random_cards(db_session, count=1, seed=99)
        assert len(drawn) == 1
        assert isinstance(drawn[0], TarotCard)

    def test_draw_returns_tarot_card_objects(self, db_session, seeded_deck):
        """Ensure returned objects are TarotCard instances with expected attributes."""
        drawn = draw_random_cards(db_session, count=2, seed=42)
        for card in drawn:
            assert isinstance(card, TarotCard)
            assert card.name_en is not None
            assert card.name_zh is not None
            assert card.id in seeded_deck
