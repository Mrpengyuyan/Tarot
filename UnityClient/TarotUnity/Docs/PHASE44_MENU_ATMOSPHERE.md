# Phase 44 — Menu Atmosphere, Ritual Copy, Corner Exit

Date: 2026-07-16

Driven by the second menu review: the deck on the right clipped, the copy was
too direct, the centre column read as a form, and the table was too empty to
feel like anything was at stake.

## 1. The clipping was mine

Both loose cards sat at the same local Y (0.01) with a centre distance of 0.886
against a 0.8 x 1.18 footprint — two coplanar meshes intersecting. Not a
rendering artefact, a staging mistake. Real scattered cards lie *on* each other:
the second is lifted one card thickness (0.023) and they are spread so only a
corner overlaps.

## 2. Copy: instructions became ritual

| | Before | After |
|---|---|---|
| Subtitle | 在安静的牌桌前，把问题交给牌面。 | **你尚未开口，牌已听见。** |
| Primary | 开始占卜 | **入席问牌** |
| Status | 准备开始一场安静的占卜。 | **烛火已燃，牌已洗过。** |
| Exit | 退出占卜 | **离席** |

入席 / 离席 (take your seat / leave the table) gives the two actions one
vocabulary, and it is what a diviner would say. "你尚未开口，牌已听见" carries
the tension the review asked for: it is a gentle threat.

Note: the status line is set both in `MainMenuController` (runtime) **and** in
the scene. The first pass only changed the runtime string, so the editor and the
capture pipeline still showed the old copy — the serialized value is what the
first frame renders.

## 3. Layout: one way in

Stacked centre-column button / status / quit read as a form. Now: the invitation
owns the centre axis; the status line drops to the foot of the frame where a
system line belongs (it still reports backend/offline state — that is its job);
and quit becomes a bare link pinned to the bottom-right corner.

**On whether the menu needs a quit at all** (the review asked): the function
stays. This is a downloadable desktop game and Phase 34 added it precisely
because players had no way out but Cmd+Q. The problem was never that it existed,
it was that it sat in the same column as the invitation. A plaque in the corner
still read as a competing button, so the graphic is fully transparent and only
the label shows; the Button keeps the Image as its raycast target.

## 4. The table now has things on it

- **Arcane circle** — a gold ritual circle (degree scale, twelve houses,
  interlocked triangles, seven-pointed sanctum, alchemical marks, star field)
  composed in `Tools/UiKitGenerator/gen_arcane.py` and laid on the cloth as a
  flat decal: nothing to light, nothing to clip. **Kept faint (alpha 0.17).**
  The first pass ran it at 0.5 and it out-shouted the cards, which breaks the
  one rule the art direction rests on — the brightest, sharpest thing on the
  table is always a card. It is ground, not subject.
- **Dust motes** in the candlelight.
- **Trinkets** — coins and a ring, seated in the candle pools. Cheap geometry,
  but the difference between "two candles on felt" and somewhere a person works.

## Verification

Two capture iterations at 2560x1440 reviewed. EditMode 228/228, PlayMode 30/30.
Era tests updated to guard the *new* intent rather than deleted: Phase 34 now
asserts quit is a bottom-right corner link that still reads as an exit, Phase 40
asserts it carries no plaque, Phase 7 asserts the ritual copy.
