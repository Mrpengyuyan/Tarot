from __future__ import annotations

from datetime import datetime, timedelta, timezone
import logging
import random
from math import ceil
from typing import Callable, List, Optional, cast

from fastapi import APIRouter, BackgroundTasks, Depends, HTTPException, Query
from fastapi.responses import JSONResponse, PlainTextResponse
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session
from starlette.convertors import IntegerConvertor, StringConvertor, register_url_convertor

from app.api.deps import get_current_active_user
from app.core.config import settings
from app.crud import card as card_crud
from app.crud import prediction as prediction_crud
from app.crud import spread as spread_crud
from app.db.session import get_db, get_session_factory
from app.models.record import Prediction as PredictionModel
from app.models.record import InterpretationRunState, PredictionStatus, QuestionType
from app.models.user import User
from app.schemas.limits import DatabaseInteger, MAX_DATABASE_INTEGER, SearchTerm
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
from app.services import interpretation_orchestrator
from app.services.coze_service import CozeBudgetExceededError
from app.services.tarot_service import tarot_interpretation_service

logger = logging.getLogger(__name__)
router = APIRouter()

_normalize_key_themes = interpretation_orchestrator.normalize_key_themes
_as_utc = interpretation_orchestrator.as_utc
_budget_retry_time = interpretation_orchestrator.budget_retry_time
_interpretation_json = interpretation_orchestrator.interpretation_json
_processing_json = interpretation_orchestrator.processing_json


class _RecordIdConvertor(StringConvertor):
    # Keep numeric-only routes, but let Pydantic validate before converting long IDs.
    regex = IntegerConvertor.regex

    def to_string(self, value: str) -> str:
        return IntegerConvertor().to_string(int(value))


register_url_convertor("record_id", _RecordIdConvertor())


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


def _require_ownership(
    db: Session,
    prediction_id: int,
    current_user: User,
    *,
    allow_superuser: bool = False,
) -> None:
    if allow_superuser and current_user.is_superuser:
        return
    if not prediction_crud.validate_prediction_ownership(
        db,
        prediction_id=prediction_id,
        user_id=current_user.id,
    ):
        raise HTTPException(status_code=404, detail="Record not found")


def _require_owned_prediction(
    db: Session,
    prediction_id: int,
    current_user: User,
    *,
    allow_superuser: bool = False,
    with_details: bool = False,
) -> PredictionModel:
    _require_ownership(db, prediction_id, current_user, allow_superuser=allow_superuser)
    prediction = (
        prediction_crud.get_prediction_with_details(db, prediction_id=prediction_id)
        if with_details
        else prediction_crud.get_prediction_by_id(db, prediction_id=prediction_id)
    )
    if not prediction:
        raise HTTPException(status_code=404, detail="Record not found")
    return prediction


