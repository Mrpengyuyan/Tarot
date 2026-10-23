import json

import pytest
from sqlalchemy import select, text
from sqlalchemy.exc import IntegrityError

from app.crud import spread as spread_crud
from app.models.spread import SpreadType
from app.schemas.spread import SpreadPosition, SpreadTypeCreate, SpreadTypeUpdate


class ExtendedPosition(SpreadPosition):
    note: str


def _spread(**overrides):
    return SpreadTypeCreate(**{
        "name": "Serialization spread", "description": "Retained description", "card_count": 2,
        "positions": [
            {"position": 2, "name": "Future", "meaning": "Second meaning"},
            {"position": 1, "name": "\u5f53\u4e0b\U0001f319", "meaning": "First meaning"},
        ],
        **overrides,
    })


def _saved_positions(db, spread_id):
    raw = db.execute(text("SELECT positions FROM spread_types WHERE id = :id"), {"id": spread_id}).scalar_one()
    return json.loads(raw)


@pytest.mark.parametrize("mode", ["single", "batch"])
@pytest.mark.parametrize("position_kind", ["mapping", "model", "extended"])
def test_creation_preserves_nested_positions_and_defaults(db_session, mode, position_kind):
    payload = _spread()
    if position_kind != "mapping":
        position_type = ExtendedPosition if position_kind == "extended" else SpreadPosition
        extras = {"note": "Keep extension"} if position_kind == "extended" else {}
        positions = [position_type(**position.model_dump(), **extras) for position in payload.positions]
        payload = _spread(positions=positions)
    if mode == "single":
        created = spread_crud.create_spread(db_session, payload)
    else:
        created = spread_crud.batch_create_spreads(db_session, [payload])[0]

    expected = [
        {"position": 2, "name": "Future", "meaning": "Second meaning"},
        {"position": 1, "name": "\u5f53\u4e0b\U0001f319", "meaning": "First meaning"},
    ]
    if position_kind == "extended":
        expected = [{**position, "note": "Keep extension"} for position in expected]
    assert _saved_positions(db_session, created.id) == expected
    assert (created.difficulty_level, created.is_active, created.is_beginner_friendly) == (1, True, False)
    assert (created.name_en, created.layout_image_url, created.usage_count) == (None, None, 0)
    assert (created.suitable_for_love, created.suitable_for_career, created.suitable_for_finance,
            created.suitable_for_health, created.suitable_for_general) == (True, True, True, True, True)


@pytest.mark.parametrize("position_kind", ["omitted", "mapping", "extended"])
def test_update_keeps_omitted_values_and_clears_explicit_nullable_field(db_session, position_kind):
    created = spread_crud.create_spread(db_session, _spread(
        name_en="Clear this", layout_image_url="keep.png", difficulty_level=4,
        is_active=False, is_beginner_friendly=True, suitable_for_career=False,
    ))
    created.usage_count = 37
    db_session.commit()
    changes = {"name_en": None}
    if position_kind != "omitted":
        changes["positions"] = [
            {"position": 1, "name": "New first", "meaning": "New first meaning"},
            {"position": 2, "name": "New second", "meaning": "New second meaning"},
        ]
        if position_kind == "extended":
            changes["positions"] = [ExtendedPosition(**position, note="Not stored by updates")
                                    for position in changes["positions"]]
    saved = spread_crud.update_spread(db_session, created, SpreadTypeUpdate(**changes))

    expected = [
        {"position": 1, "name": "New first", "meaning": "New first meaning"},
        {"position": 2, "name": "New second", "meaning": "New second meaning"},
    ] if position_kind != "omitted" else [
        {"position": 2, "name": "Future", "meaning": "Second meaning"},
        {"position": 1, "name": "\u5f53\u4e0b\U0001f319", "meaning": "First meaning"},
    ]
    assert _saved_positions(db_session, saved.id) == expected
    assert (saved.name, saved.description, saved.card_count) == ("Serialization spread", "Retained description", 2)
    assert (saved.name_en, saved.layout_image_url, saved.usage_count) == (None, "keep.png", 37)
    assert (saved.difficulty_level, saved.is_active, saved.is_beginner_friendly, saved.suitable_for_career) == (
        4, False, True, False,
    )


def test_batch_commit_failure_can_roll_back_every_spread(db_session, db_session_factory):
    db_session.execute(text("""
        CREATE TRIGGER reject_second_spread BEFORE INSERT ON spread_types
        WHEN NEW.name = 'Rejected'
        BEGIN SELECT RAISE(ABORT, 'injected batch failure'); END
    """))
    db_session.commit()
    with pytest.raises(IntegrityError):
        spread_crud.batch_create_spreads(db_session, [_spread(name="First"), _spread(name="Rejected")])
    db_session.rollback()

    with db_session_factory() as verification:
        assert verification.scalars(select(SpreadType)).all() == []
