from concurrent.futures import ThreadPoolExecutor

import pytest
from sqlalchemy import func, select, text

from app.api.v1.endpoints import records as records_endpoint
from app.core.config import settings
from app.models.user import User
from app.services.guest_session_limiter import GuestSessionLimitReached, claim_guest_session


def test_guest_session_creation_is_limited_per_source(client, db_session):
    responses = [client.post("/api/v1/guest-session") for _ in range(11)]

    assert [response.status_code for response in responses] == [200] * 10 + [429]
    assert 1 <= int(responses[-1].headers["Retry-After"]) <= 3600
    assert db_session.scalar(select(func.count(User.id)).where(User.is_guest.is_(True))) == 10
    source_hash = db_session.execute(text("SELECT source_hash FROM guest_session_windows")).scalar_one()
    assert len(source_hash) == 64
    assert "testclient" not in source_hash


def test_guest_session_limit_is_atomic_under_parallel_requests(client):
    with ThreadPoolExecutor(max_workers=4) as executor:
        responses = list(executor.map(lambda _: client.post("/api/v1/guest-session"), range(12)))

    assert sorted(response.status_code for response in responses) == [200] * 10 + [429] * 2


def test_forwarded_for_header_does_not_bypass_guest_session_limit(client, monkeypatch):
    monkeypatch.setattr(settings, "GUEST_SESSIONS_PER_HOUR", 1)

    first = client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "198.51.100.1"})
    second = client.post("/api/v1/guest-session", headers={"X-Forwarded-For": "198.51.100.2"})

    assert first.status_code == 200
    assert second.status_code == 429


def test_guest_session_limits_are_independent_between_sources(db_session, monkeypatch):
    monkeypatch.setattr(settings, "GUEST_SESSIONS_PER_HOUR", 1)

    claim_guest_session(db_session, "198.51.100.1")
    with pytest.raises(GuestSessionLimitReached):
        claim_guest_session(db_session, "198.51.100.1")
    claim_guest_session(db_session, "198.51.100.2")

    assert db_session.execute(text("SELECT COUNT(*) FROM guest_session_windows")).scalar_one() == 2


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

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None, before_send=None):  # noqa: ANN001
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


def test_guest_deleting_record_does_not_refund_daily_reading(client, seeded_spread_and_cards, monkeypatch):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 1)
    token = client.post("/api/v1/guest-session").json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}
    payload = {
        "question": "Will this still count after deletion?",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    }

    record = client.post("/api/v1/records/", headers=headers, json=payload)
    assert record.status_code == 200
    assert client.delete(f"/api/v1/records/{record.json()['id']}", headers=headers).status_code == 200
    assert client.post("/api/v1/records/", headers=headers, json=payload).status_code == 429


def test_guest_changing_email_does_not_remove_guest_limit(client, seeded_spread_and_cards, monkeypatch):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 1)
    token = client.post("/api/v1/guest-session").json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}
    update = client.put("/api/v1/users/me", headers=headers, json={"email": "guest-now-registered@example.com"})
    assert update.status_code == 200
    payload = {
        "question": "Does my original guest status still apply?",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    }

    assert client.post("/api/v1/records/", headers=headers, json=payload).status_code == 200
    assert client.post("/api/v1/records/", headers=headers, json=payload).status_code == 429


def test_registered_account_has_its_own_daily_reading_limit(client, seeded_spread_and_cards):
    assert client.post(
        "/api/v1/register",
        json={"username": "limited_player", "email": "limited@example.com", "password": "password123"},
    ).status_code == 200
    token = client.post(
        "/api/v1/login",
        data={"username": "limited_player", "password": "password123"},
    ).json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}
    payload = {
        "question": "How many readings can I create?",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    }

    for _ in range(3):
        assert client.post("/api/v1/records/", headers=headers, json=payload).status_code == 200
    assert client.post("/api/v1/records/", headers=headers, json=payload).status_code == 429


def test_parallel_guest_record_creation_claims_only_one_slot(client, seeded_spread_and_cards, monkeypatch):
    monkeypatch.setattr(settings, "GUEST_DAILY_READING_LIMIT", 1)
    token = client.post("/api/v1/guest-session").json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}
    payload = {
        "question": "Can two requests use the same slot?",
        "question_type": "general",
        "spread_type_id": seeded_spread_and_cards["spread_id"],
    }

    with ThreadPoolExecutor(max_workers=2) as pool:
        responses = list(pool.map(lambda _: client.post("/api/v1/records/", headers=headers, json=payload), range(2)))

    assert sorted(response.status_code for response in responses) == [200, 429]


def test_guest_session_refresh_keeps_access_to_own_record(client, seeded_spread_and_cards):
    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    first_token = guest_response.json()["access_token"]

    record_response = client.post(
        "/api/v1/records/",
        headers={"Authorization": f"Bearer {first_token}"},
        json={
            "question": "续期之后还能看到这条记录吗？",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )
    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]

    csrf_token = client.cookies.get(settings.CSRF_COOKIE_NAME)
    assert csrf_token
    refresh_response = client.post("/api/v1/refresh", headers={settings.CSRF_HEADER_NAME: csrf_token})

    assert refresh_response.status_code == 200
    refreshed_token = refresh_response.json()["access_token"]
    assert refreshed_token

    detail_response = client.get(
        f"/api/v1/records/{prediction_id}",
        headers={"Authorization": f"Bearer {refreshed_token}"},
    )
    assert detail_response.status_code == 200
    assert detail_response.json()["id"] == prediction_id


def test_guest_session_can_run_async_interpretation(client, seeded_spread_and_cards, monkeypatch):
    guest_response = client.post("/api/v1/guest-session")
    assert guest_response.status_code == 200
    headers = {"Authorization": f"Bearer {guest_response.json()['access_token']}"}

    record_response = client.post(
        "/api/v1/records/",
        headers=headers,
        json={
            "question": "访客也能在后台生成解读吗？",
            "question_type": "general",
            "spread_type_id": seeded_spread_and_cards["spread_id"],
        },
    )
    assert record_response.status_code == 200
    prediction_id = record_response.json()["id"]
    assert client.post(f"/api/v1/records/{prediction_id}/draw", headers=headers).status_code == 200

    async def fake_create_interpretation(db, prediction, cards_data, user_context=None, before_send=None):  # noqa: ANN001
        return {
            "overall_interpretation": "访客的后台解读已经生成。",
            "summary": "可以继续。",
            "model_used": "guest_async_mock_ai",
        }

    monkeypatch.setattr(
        records_endpoint.tarot_interpretation_service,
        "create_interpretation",
        fake_create_interpretation,
    )

    start_response = client.post(f"/api/v1/records/{prediction_id}/interpret/async", headers=headers)
    assert start_response.status_code == 202

    detail_response = client.get(f"/api/v1/records/{prediction_id}", headers=headers)
    assert detail_response.status_code == 200
    detail = detail_response.json()
    assert detail["status"] == "completed"
    assert detail["interpretation"]["model_used"] == "guest_async_mock_ai"
