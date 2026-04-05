"""End-to-end smoke tests: full user journey from register → interpret → query.

These tests exercise the complete chain that a real user follows,
verifying that all components integrate correctly.
"""
from __future__ import annotations

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings


def _register_and_login(client, username: str):
    register_resp = client.post(
        "/api/v1/register",
        json={
            "username": username,
            "email": f"{username}@example.com",
            "password": "password123",
        },
    )
    assert register_resp.status_code == 200, f"Register failed: {register_resp.text}"

    login_resp = client.post(
        "/api/v1/login",
        data={"username": username, "password": "password123"},
    )
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    csrf_token = login_resp.cookies.get(settings.CSRF_COOKIE_NAME) or client.cookies.get(
        settings.CSRF_COOKIE_NAME
    )
    if csrf_token:
        client.headers.update({settings.CSRF_HEADER_NAME: csrf_token})
    return login_resp


def test_full_reading_lifecycle(client, seeded_spread_and_cards, monkeypatch):
    """
    Complete lifecycle:
    1. Register & login
    2. Create prediction
    3. Draw cards
    4. Generate AI interpretation (mocked)
    5. Get detail with interpretation
    6. Update prediction (favorite + notes)
    7. Verify in stats
    8. Verify in dashboard overview
    9. Delete prediction
    """
    _register_and_login(client, username="e2e_lifecycle_user")
    spread_id = seeded_spread_and_cards["spread_id"]

    # ── 1. Create prediction ──
    create_resp = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": "What should I focus on for my career growth?",
            "question_type": "career",
        },
    )
    assert create_resp.status_code == 200
    prediction = create_resp.json()
    prediction_id = prediction["id"]
    assert prediction["status"] == "pending"
    assert prediction["question_type"] == "career"

    # ── 2. Draw cards ──
    draw_resp = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert draw_resp.status_code == 200
    draw_body = draw_resp.json()
    assert len(draw_body["card_draws"]) == 3  # spread has 3 positions

    # ── 3. Mock AI interpretation ──
    async def fake_ai(db, prediction, cards_data, user_context=None):
        return {
            "overall_interpretation": "Career growth requires patience and strategic planning.",
            "card_analysis": "Each card points to incremental progress.",
            "advice": "Focus on one major goal at a time.",
            "warning": "Avoid spreading yourself too thin.",
            "summary": "Strategic patience is your key theme.",
            "key_themes": ["strategy", "patience", "growth"],
            "model_used": "e2e_mock_ai",
            "model_version": "v-e2e",
            "confidence_score": 0.92,
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_ai,
    )

    interpret_resp = client.post(f"/api/v1/records/{prediction_id}/interpret?force_ai=true")
    assert interpret_resp.status_code == 200
    interp_body = interpret_resp.json()
    assert interp_body["overall_interpretation"]
    assert interp_body["model_used"] == "e2e_mock_ai"
    interp_id = interp_body["id"]

    # ── 4. Get detail with full interpretation ──
    detail_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert detail_resp.status_code == 200
    detail = detail_resp.json()
    assert detail["status"] == "completed"
    assert detail["interpretation"]["id"] == interp_id
    assert "patience" in detail["interpretation"]["overall_interpretation"].lower()
    assert len(detail["card_draws"]) == 3

    # ── 5. Update: add to favorites + notes + rating ──
    update_resp = client.put(
        f"/api/v1/records/{prediction_id}",
        json={
            "is_favorite": True,
            "user_notes": "Good reading, revisit next month.",
            "user_rating": 5,
        },
    )
    assert update_resp.status_code == 200
    assert update_resp.json()["is_favorite"] is True
    assert update_resp.json()["user_rating"] == 5

    # ── 6. Stats should reflect the prediction ──
    stats_resp = client.get("/api/v1/records/stats")
    assert stats_resp.status_code == 200
    stats = stats_resp.json()
    assert stats["total_predictions"] >= 1
    assert stats["completed_predictions"] >= 1
    assert stats["favorite_predictions"] >= 1

    # ── 7. Dashboard overview ──
    overview_resp = client.get("/api/v1/records/dashboard/recent-overview?limit=4")
    assert overview_resp.status_code == 200
    overview = overview_resp.json()
    assert any(item["id"] == prediction_id for item in overview)

    # ── 8. Favorites filter ──
    fav_resp = client.get("/api/v1/records/?favorites_only=true")
    assert fav_resp.status_code == 200
    fav_items = fav_resp.json()
    assert any(item["id"] == prediction_id for item in fav_items)

    # ── 9. Search by keyword ──
    search_resp = client.get("/api/v1/records/?search=career%20growth")
    assert search_resp.status_code == 200

    # ── 10. Delete ──
    delete_resp = client.delete(f"/api/v1/records/{prediction_id}")
    assert delete_resp.status_code == 200

    gone_resp = client.get(f"/api/v1/records/{prediction_id}")
    assert gone_resp.status_code == 404


