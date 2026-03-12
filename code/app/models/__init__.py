from app.db.base_class import Base

from .record import CardDraw, Interpretation, Prediction, PredictionStatus, QuestionType
from .spread import SpreadType
from .tarot_card import CardType, Suit, TarotCard
from .user import User

__all__ = [
    "Base",
    "User",
    "TarotCard",
    "CardType",
    "Suit",
    "SpreadType",
    "Prediction",
    "CardDraw",
    "Interpretation",
    "QuestionType",
    "PredictionStatus",
]
