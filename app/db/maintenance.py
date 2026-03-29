"""Database maintenance helpers."""

from __future__ import annotations

import re
from typing import Any

from sqlalchemy.orm import Session, joinedload

from app.models.record import CardDraw, Prediction
from app.models.spread import SpreadType

INVALID_QUESTION_PATTERN = re.compile(r"^[?\uFFFD锛燂拷\s]+$")
SYNTHETIC_QUESTION_PATTERNS = [
    re.compile(r"^关于(?:感情关系|事业发展|财务规划|身心状态|当前状态|人生课题)的占卜(?:（.*）)?$"),
    re.compile(r"^关于当前状态的占卜：.+$"),
]


def is_corrupted_question(value: str | None) -> bool:
    text = (value or "").strip()
    if not text:
        return True

    if INVALID_QUESTION_PATTERN.fullmatch(text):
        return True

    invalid_count = len(re.findall(r"[?\uFFFD锛燂拷]", text))
    return invalid_count > 0 and invalid_count / len(text) >= 0.5


def is_synthetic_question(value: str | None) -> bool:
    text = (value or "").strip()
    return any(pattern.fullmatch(text) for pattern in SYNTHETIC_QUESTION_PATTERNS)


def build_replacement(
    question_type: str | None,
    spread_name: str | None,
    spread_name_en: str | None,
    card_name: str | None,
    is_reversed: bool | None,
) -> str:
    normalized_type = (question_type or "").strip()
    normalized_spread = (spread_name or "").strip().lower()
    normalized_spread_en = (spread_name_en or "").strip().lower()
    card_title = ""

    if card_name and card_name.strip():
        position_suffix = "逆位" if is_reversed else "正位"
        card_title = f"{card_name.strip()}{position_suffix}"

    if normalized_type == "love":
        if card_title:
            return f"如果参考{card_title}，这段关系接下来会如何发展？"
        return "这段关系接下来会如何发展？"

    if normalized_type == "career":
        if card_title:
            return f"结合{card_title}，我最近的工作方向应该如何调整？"
        return "我最近的工作方向应该如何调整？"

    if normalized_type == "finance":
        if card_title:
            return f"从{card_title}来看，我最近的财务决策需要注意什么？"
        return "我最近的财务决策需要注意什么？"

    if normalized_type == "health":
        if card_title:
            return f"参考{card_title}，我最近的状态该如何调节？"
        return "我最近的状态该如何调节？"

    if "past-present-future" in normalized_spread_en or "过去" in normalized_spread:
        if card_title:
            return f"围绕{card_title}的提示，这件事接下来最值得我关注的变化是什么？"
        return "这件事接下来最值得我关注的变化是什么？"

    if "celtic" in normalized_spread_en or "凯尔特" in normalized_spread:
        if card_title:
            return f"从{card_title}来看，我当下面对的核心课题是什么？"
        return "我当下面对的核心课题是什么？"

    if card_title:
        return f"{card_title}现在最想提醒我的是什么？"

    return "我现在最需要关注什么？"


def repair_prediction_questions(
    db: Session,
    include_synthetic: bool = True,
    commit: bool = True,
) -> dict[str, Any]:
    repaired = 0
    scanned = 0

    predictions = (
        db.query(Prediction)
        .options(
            joinedload(Prediction.spread_type),
            joinedload(Prediction.card_draws).joinedload(CardDraw.tarot_card),
        )
        .order_by(Prediction.id.asc())
        .all()
    )

    for prediction in predictions:
        scanned += 1
        needs_repair = is_corrupted_question(prediction.question)
        if not needs_repair and include_synthetic:
            needs_repair = is_synthetic_question(prediction.question)

        if not needs_repair:
            continue

        spread = prediction.spread_type if isinstance(prediction.spread_type, SpreadType) else None
        primary_draw = None
        if prediction.card_draws:
            primary_draw = sorted(prediction.card_draws, key=lambda item: item.position)[0]

        prediction.question = build_replacement(
            prediction.question_type.value if prediction.question_type else None,
            getattr(spread, "name", None),
            getattr(spread, "name_en", None),
            primary_draw.tarot_card.name_zh if primary_draw and primary_draw.tarot_card else None,
            primary_draw.is_reversed if primary_draw else None,
        )
        repaired += 1

    if commit:
        if repaired:
            db.commit()
        else:
            db.rollback()
    elif repaired:
        db.rollback()

    return {
        "scanned": scanned,
        "repaired": repaired,
        "include_synthetic": include_synthetic,
    }
