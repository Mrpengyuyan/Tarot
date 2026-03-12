from __future__ import annotations

from app.api.v1.endpoints import records as records_endpoint


def _register_and_login(client, username: str = "reader"):
    register_payload = {
        "username": username,
        "email": f"{username}@example.com",
        "password": "password123",
    }
    register_resp = client.post("/api/v1/register", json=register_payload)
    assert register_resp.status_code == 200

    login_resp = client.post(
        "/api/v1/login",
        data={"username": username, "password": "password123"},
    )
    assert login_resp.status_code == 200


def _create_prediction(client, spread_id: int) -> int:
    create_resp = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": "How should I plan my career next?",
            "question_type": "career",
        },
    )
    assert create_resp.status_code == 200
    return create_resp.json()["id"]


def test_interpretation_flow_persists_and_is_idempotent(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="interpret_user")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200
    assert len(draw_resp.json()["card_draws"]) == 3

    calls = {"count": 0}

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        calls["count"] += 1
        return {
            "overall_interpretation": "Momentum is positive, maintain a steady pace.",
            "card_analysis": "The three cards map to action, balance, and outcome.",
            "relationship_analysis": "Past experience strengthens current judgment.",
            "advice": "Focus on one or two high-impact goals.",
            "warning": "Avoid overcommitting resources in one step.",
            "summary": "Progress steadily with clear priorities.",
            "key_themes": ["pace", "focus", "action"],
            "model_used": "unit_test_ai",
            "model_version": "v-test",
            "confidence_score": 0.9,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    first_interpret = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert first_interpret.status_code == 200
    body = first_interpret.json()
    assert body["overall_interpretation"]
    assert body["model_used"] == "unit_test_ai"
    assert calls["count"] == 1

    second_interpret = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert second_interpret.status_code == 200
    assert second_interpret.json()["id"] == body["id"]
    assert calls["count"] == 1

    detail_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert detail_resp.status_code == 200
    assert detail_resp.json()["status"] == "completed"
    assert detail_resp.json()["interpretation"]["overall_interpretation"]


def test_interpretation_requires_cards_drawn(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="no_draw_user")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    calls = {"count": 0}

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        calls["count"] += 1
        return {"overall_interpretation": "should not be used"}

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    interpret_resp = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert interpret_resp.status_code == 400
    assert calls["count"] == 0


def test_interpretation_failure_sets_prediction_failed(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="error_user")
    prediction_id = _create_prediction(client, seeded_spread_and_cards["spread_id"])

    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        raise RuntimeError("simulated ai failure")

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    interpret_resp = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert interpret_resp.status_code == 502

    detail_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert detail_resp.status_code == 200
    assert detail_resp.json()["status"] == "failed"
