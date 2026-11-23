from __future__ import annotations

from datetime import datetime, timedelta, timezone
import logging
from typing import Callable, Optional

from fastapi import HTTPException
from fastapi.encoders import jsonable_encoder
from fastapi.responses import JSONResponse
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.crud import card as card_crud
from app.crud import prediction as prediction_crud
from app.crud import spread as spread_crud
from app.models.record import InterpretationRunState, Prediction as PredictionModel, PredictionStatus
from app.schemas.prediction import Interpretation, InterpretationCreate
from app.services.coze_service import (
    CozeBudgetExceededError,
    CozeError,
    CozeHttpStatusError,
    CozeRequestError,
    CozeTimeoutError,
)
from app.services.tarot_service import tarot_interpretation_service

logger = logging.getLogger(__name__)


def normalize_key_themes(value) -> Optional[str]:  # noqa: ANN001
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


def as_utc(value: Optional[datetime]) -> Optional[datetime]:
    if value is None or value.tzinfo is not None:
        return value
    return value.replace(tzinfo=timezone.utc)


def budget_retry_time(exc: CozeBudgetExceededError) -> datetime:
    now = datetime.now(timezone.utc)
    if "month" in str(exc).lower():
        if now.month == 12:
            return now.replace(year=now.year + 1, month=1, day=1, hour=0, minute=0, second=0, microsecond=0)
        return now.replace(month=now.month + 1, day=1, hour=0, minute=0, second=0, microsecond=0)
    return now.replace(hour=0, minute=0, second=0, microsecond=0) + timedelta(days=1)


def interpretation_json(interpretation) -> JSONResponse:  # noqa: ANN001
    return JSONResponse(
        status_code=200,
        content=jsonable_encoder(Interpretation.model_validate(interpretation)),
    )


def processing_json(prediction_id: int) -> JSONResponse:
    return JSONResponse(
        status_code=202,
        content={"prediction_id": prediction_id, "status": "processing"},
    )


def complete_existing_interpretation(
    db: Session,
    prediction: PredictionModel,
    *,
    crud=prediction_crud,
) -> None:
    if prediction.status != PredictionStatus.COMPLETED or prediction.completed_at is None:
        crud.update_prediction_status(db, prediction_id=prediction.id, status=PredictionStatus.COMPLETED)
    if prediction.ai_run_state != InterpretationRunState.COMPLETED.value:
        prediction.ai_run_state = InterpretationRunState.COMPLETED.value
        db.commit()


def store_interpretation(
    db: Session,
    prediction_id: int,
    interpretation_create: InterpretationCreate,
    expected_started_at: datetime | None = None,
    *,
    crud=prediction_crud,
):
    """Persist an interpretation and mark the prediction COMPLETED; return the existing one on a race."""
    try:
        interpretation = crud.create_interpretation(
            db,
            prediction_id=prediction_id,
            interpretation_create=interpretation_create,
            prediction_status=PredictionStatus.COMPLETED,
            expected_started_at=expected_started_at,
        )
    except crud.InterpretationClaimLost as exc:
        existing = crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if existing:
            prediction = crud.get_prediction_by_id(db, prediction_id=prediction_id)
            if prediction:
                complete_existing_interpretation(db, prediction, crud=crud)
            return existing
        raise HTTPException(status_code=409, detail="Interpretation generation was superseded") from exc
    except IntegrityError:
        db.rollback()
        interpretation = crud.get_prediction_interpretation(db, prediction_id=prediction_id)
        if interpretation:
            prediction = crud.get_prediction_by_id(db, prediction_id=prediction_id)
            if prediction:
                complete_existing_interpretation(db, prediction, crud=crud)
            return interpretation
        raise

    return interpretation


