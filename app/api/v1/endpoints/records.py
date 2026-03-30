from __future__ import annotations

import logging
import random
from typing import List, Optional, cast

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.api.deps import get_current_active_user
from app.crud import card as card_crud
from app.crud import prediction as prediction_crud
from app.crud import spread as spread_crud
from app.db.session import get_db
from app.models.record import Prediction as PredictionModel
from app.models.record import PredictionStatus, QuestionType
from app.schemas.prediction import (
    CardDraw as CardDrawSchema,
    CardDrawCreate,
    CardDrawWithMeaning,
    DrawCardsResponse,
    Interpretation,
    InterpretationCreate,
    Prediction,
    PredictionCreate,
    PredictionDetail,
    PredictionRecentOverview,
    PredictionSimple,
    PredictionStats,
    PredictionStatusEnum,
    PredictionUpdate,
    QuestionTypeEnum,
)
from app.schemas.user import User
from app.services.coze_service import CozeError, CozeHttpStatusError, CozeRequestError, CozeTimeoutError
from app.services.tarot_service import tarot_interpretation_service

logger = logging.getLogger(__name__)
router = APIRouter()


def _normalize_key_themes(value) -> Optional[str]:  # noqa: ANN001
    if value is None:
        return None
    if isinstance(value, str):
        text = value.strip()
        return text or None
    if isinstance(value, list):
        parts = [str(item).strip() for item in value if str(item).strip()]
        return ",".join(parts) if parts else None
    text = str(value).strip()
    return text or None


