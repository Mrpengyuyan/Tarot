from typing import List, Optional

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_active_user
from app.crud import spread as spread_crud
from app.db.session import get_db
from app.schemas.spread import (
    SpreadSuitability,
    SpreadType,
    SpreadTypeCreate,
    SpreadTypeSimple,
    SpreadTypeUpdate,
)
from app.schemas.user import User

router = APIRouter()
VALID_QUESTION_TYPES = {"love", "career", "finance", "health", "general"}


@router.get("/", response_model=List[SpreadTypeSimple], summary="Get spreads")
def get_spreads(
    skip: int = Query(0, ge=0, description="Records to skip"),
    limit: int = Query(100, ge=1, le=500, description="Records to return"),
    difficulty: Optional[int] = Query(None, ge=1, le=5, description="Filter by difficulty"),
    card_count: Optional[int] = Query(None, ge=1, le=78, description="Filter by card count"),
    question_type: Optional[str] = Query(None, description="Filter by question type"),
    beginner_friendly: Optional[bool] = Query(None, description="Filter by beginner-friendly"),
    search: Optional[str] = Query(None, description="Search keyword"),
    active_only: bool = Query(True, description="Only active spreads"),
    db: Session = Depends(get_db),
):
    if question_type and question_type.lower() not in VALID_QUESTION_TYPES:
        raise HTTPException(
            status_code=400,
            detail=f"question_type must be one of: {', '.join(sorted(VALID_QUESTION_TYPES))}",
        )

    try:
        return spread_crud.get_spreads(
            db,
            skip=skip,
            limit=limit,
            active_only=active_only,
            difficulty_level=difficulty,
            card_count=card_count,
            question_type=question_type,
            beginner_friendly=beginner_friendly,
            search_term=search,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/popular", response_model=List[SpreadTypeSimple], summary="Get popular spreads")
def get_popular_spreads(
    limit: int = Query(10, ge=1, le=50, description="Records to return"),
    db: Session = Depends(get_db),
):
    return spread_crud.get_popular_spreads(db, limit=limit)


@router.get("/beginner", response_model=List[SpreadTypeSimple], summary="Get beginner spreads")
def get_beginner_spreads(
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    db: Session = Depends(get_db),
):
    return spread_crud.get_beginner_friendly_spreads(db, skip=skip, limit=limit)


@router.get("/by-question-type/{question_type}", response_model=List[SpreadTypeSimple], summary="Get spreads by question type")
def get_spreads_by_question_type(
    question_type: str,
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db),
):
    normalized = question_type.lower()
    if normalized not in VALID_QUESTION_TYPES:
        raise HTTPException(
            status_code=400,
            detail=f"question_type must be one of: {', '.join(sorted(VALID_QUESTION_TYPES))}",
        )

    return spread_crud.get_spreads_for_question_type(
        db,
        question_type=normalized,
        skip=skip,
        limit=limit,
    )


@router.get("/count", summary="Get spread counts")
def get_spreads_count(db: Session = Depends(get_db)):
    total_count = spread_crud.get_total_spreads_count(db, active_only=True)
    beginner_count = len(spread_crud.get_beginner_friendly_spreads(db, limit=1000))

    difficulty_stats = {}
    for level in range(1, 6):
        count = len(spread_crud.get_spreads_by_difficulty(db, difficulty_level=level, limit=1000))
        difficulty_stats[f"level_{level}"] = count

    return {
        "total_spreads": total_count,
        "beginner_friendly": beginner_count,
        "difficulty_distribution": difficulty_stats,
    }


@router.get("/search", response_model=List[SpreadTypeSimple], summary="Search spreads")
def search_spreads(
    q: str = Query(..., description="Search keyword"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100),
    db: Session = Depends(get_db),
):
    return spread_crud.search_spreads(db, search_term=q, skip=skip, limit=limit)


@router.get("/{spread_id}", response_model=SpreadType, summary="Get spread detail")
def get_spread_detail(
    spread_id: int,
    db: Session = Depends(get_db),
):
    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="Spread not found")
    return spread


@router.get("/{spread_id}/suitability", response_model=SpreadSuitability, summary="Get spread suitability")
def get_spread_suitability(
    spread_id: int,
    db: Session = Depends(get_db),
):
    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="Spread not found")

    return SpreadSuitability(
        id=spread.id,
        name=spread.name,
        suitable_for_love=spread.suitable_for_love,
        suitable_for_career=spread.suitable_for_career,
        suitable_for_finance=spread.suitable_for_finance,
        suitable_for_health=spread.suitable_for_health,
        suitable_for_general=spread.suitable_for_general,
    )


@router.post("/", response_model=SpreadType, summary="Create spread")
def create_spread(
    spread_create: SpreadTypeCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    existing_spread = spread_crud.get_spread_by_name(db, name=spread_create.name)
    if existing_spread:
        raise HTTPException(status_code=400, detail="Spread with same name already exists")

    if len(spread_create.positions) != spread_create.card_count:
        raise HTTPException(status_code=400, detail="positions count does not match card_count")

    return spread_crud.create_spread(db=db, spread_create=spread_create)


@router.put("/{spread_id}", response_model=SpreadType, summary="Update spread")
def update_spread(
    spread_id: int,
    spread_update: SpreadTypeUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    spread = spread_crud.get_spread_by_id(db, spread_id=spread_id)
    if not spread:
        raise HTTPException(status_code=404, detail="Spread not found")

    if spread_update.positions is not None:
        expected_card_count = (
            spread_update.card_count
            if spread_update.card_count is not None
            else spread.card_count
        )
        if len(spread_update.positions) != expected_card_count:
            raise HTTPException(status_code=400, detail="positions count does not match card_count")

    return spread_crud.update_spread(db=db, db_spread=spread, spread_update=spread_update)


@router.delete("/{spread_id}", summary="Delete spread")
def delete_spread(
    spread_id: int,
    hard_delete: bool = Query(False, description="Hard delete"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    if hard_delete:
        success = spread_crud.hard_delete_spread(db, spread_id=spread_id)
        message = "Spread hard deleted"
    else:
        success = spread_crud.delete_spread(db, spread_id=spread_id)
        message = "Spread marked inactive"

    if not success:
        raise HTTPException(status_code=404, detail="Spread not found")

    return {"message": message}


@router.post("/{spread_id}/use", summary="Record spread usage")
def use_spread(
    spread_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    del current_user
    if not spread_crud.validate_spread_exists(db, spread_id=spread_id):
        raise HTTPException(status_code=404, detail="Spread not found or inactive")

    success = spread_crud.increment_spread_usage(db, spread_id=spread_id)
    if not success:
        raise HTTPException(status_code=500, detail="Failed to record spread usage")

    return {"message": "Spread usage recorded"}


@router.post("/batch", response_model=List[SpreadType], summary="Batch create spreads")
def batch_create_spreads(
    spreads_data: List[SpreadTypeCreate],
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    if len(spreads_data) > 50:
        raise HTTPException(status_code=400, detail="At most 50 spreads per batch")

    for spread_data in spreads_data:
        if len(spread_data.positions) != spread_data.card_count:
            raise HTTPException(
                status_code=400,
                detail=f"Spread '{spread_data.name}' positions count does not match card_count",
            )

    return spread_crud.batch_create_spreads(db=db, spreads_data=spreads_data)
