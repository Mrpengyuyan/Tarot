from __future__ import annotations

from app.api.v1.endpoints import records as records_endpoint


def _register_and_login(client, username: str):
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


def _create_prediction(client, spread_id: int, question: str, question_type: str) -> int:
    create_resp = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": question,
            "question_type": question_type,
        },
    )
    assert create_resp.status_code == 200
    return create_resp.json()["id"]


def test_records_list_supports_search_filter_and_sort(client, seeded_spread_and_cards):
    _register_and_login(client, username="list_query_user")
    spread_id = seeded_spread_and_cards["spread_id"]

    favorite_id = _create_prediction(
        client,
        spread_id,
        question="Career planning question",
        question_type="career",
    )
    other_id = _create_prediction(
        client,
        spread_id,
        question="Love insight request",
        question_type="love",
    )

    favorite_resp = client.put(f"/api/v1/records/{favorite_id}", json={"is_favorite": True})
    assert favorite_resp.status_code == 200

    filtered_resp = client.get(
        "/api/v1/records/?question_type=career&search=planning&sort_by=created_at&sort_order=desc"
    )
    assert filtered_resp.status_code == 200
    filtered_body = filtered_resp.json()
    assert [item["id"] for item in filtered_body] == [favorite_id]

    favorites_resp = client.get("/api/v1/records/?favorites_only=true")
    assert favorites_resp.status_code == 200
    favorites_body = favorites_resp.json()
    assert [item["id"] for item in favorites_body] == [favorite_id]
    assert other_id not in [item["id"] for item in favorites_body]

    invalid_sort_resp = client.get("/api/v1/records/?sort_by=unknown_field")
    assert invalid_sort_resp.status_code == 400


def test_recent_overview_returns_spread_name_and_interpretation_summary(client, seeded_spread_and_cards, monkeypatch):
    _register_and_login(client, username="recent_overview_user")
    spread_id = seeded_spread_and_cards["spread_id"]
    prediction_id = _create_prediction(
        client,
        spread_id,
        question="What should I focus on next?",
        question_type="general",
    )

    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200

    async def fake_ai_create_interpretation(db, prediction, cards_data, user_context=None):  # noqa: ANN001
        del db, prediction, cards_data, user_context
        return {
            "overall_interpretation": "Focus on steady progress and practical execution.",
            "summary": "Steady progress is the key theme.",
            "card_analysis": None,
            "relationship_analysis": None,
            "advice": None,
            "warning": None,
            "key_themes": ["progress", "focus"],
            "model_used": "unit_test_ai",
            "model_version": "v-test",
            "confidence_score": 0.9,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_ai_create_interpretation,
    )

    interpret_resp = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert interpret_resp.status_code == 200

    overview_resp = client.get("/api/v1/records/dashboard/recent-overview?limit=4")
    assert overview_resp.status_code == 200
    overview_body = overview_resp.json()
    assert overview_body
    first_item = overview_body[0]
    assert first_item["id"] == prediction_id
    assert first_item["spread_name"] == "Test Three Card Spread"
    assert first_item["spread_name_en"] == "Test Three Card Spread"
    assert "Steady progress" in first_item["interpretation_summary"]

    legacy_overview_resp = client.get("/api/v1/records/recent-overview?limit=4")
    assert legacy_overview_resp.status_code == 200
    assert legacy_overview_resp.json()[0]["id"] == prediction_id
