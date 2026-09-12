"""Health and system status endpoints."""

from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.responses import JSONResponse
from sqlalchemy.orm import Session

from app.api.deps import get_current_superuser, get_current_user_optional
from app.core.config import settings
from app.crud.card import get_total_cards_count
from app.crud.spread import get_total_spreads_count
from app.db.session import get_db
from app.models.user import User as UserModel
from app.services.tarot_service import tarot_interpretation_service

router = APIRouter()


def _is_production_like_environment() -> bool:
    environment = str(settings.ENVIRONMENT).strip().lower()
    return environment in {"prod", "production", "staging"}


def _requires_admin_for_detailed_health() -> bool:
    return bool(settings.HEALTH_ADMIN_ONLY or _is_production_like_environment())


def _resolve_is_configured(status_payload: dict) -> bool:
    return bool(
        status_payload.get("provider_configured")
        if status_payload.get("provider_configured") is not None
        else status_payload.get("coze_configured", False)
    )


def _health_http_status(health_status: str) -> int:
    return status.HTTP_200_OK if health_status == "healthy" else status.HTTP_503_SERVICE_UNAVAILABLE


def _sanitize_ai_message(health_status: str, raw_message: str | None) -> str | None:
    if health_status == "healthy":
        return raw_message or "AI service is available"
    if health_status == "not_configured":
        return "AI service not configured"
    return "AI service unavailable"


async def _call_ai_health_check(*, deep: bool) -> dict:
    try:
        return await tarot_interpretation_service.health_check(deep=deep)
    except TypeError:
        # Backward-compatible path for tests or legacy monkeypatches without `deep`.
        return await tarot_interpretation_service.health_check()


@router.get("/", summary="Basic health check")
async def health_check():
    return {
        "status": "healthy",
        "timestamp": datetime.now().isoformat(),
        "service": "tarot-game-api",
        "version": "1.0.0",
    }


@router.get("/status", summary="Detailed system status")
async def system_status(
    db: Session = Depends(get_db),
    current_user: UserModel | None = Depends(get_current_user_optional),
):
    if _requires_admin_for_detailed_health() and not (current_user and current_user.is_superuser):
        raise HTTPException(status_code=403, detail="Admin privileges required")

    try:
        db_status = True
        db_error = None
        try:
            total_cards = get_total_cards_count(db)
            total_spreads = get_total_spreads_count(db)
        except Exception:
            db_status = False
            db_error = "database_unavailable"
            total_cards = total_spreads = -1

        ai_status = await _call_ai_health_check(deep=False)
        ai_health_status = str(ai_status.get("status", "unknown"))
        public_ai_status = {
            "status": ai_health_status,
            "is_healthy": bool(ai_status.get("is_healthy", False)),
            "is_configured": _resolve_is_configured(ai_status),
            "provider": ai_status.get("provider", "deepseek"),
            "message": _sanitize_ai_message(ai_health_status, ai_status.get("message")),
        }

        status_payload = {
            "timestamp": datetime.now().isoformat(),
            "service_name": "tarot-game-api",
            "version": "1.0.0",
            "overall_status": "healthy",
            "components": {
                "database": {
                    "status": "healthy" if db_status else "unhealthy",
                    "details": {
                        "connected": db_status,
                        "error": db_error,
                    },
                },
                "ai_service": public_ai_status,
                "api": {
                    "status": "healthy",
                    "details": "api operational",
                },
            },
            "statistics": {
                "total_tarot_cards": total_cards,
                "total_spreads": total_spreads,
                "data_integrity": "good" if total_cards > 0 and total_spreads > 0 else "warning",
            },
        }

        if not db_status:
            status_payload["overall_status"] = "unhealthy"
        elif not ai_status.get("is_healthy", False):
            status_payload["overall_status"] = "degraded"

        return JSONResponse(
            status_code=_health_http_status(status_payload["overall_status"]),
            content=status_payload,
        )
    except Exception:
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content={
                "timestamp": datetime.now().isoformat(),
                "service_name": "tarot-game-api",
                "overall_status": "unhealthy",
                "error": "health_check_failed",
            },
        )


