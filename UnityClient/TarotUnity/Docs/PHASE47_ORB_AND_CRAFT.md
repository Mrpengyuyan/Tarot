# Phase 47 — The Orb Becomes Glass; Button and Deck Craft

Date: 2026-07-16

Answering three notes: the scrying orb looked cheap and dragged the whole menu
down, the primary button was a flat purple plaque, and the deck's stacked edges
were dead black.

## The orb: why buying an asset was the wrong fix

The brief suggested sourcing a crystal ball image or a game asset. Research
(Unity URP glass/Fresnel techniques, plus a survey of the CC0 asset sites) says
that would not have solved it, and the reason is worth writing down:

**The geometry was never the problem.** A crystal ball *is* a sphere, and we
already had one. It read as a plastic marble because of the shading, and a
downloaded model would have arrived as a sphere plus somebody else's art
direction to fight.

What actually makes glass read as glass:

1. **Fresnel.** Reflectance climbs toward grazing angles, so a glass sphere has
   a bright rim and a dark centre. The old orb was a matte Lit sphere with a
   flat tint — no rim at all. This is the single biggest tell, and it is why a
   halo quad had been propped behind it: it was faking an edge the material
   could not produce.
2. **Something inside.** `gen_orb.py` composes an equirectangular interior —
   nebula at two scales, a spiral drawn through the mist, stars caught in the
   glass, poles darkened so the map's pinch reads as depth rather than a seam.
3. **Parallax.** The interior is sampled along a view-direction offset, so it
   sits *behind* the surface and slides as the camera breathes. Without the
   offset the nebula is a decal painted on the ball.
4. **Tight speculars.** Glass answers a candle with a small hard dot, not the
   broad falloff of the wax beside it. The shader loops the additional lights.

`Assets/Shaders/TarotScryingOrb.shader` (following the Phase 28 holographic-card
precedent). The halo quad is retired — with a real rim it only fogged the edge
it was meant to sell.

First pass ran the interior at a bright royal blue and it read as a painted
marble; deepened to a smoky violet with more parallax so the nebula is
half-seen.

## Button

`TarotButton` regenerated with an inner shadow under the top edge (the plaque is
recessed into its frame) and a bevel on the gold moulding — highlight on the
upper face, shadow on the lower — drawn after the metal sheen so it rides on top
rather than being averaged into it.

## Deck

`MP_DeckBody` was a near-black tint, so the stacked card sides had no value
separation and the deck read as one solid lump. It is aged paper now: each
card's edge catches the candles and throws a shadow on the one below. A deck's
charm is that you can count it.

## Two additions taken back out

A card fan and wax spills were added and then removed in the same phase:

- The fan sat at x=-2.35, which is exactly where the front-left candle stands,
  so the candle grew out of the cards. Even placed clear it was wrong: that side
  already carries the deck, two candles, the censer and the coins, and a second
  pile of cards says nothing the deck does not already say.
- The spills duplicated the `WaxPool` every candle has carried since Phase 42,
  so each candle stood in two offset discs and read as being served on a saucer.

The removal is code (`RemoveOverAdditions`), not a hand revert, so a re-run over
an already-staged scene lands in the same place. **The table is full** — further
elements belong in the backdrop, not on the cloth.

## Verification

Four capture iterations at 2560x1440. EditMode 244/244, PlayMode 30/30.
