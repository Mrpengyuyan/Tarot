"""Phase 74: a drawn card is reversed half the time, as a real shuffle turns cards over."""
import random

from app.api.v1.endpoints import records


def test_reversed_probability_is_one_half():
    assert records.REVERSED_PROBABILITY == 0.5


def test_orientations_are_random_and_about_half_reversed():
    rng = random.Random(74)
    flags = records.draw_orientations(4000, rng)
    assert len(flags) == 4000
    rate = sum(flags) / len(flags)
    assert 0.46 < rate < 0.54


def test_orientations_follow_the_seed():
    assert records.draw_orientations(10, random.Random(7)) == records.draw_orientations(10, random.Random(7))
