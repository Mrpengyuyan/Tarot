import pytest
from sqlalchemy import event, select

from app.core.security import create_access_token, get_password_hash
from app.crud import card as card_crud
from app.db.base_class import Base
from app.models.record import CardDraw, Prediction, PredictionStatus
from app.models.tarot_card import CardType, Suit, TarotCard
from app.models.user import User
from app.schemas.card import TarotCardUpdate


@pytest.fixture(scope="module")
def password_hash():
    return get_password_hash("card-history-test-password")


@pytest.fixture()
def admin(client, db_session, password_hash):
    user = User(username="card_history_admin", email="card-history@example.com",
                hashed_password=password_hash, is_superuser=True)
    db_session.add(user)
    db_session.commit()
    return {"id": user.id, "headers": {"Authorization": "Bearer " + create_access_token(
        {"sub": str(user.id), "sub_type": "user_id"},
    )}}


def _new_reading(client, admin, spread_id):
    response = client.post("/api/v1/records/", headers=admin["headers"], json={
        "question": "Retained card history question", "question_type": "general", "spread_type_id": spread_id,
    })
    assert response.status_code == 200
    return response.json()["id"]


@pytest.fixture(params=["drawn", "completed", "failed"])
def reading(request, client, db_session, admin, seeded_spread_and_cards):
    record_id = _new_reading(client, admin, seeded_spread_and_cards["spread_id"])
    drawn = client.post(f"/api/v1/records/{record_id}/draw?seed=0", headers=admin["headers"])
    assert drawn.status_code == 200
    if request.param == "completed":
        response = client.post(f"/api/v1/records/{record_id}/interpret", headers=admin["headers"], json={
            "overall_interpretation": "Retained original card interpretation", "confidence_score": 0.8,
        })
        assert response.status_code == 200
    elif request.param == "failed":
        db_session.get(Prediction, record_id).status = PredictionStatus.FAILED
        db_session.commit()
    return {"id": record_id, "card_id": drawn.json()["card_draws"][0]["tarot_card_id"],
            "drawn_ids": {draw["tarot_card_id"] for draw in drawn.json()["card_draws"]}}


def _snapshot(db):
    db.expire_all()
    return {table.name: db.execute(select(table)).all() for table in Base.metadata.sorted_tables}


@pytest.mark.parametrize("field,value", [
    ("name_en", "Different card"), ("name_zh", "Different card name"),
    ("card_number", 99), ("card_type", "minor_arcana"), ("suit", "cups"),
])
def test_referenced_identity_changes_reject_the_entire_update(
    client, db_session, admin, reading, field, value,
):
    url = f"/api/v1/cards/{reading['card_id']}"
    record_url = f"/api/v1/records/{reading['id']}"
    before_card = client.get(url).json()
    before_detail = client.get(record_url, headers=admin["headers"]).json()
    before_draws = client.get(record_url + "/cards", headers=admin["headers"]).json()
    before = _snapshot(db_session)

    response = client.put(url, headers=admin["headers"], json={
        field: value, "description": "Must not partially save", "image_url": "must-not-save.png",
        "upright_meaning": "Must not replace meaning",
    })

    assert response.status_code == 409
    assert _snapshot(db_session) == before
    assert client.get(url).json() == before_card
    assert client.get(record_url, headers=admin["headers"]).json() == before_detail
    assert client.get(record_url + "/cards", headers=admin["headers"]).json() == before_draws


@pytest.mark.parametrize("mode", ["empty", "identity", "full", "numeric-string"])
def test_referenced_identity_accepts_unchanged_values(client, db_session, admin, reading, mode):
    url = f"/api/v1/cards/{reading['card_id']}"
    card = client.get(url).json()
    payload = {} if mode == "empty" else {field: card[field] for field in (
        "name_en", "name_zh", "card_number", "card_type", "suit",
    )}
    if mode == "full":
        payload = {field: value for field, value in card.items() if field != "id"}
    elif mode == "numeric-string":
        payload["card_number"] = str(card["card_number"])
    before = _snapshot(db_session)
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    assert response.json() == card
    assert _snapshot(db_session) == before