@router.get("/", response_model=List[PredictionSimple], summary="List user records")
def get_user_predictions(
    skip: int = Query(0, ge=0, le=MAX_DATABASE_INTEGER, description="Records to skip"),
    limit: int = Query(20, ge=1, le=100, description="Records to return"),
    status: Optional[PredictionStatusEnum] = Query(None, description="Filter by record status"),
    question_type: Optional[QuestionTypeEnum] = Query(None, description="Filter by question type"),
    favorites_only: bool = Query(False, description="Only favorite records"),
    search: Optional[SearchTerm] = Query(None, description="Search question/user notes"),
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


@router.get("/{prediction_id:record_id}", response_model=PredictionDetail, summary="Get record detail")
def get_prediction_detail(
    prediction_id: DatabaseInteger,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user, with_details=True)
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
    except IntegrityError as exc:
        db.rollback()
        raise HTTPException(status_code=409, detail="Reading data changed; select an active spread and try again") from exc


@router.put("/{prediction_id:record_id}", response_model=Prediction, summary="Update record")
def update_prediction(
    prediction_id: DatabaseInteger,
    prediction_update: PredictionUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user)
    return prediction_crud.update_prediction(
        db=db,
        db_prediction=prediction,
        prediction_update=prediction_update,
    )


@router.delete("/{prediction_id:record_id}", summary="Delete record")
def delete_prediction(
    prediction_id: DatabaseInteger,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    _require_ownership(db, prediction_id, current_user)
    try:
        success = prediction_crud.delete_prediction(db, prediction_id=prediction_id)
    except IntegrityError as exc:
        db.rollback()
        raise HTTPException(status_code=409, detail="Reading changed while being deleted; please try again") from exc
    if not success:
        raise HTTPException(status_code=404, detail="Record not found")
    return {"message": "Record deleted"}


# A shuffled deck turns about half its cards over; the Unity client's offline draw uses the same odds.
REVERSED_PROBABILITY = 0.5


def draw_orientations(count: int, rng) -> List[bool]:
    """Whether each of ``count`` drawn cards lies reversed."""
    return [rng.random() < REVERSED_PROBABILITY for _ in range(count)]


@router.post("/{prediction_id:record_id}/draw", response_model=DrawCardsResponse, summary="Draw cards for record")
def draw_cards_for_prediction(
    prediction_id: DatabaseInteger,
    seed: Optional[int] = Query(None, description="Optional deterministic draw seed (for reproducible experiments)"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user)

    if not prediction.question or not prediction.question.strip():
        raise HTTPException(status_code=409, detail="A question is required; create a new reading before drawing cards")

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


@router.get("/{prediction_id:record_id}/cards", response_model=List[CardDrawWithMeaning], summary="Get drawn cards")
def get_prediction_cards(
    prediction_id: DatabaseInteger,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user)

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
    return await interpretation_orchestrator.build_ai_interpretation_create(
        db,
        prediction,
        user_context,
        crud=prediction_crud,
        card_repo=card_crud,
        spread_repo=spread_crud,
        tarot_service=tarot_interpretation_service,
    )


def _store_interpretation(
    db: Session,
    prediction_id: int,
    interpretation_create: InterpretationCreate,
    expected_started_at: datetime | None = None,
):
    return interpretation_orchestrator.store_interpretation(
        db,
        prediction_id,
        interpretation_create,
        expected_started_at,
        crud=prediction_crud,
    )


def _complete_existing_interpretation(db: Session, prediction: PredictionModel) -> None:
    interpretation_orchestrator.complete_existing_interpretation(db, prediction, crud=prediction_crud)


@router.post("/{prediction_id:record_id}/interpret", response_model=Interpretation, summary="Create AI interpretation")
async def create_ai_interpretation(
    prediction_id: DatabaseInteger,
    interpretation_create: Optional[InterpretationCreate] = None,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
    force_ai: bool = Query(False, description="Force AI generation even when manual payload is provided"),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user, allow_superuser=True)

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

    try:
        if not interpretation_create or force_ai:
            interpretation_create = await _build_ai_interpretation_create(db, prediction, user_context)
        return _store_interpretation(db, prediction_id, interpretation_create, expected_started_at)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("AI interpretation generation or storage failed (record=%s, error_type=%s)",
                     prediction_id, type(exc).__name__)
        return PlainTextResponse("Internal Server Error", status_code=500)


async def generate_and_store_interpretation(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
):
    """Generate with AI and persist; the entry point used by background generation."""
    return await interpretation_orchestrator.generate_and_store_interpretation(
        db,
        prediction,
        user_context,
        crud=prediction_crud,
        card_repo=card_crud,
        spread_repo=spread_crud,
        tarot_service=tarot_interpretation_service,
    )


async def run_interpretation_job(
    session_factory: Callable[[], Session],
    prediction_id: int,
    user_context: Optional[str],
    claimed_at: datetime,
) -> None:
    """Background task. Opens its own session because the request session is already closed."""
    await interpretation_orchestrator.run_interpretation_job(
        session_factory,
        prediction_id,
        user_context,
        claimed_at,
        crud=prediction_crud,
        card_repo=card_crud,
        spread_repo=spread_crud,
        tarot_service=tarot_interpretation_service,
    )


@router.post(
    "/{prediction_id:record_id}/interpret/async",
    summary="Start AI interpretation in the background",
    responses={
        200: {"model": Interpretation, "description": "Interpretation already exists"},
        202: {"description": "Generation started or already running"},
        429: {"description": "Interpretation attempts exhausted"},
    },
)
async def start_ai_interpretation_async(
    prediction_id: DatabaseInteger,
    background_tasks: BackgroundTasks,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
    db: Session = Depends(get_db),
    session_factory: Callable[[], Session] = Depends(get_session_factory),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user, allow_superuser=True)

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
    "/{prediction_id:record_id}/interpret/retry",
    summary="Explicitly retry a failed AI interpretation once",
    responses={200: {"model": Interpretation}, 202: {"description": "Manual retry started"}},
)
async def retry_ai_interpretation(
    prediction_id: DatabaseInteger,
    background_tasks: BackgroundTasks,
    user_context: Optional[str] = Query(None, max_length=2000, description="Additional user context"),
    db: Session = Depends(get_db),
    session_factory: Callable[[], Session] = Depends(get_session_factory),
    current_user: User = Depends(get_current_active_user),
):
    prediction = _require_owned_prediction(db, prediction_id, current_user, allow_superuser=True)
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


@router.get("/{prediction_id:record_id}/interpretation", response_model=Interpretation, summary="Get interpretation")
def get_prediction_interpretation(
    prediction_id: DatabaseInteger,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    _require_ownership(db, prediction_id, current_user)

    interpretation = prediction_crud.get_prediction_interpretation(db, prediction_id=prediction_id)
    if not interpretation:
        raise HTTPException(status_code=404, detail="Interpretation not found")
    return interpretation


@router.put("/{prediction_id:record_id}/interpretation", response_model=Interpretation, summary="Update interpretation")
def update_interpretation(
    prediction_id: DatabaseInteger,
    interpretation_update: InterpretationUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_active_user),
):
    _require_ownership(db, prediction_id, current_user, allow_superuser=True)

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
    skip: int = Query(0, ge=0, le=MAX_DATABASE_INTEGER),
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
