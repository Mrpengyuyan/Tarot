# Phase 46 — Candles as Wax, and Lit

Date: 2026-07-16

Continuing the menu polish. With the mid-ground filled, the candles became the
loudest remaining tell: perfect cylinders in one flat cream, with one flat
emission value across the whole body. That is plastic tubing, not wax.

## What wax actually does

Three things the old candles did none of:

1. **It runs.** Drips down the side, beading where they cool.
2. **It is translucent.** The flame lights the wax it sits in — bright at the
   rim, falling off down the stick. Not the whole stick at one value.
3. **It gets dirty.** Soot at the burnt rim, spill at the base.

`Tools/UiKitGenerator/gen_wax.py` composes two maps against Unity's cylinder
side UV (V=0 base, V=1 rim): `WaxColor` (cream, poured grain, drips with contact
shadow, soot at the rim, grubby base) and `WaxEmission` (the translucency).

**The translucency map needed a floor.** The first pass fell to pure black by a
third of the way down, and the candle bodies went black — a candle's own flame
sits directly above its vertical sides and grazes them at N·L≈0, and the fill
light is too far off to reach, so with no emission there was no light on the wax
at all. It read as a charred stick. The curve now lands on 0.24 rather than
zero: brightest at the flame, present all the way down.

## Flames now burn

`CandleFlickerController` (runtime): two layered Perlin bands — a slow wander
plus a faster shimmer — drive light intensity and the flame billboard's stretch.
A still flame is the clearest tell that a lit scene is a render.

- Each candle gets its own **position in the noise field**, not a time offset;
  two candles flickering in unison is its own kind of wrong.
- The back pair flicker faster and shallower so they do not pull the eye.
- Allocation-free per frame (Perlin sampling and struct math only), per the
  Phase 36 baseline.

## Verification

Two capture iterations at 2560x1440. EditMode 238/238, PlayMode 30/30.

**The flicker cannot be seen in a still capture** — it needs the build or Play
mode. Everything else here (wax grain, drips, the translucency falloff) is
verified in the captures.
