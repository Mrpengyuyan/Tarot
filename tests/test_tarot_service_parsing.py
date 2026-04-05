"""Parametrized tests for TarotInterpretationService JSON parsing robustness.

These tests cover various AI response formats that may occur in production,
including markdown fences, nested interpretation objects, confidence clamping,
and plain text fallback — all of which directly affect interpretation quality.
"""
from __future__ import annotations

import pytest

from app.services.tarot_service import TarotInterpretationService


@pytest.fixture()
def service():
    return TarotInterpretationService()


# ── _extract_json_candidate ────────────────────────────────────────────

class TestExtractJsonCandidate:
    def test_extracts_from_markdown_code_fence(self, service):
        text = '```json\n{"overall_interpretation": "ok"}\n```'
        result = service._extract_json_candidate(text)
        assert result == '{"overall_interpretation": "ok"}'

    def test_extracts_from_markdown_code_fence_case_insensitive(self, service):
        text = '```JSON\n{"key": "value"}\n```'
        result = service._extract_json_candidate(text)
        assert result == '{"key": "value"}'

    def test_extracts_bare_json_object(self, service):
        text = '{"overall_interpretation": "bare object"}'
        result = service._extract_json_candidate(text)
        assert result == text

    def test_extracts_json_embedded_in_prose(self, service):
        text = 'Here is the reading:\n{"overall_interpretation": "embedded"}\nEnjoy!'
        result = service._extract_json_candidate(text)
        assert '"embedded"' in result

    def test_handles_nested_braces(self, service):
        text = '{"outer": {"inner": "value"}, "key": "ok"}'
        result = service._extract_json_candidate(text)
        assert result == text

    def test_returns_none_for_empty_input(self, service):
        assert service._extract_json_candidate("") is None
        assert service._extract_json_candidate(None) is None
        assert service._extract_json_candidate("   ") is None

    def test_returns_none_for_no_json(self, service):
        assert service._extract_json_candidate("This is plain text with no JSON.") is None

    def test_handles_json_with_chinese_content(self, service):
        text = '{"overall_interpretation": "整体运势良好，保持积极心态。"}'
        result = service._extract_json_candidate(text)
        assert "整体运势" in result


# ── _parse_interpretation_payload ────────────────────────────────────────

