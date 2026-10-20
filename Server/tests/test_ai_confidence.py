import json

import httpx
import pytest
from sqlalchemy import select

from app.models.budget import AIBudgetReservation
from app.services.budget_store import DatabaseBudgetStore
from app.services.coze_service import CozeService
from app.services.tarot_service import TarotInterpretationService, tarot_interpretation_service


ANSWER = "Usable answer \u5854\u7f57 \U0001f52e"
PRIVATE_QUESTION = "PRIVATE_CONFIDENCE_QUESTION"
INVALID_SCORES = [
    pytest.param("NaN", id="nan"), pytest.param("Infinity", id="infinity"),
    pytest.param("-Infinity", id="negative-infinity"), pytest.param("1e309", id="exponent-overflow"),
    pytest.param("-1e309", id="negative-exponent-overflow"), pytest.param('"NaN"', id="nan-string"),
    pytest.param('"Infinity"', id="infinity-string"), pytest.param('"-Infinity"', id="negative-infinity-string"),
    pytest.param('"1e309"', id="exponent-string"), pytest.param("9" * 400, id="integer-overflow"),
    pytest.param("-" + "9" * 400, id="negative-integer-overflow"),
    pytest.param("9" * 5000, id="decoder-overflow"),
    pytest.param("-" + "9" * 5000, id="negative-decoder-overflow"),
    pytest.param('"' + "9" * 5000 + '"', id="oversized-string"),
]


def _answer(literal, field="confidence_score", nested=False):
    payload = {"overall_interpretation": ANSWER, "summary": "Usable summary", "advice": "Usable advice"}
    if literal is not None:
        payload[field] = "RAW_SCORE"
    if nested:
        payload = {"interpretation": payload}
    text = json.dumps(payload)
    return text.replace('"RAW_SCORE"', literal) if literal is not None else text


@pytest.mark.parametrize("literal", INVALID_SCORES)
@pytest.mark.parametrize("field", ["confidence_score", "confidence"])
@pytest.mark.parametrize("nested", [False, True])
def test_nonfinite_or_overflow_confidence_is_unknown_without_losing_content(literal, field, nested, caplog):
    result = TarotInterpretationService()._parse_interpretation_payload(_answer(literal, field, nested))
    assert result["overall_interpretation"] == ANSWER
    assert result["summary"] == "Usable summary" and result["advice"] == "Usable advice"
    assert result["confidence_score"] is None
    assert "9" * 100 not in caplog.text


@pytest.fixture()
def upstream(monkeypatch, db_session_factory):
    state = {"answer": "", "usage": {"prompt_tokens": 20, "completion_tokens": 10}, "posts": [], "timeout": False}
    original_client = httpx.AsyncClient

    def respond(request):
        assert request.url.host == "127.0.0.1" and request.url.port == 9
        state["posts"].append(request)
        if state["timeout"]:
            raise httpx.ReadTimeout("Simulated lost response", request=request)
        return httpx.Response(200, json={
            "choices": [{"message": {"content": state["answer"]}}], "usage": state["usage"],
        })

    def mock_client(**kwargs):
        return original_client(transport=httpx.MockTransport(respond), **kwargs)

    monkeypatch.setattr("app.services.coze_service.httpx.AsyncClient", mock_client)
    store = DatabaseBudgetStore(db_session_factory)
    service = CozeService(budget_store=store)
    service.api_key = "isolated-confidence-key-not-real"
    service.base_url = "http://127.0.0.1:9"
    service.daily_budget_usd = service.monthly_budget_usd = 1
    service.chat_input_cost_per_m = service.chat_output_cost_per_m = 1
    service.max_output_tokens = 100
    monkeypatch.setattr(tarot_interpretation_service, "ai_service", service)
    return state, store