async def build_ai_interpretation_create(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
    *,
    crud=prediction_crud,
    card_repo=card_crud,
    spread_repo=spread_crud,
    tarot_service=tarot_interpretation_service,
) -> InterpretationCreate:
    """Generate interpretation content with the AI service.

    On AI failure the prediction is marked FAILED and 504/502 is raised,
    which is the behavior the synchronous endpoint has always had.
    """
    prediction_id = prediction.id
    started_at = prediction.interpretation_started_at
    try:
        card_draws = crud.get_prediction_card_draws(db, prediction_id=prediction_id)
        if not card_draws:
            raise HTTPException(status_code=400, detail="Cards must be drawn before interpretation")

        spread = spread_repo.get_spread_by_id(db, spread_id=prediction.spread_type_id)
        cards_data = []
        for draw in card_draws:
            card = card_repo.get_card_by_id(db, card_id=draw.tarot_card_id)
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

        if not crud.interpretation_claim_is_current(db, prediction_id, started_at):
            raise HTTPException(status_code=409, detail="Interpretation generation was superseded")

        ai_kwargs = {"db": db, "prediction": prediction, "cards_data": cards_data, "user_context": user_context}
        if tarot_service.ai_service.is_configured():
            def before_send() -> None:
                if not crud.mark_interpretation_request_sending(db, prediction_id, started_at):
                    raise crud.InterpretationClaimLost

            ai_kwargs["before_send"] = before_send
        ai_payload = await tarot_service.create_interpretation(**ai_kwargs)

        return InterpretationCreate(
            overall_interpretation=ai_payload.get("overall_interpretation", ""),
            card_analysis=ai_payload.get("card_analysis"),
            relationship_analysis=ai_payload.get("relationship_analysis"),
            advice=ai_payload.get("advice"),
            warning=ai_payload.get("warning"),
            summary=ai_payload.get("summary"),
            key_themes=normalize_key_themes(ai_payload.get("key_themes")),
            model_used=ai_payload.get(
                "model_used",
                tarot_service.default_model_name()
                if tarot_service.ai_service.is_configured()
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
    except crud.InterpretationClaimLost as exc:
        raise HTTPException(status_code=409, detail="Interpretation generation was superseded") from exc
    except CozeBudgetExceededError as exc:
        if exc.request_started:
            crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at, error="budget_after_send")
        else:
            crud.release_interpretation_generation(
                db,
                prediction_id=prediction_id,
                started_at=started_at,
                budget_retry_at=budget_retry_time(exc),
            )
        raise HTTPException(status_code=429, detail="AI budget exhausted. Please try again later.") from exc
    except CozeTimeoutError as exc:
        logger.error("AI interpretation timed out (record=%s, error_type=%s)", prediction_id, type(exc).__name__)
        crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at, error="timeout")
        raise HTTPException(status_code=504, detail="AI interpretation request timed out") from exc
    except CozeHttpStatusError as exc:
        logger.error(
            "AI interpretation provider HTTP error (record=%s, status=%s, error_type=%s)",
            prediction_id,
            exc.status_code,
            type(exc).__name__,
        )
        crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation upstream service error") from exc
    except (CozeRequestError, CozeError) as exc:
        logger.error("AI interpretation upstream request failed (record=%s, error_type=%s)", prediction_id, type(exc).__name__)
        crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation request failed") from exc
    except Exception as exc:
        logger.error("AI interpretation generation failed (record=%s, error_type=%s)", prediction_id, type(exc).__name__)
        crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=started_at)
        raise HTTPException(status_code=502, detail="AI interpretation service unavailable") from exc


async def generate_and_store_interpretation(
    db: Session,
    prediction: PredictionModel,
    user_context: Optional[str],
    *,
    crud=prediction_crud,
    card_repo=card_crud,
    spread_repo=spread_crud,
    tarot_service=tarot_interpretation_service,
):
    """Generate with AI and persist; the entry point used by background generation."""
    expected_started_at = prediction.interpretation_started_at
    interpretation_create = await build_ai_interpretation_create(
        db,
        prediction,
        user_context,
        crud=crud,
        card_repo=card_repo,
        spread_repo=spread_repo,
        tarot_service=tarot_service,
    )
    return store_interpretation(db, prediction.id, interpretation_create, expected_started_at, crud=crud)


async def run_interpretation_job(
    session_factory: Callable[[], Session],
    prediction_id: int,
    user_context: Optional[str],
    claimed_at: datetime,
    *,
    crud=prediction_crud,
    card_repo=card_crud,
    spread_repo=spread_crud,
    tarot_service=tarot_interpretation_service,
) -> None:
    """Background task. Opens its own session because the request session is already closed."""
    try:
        db = session_factory()
    except Exception as exc:  # noqa: BLE001 - a lost task can be reclaimed after the stale timeout
        logger.error(
            "Background interpretation for record %s failed to open a session (error_type=%s)",
            prediction_id,
            type(exc).__name__,
        )
        return
    try:
        prediction = crud.get_prediction_by_id(db, prediction_id=prediction_id)
        if prediction is None:
            logger.warning("Background interpretation skipped: record %s not found", prediction_id)
            return
        if prediction.status != PredictionStatus.PROCESSING or as_utc(prediction.interpretation_started_at) != as_utc(claimed_at):
            logger.info("Background interpretation skipped: record %s claim was superseded", prediction_id)
            return
        await generate_and_store_interpretation(
            db,
            prediction,
            user_context,
            crud=crud,
            card_repo=card_repo,
            spread_repo=spread_repo,
            tarot_service=tarot_service,
        )
    except Exception as exc:  # noqa: BLE001 - background work must never raise into the event loop
        logger.error("Background interpretation for record %s failed (error_type=%s)", prediction_id, type(exc).__name__)
        try:
            db.rollback()
            crud.fail_interpretation_generation(db, prediction_id=prediction_id, started_at=claimed_at)
        except Exception as exc:  # noqa: BLE001 - stale claims remain retryable if the database is unavailable
            logger.error(
                "Background interpretation for record %s could not be marked failed (error_type=%s)",
                prediction_id,
                type(exc).__name__,
            )
    finally:
        try:
            db.close()
        except Exception as exc:  # noqa: BLE001 - closing a broken connection must not mask the task failure
            logger.error(
                "Background interpretation for record %s could not close its session (error_type=%s)",
                prediction_id,
                type(exc).__name__,
            )
