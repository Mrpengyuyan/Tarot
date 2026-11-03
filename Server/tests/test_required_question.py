from __future__ import annotations

import pytest

from app.core.config import settings
from app.models.record import CardDraw, Prediction, PredictionStatus
from app.models.user import User


def _guest_headers(client):
    response = client.post("/api/v1/guest-session")
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


@pytest.mark.parametrize("question", ["", "   ", "\t\r\n", "\u3000\u00a0", None])
def test_missing_question_does_not_create_a_reading_or_consume_quota(
    client, db_session, seeded_spread_and_cards, monkeypatch, question
):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 1)
    headers = _guest_headers(client)
    payload = {"spread_type_id": seeded_spread_and_cards["spread_id"], "question_type": "general"}

    for invalid_payload in (payload, {**payload, "question": question}):
        response = client.post("/api/v1/records/", headers=headers, json=invalid_payload)
        assert response.status_code == 422
        assert any(error["loc"][-1] == "question" for error in response.json()["detail"])

    db_session.expire_all()
    assert db_session.query(Prediction).count() == 0
    assert db_session.query(CardDraw).count() == 0
    assert db_session.query(User).one().prediction_count == 0

    response = client.post(
        "/api/v1/records/", headers=headers, json={**payload, "question": "  What should I focus on?  "}
    )
    assert response.status_code == 200
    assert response.json()["question"] == "What should I focus on?"


@pytest.mark.parametrize("question", ["", "   ", "\t\r\n", "\u3000\u00a0"])
def test_existing_blank_question_cannot_draw_cards(client, db_session, seeded_spread_and_cards, question):
    headers = _guest_headers(client)
    response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "spread_type_id": seeded_spread_and_cards["spread_id"],
            "question_type": "general",
            "question": "An initially valid question?",
        },
    )
    assert response.status_code == 200
    prediction_id = response.json()["id"]
    prediction = db_session.get(Prediction, prediction_id)
    prediction.question = question  # Simulate a historical record without changing the schema.
    db_session.commit()

    response = client.post(f"/api/v1/records/{prediction_id}/draw", headers=headers)
    assert response.status_code == 409
    assert "question" in response.json()["detail"].lower()
    db_session.expire_all()
    assert db_session.query(CardDraw).count() == 0
    assert prediction.status == PredictionStatus.PENDING
    assert prediction.ai_requests_started == 0
    assert prediction.question == question


def test_valid_question_can_create_and_draw(client, seeded_spread_and_cards):
    headers = _guest_headers(client)
    response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "spread_type_id": seeded_spread_and_cards["spread_id"],
            "question_type": "general",
            "question": "  What comes next?  ",
        },
    )
    assert response.status_code == 200
    assert response.json()["question"] == "What comes next?"
    response = client.post(f"/api/v1/records/{response.json()['id']}/draw", headers=headers)
    assert response.status_code == 200
    assert len(response.json()["card_draws"]) == 3
