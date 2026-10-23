import pytest

from app.services.enhanced_tarot_interpretation import EnhancedTarotInterpretationService


@pytest.fixture()
def parse_response():
    service = EnhancedTarotInterpretationService()

    def parse(response):
        return service._parse_spread_aware_response(
            response, "Original question", "general",
            {"id": 7, "name": "Acceptance spread", "description": "Original description"},
            [{"card_name": "The Fool", "position_name": "Now", "reversed": True,
              "position_meaning": "Current situation", "card_meaning": "Consider the next step"}],
        )

    return parse


@pytest.mark.parametrize("response,overview", [
    ('{"interpretation":{"overview":"Direct"}}', "Direct"),
    ('```json\n{"interpretation":{"overview":"Fenced"}}\n```', "Fenced"),
    ('Before {"interpretation":{"overview":"Embedded"}} after', "Embedded"),
    pytest.param(
        '{"interpretation":{"overview":"First"}} {"interpretation":{"overview":"Second"}}',
        "First", id="first-object",
    ),
    ('```json\nnot-json\n```\n{"interpretation":{"overview":"Outside"}}', "Outside"),
    ('[{"interpretation":{"overview":"Inside array"}}]', "Inside array"),
    (r'Before {"interpretation":{"overview":"Quoted {x}, \"q\", and \\ path"}} after',
     'Quoted {x}, "q", and \\ path'),
])
def test_structured_response_preserves_overview_and_reading_context(parse_response, response, overview):
    result = parse_response(response)
    assert result["interpretation"]["overview"] == overview
    assert result["interpretation"]["card_details"] == []
    assert result["interpretation"]["action_recommendations"] == {}
    assert (result["question"], result["question_type"]) == ("Original question", "general")
    assert result["spread_info"] == {
        "id": 7, "name": "Acceptance spread", "description": "Original description", "card_count": 1,
    }
    assert result["cards_drawn"] == [{
        "name": "The Fool", "position": "Now", "is_reversed": True,
        "position_meaning": "Current situation", "card_meaning": "Consider the next step",
    }]


@pytest.mark.parametrize("response", [
    "Plain \u4e2d\u6587\U0001f319 reading", "", "{}", "[]", '"quoted text"', '{"interpretation":',
])
def test_plain_or_unusable_json_keeps_text_in_fallback(parse_response, response):
    result = parse_response(response)
    assert result["interpretation"]["overview"] == response
    assert (result["question"], result["question_type"], result["spread_type"]) == (
        "Original question", "general", "Acceptance spread",
    )
    assert result["spread_info"]["card_count"] == 1
    assert result["cards_drawn"][0]["is_reversed"] is True
    assert result["interpretation"]["action_recommendations"]["immediate"]


@pytest.mark.parametrize("length,kept,suffix", [(799, 799, ""), (800, 800, ""), (801, 800, "...")])
def test_text_fallback_truncates_by_characters_without_changing_boundary(parse_response, length, kept, suffix):
    result = parse_response("\u6708" * length)
    assert result["interpretation"]["overview"] == "\u6708" * kept + suffix
