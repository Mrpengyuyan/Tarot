"""
Tarot interpretation service.

Responsibilities:
1. Build structured model input from spread/question/cards context.
2. Call LLM service (DeepSeek-backed compatibility wrapper).
3. Parse and normalize interpretation payload.
4. Provide optional mock fallback for local debugging.
"""

from __future__ import annotations

import json
import logging
import re
from datetime import datetime
from typing import Any, Dict, List, Optional

from sqlalchemy.orm import Session

from app.core.config import settings
from app.models.record import Prediction
from app.models.tarot_card import TarotCard
from app.services.coze_service import CozeError, coze_service

logger = logging.getLogger(__name__)


class TarotPromptTemplate:
    @staticmethod
    def format_card(card: TarotCard, position: str, is_reversed: bool) -> Dict[str, Any]:
        orientation = "reversed" if is_reversed else "upright"
        meaning = card.reversed_meaning if is_reversed else card.upright_meaning
        keywords = card.keywords_reversed if is_reversed else card.keywords_upright

        return {
            "name_zh": card.name_zh,
            "name_en": card.name_en,
            "position": position,
            "orientation": orientation,
            "meaning": meaning or "",
            "keywords": keywords or "",
            "description": card.description or "",
        }

    @staticmethod
    def _output_schema() -> Dict[str, str]:
        return {
            "overall_interpretation": "string, required, complete holistic reading in Simplified Chinese",
            "card_analysis": "string, optional, per-card analysis",
            "relationship_analysis": "string, optional, inter-card relationship analysis",
            "advice": "string, optional, practical next-step suggestions",
            "warning": "string, optional, risk reminders",
            "summary": "string, optional, concise summary",
            "key_themes": "string or string[], optional, key themes",
            "confidence_score": "number in [0,1], optional",
        }

    @classmethod
    def create_interpretation_messages(
        cls,
        *,
        question: str,
        question_type: str,
        spread_name: str,
        spread_description: str,
        cards: List[Dict[str, Any]],
        user_context: Optional[str] = None,
    ) -> List[Dict[str, str]]:
        payload = {
            "question": question,
            "question_type": question_type,
            "spread_name": spread_name,
            "spread_description": spread_description,
            "cards": cards,
            "user_context": user_context or "",
        }

        schema = cls._output_schema()
        system_prompt = (
            "You are an experienced tarot reader. "
            "Return only a valid JSON object. "
            "No markdown, no prose outside JSON. "
            "Write interpretation content in Simplified Chinese."
        )
        user_prompt = (
            "Interpret the tarot reading from the JSON input below.\n"
            f"Input JSON:\n{json.dumps(payload, ensure_ascii=False)}\n\n"
            "Output JSON schema (field description):\n"
            f"{json.dumps(schema, ensure_ascii=False)}\n\n"
            "If a field cannot be inferred, return an empty string for that field."
        )

        return [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt},
        ]


