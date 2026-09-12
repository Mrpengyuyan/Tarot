from typing import List, Optional

from sqlalchemy import and_, func, or_, update
from sqlalchemy.orm import Session

from app.models.spread import SpreadType
from app.schemas.spread import SpreadTypeCreate, SpreadTypeUpdate

_QUESTION_TYPE_SUITABILITY = {
    "love": SpreadType.suitable_for_love,
    "career": SpreadType.suitable_for_career,
    "finance": SpreadType.suitable_for_finance,
    "health": SpreadType.suitable_for_health,
    "general": SpreadType.suitable_for_general,
}


def _build_spreads_query(
    db: Session,
    *,
    active_only: bool = True,
    difficulty_level: Optional[int] = None,
    card_count: Optional[int] = None,
    question_type: Optional[str] = None,
    beginner_friendly: Optional[bool] = None,
    search_term: Optional[str] = None,
):
    query = db.query(SpreadType)

    if active_only:
        query = query.filter(SpreadType.is_active.is_(True))

    if difficulty_level is not None:
        query = query.filter(SpreadType.difficulty_level == difficulty_level)

    if card_count is not None:
        query = query.filter(SpreadType.card_count == card_count)

    if beginner_friendly is not None:
        query = query.filter(SpreadType.is_beginner_friendly.is_(beginner_friendly))

    if question_type:
        normalized = question_type.strip().lower()
        suitability_field = _QUESTION_TYPE_SUITABILITY.get(normalized)
        if suitability_field is None:
            raise ValueError("Unsupported question type")
        query = query.filter(suitability_field.is_(True))

    if search_term:
        search_pattern = f"%{search_term}%"
        query = query.filter(
            or_(
                SpreadType.name.ilike(search_pattern),
                SpreadType.name_en.ilike(search_pattern),
                SpreadType.description.ilike(search_pattern),
            )
        )

    return query


def get_spread_by_id(db: Session, spread_id: int) -> Optional[SpreadType]:
    return db.query(SpreadType).filter(SpreadType.id == spread_id).first()


def get_spread_by_name(db: Session, name: str) -> Optional[SpreadType]:
    return db.query(SpreadType).filter(SpreadType.name == name).first()


def get_spreads(
    db: Session,
    skip: int = 0,
    limit: int = 100,
    active_only: bool = True,
    difficulty_level: Optional[int] = None,
    card_count: Optional[int] = None,
    question_type: Optional[str] = None,
    beginner_friendly: Optional[bool] = None,
    search_term: Optional[str] = None,
) -> List[SpreadType]:
    query = _build_spreads_query(
        db,
        active_only=active_only,
        difficulty_level=difficulty_level,
        card_count=card_count,
        question_type=question_type,
        beginner_friendly=beginner_friendly,
        search_term=search_term,
    )
    return query.offset(skip).limit(limit).all()