@pytest.mark.parametrize("clear", [False, True])
def test_referenced_card_keeps_text_and_artwork_maintenance_available(client, admin, reading, clear):
    url = f"/api/v1/cards/{reading['card_id']}"
    original = client.get(url).json()
    payload = {"upright_meaning": "Updated upright meaning", "reversed_meaning": "Updated reversed meaning"}
    payload.update({field: None if clear else "Updated text" for field in (
        "image_url", "upright_love", "upright_career", "upright_finance", "upright_health",
        "reversed_love", "reversed_career", "reversed_finance", "reversed_health", "description",
        "keywords_upright", "keywords_reversed", "element", "zodiac", "planet",
    )})
    payload.update({field: original[field] for field in ("name_en", "name_zh", "card_number", "card_type", "suit")})
    response = client.put(url, headers=admin["headers"], json=payload)
    assert response.status_code == 200
    saved = client.get(url).json()
    assert all(saved[field] == value for field, value in payload.items())
    draws = client.get(f"/api/v1/records/{reading['id']}/cards", headers=admin["headers"]).json()
    meaning = next(draw["card_meaning"] for draw in draws if draw["tarot_card_id"] == reading["card_id"])
    assert meaning["name_en"] == original["name_en"]
    assert meaning["meaning"] in ("Updated upright meaning", "Updated reversed meaning")


def test_nonnull_referenced_suit_cannot_be_cleared(client, db_session, admin, reading):
    card = db_session.get(TarotCard, reading["card_id"])
    card.card_type, card.suit = CardType.MINOR_ARCANA, Suit.CUPS
    db_session.commit()
    before = _snapshot(db_session)
    response = client.put(f"/api/v1/cards/{reading['card_id']}", headers=admin["headers"], json={"suit": None})
    assert response.status_code == 409
    assert _snapshot(db_session) == before


def test_direct_crud_cannot_change_referenced_identity(db_session, reading):
    card = db_session.get(TarotCard, reading["card_id"])
    before = _snapshot(db_session)
    with pytest.raises(ValueError):
        card_crud.update_card(db_session, card, TarotCardUpdate(
            card_number=99, description="Must not partially save",
        ))
    assert _snapshot(db_session) == before


def test_other_card_references_do_not_block_unused_card_editing(
    client, db_session, admin, reading, seeded_spread_and_cards,
):
    card_id = next(card_id for card_id in seeded_spread_and_cards["card_ids"] if card_id not in reading["drawn_ids"])
    payload = {"name_en": "Unused changed card", "name_zh": "Unused changed name", "card_number": 99,
               "card_type": "minor_arcana", "suit": "wands"}
    count = db_session.query(TarotCard).count()
    response = client.put(f"/api/v1/cards/{card_id}", headers=admin["headers"], json=payload)
    assert response.status_code == 200
    assert all(response.json()[field] == value for field, value in payload.items())
    assert db_session.query(TarotCard).count() == count
    assert client.put(f"/api/v1/cards/{card_id}", headers=admin["headers"], json={"suit": None}).status_code == 200


def test_pending_reading_without_draws_does_not_lock_cards(client, admin, seeded_spread_and_cards):
    _new_reading(client, admin, seeded_spread_and_cards["spread_id"])
    response = client.put(f"/api/v1/cards/{seeded_spread_and_cards['card_ids'][0]}",
                          headers=admin["headers"], json={"name_en": "Still editable before draw"})
    assert response.status_code == 200
    assert response.json()["name_en"] == "Still editable before draw"


def test_deleting_last_draw_reference_allows_identity_editing(client, admin, reading):
    assert client.delete(f"/api/v1/records/{reading['id']}", headers=admin["headers"]).status_code == 200
    response = client.put(f"/api/v1/cards/{reading['card_id']}", headers=admin["headers"], json={"name_en": "Editable again"})
    assert response.status_code == 200
    assert response.json()["name_en"] == "Editable again"


