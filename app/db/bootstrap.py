import logging

from sqlalchemy.orm import Session

from app.crud.card import get_total_cards_count
from app.crud.spread import get_total_spreads_count
from app.core.config import settings
from app.db.maintenance import repair_prediction_questions
from app.scripts.init_tarot_data import init_spreads, init_tarot_cards

logger = logging.getLogger(__name__)


def ensure_reference_data(db: Session) -> dict:
    """Ensure tarot cards and spread definitions exist in the database."""
    result = {
        "cards_before": 0,
        "spreads_before": 0,
        "cards_after": 0,
        "spreads_after": 0,
        "cards_imported": 0,
        "spreads_imported": 0,
        "questions_repaired": 0,
    }

    result["cards_before"] = get_total_cards_count(db)
    result["spreads_before"] = get_total_spreads_count(db, active_only=False)

    if result["cards_before"] == 0:
        logger.info("No tarot cards found in database. Importing seed data.")
        result["cards_imported"] = init_tarot_cards(db)

    if result["spreads_before"] == 0:
        logger.info("No spreads found in database. Importing seed data.")
        result["spreads_imported"] = init_spreads(db)

    result["cards_after"] = get_total_cards_count(db)
    result["spreads_after"] = get_total_spreads_count(db, active_only=False)

    if settings.AUTO_REPAIR_PREDICTION_QUESTIONS_ON_STARTUP:
        maintenance = repair_prediction_questions(db, include_synthetic=True, commit=True)
        result["questions_repaired"] = maintenance["repaired"]

    return result
