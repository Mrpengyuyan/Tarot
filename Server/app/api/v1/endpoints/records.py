from __future__ import annotations

from datetime import datetime, timedelta, timezone
import logging
import random
from math import ceil
from typing import Callable, List, Optional, cast

from fastapi import APIRouter, BackgroundTasks, Depends, HTTPException, Query
from fastapi.encoders import jsonable_encoder
from fastapi.responses import JSONResponse
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.api.deps import get_current_active_user
from app.core.config import settings
from app.crud import card as card_crud
from app.crud import prediction as prediction_crud
from app.crud import spread as spread_crud
from app.db.session import get_db, get_session_factory
from app.models.record import Prediction as PredictionModel
from app.models.record import InterpretationRunState, PredictionStatus, QuestionType
from app.models.user import User
from app.schemas.prediction import (
    CardDraw as CardDrawSchema,
    CardDrawCreate,
    CardDrawWithMeaning,
    DrawCardsResponse,
    Interpretation,
    InterpretationCreate,
    InterpretationRunSummary,
    InterpretationUpdate,
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
from app.services.coze_service import CozeBudgetExceededError, CozeError, CozeHttpStatusError, CozeRequestError, CozeTimeoutError
from app.services.tarot_service import tarot_interpretation_service

logger = logging.getLogger(__name__)
router = APIRouter()


def _is_guest_account(current_user: User) -> bool:
    return bool(current_user.is_guest)


def _daily_reading_limit_error(*, is_guest: bool) -> HTTPException:
    now = datetime.now(timezone.utc)
    day_start = now.replace(hour=0, minute=0, second=0, microsecond=0)
    seconds_until_reset = max(1, ceil((day_start + timedelta(days=1) - now).total_seconds()))
    return HTTPException(
        status_code=429,
        detail=("Guest" if is_guest else "User") + " daily reading limit reached. Please try again tomorrow.",
        headers={"Retry-After": str(seconds_until_reset)},
    )


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
    prediction_crud.expire_stale_interpretation_run(db, prediction_id, datetime.now(timezone.utc))
    db.refresh(prediction)
    now = datetime.now(timezone.utc)
    retry_after = _as_utc(prediction.ai_budget_retry_at)
    can_retry = prediction.ai_run_state == InterpretationRunState.RETRY_REQUIRED.value or (
        prediction.ai_run_state == InterpretationRunState.BUDGET_BLOCKED.value
        and (retry_after is None or retry_after <= now)
    )
    return PredictionDetail.model_validate(prediction).model_copy(update={
        "interpretation_run": InterpretationRunSummary(
            state=prediction.ai_run_state,
            can_retry=can_retry and prediction.ai_requests_started < 2,
            requests_started=prediction.ai_requests_started,
            retry_after=retry_after,
        )
    })


@router.post("/", response_model=Prediction, summary="Create record")
def create_prediction(
    prediction_create: PredictionCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    if not spread_crud.validate_spread_exists(db, spread_id=prediction_create.spread_type_id):
        raise HTTPException(status_code=400, detail="Spread does not exist or is inactive")

    is_guest = _is_guest_account(current_user)
    daily_limit = 0 if current_user.is_superuser else max(
        0,
        int(settings.GUEST_DAILY_READING_LIMIT if is_guest else settings.USER_DAILY_READING_LIMIT),
    )
    try:
        return prediction_crud.create_prediction_with_stats(
            db,
            user_id=current_user.id,
            prediction_create=prediction_create,
            daily_limit=daily_limit,
        )
    except prediction_crud.DailyReadingLimitExceeded as exc:
        raise _daily_reading_limit_error(is_guest=is_guest) from exc


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


# A shuffled deck turns about half its cards over; the Unity client's offline draw uses the same odds.
REVERSED_PROBABILITY = 0.5


def draw_orientations(count: int, rng) -> List[bool]:
    """Whether each of ``count`` drawn cards lies reversed."""
    return [rng.random() < REVERSED_PROBABILITY for _ in range(count)]


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
        raise HTTPException(status_code=409, detail="Cards already drawn for this record")

    spread = spread_crud.get_spread_by_id(db, spread_id=prediction.spread_type_id)
    if not spread:
        raise HTTPException(status_code=400, detail="Spread not found")

    try:
        cards = card_crud.draw_random_cards(db, count=spread.card_count, seed=seed)
    except ValueError as exc:
        raise HTTPException(status_code=500, detail="Not enough tarot cards in database") from exc

    rng = random.Random(seed) if seed is not None else random
    orientations = draw_orientations(len(cards), rng)
    card_draws_data = [
        CardDrawCreate(
            tarot_card_id=card.id,
            position=i + 1,
            is_reversed=orientations[i],
        )
        for i, card in enumerate(cards)
    ]

    try:
        card_draws = prediction_crud.batch_create_card_draws(
            db,
            prediction_id=prediction_id,
            card_draws_data=card_draws_data,
            prediction_status=PredictionStatus.PROCESSING,
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


async def _build_ai_interpretation_create(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
) -> InterpretationCreate:
    """Generate interpretation content with the AI service.

    On AI failure the prediction is marked FAILED and 504/502 is raised,
    which is the behavior the synchronous endpoint has always had.
    """
    prediction_id = prediction.id
    started_at = prediction.interpretation_started_at
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

        if not prediction_crud.interpretation_claim_is_current(db, prediction_id, started_at):
            raise HTTPException(status_code=409, detail="Interpretation generation was superseded")

        ai_kwargs = {"db": db, "prediction": prediction, "cards_data": cards_data, "user_context": user_context}
        if tarot_interpretation_service.ai_service.is_configured():
            def before_send() -> None:
                if not prediction_crud.mark_interpretation_request_sending(db, prediction_id, started_at):
                    raise prediction_crud.InterpretationClaimLost

            ai_kwargs["before_send"] = before_send
        ai_payload = await tarot_interpretation_service.create_interpretation(**ai_kwargs)

        return InterpretationCreate(
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
    except prediction_crud.InterpretationClaimLost as exc:
        raise HTTPException(status_code=409, detail="Interpretation generation was superseded") from exc
    except CozeBudgetExceededError as exc:
        if exc.request_started:
            prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at, error="budget_after_send")
        else:
            prediction_crud.release_interpretation_generation(
                db,
                prediction_id=prediction_id,
                started_at=started_at,
                budget_retry_at=_budget_retry_time(exc),
            )
        raise HTTPException(status_code=429, detail="AI budget exhausted. Please try again later.") from exc
    except CozeTimeoutError as exc:
        logger.error("AI interpretation timed out: %s", exc)
        prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at, error="timeout")
        raise HTTPException(status_code=504, detail="AI interpretation request timed out") from exc
    except CozeHttpStatusError as exc:
        logger.error("AI interpretation provider HTTP error: %s", exc)
        prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation upstream service error") from exc
    except (CozeRequestError, CozeError) as exc:
        logger.error("AI interpretation upstream request failed: %s", exc)
        prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation request failed") from exc
    except Exception as exc:
        logger.error("AI interpretation generation failed: %s", exc)
        prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation service unavailable") from exc


def _store_interpretation(
    db: Session,
    prediction_id: int,
    interpretation_create: InterpretationCreate,
    expected_started_at: datetime | None = None,
):
    """Persist an interpretation and mark the prediction COMPLETED; return the existing one on a race."""
    try:
        interpretation = prediction_crud.create_interpretation(
            db,
            prediction_id=prediction_id,
            interpretation_create=interpretation_create,
            prediction_status=PredictionStatus.COMPLETED,
            expected_started_at=expected_started_at,
        )
    except prediction_crud.InterpretationClaimLost as exc:
        existing = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if existing:
            prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
            if prediction:
                _complete_existing_interpretation(db, prediction)
            return existing
        raise HTTPException(status_code=409, detail="Interpretation generation was superseded") from exc
    except IntegrityError:
        db.rollback()
        interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if interpretation:
            prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
            if prediction:
                _complete_existing_interpretation(db, prediction)
            return interpretation
        raise

    return interpretation


def _complete_existing_interpretation(db: Session, prediction: PredictionModel) -> None:
    if prediction.status != PredictionStatus.COMPLETED or prediction.completed_at is None:
        prediction_crud.update_prediction_status(db, prediction_id=prediction.id, status=PredictionStatus.COMPLETED)
    if prediction.ai_run_state != InterpretationRunState.COMPLETED.value:
        prediction.ai_run_state = InterpretationRunState.COMPLETED.value
        db.commit()


@router.post("/{prediction_id:int}/interpret", response_model=Interpretation, summary="Create AI interpretation")
async def create_ai_interpretation(
    prediction_id: int,
    interpretation_create: Optional[InterpretationCreate] = None,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
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
        _complete_existing_interpretation(db, prediction)
        return existing_interpretation

    expected_started_at = None
    if not interpretation_create or force_ai:
        if not prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id):
            raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

        now = datetime.now(timezone.utc)
        prediction_crud.expire_stale_interpretation_run(db, prediction_id, now)
        db.refresh(prediction)
        if prediction.ai_run_state != InterpretationRunState.NOT_STARTED.value:
            if prediction.ai_run_state in (InterpretationRunState.SCHEDULED.value, InterpretationRunState.SENDING.value):
                raise HTTPException(status_code=409, detail="Interpretation generation is already in progress")
            if prediction.ai_run_state == InterpretationRunState.BUDGET_BLOCKED.value:
                raise HTTPException(status_code=429, detail="AI budget exhausted. Please try again later.")
            if prediction.ai_requests_started >= 2:
                raise HTTPException(status_code=429, detail="Interpretation requests exhausted")
            raise HTTPException(status_code=409, detail="Manual retry required")
        stale_before = now - timedelta(seconds=max(1, int(settings.AI_INTERPRETATION_STALE_SECONDS)))
        max_attempts = max(1, int(settings.AI_INTERPRETATION_MAX_ATTEMPTS))
        claimed = prediction_crud.claim_interpretation_generation(
            db,
            prediction_id=prediction_id,
            now=now,
            stale_before=stale_before,
            max_attempts=max_attempts,
        )
        if not claimed:
            existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
            if existing_interpretation:
                return existing_interpretation
            db.refresh(prediction)
            started_at = _as_utc(prediction.interpretation_started_at)
            if prediction.status == PredictionStatus.PROCESSING and started_at is not None and started_at >= stale_before:
                raise HTTPException(status_code=409, detail="Interpretation generation is already in progress")
            if prediction.interpretation_attempts >= max_attempts:
                raise HTTPException(status_code=429, detail="Interpretation attempts exhausted")
            raise HTTPException(status_code=409, detail="Interpretation generation is already in progress")

        db.refresh(prediction)
        expected_started_at = prediction.interpretation_started_at
        interpretation_create = await _build_ai_interpretation_create(db, prediction, user_context)

    return _store_interpretation(db, prediction_id, interpretation_create, expected_started_at)


async def generate_and_store_interpretation(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
):
    """Generate with AI and persist; the entry point used by background generation."""
    expected_started_at = prediction.interpretation_started_at
    interpretation_create = await _build_ai_interpretation_create(db, prediction, user_context)
    return _store_interpretation(db, prediction.id, interpretation_create, expected_started_at)


async def run_interpretation_job(
    session_factory: Callable[[], Session],
    prediction_id: int,
    user_context: Optional[str],
    claimed_at: datetime,
) -> None:
    """Background task. Opens its own session because the request session is already closed."""
    try:
        db = session_factory()
    except Exception:  # noqa: BLE001 - a lost task can be reclaimed after the stale timeout
        logger.exception("Background interpretation for record %s failed to open a session", prediction_id)
        return
    try:
        prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
        if prediction is None:
            logger.warning("Background interpretation skipped: record %s not found", prediction_id)
            return
        if prediction.status != PredictionStatus.PROCESSING or _as_utc(prediction.interpretation_started_at) != _as_utc(claimed_at):
            logger.info("Background interpretation skipped: record %s claim was superseded", prediction_id)
            return
        await generate_and_store_interpretation(db, prediction, user_context)
    except Exception as exc:  # noqa: BLE001 - background work must never raise into the event loop
        logger.error("Background interpretation for record %s failed: %s", prediction_id, exc)
        try:
            db.rollback()
            prediction_crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=claimed_at)
        except Exception:  # noqa: BLE001 - stale claims remain retryable if the database is unavailable
            logger.exception("Background interpretation for record %s could not be marked failed", prediction_id)
    finally:
        try:
            db.close()
        except Exception:  # noqa: BLE001 - closing a broken connection must not mask the task failure
            logger.exception("Background interpretation for record %s could not close its session", prediction_id)


def _interpretation_json(interpretation) -> JSONResponse:  # noqa: ANN001
    return JSONResponse(
        status_code=200,
        content=jsonable_encoder(Interpretation.model_validate(interpretation)),
    )


def _processing_json(prediction_id: int) -> JSONResponse:
    return JSONResponse(
        status_code=202,
        content={"prediction_id": prediction_id, "status": "processing"},
    )


def _as_utc(value: Optional[datetime]) -> Optional[datetime]:
    if value is None or value.tzinfo is not None:
        return value
    return value.replace(tzinfo=timezone.utc)


def _budget_retry_time(exc: CozeBudgetExceededError) -> datetime:
    now = datetime.now(timezone.utc)
    if "month" in str(exc).lower():
        if now.month == 12:
            return now.replace(year=now.year + 1, month=1, day=1, hour=0, minute=0, second=0, microsecond=0)
        return now.replace(month=now.month + 1, day=1, hour=0, minute=0, second=0, microsecond=0)
    return now.replace(hour=0, minute=0, second=0, microsecond=0) + timedelta(days=1)


@router.post(
    "/{prediction_id:int}/interpret/async",
    summary="Start AI interpretation in the background",
    responses={
        200: {"model": Interpretation, "description": "Interpretation already exists"},
        202: {"description": "Generation started or already running"},
        429: {"description": "Interpretation attempts exhausted"},
    },
)
async def start_ai_interpretation_async(
    prediction_id: int,
    background_tasks: BackgroundTasks,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
    db: Session = Depends(get_db),
    session_factory: Callable[[], Session] = Depends(get_session_factory),
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
        _complete_existing_interpretation(db, prediction)
        return _interpretation_json(existing_interpretation)

    if not prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id):
        raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

    now = datetime.now(timezone.utc)
    prediction_crud.expire_stale_interpretation_run(db, prediction_id, now)
    db.refresh(prediction)
    if prediction.ai_run_state != InterpretationRunState.NOT_STARTED.value:
        if prediction.ai_run_state in (InterpretationRunState.SCHEDULED.value, InterpretationRunState.SENDING.value):
            return _processing_json(prediction_id)
        if prediction.ai_run_state == InterpretationRunState.BUDGET_BLOCKED.value:
            raise HTTPException(status_code=429, detail="AI budget exhausted. Please try again later.")
        if prediction.ai_requests_started >= 2:
            raise HTTPException(status_code=429, detail="Interpretation requests exhausted")
        raise HTTPException(status_code=409, detail="Manual retry required")
    stale_before = now - timedelta(seconds=max(1, int(settings.AI_INTERPRETATION_STALE_SECONDS)))
    max_attempts = max(1, int(settings.AI_INTERPRETATION_MAX_ATTEMPTS))
    if prediction_crud.claim_interpretation_generation(
        db,
        prediction_id=prediction_id,
        now=now,
        stale_before=stale_before,
        max_attempts=max_attempts,
    ):
        background_tasks.add_task(run_interpretation_job, session_factory, prediction_id, user_context, now)
        return _processing_json(prediction_id)

    db.refresh(prediction)
    existing_interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing_interpretation:
        _complete_existing_interpretation(db, prediction)
        return _interpretation_json(existing_interpretation)

    started_at = _as_utc(prediction.interpretation_started_at)
    if prediction.status == PredictionStatus.PROCESSING and started_at is not None and started_at >= stale_before:
        return _processing_json(prediction_id)

    if prediction.interpretation_attempts >= max_attempts:
        raise HTTPException(status_code=429, detail="Interpretation attempts exhausted")

    return _processing_json(prediction_id)


@router.post(
    "/{prediction_id:int}/interpret/retry",
    summary="Explicitly retry a failed AI interpretation once",
    responses={200: {"model": Interpretation}, 202: {"description": "Manual retry started"}},
)
async def retry_ai_interpretation(
    prediction_id: int,
    background_tasks: BackgroundTasks,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
    db: Session = Depends(get_db),
    session_factory: Callable[[], Session] = Depends(get_session_factory),
    current_user: User = Depends(get_current_active_user),
):
    if not current_user.is_superuser and not prediction_crud.validate_prediction_ownership(
        db, prediction_id=prediction_id, user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")
    prediction = prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    if prediction is None:
        raise HTTPException(status_code=404, detail="Record not found")
    existing = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if existing:
        return _interpretation_json(existing)
    if not prediction_crud.get_prediction_card_draws(db, prediction_id=prediction_id):
        raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

    now = datetime.now(timezone.utc)
    prediction_crud.expire_stale_interpretation_run(db, prediction_id, now)
    db.refresh(prediction)
    if prediction.ai_run_state == InterpretationRunState.BUDGET_BLOCKED.value:
        retry_at = _as_utc(prediction.ai_budget_retry_at)
        if retry_at is not None and retry_at > now:
            raise HTTPException(status_code=429, detail="AI budget exhausted. Please try again later.")
    if prediction.ai_requests_started >= 2:
        raise HTTPException(status_code=429, detail="Interpretation requests exhausted")
    if prediction.ai_run_state not in (
        InterpretationRunState.RETRY_REQUIRED.value, InterpretationRunState.BUDGET_BLOCKED.value,
    ):
        raise HTTPException(status_code=409, detail="Interpretation is not ready for manual retry")
    if not prediction_crud.claim_manual_interpretation_retry(db, prediction_id, now):
        raise HTTPException(status_code=409, detail="Interpretation generation is already in progress")
    background_tasks.add_task(run_interpretation_job, session_factory, prediction_id, user_context, now)
    return _processing_json(prediction_id)


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
    interpretation_update: InterpretationUpdate,
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