def test_remaining_draw_references_still_protect_identity(client, db_session, admin, reading, seeded_spread_and_cards):
    other_id = _new_reading(client, admin, seeded_spread_and_cards["spread_id"])
    assert client.post(f"/api/v1/records/{other_id}/draw?seed=0", headers=admin["headers"]).status_code == 200
    assert client.delete(f"/api/v1/records/{reading['id']}", headers=admin["headers"]).status_code == 200
    before = _snapshot(db_session)
    response = client.put(f"/api/v1/cards/{reading['card_id']}", headers=admin["headers"], json={"name_en": "Must not replace"})
    assert response.status_code == 409
    assert _snapshot(db_session) == before


def test_draw_inserted_immediately_before_update_is_still_protected(
    client, db_session, db_session_factory, admin, seeded_spread_and_cards,
):
    record_id = _new_reading(client, admin, seeded_spread_and_cards["spread_id"])
    card_id = seeded_spread_and_cards["card_ids"][0]
    url = f"/api/v1/cards/{card_id}"
    before = client.get(url).json()
    inserted = []
    engine = db_session.get_bind()

    def insert_draw(_connection, _cursor, statement, _parameters, _context, _executemany):
        if inserted or not statement.lstrip().upper().startswith("UPDATE TAROT_CARDS"):
            return
        inserted.append(True)
        with db_session_factory() as other:
            other.add(CardDraw(prediction_id=record_id, tarot_card_id=card_id, position=1, is_reversed=False))
            other.commit()

    event.listen(engine, "before_cursor_execute", insert_draw)
    try:
        response = client.put(url, headers=admin["headers"], json={"name_en": "Must not replace", "description": "Must not save"})
    finally:
        event.remove(engine, "before_cursor_execute", insert_draw)
    assert inserted == [True]
    assert response.status_code == 409
    assert client.get(url).json() == before
    db_session.expire_all()
    assert db_session.scalars(select(CardDraw)).one().tarot_card_id == card_id


def test_stale_unchanged_identity_does_not_overwrite_newly_drawn_card(
    client, db_session, admin, seeded_spread_and_cards,
):
    card_id = seeded_spread_and_cards["card_ids"][-1]
    stale_card = db_session.get(TarotCard, card_id)
    unchanged = TarotCardUpdate(name_en=stale_card.name_en, card_number=stale_card.card_number,
                               description="Updated without replacing identity")
    assert client.put(f"/api/v1/cards/{card_id}", headers=admin["headers"], json={"name_en": "New identity", "card_number": 99}).status_code == 200
    record_id = _new_reading(client, admin, seeded_spread_and_cards["spread_id"])
    drawn = client.post(f"/api/v1/records/{record_id}/draw?seed=0", headers=admin["headers"])
    assert drawn.status_code == 200 and card_id in {draw["tarot_card_id"] for draw in drawn.json()["card_draws"]}

    card_crud.update_card(db_session, stale_card, unchanged)

    saved = client.get(f"/api/v1/cards/{card_id}").json()
    assert saved["name_en"] == "New identity" and saved["card_number"] == 99
    assert saved["description"] == "Updated without replacing identity"


def test_mixed_stale_identity_update_preserves_other_concurrent_identity_changes(
    client, db_session, admin, seeded_spread_and_cards,
):
    card_id = seeded_spread_and_cards["card_ids"][0]
    stale_card = db_session.get(TarotCard, card_id)
    update = TarotCardUpdate(name_en=stale_card.name_en, card_number=99, description="Requested metadata")
    assert client.put(f"/api/v1/cards/{card_id}", headers=admin["headers"],
                      json={"name_en": "Concurrent new name"}).status_code == 200

    card_crud.update_card(db_session, stale_card, update)

    saved = client.get(f"/api/v1/cards/{card_id}").json()
    assert saved["name_en"] == "Concurrent new name"
    assert saved["card_number"] == 99
    assert saved["description"] == "Requested metadata"
