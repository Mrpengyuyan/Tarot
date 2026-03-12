from __future__ import annotations

from app.crud.prediction import create_prediction_with_stats
from app.crud.user import create_user
from app.models.spread import SpreadType
from app.schemas.prediction import PredictionCreate
from app.schemas.user import UserCreate
from app.services.tarot_service import TarotInterpretationService


def test_create_prediction_with_stats_updates_user_and_spread_counters(db_session):
    user = create_user(
        db_session,
        UserCreate(
            username="counter_user",
            email="counter_user@example.com",
            password="password123",
        ),
    )
    spread = SpreadType(
        name="Counter Spread",
        name_en="Counter Spread",
        description="Counter spread description",
        card_count=1,
        positions=[{"position": 1, "name": "Now", "meaning": "Current state"}],
        is_active=True,
        usage_count=0,
    )
    db_session.add(spread)
    db_session.commit()
    db_session.refresh(spread)

    prediction = create_prediction_with_stats(
        db_session,
        user_id=user.id,
        prediction_create=PredictionCreate(
            spread_type_id=spread.id,
            question="Verify counter update behavior",
            question_type="general",
        ),
    )

    db_session.refresh(user)
    db_session.refresh(spread)

    assert prediction.id is not None
    assert user.prediction_count == 1
    assert spread.usage_count == 1


def test_parse_interpretation_payload_handles_nested_json_and_plain_text():
    service = TarotInterpretationService()

    nested_json_response = """
    ```json
    {
      "interpretation": {
        "overall_interpretation": "Overall trend is positive; proceed step by step.",
        "advice": "Prioritize high-impact tasks first",
        "key_themes": ["pace", "focus"],
        "confidence_score": 1.5
      }
    }
    ```
    """
    parsed_nested = service._parse_interpretation_payload(nested_json_response)
    assert parsed_nested["overall_interpretation"] == "Overall trend is positive; proceed step by step."
    assert parsed_nested["advice"] == "Prioritize high-impact tasks first"
    assert parsed_nested["confidence_score"] == 1.0
    assert parsed_nested["key_themes"] is not None
    assert "pace" in parsed_nested["key_themes"]
    assert "focus" in parsed_nested["key_themes"]

    plain_text_response = "Model output that is not JSON."
    parsed_plain = service._parse_interpretation_payload(plain_text_response)
    assert parsed_plain["overall_interpretation"] == plain_text_response
    assert parsed_plain["confidence_score"] is None