@router.get("/", response_model=List[PredictionSimple], summary="List user records")
def get_user_predictions(
    skip: int = Query(0, ge=0, description="Records to skip"),
    limit: int = Query(20, ge=1, le=100, description="Records to return"),
    status: Optional[PredictionStatusEnum] = Query(None, description="Filter by record status"),
    question_type: Optional[QuestionTypeEnum] = Query(None, description="Filter by question type"),
    favorites_only: bool = Query(False, description="Only favorite records"),
    search: Optional[str] = Query(None, description="Search question/user notes"),
    sort_by: str = Query("created_at", description="Sort field: created_at/completed_at/status/question_type"),
    sort_order: str = Query("desc", description="Sort order: asc/desc"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if sort_by not in {"created_at", "completed_at", "status", "question_type"}:
        raise HTTPException(status_code=400, detail="Unsupported sort field")
    if sort_order not in {"asc", "desc"}:
        raise HTTPException(status_code=400, detail="Unsupported sort order")

    return prediction_crud.get_filtered_user_predictions(
        db,
        user_id=current_user.id,
        skip=skip,
        limit=limit,
        status=PredictionStatus(status.value) if status else None,
        question_type=QuestionType(question_type.value) if question_type else None,
        favorites_only=favorites_only,
        search_term=search,
        sort_by=sort_by,
        sort_order=sort_order,
    )


@router.get("/stats", response_model=PredictionStats, summary="Get record stats")
def get_user_prediction_stats(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    return prediction_crud.get_user_prediction_stats(db, user_id=current_user.id)


@router.get("/recent", response_model=List[PredictionSimple], summary="List recent records")
def get_recent_predictions(
    days: int = Query(7, ge=1, le=30, description="Recent day window"),
    limit: int = Query(10, ge=1, le=50, description="Records to return"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    return prediction_crud.get_recent_predictions(
        db,
        user_id=current_user.id,
        days=days,
        limit=limit,
    )


@router.get("/recent-overview", response_model=List[PredictionRecentOverview], summary="List recent record overviews")
@router.get(
    "/dashboard/recent-overview",
    response_model=List[PredictionRecentOverview],
    summary="List recent record overviews",
)
def get_recent_prediction_overview(
    limit: int = Query(4, ge=1, le=12, description="Records to return"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    predictions = prediction_crud.get_recent_prediction_overview(
        db,
        user_id=current_user.id,
        limit=limit,
    )

    result: List[PredictionRecentOverview] = []
    for prediction in predictions:
        interpretation_summary = None
        if prediction.interpretation:
            interpretation_summary = (
                prediction.interpretation.summary
                or prediction.interpretation.overall_interpretation
            )

        result.append(
            PredictionRecentOverview(
                id=prediction.id,
                question=prediction.question,
                question_type=QuestionTypeEnum(prediction.question_type.value),
                status=PredictionStatusEnum(prediction.status.value),
                created_at=prediction.created_at,
                is_favorite=prediction.is_favorite,
                user_rating=prediction.user_rating,
                spread_name=prediction.spread_type.name if prediction.spread_type else None,
                spread_name_en=prediction.spread_type.name_en if prediction.spread_type else None,
                interpretation_summary=interpretation_summary,
            )
        )

    return result


@router.get("/{prediction_id:int}", response_model=PredictionDetail, summary="Get record detail")
def get_prediction_detail(
    prediction_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_with_details(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")
    return prediction


@router.post("/", response_model=Prediction, summary="Create record")
def create_prediction(
    prediction_create: PredictionCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not spread_crud.validate_spread_exists(db, spread_id=prediction_create.spread_type_id):
        raise HTTPException(status_code=400, detail="Spread does not exist or is inactive")

    return prediction_crud.create_prediction_with_stats(
        db,
        user_id=current_user.id,
        prediction_create=prediction_create,
    )


@router.put("/{prediction_id:int}", response_model=Prediction, summary="Update record")
def update_prediction(
    prediction_id: int,
    prediction_update: PredictionUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    return prediction_crud.update_prediction(
        db=db,
        db_prediction=prediction,
        prediction_update=prediction_update,
    )


@router.delete("/{prediction_id:int}", summary="Delete record")
def delete_prediction(
    prediction_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    success = prediction_crud.delete_prediction(db, prediction_id=prediction_id)
    if not success:
        raise HTTPException(status_code=404, detail="Record not found")
    return {"message": "Record deleted"}


@router.post("/{prediction_id:int}/draw", response_model=DrawCardsResponse, summary="Draw cards for record")
def draw_cards_for_prediction(
    prediction_id: int,
    seed: Optional[int] = Query(None, description="Optional deterministic draw seed (for reproducible experiments)"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    existing_draws = prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id)
    if existing_draws:
        raise HTTPException(status_code=400, detail="Cards already drawn for this record")

    spread = spread_crud.get_spread_by_id(db, spread_id=prediction.spread_type_id)
    if not spread:
        raise HTTPException(status_code=400, detail="Spread not found")

    cards = card_crud.draw_random_cards(db, count=spread.card_count, seed=seed)
    if len(cards) < spread.card_count:
        raise HTTPException(status_code=500, detail="Not enough tarot cards in database")

    rng = random.Random(seed) if seed is not None else random
    card_draws_data = [
        CardDrawCreate(
            tarot_card_id=card.id,
            position=i + 1,
            is_reversed=(rng.random() < 0.3),
        )
        for i, card in enumerate(cards)
    ]

    try:
        card_draws = prediction_crud.batch_create_card_draws(
            db,
            prediction_id=prediction_id,
            card_draws_data=card_draws_data,
        )
        prediction_crud.update_prediction_status(
            db,
            prediction_id=prediction_id,
            status=PredictionStatus.PROCESSING,
        )
    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=409, detail="Cards already drawn for this record")

    return DrawCardsResponse(
        prediction_id=prediction_id,
        card_draws=cast(List[CardDrawSchema], card_draws),
        status="success",
    )


@router.get("/{prediction_id:int}/cards", response_model=List[CardDrawWithMeaning], summary="Get drawn cards")
def get_prediction_cards(
    prediction_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    card_draws = prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id)
    spread = spread_crud.get_spread_by_id(db, spread_id=prediction.spread_type_id)

    from app.schemas.card import TarotCardMeaning, TarotCardSimple

    result: List[CardDrawWithMeaning] = []
    for draw in card_draws:
        aspect = prediction.question_type.value if prediction.question_type.value != "general" else "general"
        meaning = card_crud.get_card_meaning(
            db,
            card_id=draw.tarot_card_id,
            is_reversed=draw.is_reversed,
            aspect=aspect,
        )
        keywords = card_crud.get_card_keywords(
            db,
            card_id=draw.tarot_card_id,
            is_reversed=draw.is_reversed,
        )
        position_name = spread.get_position_name(draw.position) if spread else f"Position {draw.position}"
        position_meaning = spread.get_position_meaning(draw.position) if spread else ""

        card_meaning = TarotCardMeaning(
            id=draw.tarot_card.id,
            name_zh=draw.tarot_card.name_zh,
            name_en=draw.tarot_card.name_en,
            is_reversed=draw.is_reversed,
            meaning=meaning or "",
            keywords=keywords,
            position=draw.position,
            position_name=position_name,
            position_meaning=position_meaning,
        )
        result.append(
            CardDrawWithMeaning(
                id=draw.id,
                prediction_id=draw.prediction_id,
                tarot_card_id=draw.tarot_card_id,
                position=draw.position,
                is_reversed=draw.is_reversed,
                drawn_at=draw.drawn_at,
                tarot_card=TarotCardSimple.model_validate(draw.tarot_card),
                card_meaning=card_meaning,
                position_name=position_name,
                position_meaning=position_meaning,
            )
        )

    return result


@router.post("/{prediction_id:int}/interpret", response_model=Interpretation, summary="Create AI interpretation")
async def create_ai_interpretation(
    prediction_id: int,
    interpretation_create: Optional[InterpretationCreate] = None,
    user_context: Optional[str] = Query(None, description="Additional user context"),
    force_ai: bool = Query(False, description="Force AI generation even when manual payload is provided"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser and not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")

    existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing_interpretation:
        if prediction.status != PredictionStatus.COMPLETED or prediction.completed_at is None:
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.COMPLETED,
            )
        return existing_interpretation

    if not interpretation_create or force_ai:
        try:
            card_draws = prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id)
            if not card_draws:
                raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

            spread = spread_crud.get_spread_by_id(db, spread_id=prediction.spread_type_id)
            cards_data = []
            for draw in card_draws:
                card = card_crud.get_card_by_id(db, card_id=draw.tarot_card_id)
                if not card:
                    continue
                position_name = spread.get_position_name(draw.position) if spread else f"Position {draw.position}"
                cards_data.append(
                    {
                        "card": card,
                        "position": position_name,
                        "is_reversed": draw.is_reversed,
                    }
                )

            if len(cards_data) != len(card_draws):
                raise HTTPException(
                    status_code=500,
                    detail="Card data is incomplete; please redraw cards",
                )

            ai_payload = await tarot_interpretation_service.create_interpretation(
                db=db,
                prediction=prediction,
                cards_data=cards_data,
                user_context=user_context,
            )

            interpretation_create = InterpretationCreate(
                overall_interpretation=ai_payload.get("overall_interpretation", ""),
                card_analysis=ai_payload.get("card_analysis"),
                relationship_analysis=ai_payload.get("relationship_analysis"),
                advice=ai_payload.get("advice"),
                warning=ai_payload.get("warning"),
                summary=ai_payload.get("summary"),
                key_themes=_normalize_key_themes(ai_payload.get("key_themes")),
                model_used=ai_payload.get(
                    "model_used",
                    tarot_interpretation_service.default_model_name()
                    if tarot_interpretation_service.ai_service.is_configured()
                    else "mock_ai",
                ),
                model_version=ai_payload.get("model_version"),
                confidence_score=(
                    ai_payload.get("confidence_score")
                    if ai_payload.get("confidence_score") is not None
                    else 0.85
                ),
            )
        except HTTPException:
            raise
        except CozeTimeoutError as exc:
            logger.error("AI interpretation timed out: %s", exc)
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.FAILED,
            )
            raise HTTPException(status_code=504, detail="AI interpretation request timed out") from exc
        except CozeHttpStatusError as exc:
            logger.error("AI interpretation provider HTTP error: %s", exc)
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.FAILED,
            )
            raise HTTPException(status_code=502, detail="AI interpretation upstream service error") from exc
        except (CozeRequestError, CozeError) as exc:
            logger.error("AI interpretation upstream request failed: %s", exc)
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.FAILED,
            )
            raise HTTPException(status_code=502, detail="AI interpretation request failed") from exc
        except Exception as exc:
            logger.error("AI interpretation generation failed: %s", exc)
            prediction_crud.update_prediction_status(
                db,
                prediction_id=prediction_id,
                status=PredictionStatus.FAILED,
            )
            raise HTTPException(status_code=502, detail="AI interpretation service unavailable") from exc

    try:
        interpretation = prediction_crud.create_interpretation(
            db,
            prediction_id=prediction_id,
            interpretation_create=interpretation_create,
        )
    except IntegrityError:
        db.rollback()
        interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if interpretation:
            return interpretation
        raise

    prediction_crud.update_prediction_status(
        db,
        prediction_id=prediction_id,
        status=PredictionStatus.COMPLETED,
    )
    return interpretation


@router.get("/{prediction_id:int}/interpretation", response_model=Interpretation, summary="Get interpretation")
def get_prediction_interpretation(
    prediction_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if not interpretation:
        raise HTTPException(status_code=404, detail="Interpretation not found")
    return interpretation


@router.put("/{prediction_id:int}/interpretation", response_model=Interpretation, summary="Update interpretation")
def update_interpretation(
    prediction_id: int,
    interpretation_update: InterpretationCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser and not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")

    interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if not interpretation:
        raise HTTPException(status_code=404, detail="Interpretation not found")

    return prediction_crud.update_interpretation(
        db=db,
        db_interpretation=interpretation,
        interpretation_update=interpretation_update,
    )


@router.get("/admin/all", response_model=List[PredictionSimple], summary="Admin list all records")
def get_all_predictions_admin(
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=200),
    status: Optional[PredictionStatusEnum] = Query(None),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser:
        raise HTTPException(status_code=403, detail="Admin privileges required")

    query = db.query(PredictionModel)
    if status:
        query = query.filter(PredictionModel.status == PredictionStatus(status.value))

    return query.order_by(PredictionModel.created_at.desc()).offset(skip).limit(limit).all()