@router.get("/ai", summary="AI service status")
async def ai_service_status():
    try:
        status_payload = await _call_ai_health_check(deep=False)
        details = status_payload.get("details") or {}
        if isinstance(details, dict):
            details = {
                "model_used": details.get("model_used"),
                "fallback_used": details.get("fallback_used"),
            }
        else:
            details = None

        health_status = str(status_payload.get("status", "unknown"))
        response_payload = {
            "timestamp": datetime.now().isoformat(),
            "service_name": status_payload.get("service_name", "tarot-interpretation-service"),
            "status": health_status,
            "is_healthy": bool(status_payload.get("is_healthy", False)),
            "is_configured": _resolve_is_configured(status_payload),
            "provider": status_payload.get("provider"),
            "message": _sanitize_ai_message(health_status, status_payload.get("message")),
            "details": details,
        }
        return JSONResponse(
            status_code=_health_http_status(health_status),
            content=response_payload,
        )
    except Exception:
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content={
                "timestamp": datetime.now().isoformat(),
                "service_name": "tarot-interpretation-service",
                "status": "error",
                "error": "ai_health_check_failed",
            },
        )


@router.get("/ai/deep", summary="Admin deep AI health probe")
async def ai_service_status_deep(
    current_user: UserModel = Depends(get_current_superuser),
):
    del current_user
    try:
        status_payload = await _call_ai_health_check(deep=True)
        health_status = str(status_payload.get("status", "unknown"))
        return JSONResponse(
            status_code=_health_http_status(health_status),
            content={
                "timestamp": datetime.now().isoformat(),
                "service_name": status_payload.get("service_name", "tarot-interpretation-service"),
                "status": health_status,
                "is_healthy": bool(status_payload.get("is_healthy", False)),
                "is_configured": _resolve_is_configured(status_payload),
                "provider": status_payload.get("provider"),
                "message": _sanitize_ai_message(health_status, status_payload.get("message")),
                "details": status_payload.get("details"),
            },
        )
    except Exception:
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content={
                "timestamp": datetime.now().isoformat(),
                "service_name": "tarot-interpretation-service",
                "status": "error",
                "error": "ai_deep_health_check_failed",
            },
        )


@router.get("/metrics", summary="System metrics")
async def system_metrics(
    db: Session = Depends(get_db),
    current_user: UserModel | None = Depends(get_current_user_optional),
):
    if _requires_admin_for_detailed_health() and not (current_user and current_user.is_superuser):
        raise HTTPException(status_code=403, detail="Admin privileges required")

    try:
        metrics = {
            "timestamp": datetime.now().isoformat(),
            "database": {
                "tarot_cards_count": get_total_cards_count(db),
                "spreads_count": get_total_spreads_count(db),
            },
            "ai_service": {
                "configured": tarot_interpretation_service.ai_service.is_configured(),
                "service_type": "deepseek" if tarot_interpretation_service.ai_service.is_configured() else "mock",
            },
        }

        expected_cards = int(settings.METRICS_EXPECTED_TAROT_CARDS or 0)
        expected_spreads = int(settings.METRICS_EXPECTED_SPREADS or 0)
        cards_score = (
            min(metrics["database"]["tarot_cards_count"] / expected_cards * 100, 100)
            if expected_cards > 0
            else None
        )
        spreads_score = (
            min(metrics["database"]["spreads_count"] / expected_spreads * 100, 100)
            if expected_spreads > 0
            else None
        )
        available_scores = [score for score in (cards_score, spreads_score) if score is not None]
        overall_score = (sum(available_scores) / len(available_scores)) if available_scores else None

        metrics["data_integrity"] = {
            "cards_completeness": f"{cards_score:.1f}%"
            if cards_score is not None
            else "n/a (no expected baseline configured)",
            "spreads_completeness": f"{spreads_score:.1f}%"
            if spreads_score is not None
            else "n/a (no expected baseline configured)",
            "overall_score": f"{overall_score:.1f}%"
            if overall_score is not None
            else "n/a (no expected baseline configured)",
            "expected_cards": expected_cards if expected_cards > 0 else None,
            "expected_spreads": expected_spreads if expected_spreads > 0 else None,
        }
        return metrics
    except Exception:
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content={
                "timestamp": datetime.now().isoformat(),
                "error": "metrics_generation_failed",
                "status": "failed",
            },
        )
