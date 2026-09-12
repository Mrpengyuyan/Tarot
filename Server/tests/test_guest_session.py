from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings


def test_guest_session_returns_bearer_token_and_authenticated_profile(client):
    guest_response = client.post("/api/v1/guest-session")

    assert guest_response.status_code == 200
    payload = guest_response.json()
    assert payload["token_type"] == "bearer"
    assert payload["access_token"]

    profile_response = client.get(
        "/api/v1/users/me",
        headers={"Authorization": f"Bearer {payload['access_token']}"},
    )

    assert profile_response.status_code == 200
    profile = profile_response.json()
    assert profile["username"].startswith("guest_")
    assert profile["email"].endswith("@guest.tarot.game")
    assert profile["is_active"] is True


def test_guest_sessions_are_unique_and_independently_authenticated(client):
    first_response = client.post("/api/v1/guest-session")
    second_response = client.post("/api/v1/guest-session")

    assert first_response.status_code == 200
    assert second_response.status_code == 200

    first_token = first_response.json()["access_token"]
    second_token = second_response.json()["access_token"]
    assert first_token != second_token

    first_profile = client.get(
        "/api/v1/users/me",
        headers={"Authorization": f"Bearer {first_token}"},
    )
    second_profile = client.get(
        "/api/v1/users/me",
        headers={"Authorization": f"Bearer {second_token}"},
    )

    assert first_profile.status_code == 200
    assert second_profile.status_code == 200
    assert first_profile.json()["username"] != second_profile.json()["username"]


def test_guest_session_can_create_and_draw_a_reading(client, seeded_spread_and_cards):
    guest_response = client.post("/api/v1/guest-session")
    token = guest_response.json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}

    record_response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "question": "What should I focus on next?",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )

    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]

    draw_response = client.post(
        f"/api/v1/records/{prediction_id}/draw",
        headers=headers,
    )

    assert draw_response.status_code == 200
    assert draw_response.json()["status"] == "success"
    assert len(draw_response.json()["card_draws"]) == 3


def test_guest_session_completes_full_reading_lifecycle(client, seeded_spread_and_cards, monkeypatch):
    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    headers = {"Authorization": f"Bearer {guest_response.json()['access_token']}"}

    record_response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "question": "Can I move forward with more focus?",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )
    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]

    draw_response = client.post(
        f"/api/v1/records/{prediction_id}/draw",
        headers=headers,
    )
    assert draw_response.status_code == 200
    assert len(draw_response.json()["card_draws"]) == 3

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        return {
            "overall_interpretation": "Steady focus creates room for meaningful progress.",
            "card_analysis": "The spread favors deliberate action.",
            "advice": "Choose one next step and complete it.",
            "warning": "Avoid scattering your attention.",
            "summary": "Focus supports progress.",
            "key_themes": ["focus", "progress"],
            "model_used": "guest_flow_mock_ai",
            "model_version": "test",
            "confidence_score": 0.9,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    interpretation_response = client.post(
        f"/api/v1/records/{prediction_id}/interpret?force_ai=true",
        headers=headers,
    )
    assert interpretation_response.status_code == 200
    assert interpretation_response.json()["model_used"] == "guest_flow_mock_ai"

    detail_response = client.get(
        f"/api/v1/records/{prediction_id}",
        headers=headers,
    )
    assert detail_response.status_code == 200
    detail = detail_response.json()
    assert detail["status"] == "completed"
    assert len(detail["card_draws"]) == 3
    assert detail["interpretation"]["summary"] == "Focus supports progress."


def test_guest_session_enforces_configured_daily_reading_limit(
    client,
    seeded_spread_and_cards,
    monkeypatch,
):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 1)

    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    headers = {"Authorization": f"Bearer {guest_response.json()['access_token']}"}
    payload = {
        "question": "What should I focus on today?",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    }

    first_record = client.post("/api/v1/records/", headers=headers, json=payload)
    assert first_record.status_code == 200

    second_record = client.post("/api/v1/records/", headers=headers, json=payload)
    assert second_record.status_code == 429
    assert "daily reading limit" in second_record.json()["detail"].lower()
    assert int(second_record.headers["retry-after"]) > 0