def test_cross_user_isolation(client, seeded_spread_and_cards, monkeypatch):
    """
    Verify that user A cannot see user B's predictions.
    """
    spread_id = seeded_spread_and_cards["spread_id"]

    # ── User A creates a prediction ──
    _register_and_login(client, username="isolation_user_a")
    create_a = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": "User A's private question",
            "question_type": "general",
        },
    )
    assert create_a.status_code == 200
    prediction_a_id = create_a.json()["id"]

    # ── Switch to User B ──
    client.cookies.clear()
    client.headers.pop(settings.CSRF_HEADER_NAME, None)
    _register_and_login(client, username="isolation_user_b")

    # ── User B should NOT see User A's prediction ──
    list_resp = client.get("/api/v1/records/")
    assert list_resp.status_code == 200
    ids = [item["id"] for item in list_resp.json()]
    assert prediction_a_id not in ids

    # ── User B should NOT be able to access User A's prediction directly ──
    detail_resp = client.get(f"/api/v1/records/{prediction_a_id}")
    assert detail_resp.status_code in (403, 404)


def test_register_duplicate_username_fails(client):
    """Duplicate username registration should return a clear error."""
    _register_and_login(client, username="dupe_user")

    dupe_resp = client.post(
        "/api/v1/register",
        json={
            "username": "dupe_user",
            "email": "dupe_different@example.com",
            "password": "password123",
        },
    )
    assert dupe_resp.status_code in (400, 409, 422)


def test_login_with_wrong_password(client):
    """Wrong password should return 401."""
    client.post(
        "/api/v1/register",
        json={
            "username": "wrong_pw_user",
            "email": "wrong_pw@example.com",
            "password": "correct_password",
        },
    )

    login_resp = client.post(
        "/api/v1/login",
        data={"username": "wrong_pw_user", "password": "wrong_password"},
    )
    assert login_resp.status_code == 401


def test_draw_cards_twice_returns_conflict(client, seeded_spread_and_cards):
    """Drawing cards for the same prediction twice should be idempotent or return conflict."""
    _register_and_login(client, username="double_draw_user")
    spread_id = seeded_spread_and_cards["spread_id"]

    create_resp = client.post(
        "/api/v1/records/",
        json={
            "spread_type_id": spread_id,
            "question": "Double draw test",
            "question_type": "general",
        },
    )
    prediction_id = create_resp.json()["id"]

    first_draw = client.post(f"/api/v1/records/{prediction_id}/draw")
    assert first_draw.status_code == 200

    second_draw = client.post(f"/api/v1/records/{prediction_id}/draw")
    # Should either return the existing cards or a conflict
    assert second_draw.status_code in (200, 409)


def test_interpret_nonexistent_prediction_returns_404(client, seeded_spread_and_cards):
    """Interpreting a non-existent prediction should return 404."""
    _register_and_login(client, username="interp_404_user")
    resp = client.post("/api/v1/records/99999/interpret?force_ai=true")
    assert resp.status_code in (403, 404)