class TestParseInterpretationPayload:
    def test_standard_json_response(self, service):
        response = '{"overall_interpretation": "Good outlook", "advice": "Stay focused"}'
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == "Good outlook"
        assert parsed["advice"] == "Stay focused"

    def test_nested_interpretation_object(self, service):
        """AI sometimes wraps the content in an 'interpretation' key."""
        response = '''{
            "interpretation": {
                "overall_interpretation": "Nested content",
                "advice": "Nested advice"
            }
        }'''
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == "Nested content"
        assert parsed["advice"] == "Nested advice"

    def test_alternative_field_names(self, service):
        """AI may use alternative field names that should be mapped correctly."""
        response = '''{
            "overall": "Overall via alt key",
            "cards_analysis": "Card analysis via alt",
            "action_recommendations": "Action via alt",
            "risk_warning": "Warning via alt",
            "conclusion": "Conclusion as summary",
            "themes": ["theme1", "theme2"]
        }'''
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == "Overall via alt key"
        assert parsed["card_analysis"] == "Card analysis via alt"
        assert parsed["advice"] == "Action via alt"
        assert parsed["warning"] == "Warning via alt"
        assert parsed["summary"] == "Conclusion as summary"
        assert "theme1" in parsed["key_themes"]

    def test_confidence_score_clamped_to_0_1(self, service):
        """Confidence values > 1.0 should be clamped to 1.0."""
        response = '{"overall_interpretation": "Test", "confidence_score": 1.5}'
        parsed = service._parse_interpretation_payload(response)
        assert parsed["confidence_score"] == 1.0

    def test_confidence_score_clamped_negative(self, service):
        """Negative confidence should be clamped to 0.0."""
        response = '{"overall_interpretation": "Test", "confidence_score": -0.5}'
        parsed = service._parse_interpretation_payload(response)
        assert parsed["confidence_score"] == 0.0

    def test_confidence_score_none_when_invalid(self, service):
        """Non-numeric confidence should be set to None."""
        response = '{"overall_interpretation": "Test", "confidence_score": "high"}'
        parsed = service._parse_interpretation_payload(response)
        assert parsed["confidence_score"] is None

    def test_confidence_score_valid(self, service):
        """Valid confidence should pass through unchanged."""
        response = '{"overall_interpretation": "Test", "confidence_score": 0.87}'
        parsed = service._parse_interpretation_payload(response)
        assert parsed["confidence_score"] == pytest.approx(0.87, abs=0.01)

    def test_plain_text_fallback(self, service):
        """Non-JSON response should be used as overall_interpretation directly."""
        response = "The cards indicate a period of growth and transformation."
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == response
        assert parsed["confidence_score"] is None

    def test_markdown_fenced_json(self, service):
        """JSON inside markdown code fence should be extracted correctly."""
        response = """Here is your reading:
```json
{
    "overall_interpretation": "从牌面来看，整体趋势积极。",
    "advice": "保持耐心",
    "confidence_score": 0.9
}
```
Good luck!"""
        parsed = service._parse_interpretation_payload(response)
        assert "积极" in parsed["overall_interpretation"]
        assert parsed["advice"] == "保持耐心"
        assert parsed["confidence_score"] == pytest.approx(0.9, abs=0.01)

    def test_key_themes_as_list(self, service):
        """key_themes as a list should be joined into a comma-separated string."""
        response = '{"overall_interpretation": "Test", "key_themes": ["love", "patience", "growth"]}'
        parsed = service._parse_interpretation_payload(response)
        assert "love" in parsed["key_themes"]
        assert "patience" in parsed["key_themes"]
        assert "growth" in parsed["key_themes"]

    def test_key_themes_as_string(self, service):
        """key_themes as a string should be preserved."""
        response = '{"overall_interpretation": "Test", "key_themes": "love, patience"}'
        parsed = service._parse_interpretation_payload(response)
        assert "love" in parsed["key_themes"]

    def test_all_fields_none_in_json(self, service):
        """All optional fields being null should not crash."""
        response = '''{
            "overall_interpretation": "Only this field set",
            "card_analysis": null,
            "advice": null,
            "warning": null,
            "summary": null,
            "key_themes": null,
            "confidence_score": null
        }'''
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == "Only this field set"
        assert parsed["card_analysis"] is None
        assert parsed["advice"] is None
        assert parsed["confidence_score"] is None

    def test_empty_json_object_falls_back_to_raw_text(self, service):
        """An empty JSON object should use the raw text as overall_interpretation."""
        response = "{}"
        parsed = service._parse_interpretation_payload(response)
        assert parsed["overall_interpretation"] == "{}"


# ── _stringify ──────────────────────────────────────────────────────────

class TestStringify:
    def test_none_returns_none(self, service):
        assert service._stringify(None) is None

    def test_empty_string_returns_none(self, service):
        assert service._stringify("") is None
        assert service._stringify("   ") is None

    def test_string_returns_stripped(self, service):
        assert service._stringify("  hello  ") == "hello"

    def test_list_joins_items(self, service):
        assert service._stringify(["a", "b", "c"]) == "a, b, c"

    def test_list_skips_empty_items(self, service):
        assert service._stringify(["a", "", " ", "b"]) == "a, b"

    def test_empty_list_returns_none(self, service):
        assert service._stringify([]) is None

    def test_dict_formats_key_value(self, service):
        result = service._stringify({"key1": "value1", "key2": "value2"})
        assert "key1: value1" in result
        assert "key2: value2" in result

    def test_dict_skips_empty_values(self, service):
        result = service._stringify({"key1": "value1", "key2": "", "key3": "value3"})
        assert "key2" not in result

    def test_number_returns_as_string(self, service):
        assert service._stringify(42) == "42"
