from __future__ import annotations

import pytest


def _guest_headers(client):
    response = client.post("/api/v1/guest-session")
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


def test_record_question_rejects_blank_and_overlong_input(client, seeded_spread_and_cards):
    headers = _guest_headers(client)
    payload = {
        "spread_type_id": seeded_spread_and_cards["spread_id"],
        "question_type": "general",
    }

    for question in ("   ", "x" * 2001):
        response = client.post("/api/v1/records/", headers=headers, json={**payload, "question": question})
        assert response.status_code == 422

    response = client.post("/api/v1/records/", headers=headers, json={**payload, "question": "x" * 2000})
    assert response.status_code == 200


@pytest.mark.parametrize("suffix", ["interpret?force_ai=true", "interpret/async", "interpret/retry"])
def test_interpretation_entrypoints_reject_overlong_context(client, seeded_spread_and_cards, suffix):
    headers = _guest_headers(client)
    response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "spread_type_id": seeded_spread_and_cards["spread_id"],
            "question_type": "general",
            "question": "What next?",
        },
    )
    assert response.status_code == 200
    prediction_id = response.json()["id"]

    response = client.post(
        f"/api/v1/records/{prediction_id}/{suffix}",
        headers=headers,
        params={"user_context": "x" * 2001},
    )
    assert response.status_code == 422