@pytest.mark.parametrize("mode", ["sync", "async", "manual-retry"])
@pytest.mark.parametrize("unknown_usage", [False, True])
@pytest.mark.parametrize("literal,expected", [
    pytest.param("NaN", 0.85, id="nan"), pytest.param("Infinity", 0.85, id="infinity"),
    pytest.param("-Infinity", 0.85, id="negative-infinity"),
    pytest.param("9" * 400, 0.85, id="integer-overflow"),
    pytest.param("9" * 5000, 0.85, id="decoder-overflow"),
    pytest.param("0.8", 0.8, id="normal"), pytest.param("1.5", 1.0, id="clamped"),
    pytest.param("null", 0.85, id="null"), pytest.param(None, 0.85, id="omitted"),
    pytest.param('"0.75"', 0.75, id="numeric-string"),
])
def test_ai_confidence_preserves_answer_request_limit_and_budget_policy(
    client, seeded_spread_and_cards, upstream, db_session, caplog, mode, unknown_usage, literal, expected,
):
    state, store = upstream
    state["answer"] = _answer(literal)
    if unknown_usage:
        state["usage"] = {"prompt_tokens": 20, "completion_tokens": {"invalid": "PRIVATE_USAGE"}}
    guest = client.post("/api/v1/guest-session")
    assert guest.status_code == 200
    headers = {"Authorization": "Bearer " + guest.json()["access_token"]}
    created = client.post("/api/v1/records/", headers=headers, json={
        "question": PRIVATE_QUESTION, "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    })
    assert created.status_code == 200
    path = f"/api/v1/records/{created.json()['id']}"
    assert client.post(path + "/draw", headers=headers).status_code == 200
    if mode == "manual-retry":
        state["timeout"] = True
        assert client.post(path + "/interpret", headers=headers).status_code == 504
        assert len(state["posts"]) == 1
        assert client.post(path + "/interpret/async", headers=headers).status_code == 409
        assert len(state["posts"]) == 1
        state["timeout"] = False
    suffix = {"sync": "/interpret", "async": "/interpret/async", "manual-retry": "/interpret/retry"}[mode]
    response = client.post(path + suffix, headers=headers)
    assert response.status_code == (200 if mode == "sync" else 202)
    detail = client.get(path, headers=headers)
    assert detail.status_code == 200
    payload = detail.json()
    assert payload["status"] == "completed"
    assert payload["interpretation"]["overall_interpretation"] == ANSWER
    assert payload["interpretation"]["summary"] == "Usable summary"
    assert payload["interpretation"]["advice"] == "Usable advice"
    assert payload["interpretation"]["confidence_score"] == expected
    expected_requests = 2 if mode == "manual-retry" else 1
    assert payload["interpretation_run"]["state"] == "completed"
    assert payload["interpretation_run"]["can_retry"] is False
    assert payload["interpretation_run"]["requests_started"] == len(state["posts"]) == expected_requests
    reservations = db_session.scalars(select(AIBudgetReservation)).all()
    assert len(reservations) == expected_requests
    # Normal statistics keep the existing upward rounding; uncertain charges stay reserved.
    settled = [item.actual_micros for item in reservations if item.actual_micros is not None]
    assert settled == ([] if unknown_usage else [31])
    held = sum(item.estimated_micros for item in reservations if item.actual_micros is None)
    assert store.snapshot() == {
        "daily_reserved_micros": held, "monthly_reserved_micros": held,
        "daily_spent_micros": 0 if unknown_usage else 31, "monthly_spent_micros": 0 if unknown_usage else 31,
    }
    before = store.snapshot()
    for endpoint in ("/interpret", "/interpret/async", "/interpret/retry"):
        assert client.post(path + endpoint, headers=headers).status_code == 200
    assert len(state["posts"]) == expected_requests and store.snapshot() == before
    assert PRIVATE_QUESTION not in caplog.text and ANSWER not in caplog.text
    assert "PRIVATE_USAGE" not in caplog.text and "9" * 100 not in caplog.text