def get_spreads_by_difficulty(
    db: Session,
    difficulty_level: int,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return get_spreads(
        db,
        skip=skip,
        limit=limit,
        active_only=True,
        difficulty_level=difficulty_level,
    )


def get_spreads_by_card_count(
    db: Session,
    card_count: int,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return get_spreads(
        db,
        skip=skip,
        limit=limit,
        active_only=True,
        card_count=card_count,
    )


def get_beginner_friendly_spreads(
    db: Session,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return get_spreads(
        db,
        skip=skip,
        limit=limit,
        active_only=True,
        beginner_friendly=True,
    )


def get_spreads_for_question_type(
    db: Session,
    question_type: str,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return get_spreads(
        db,
        skip=skip,
        limit=limit,
        active_only=True,
        question_type=question_type,
    )


def search_spreads(
    db: Session,
    search_term: str,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return get_spreads(
        db,
        skip=skip,
        limit=limit,
        active_only=True,
        search_term=search_term,
    )


def create_spread(db: Session, spread_create: SpreadTypeCreate) -> SpreadType:
    positions_data = [pos.model_dump() for pos in spread_create.positions]
    spread_data = spread_create.model_dump()
    spread_data["positions"] = positions_data

    db_spread = SpreadType(**spread_data)
    db.add(db_spread)
    db.commit()
    db.refresh(db_spread)
    return db_spread


def update_spread(db: Session, db_spread: SpreadType, spread_update: SpreadTypeUpdate) -> SpreadType:
    update_data = spread_update.model_dump(exclude_unset=True)

    if "positions" in update_data and update_data["positions"] is not None:
        normalized_positions = []
        for pos in update_data["positions"]:
            if hasattr(pos, "model_dump"):
                normalized_positions.append(pos.model_dump())
            elif isinstance(pos, dict):
                normalized_positions.append(pos)
        update_data["positions"] = normalized_positions

    for field, value in update_data.items():
        setattr(db_spread, field, value)

    db.commit()
    db.refresh(db_spread)
    return db_spread


def delete_spread(db: Session, spread_id: int) -> bool:
    db_spread = get_spread_by_id(db, spread_id)
    if db_spread:
        db_spread.is_active = False
        db.commit()
        return True
    return False


def hard_delete_spread(db: Session, spread_id: int) -> bool:
    db_spread = get_spread_by_id(db, spread_id)
    if db_spread:
        db.delete(db_spread)
        db.commit()
        return True
    return False


def get_total_spreads_count(db: Session, active_only: bool = True) -> int:
    query = db.query(SpreadType)
    if active_only:
        query = query.filter(SpreadType.is_active.is_(True))
    return query.count()


def get_popular_spreads(db: Session, limit: int = 10) -> List[SpreadType]:
    return (
        db.query(SpreadType)
        .filter(SpreadType.is_active.is_(True))
        .order_by(SpreadType.usage_count.desc())
        .limit(limit)
        .all()
    )


def increment_spread_usage(db: Session, spread_id: int) -> bool:
    result = db.execute(
        update(SpreadType)
        .where(SpreadType.id == spread_id)
        .values(usage_count=func.coalesce(SpreadType.usage_count, 0) + 1)
    )
    if (result.rowcount or 0) <= 0:
        return False
    db.commit()
    return True


def validate_spread_exists(db: Session, spread_id: int) -> bool:
    return (
        db.query(SpreadType)
        .filter(
            and_(
                SpreadType.id == spread_id,
                SpreadType.is_active.is_(True),
            )
        )
        .first()
        is not None
    )


def get_spreads_by_difficulty_range(
    db: Session,
    min_difficulty: int,
    max_difficulty: int,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return (
        db.query(SpreadType)
        .filter(
            and_(
                SpreadType.difficulty_level >= min_difficulty,
                SpreadType.difficulty_level <= max_difficulty,
                SpreadType.is_active.is_(True),
            )
        )
        .offset(skip)
        .limit(limit)
        .all()
    )


def get_spreads_by_card_count_range(
    db: Session,
    min_cards: int,
    max_cards: int,
    skip: int = 0,
    limit: int = 100,
) -> List[SpreadType]:
    return (
        db.query(SpreadType)
        .filter(
            and_(
                SpreadType.card_count >= min_cards,
                SpreadType.card_count <= max_cards,
                SpreadType.is_active.is_(True),
            )
        )
        .offset(skip)
        .limit(limit)
        .all()
    )


def batch_create_spreads(db: Session, spreads_data: List[SpreadTypeCreate]) -> List[SpreadType]:
    db_spreads: List[SpreadType] = []
    for spread_data in spreads_data:
        positions_data = [pos.model_dump() for pos in spread_data.positions]
        spread_dict = spread_data.model_dump()
        spread_dict["positions"] = positions_data

        db_spread = SpreadType(**spread_dict)
        db.add(db_spread)
        db_spreads.append(db_spread)

    db.commit()
    for db_spread in db_spreads:
        db.refresh(db_spread)

    return db_spreads
