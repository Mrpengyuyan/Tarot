from app.services.tarot_service import TarotPromptTemplate


def test_card_analysis_schema_asks_for_one_line_per_card():
    description = TarotPromptTemplate._output_schema()["card_analysis"]

    assert "one line per card" in description
    assert "（正位|逆位）" in description


def test_card_analysis_format_reaches_the_prompt():
    messages = TarotPromptTemplate.create_interpretation_messages(
        question="我接下来该专注什么？",
        question_type="general",
        spread_name="过去现在未来",
        spread_description="",
        cards=[],
    )

    assert messages[1]["role"] == "user"
    assert "one line per card" in messages[1]["content"]
