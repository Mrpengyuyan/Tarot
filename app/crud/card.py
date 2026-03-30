import random
from typing import List, Optional

from sqlalchemy import and_, or_
from sqlalchemy.orm import Session

from app.models.tarot_card import CardType, Suit, TarotCard
from app.schemas.card import TarotCardCreate, TarotCardUpdate


def _build_cards_query(
    db: Session,
    *,
    card_type: Optional[CardType] = None,
    suit: Optional[Suit] = None,
    search_term: Optional[str] = None,
):
    query = db.query(TarotCard)

    if card_type is not None:
        query = query.filter(TarotCard.card_type == card_type)

    if suit is not None:
        query = query.filter(TarotCard.suit == suit)

    if search_term:
        search_pattern = f"%{search_term}%"
        query = query.filter(
            or_(
                TarotCard.name_zh.ilike(search_pattern),
                TarotCard.name_en.ilike(search_pattern),
                TarotCard.keywords_upright.ilike(search_pattern),
                TarotCard.keywords_reversed.ilike(search_pattern),
            )
        )

    return query


def get_card_by_id(db: Session, card_id: int) -> Optional[TarotCard]:
    return db.query(TarotCard).filter(TarotCard.id == card_id).first()


def get_card_by_number_and_type(
    db: Session,
    card_number: int,
    card_type: CardType,
    suit: Optional[Suit] = None,
) -> Optional[TarotCard]:
    query = db.query(TarotCard).filter(
        and_(
            TarotCard.card_number == card_number,
            TarotCard.card_type == card_type,
        )
    )
    if suit is not None:
        query = query.filter(TarotCard.suit == suit)
    return query.first()


def get_cards(
    db: Session,
    skip: int = 0,
    limit: int = 100,
    card_type: Optional[CardType] = None,
    suit: Optional[Suit] = None,
    search_term: Optional[str] = None,
) -> List[TarotCard]:
    return (
        _build_cards_query(
            db,
            card_type=card_type,
            suit=suit,
            search_term=search_term,
        )
        .offset(skip)
        .limit(limit)
        .all()
    )


def get_cards_by_type(
    db: Session,
    card_type: CardType,
    skip: int = 0,
    limit: int = 100,
) -> List[TarotCard]:
    return get_cards(db, skip=skip, limit=limit, card_type=card_type)


def get_cards_by_suit(db: Session, suit: Suit, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    return get_cards(db, skip=skip, limit=limit, suit=suit)


def get_cards_by_type_and_suit(
    db: Session,
    card_type: CardType,
    suit: Suit,
    skip: int = 0,
    limit: int = 100,
) -> List[TarotCard]:
    return get_cards(db, skip=skip, limit=limit, card_type=card_type, suit=suit)


def search_cards(db: Session, search_term: str, skip: int = 0, limit: int = 100) -> List[TarotCard]:
    return get_cards(db, skip=skip, limit=limit, search_term=search_term)


def create_card(db: Session, card_create: TarotCardCreate) -> TarotCard:
    db_card = TarotCard(**card_create.model_dump())
    db.add(db_card)
    db.commit()
    db.refresh(db_card)
    return db_card


def update_card(db: Session, db_card: TarotCard, card_update: TarotCardUpdate) -> TarotCard:
    update_data = card_update.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(db_card, field, value)

    db.commit()
    db.refresh(db_card)
    return db_card


def delete_card(db: Session, card_id: int) -> bool:
    db_card = get_card_by_id(db, card_id)
    if db_card:
        db.delete(db_card)
        db.commit()
        return True
    return False


def get_total_cards_count(db: Session) -> int:
    return db.query(TarotCard).count()


def get_major_arcana_cards(db: Session) -> List[TarotCard]:
    return (
        db.query(TarotCard)
        .filter(TarotCard.card_type == CardType.MAJOR_ARCANA)
        .order_by(TarotCard.card_number)
        .all()
    )


def get_minor_arcana_cards(db: Session) -> List[TarotCard]:
    return (
        db.query(TarotCard)
        .filter(TarotCard.card_type == CardType.MINOR_ARCANA)
        .order_by(TarotCard.suit, TarotCard.card_number)
        .all()
    )


def draw_random_cards(
    db: Session,
    count: int,
    exclude_ids: Optional[List[int]] = None,
    seed: Optional[int] = None,
) -> List[TarotCard]:
    query = db.query(TarotCard).order_by(TarotCard.id.asc())
    if exclude_ids:
        query = query.filter(~TarotCard.id.in_(exclude_ids))

    all_cards = query.all()
    if count >= len(all_cards):
        return all_cards

    rng = random.Random(seed) if seed is not None else random
    return rng.sample(all_cards, count)


def get_card_meaning(db: Session, card_id: int, is_reversed: bool = False, aspect: str = "general") -> Optional[str]:
    card = get_card_by_id(db, card_id)
    if card:
        return card.get_meaning(is_reversed, aspect)
    return None


def get_card_keywords(db: Session, card_id: int, is_reversed: bool = False) -> List[str]:
    card = get_card_by_id(db, card_id)
    if card:
        return card.get_keywords(is_reversed)
    return []


def validate_card_exists(db: Session, card_id: int) -> bool:
    return db.query(TarotCard).filter(TarotCard.id == card_id).first() is not None


def batch_create_cards(db: Session, cards_data: List[TarotCardCreate]) -> List[TarotCard]:
    db_cards: List[TarotCard] = []
    for card_data in cards_data:
        db_card = TarotCard(**card_data.model_dump())
        db.add(db_card)
        db_cards.append(db_card)

    db.commit()
    for db_card in db_cards:
        db.refresh(db_card)

    return db_cards
