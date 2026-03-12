"""
Tarot interpretation service.

Responsibilities:
1. Build prompt with spread/question/cards context.
2. Call Coze and parse a structured interpretation payload.
3. Provide optional mock fallback for local debugging.
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
        orientation = "逆位" if is_reversed else "正位"
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
    def create_interpretation_prompt(
        *,
        question: str,
        question_type: str,
        spread_name: str,
        spread_description: str,
        cards: List[Dict[str, Any]],
        user_context: Optional[str] = None,
    ) -> str:
        payload = {
            "question": question,
            "question_type": question_type,
            "spread_name": spread_name,
            "spread_description": spread_description,
            "cards": cards,
            "user_context": user_context or "",
        }

        output_schema = {
            "overall_interpretation": "string, 必填，完整整体解读",
            "card_analysis": "string, 可选，逐牌分析",
            "relationship_analysis": "string, 可选，牌与牌之间关系",
            "advice": "string, 可选，行动建议",
            "warning": "string, 可选，风险提醒",
            "summary": "string, 可选，简短总结",
            "key_themes": "string 或 string[]，可选，关键词",
            "confidence_score": "number(0-1), 可选",
        }

        return (
            "你是一位专业塔罗解读师。请基于输入内容生成清晰、温和、可执行的中文解读。\n"
            "严格要求：\n"
            "1) 只能输出 JSON 对象；\n"
            "2) 不要输出 markdown 代码块；\n"
            "3) 不要输出 JSON 以外的任何解释文字；\n"
            "4) 若无法提供某字段可返回空字符串。\n\n"
            f"输入数据(JSON):\n{json.dumps(payload, ensure_ascii=False)}\n\n"
            f"输出字段(JSON Schema说明):\n{json.dumps(output_schema, ensure_ascii=False)}"
        )


class TarotInterpretationService:
    def __init__(self) -> None:
        self.coze_service = coze_service

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
        spread_name = spread.name if spread else "未知牌阵"
        spread_description = spread.description if spread else ""

        formatted_cards: List[Dict[str, Any]] = []
        for item in cards_data:
            card = item.get("card")
            position = item.get("position", "")
            is_reversed = bool(item.get("is_reversed", False))
            if isinstance(card, TarotCard):
                formatted_cards.append(TarotPromptTemplate.format_card(card, position, is_reversed))

        prompt = TarotPromptTemplate.create_interpretation_prompt(
            question=prediction.question,
            question_type=question_type,
            spread_name=spread_name,
            spread_description=spread_description,
            cards=formatted_cards,
            user_context=user_context,
        )

        if not self.coze_service.is_configured():
            message = "Coze is not configured (missing COZE_API_KEY/COZE_BOT_ID)."
            if settings.ALLOW_MOCK_AI_FALLBACK:
                logger.warning("%s Use mock interpretation fallback.", message)
                return self._create_mock_interpretation(prediction, formatted_cards, reason=message)
            raise CozeError(message)

        try:
            response_text = await self.coze_service.send_message_and_wait(
                message=prompt,
                user_id=str(prediction.user_id),
                max_wait_time=60,
            )
            parsed = self._parse_interpretation_payload(response_text)
            parsed.setdefault("model_used", "coze_ai")
            parsed.setdefault("model_version", None)
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
            return "，".join(items) if items else None
        if isinstance(value, dict):
            compact = []
            for k, v in value.items():
                text = str(v).strip()
                if text:
                    compact.append(f"{k}: {text}")
            return "\n".join(compact) if compact else None
        text = str(value).strip()
        return text or None

    def _extract_json_candidate(self, text: str) -> Optional[str]:
        raw = (text or "").strip()
        if not raw:
            return None

        # 1) fenced code block
        fenced = re.search(r"```(?:json)?\s*([\s\S]*?)```", raw, flags=re.IGNORECASE)
        if fenced:
            candidate = fenced.group(1).strip()
            if candidate.startswith("{") and candidate.endswith("}"):
                return candidate

        # 2) full text JSON
        if raw.startswith("{") and raw.endswith("}"):
            return raw

        # 3) first balanced JSON object in text
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

        # Allow nested structures like {"interpretation": {...}}
        nested = obj.get("interpretation")
        if isinstance(nested, dict):
            source = {**obj, **nested}
        else:
            source = obj

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
            name = card.get("name_zh", "未知牌")
            orientation = card.get("orientation", "正位")
            position = card.get("position", f"位置{idx}")
            card_lines.append(f"{idx}. {position} - {name}（{orientation}）")

        overall = (
            f"这是模拟解读结果（{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}）。\n"
            f"问题：{prediction.question}\n"
            "牌面显示你正处在一个需要平衡理性与直觉的阶段。"
            "请先关注近期最可控的决策，保持行动节奏与复盘习惯。"
        )

        card_analysis = "\n".join(card_lines) if card_lines else None
        advice = "把问题拆成 1-2 周可执行的小目标，优先处理影响最大的事项。"
        warning = "避免因为焦虑而频繁推翻计划。"
        summary = "先稳住节奏，再逐步扩大行动范围。"

        if cards:
            themes = [str(cards[0].get("name_zh", "")).strip(), "节奏", "选择"]
            key_themes = "，".join([t for t in themes if t])
        else:
            key_themes = "节奏，选择"

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
            "coze_configured": self.coze_service.is_configured(),
            "coze_healthy": False,
            "status": "unknown",
            "is_healthy": False,
        }

        if not status["coze_configured"]:
            status["status"] = "not_configured"
            status["message"] = (
                "Coze is not configured. "
                + ("Mock fallback is enabled." if settings.ALLOW_MOCK_AI_FALLBACK else "Mock fallback is disabled.")
            )
            return status

        try:
            coze_health = await self.coze_service.health_check()
            coze_ok = bool(coze_health.get("is_healthy"))
            status["coze_healthy"] = coze_ok
            status["is_healthy"] = coze_ok
            status["status"] = "healthy" if coze_ok else "degraded"
            status["details"] = coze_health
            if not coze_ok:
                status["message"] = coze_health.get("message", "Coze health check failed.")
            return status
        except Exception as exc:
            logger.error("Interpretation service health check failed: %s", exc)
            status["status"] = "unhealthy"
            status["is_healthy"] = False
            status["message"] = str(exc)
            return status


tarot_interpretation_service = TarotInterpretationService()
