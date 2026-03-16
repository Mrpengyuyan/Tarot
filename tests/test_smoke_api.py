from __future__ import annotations


def test_smoke_health_cards_spreads(client, seeded_spread_and_cards):
    health_resp = client.get("/api/v1/health/")
    assert health_resp.status_code == 200
    assert health_resp.json()["status"] == "healthy"

    spreads_resp = client.get("/api/v1/spreads/?limit=10")
    assert spreads_resp.status_code == 200
    spreads = spreads_resp.json()
    assert isinstance(spreads, list)
    assert any(item["id"] == seeded_spread_and_cards["spread_id"] for item in spreads)

    cards_resp = client.get("/api/v1/cards/?limit=10")
    assert cards_resp.status_code == 200
    cards = cards_resp.json()
    assert isinstance(cards, list)
    assert len(cards) >= 3


def test_smoke_health_ai_and_metrics(client):
    ai_resp = client.get("/api/v1/health/ai")
    assert ai_resp.status_code == 200
    ai_body = ai_resp.json()
    assert "status" in ai_body
    assert "is_healthy" in ai_body

    metrics_resp = client.get("/api/v1/health/metrics")
    assert metrics_resp.status_code == 200
    metrics_body = metrics_resp.json()
    assert "database" in metrics_body
    assert "ai_service" in metrics_body
