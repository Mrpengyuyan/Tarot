"""Deprecated compatibility layer.

This module used to host an older implementation. Keep it import-safe and
re-export the active implementation from `app.services.tarot_service`.
"""

from app.services.tarot_service import TarotInterpretationService, tarot_interpretation_service

__all__ = ["TarotInterpretationService", "tarot_interpretation_service"]