class TarotInterpretationService:
    def __init__(self) -> None:
        self.ai_service = coze_service
        # Keep old attribute name for compatibility with existing endpoints.
        self.coze_service = self.ai_service

    def default_model_name(self) -> str:
        if hasattr(self.ai_service, "chat_model"):
            return str(self.ai_service.chat_model)
        return "deepseek-chat"

    async def create_interpretation(
        self,
        db: Session,
        prediction: Prediction,
        cards_data: List[Dict[str, Any]],
        user_context: Optional[str] = None,
    ) -> Dict[str, Any]:
        del db  # currently unused, preserved for endpoint compatibility

        spread = prediction.spread_type
        question_type = prediction.question_type.value if prediction.question_type else "general"
        spread_name = spread.name if spread else "Unknown spread"
        spread_description = spread.description if spread else ""

        formatted_cards: List[Dict[str, Any]] = []
        for item in cards_data:
            card = item.get("card")
            position = item.get("position", "")
            is_reversed = bool(item.get("is_reversed", False))
            if isinstance(card, TarotCard):
                formatted_cards.append(TarotPromptTemplate.format_card(card, position, is_reversed))

        messages = TarotPromptTemplate.create_interpretation_messages(
            question=prediction.question,
            question_type=question_type,
            spread_name=spread_name,
            spread_description=spread_description,
            cards=formatted_cards,
            user_context=user_context,
        )

        if not self.ai_service.is_configured():
            message = "LLM provider is not configured (missing DEEPSEEK_API_KEY)."
            if settings.ALLOW_MOCK_AI_FALLBACK:
                logger.warning("%s Use mock interpretation fallback.", message)
                return self._create_mock_interpretation(prediction, formatted_cards, reason=message)
            raise CozeError(message)

        try:
            model_result = await self.ai_service.send_messages_and_wait(
                messages=messages,
                user_id=str(prediction.user_id),
                max_wait_time=60,
                question=prediction.question,
                user_context=user_context,
                expect_json=True,
            )
            response_text = str(model_result.get("text", "")).strip()
            parsed = self._parse_interpretation_payload(response_text)
            parsed.setdefault("model_used", model_result.get("model_used") or self.default_model_name())
            parsed.setdefault("model_version", model_result.get("model_version"))
            parsed.setdefault("confidence_score", 0.85)
            return parsed
        except Exception as exc:
            logger.error("AI interpretation generation failed: %s", exc)
            if settings.ALLOW_MOCK_AI_FALLBACK:
                return self._create_mock_interpretation(
                    prediction,
                    formatted_cards,
                    reason=f"AI call failed: {exc}",
                )
            raise

    @staticmethod
    def _stringify(value: Any) -> Optional[str]:
        if value is None:
            return None
        if isinstance(value, str):
            return value.strip() or None
        if isinstance(value, list):
            items = [str(v).strip() for v in value if str(v).strip()]
            return ", ".join(items) if items else None
        if isinstance(value, dict):
            compact = []
            for key, val in value.items():
                text = str(val).strip()
                if text:
                    compact.append(f"{key}: {text}")
            return "\n".join(compact) if compact else None
        text = str(value).strip()
        return text or None

    def _extract_json_candidate(self, text: str) -> Optional[str]:
        raw = (text or "").strip()
        if not raw:
            return None

        fenced = re.search(r"```(?:json)?\s*([\s\S]*?)```", raw, flags=re.IGNORECASE)
        if fenced:
            candidate = fenced.group(1).strip()
            if candidate.startswith("{") and candidate.endswith("}"):
                return candidate

        if raw.startswith("{") and raw.endswith("}"):
            return raw

        start = raw.find("{")
        if start < 0:
            return None

        depth = 0
        in_string = False
        escape = False
        for idx in range(start, len(raw)):
            ch = raw[idx]
            if escape:
                escape = False
                continue
            if ch == "\\":
                escape = True
                continue
            if ch == '"':
                in_string = not in_string
                continue
            if in_string:
                continue
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    return raw[start : idx + 1]
        return None

    def _parse_interpretation_payload(self, response_text: str) -> Dict[str, Any]:
        candidate = self._extract_json_candidate(response_text)
        obj: Dict[str, Any] = {}

        if candidate:
            try:
                parsed = json.loads(candidate)
                if isinstance(parsed, dict):
                    obj = parsed
            except json.JSONDecodeError:
                logger.warning("Failed to parse AI JSON payload, fallback to plain text.")

        nested = obj.get("interpretation")
        source = {**obj, **nested} if isinstance(nested, dict) else obj

        overall = self._stringify(
            source.get("overall_interpretation")
            or source.get("overall")
            or source.get("overview")
            or source.get("interpretation")
        )
        card_analysis = self._stringify(
            source.get("card_analysis")
            or source.get("cards_analysis")
            or source.get("card_details")
        )
        relationship_analysis = self._stringify(
            source.get("relationship_analysis")
            or source.get("card_connections")
            or source.get("relationship")
        )
        advice = self._stringify(
            source.get("advice")
            or source.get("action_recommendations")
            or source.get("recommendations")
        )
        warning = self._stringify(
            source.get("warning")
            or source.get("risk_warning")
            or source.get("caution")
        )
        summary = self._stringify(source.get("summary") or source.get("conclusion"))
        themes_raw = source.get("key_themes") or source.get("themes") or source.get("keywords")
        key_themes = self._stringify(themes_raw)

        confidence_value = source.get("confidence_score", source.get("confidence"))
        confidence_score: Optional[float] = None
        if confidence_value is not None:
            try:
                confidence_score = float(confidence_value)
                confidence_score = max(0.0, min(1.0, confidence_score))
            except (TypeError, ValueError):
                confidence_score = None

        if not overall:
            overall = response_text.strip()

        return {
            "overall_interpretation": overall or "",
            "card_analysis": card_analysis,
            "relationship_analysis": relationship_analysis,
            "advice": advice,
            "warning": warning,
            "summary": summary,
            "key_themes": key_themes,
            "confidence_score": confidence_score,
        }

    def _create_mock_interpretation(
        self,
        prediction: Prediction,
        cards: List[Dict[str, Any]],
        reason: str = "",
    ) -> Dict[str, Any]:
        card_lines = []
        for idx, card in enumerate(cards, start=1):
            name = card.get("name_zh", "Unknown card")
            orientation = card.get("orientation", "upright")
            position = card.get("position", f"Position {idx}")
            card_lines.append(f"{idx}. {position} - {name} ({orientation})")

        overall = (
            f"This is a mock interpretation generated at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}.\n"
            f"Question: {prediction.question}\n"
            "Current cards suggest balancing intuition with practical action. "
            "Focus on one high-impact step at a time and review progress regularly."
        )

        card_analysis = "\n".join(card_lines) if card_lines else None
        advice = "Break your next move into small, actionable steps and execute with consistency."
        warning = "Avoid overcommitting on too many goals in parallel."
        summary = "Maintain pace, prioritize clarity, and iterate."
        key_themes = ", ".join([card_lines[0].split(" - ")[1].split(" (")[0], "pace", "focus"]) if card_lines else "pace,focus"

        if reason:
            logger.warning("Using mock interpretation fallback. reason=%s", reason)

        return {
            "overall_interpretation": overall,
            "card_analysis": card_analysis,
            "relationship_analysis": None,
            "advice": advice,
            "warning": warning,
            "summary": summary,
            "key_themes": key_themes,
            "model_used": "mock_ai",
            "model_version": None,
            "confidence_score": 0.45,
        }

    async def health_check(self) -> Dict[str, Any]:
        status: Dict[str, Any] = {
            "service_name": "tarot_interpretation_service",
            "provider": "deepseek",
            "provider_configured": self.ai_service.is_configured(),
            "provider_healthy": False,
            "status": "unknown",
            "is_healthy": False,
        }

        # Backward-compatible keys used by older health callers.
        status["coze_configured"] = status["provider_configured"]
        status["coze_healthy"] = status["provider_healthy"]

        if not status["provider_configured"]:
            status["status"] = "not_configured"
            status["message"] = (
                "DeepSeek is not configured. "
                + ("Mock fallback is enabled." if settings.ALLOW_MOCK_AI_FALLBACK else "Mock fallback is disabled.")
            )
            return status

        try:
            provider_health = await self.ai_service.health_check()
            provider_ok = bool(provider_health.get("is_healthy"))
            status["provider_healthy"] = provider_ok
            status["coze_healthy"] = provider_ok
            status["is_healthy"] = provider_ok
            status["status"] = "healthy" if provider_ok else "degraded"
            status["details"] = provider_health
            if not provider_ok:
                status["message"] = provider_health.get("message", "Provider health check failed.")
            return status
        except Exception as exc:
            logger.error("Interpretation service health check failed: %s", exc)
            status["status"] = "unhealthy"
            status["is_healthy"] = False
            status["message"] = str(exc)
            return status


tarot_interpretation_service = TarotInterpretationService()

