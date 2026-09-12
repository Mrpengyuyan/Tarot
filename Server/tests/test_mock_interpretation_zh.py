from __future__ import annotations

import re
from types import SimpleNamespace

from app.services.tarot_service import tarot_interpretation_service

TEXT_FIELDS = ("overall_interpretation", "advice", "warning", "summary")


def test_mock_interpretation_is_chinese_and_marked_as_mock():
    payload = tarot_interpretation_service._create_mock_interpretation(
        SimpleNamespace(question="我接下来该专注什么？"),
        [
            {"name_zh": "愚者", "position": "过去", "orientation": "upright"},
            {"name_zh": "女祭司", "position": "现在", "orientation": "reversed"},
        ],
        reason="",
    )

    assert payload["model_used"] == "mock_ai"
    assert "模拟解读" in payload["overall_interpretation"]
    assert "我接下来该专注什么？" in payload["overall_interpretation"]
    assert payload["card_analysis"] == "1. 过去：愚者（正位）\n2. 现在：女祭司（逆位）"
    assert payload["key_themes"] == "愚者,节奏,专注"
    for field in TEXT_FIELDS:
        assert not re.search(r"[A-Za-z]{3,}", payload[field]), f"{field} still has English: {payload[field]!r}"


def test_mock_interpretation_without_cards_uses_chinese_defaults():
    payload = tarot_interpretation_service._create_mock_interpretation(
        SimpleNamespace(question="今天怎样？"),
        [],
        reason="",
    )

    assert payload["card_analysis"] is None
    assert payload["key_themes"] == "节奏,专注"
    assert "模拟解读" in payload["overall_interpretation"]
    for field in TEXT_FIELDS:
        assert not re.search(r"[A-Za-z]{3,}", payload[field]), f"{field} still has English: {payload[field]!r}"
