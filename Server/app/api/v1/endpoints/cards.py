from typing import List, Optional

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_active_user
from app.crud import card as card_crud
from app.db.session import get_db
from app.models.tarot_card import CardType, Suit
from app.schemas.card import (
    CardTypeEnum,
    SuitEnum,
    TarotCard,
    TarotCardCreate,
    TarotCardMeaning,
    TarotCardSimple,
    TarotCardUpdate,
)
from app.schemas.user import User

router = APIRouter()


@router.get("/", response_model=List[TarotCardSimple], summary="Get tarot cards")
def get_cards(
    skip: int = Query(0, ge=0, description="Records to skip"),
    limit: int = Query(100, ge=1, le=500, description="Records to return"),
    card_type: Optional[CardTypeEnum] = Query(None, description="Filter by card type"),
    suit: Optional[SuitEnum] = Query(None, description="Filter by suit"),
    search: Optional[str] = Query(None, description="Search keyword"),
    db: Session = Depends(get_db),
):
    db_card_type = CardType(card_type.value) if card_type else None
    db_suit = Suit(suit.value) if suit else None
    return card_crud.get_cards(
        db,
        skip=skip,
        limit=limit,
        card_type=db_card_type,
        suit=db_suit,
        search_term=search,
    )


@router.get("/count", summary="Get tarot card counts")
def get_cards_count(db: Session = Depends(get_db)):
    total_count = card_crud.get_total_cards_count(db)
    major_arcana = card_crud.get_major_arcana_cards(db)
    minor_arcana = card_crud.get_minor_arcana_cards(db)
    return {
        "total_cards": total_count,
        "major_arcana_count": len(major_arcana),
        "minor_arcana_count": len(minor_arcana),
    }


@router.get("/major-arcana", response_model=List[TarotCardSimple], summary="Get major arcana")
def get_major_arcana_cards(db: Session = Depends(get_db)):
    return card_crud.get_major_arcana_cards(db)


@router.get("/minor-arcana", response_model=List[TarotCardSimple], summary="Get minor arcana")
def get_minor_arcana_cards(db: Session = Depends(get_db)):
    return card_crud.get_minor_arcana_cards(db)


@router.get("/draw", response_model=List[TarotCardSimple], summary="Draw random cards")
def draw_random_cards(
    count: int = Query(1, ge=1, le=78, description="Draw count"),
    exclude_ids: Optional[List[int]] = Query(None, description="Excluded card ids"),
    seed: Optional[int] = Query(None, description="Optional deterministic draw seed"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    del current_user
    try:
        return card_crud.draw_random_cards(db, count=count, exclude_ids=exclude_ids, seed=seed)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/search", response_model=List[TarotCardSimple], summary="Search cards")
def search_cards(
    q: str = Query(..., description="Search keyword"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db),
):
    return card_crud.search_cards(db, search_term=q, skip=skip, limit=limit)


@router.get("/{card_id}", response_model=TarotCard, summary="Get card detail")
def get_card_detail(
    card_id: int,
    db: Session = Depends(get_db),
):
    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="Tarot card not found")
    return card


@router.get("/{card_id}/meaning", response_model=TarotCardMeaning, summary="Get card meaning")
def get_card_meaning(
    card_id: int,
    is_reversed: bool = Query(False, description="Is reversed"),
    aspect: str = Query("general", description="Aspect: general/love/career/finance/health"),
    position: Optional[int] = Query(None, description="Card position"),
    db: Session = Depends(get_db),
):
    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="Tarot card not found")

    meaning = card_crud.get_card_meaning(
        db,
        card_id=card_id,
        is_reversed=is_reversed,
        aspect=aspect,
    )
    keywords = card_crud.get_card_keywords(
        db,
        card_id=card_id,
        is_reversed=is_reversed,
    )

    return TarotCardMeaning(
        id=card.id,
        name_zh=card.name_zh,
        name_en=card.name_en,
        is_reversed=is_reversed,
        meaning=meaning or "",
        keywords=keywords,
        position=position,
        position_name=f"Position {position}" if position else None,
        position_meaning=None,
    )


@router.post("/", response_model=TarotCard, summary="Create card")
def create_card(
    card_create: TarotCardCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    existing_card = card_crud.get_card_by_number_and_type(
        db,
        card_number=card_create.card_number,
        card_type=CardType(card_create.card_type.value),
        suit=Suit(card_create.suit.value) if card_create.suit else None,
    )
    if existing_card:
        raise HTTPException(status_code=400, detail="Card with same number/type already exists")

    return card_crud.create_card(db=db, card_create=card_create)


@router.put("/{card_id}", response_model=TarotCard, summary="Update card")
def update_card(
    card_id: int,
    card_update: TarotCardUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    card = card_crud.get_card_by_id(db, card_id=card_id)
    if not card:
        raise HTTPException(status_code=404, detail="Tarot card not found")

    return card_crud.update_card(db=db, db_card=card, card_update=card_update)


@router.delete("/{card_id}", summary="Delete card")
def delete_card(
    card_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    success = card_crud.delete_card(db, card_id=card_id)
    if not success:
        raise HTTPException(status_code=404, detail="Tarot card not found")

    return {"message": "Tarot card deleted"}


@router.post("/batch", response_model=List[TarotCard], summary="Batch create cards")
def batch_create_cards(
    cards_data: List[TarotCardCreate],
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    if len(cards_data) > 100:
        raise HTTPException(status_code=400, detail="At most 100 cards per batch")

    return card_crud.batch_create_cards(db=db, cards_data=cards_data)
