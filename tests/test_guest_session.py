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
