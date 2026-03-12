"""Health and system status endpoints."""

from datetime import datetime

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.crud.card import get_total_cards_count
from app.crud.prediction import get_total_predictions_count
from app.crud.spread import get_total_spreads_count
from app.crud.user import get_total_users_count
from app.db.session import get_db
from app.services.tarot_service import tarot_interpretation_service

router = APIRouter()


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
):
    try:
        db_status = True
        db_error = None
        try:
            total_cards = get_total_cards_count(db)
            total_spreads = get_total_spreads_count(db)
            total_users = get_total_users_count(db)
            total_predictions = get_total_predictions_count(db)
        except Exception:
            db_status = False
            db_error = "database_unavailable"
            total_cards = total_spreads = total_users = total_predictions = -1

        ai_status = await tarot_interpretation_service.health_check()

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
                "ai_service": ai_status,
                "api": {
                    "status": "healthy",
                    "details": "api operational",
                },
            },
            "statistics": {
                "total_tarot_cards": total_cards,
                "total_spreads": total_spreads,
                "total_users": total_users,
                "total_predictions": total_predictions,
                "data_integrity": "good" if total_cards == 78 and total_spreads >= 6 else "warning",
            },
        }

        if not db_status:
            status_payload["overall_status"] = "unhealthy"
        elif not ai_status.get("is_healthy", False):
            status_payload["overall_status"] = "degraded"
            if ai_status.get("status") == "not_configured":
                status_payload["components"]["ai_service"]["message"] = "AI service not configured"

        return status_payload
    except Exception:
        return {
            "timestamp": datetime.now().isoformat(),
            "service_name": "tarot-game-api",
            "overall_status": "unhealthy",
            "error": "health_check_failed",
        }


@router.get("/ai", summary="AI service status")
async def ai_service_status():
    try:
        status_payload = await tarot_interpretation_service.health_check()
        return {
            "timestamp": datetime.now().isoformat(),
            **status_payload,
        }
    except Exception:
        return {
            "timestamp": datetime.now().isoformat(),
            "service_name": "tarot-interpretation-service",
            "status": "error",
            "error": "ai_health_check_failed",
        }


@router.get("/metrics", summary="System metrics")
async def system_metrics(
    db: Session = Depends(get_db),
):
    try:
        metrics = {
            "timestamp": datetime.now().isoformat(),
            "database": {
                "tarot_cards_count": get_total_cards_count(db),
                "spreads_count": get_total_spreads_count(db),
                "users_count": get_total_users_count(db),
                "predictions_count": get_total_predictions_count(db),
            },
            "ai_service": {
                "configured": tarot_interpretation_service.coze_service.is_configured(),
                "service_type": "coze" if tarot_interpretation_service.coze_service.is_configured() else "mock",
            },
        }

        expected_cards = 78
        expected_spreads = 6
        cards_score = min(metrics["database"]["tarot_cards_count"] / expected_cards * 100, 100)
        spreads_score = min(metrics["database"]["spreads_count"] / expected_spreads * 100, 100)

        metrics["data_integrity"] = {
            "cards_completeness": f"{cards_score:.1f}%",
            "spreads_completeness": f"{spreads_score:.1f}%",
            "overall_score": f"{(cards_score + spreads_score) / 2:.1f}%",
        }
        return metrics
    except Exception:
        return {
            "timestamp": datetime.now().isoformat(),
            "error": "metrics_generation_failed",
            "status": "failed",
        }
