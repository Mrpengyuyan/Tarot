# Phase 57 — The Candles, Modelled

Date: 2026-07-20

Phase 46 gave the candles wax *surfaces* — poured grain, painted drips, a
translucency falloff so the flame lights the wax it sits in. What it never
touched was the geometry underneath, and a close-up diagnosis showed exactly
what that was costing.

## The diagnosis

`Phase57CandleDiagnosticCapture` (read-only) measured the candles before
touching them:

```
Unity built-in Cylinder: 88 vertices, 20 radial segments -> 18.0 degrees per facet

Phase8_LeftCandle
  WaxPool  Cylinder  scale=(0.155, 0.012, 0.155)
  Body     Cylinder  scale=(0.105, 0.300, 0.105)
  Lip      Cylinder  scale=(0.118, 0.014, 0.118)
  Wick     Cylinder  scale=(0.009, 0.022, 0.009)
```

Every candle in both scenes was four Unity primitives stacked up. The close-up
render made the consequences plain:

1. **The `Lip` read as a machined collar.** It is a disc *wider* than the body
   (0.118 against 0.105) with two hard 90-degree edges. On screen it looked
   like a cap screwed onto a pill bottle. This was the loudest tell.
2. **The top was a flat lid** with the wick poking through it. A burning
   candle dishes down into a crater; this one was a perfect disc.
3. **The body was a dead-parallel tube** — no taper, and the painted drips had
   no geometry under them, so they never caught a highlight.
4. **The base was a separate disc** butted against the body, with a visible
   seam where a spill should have flowed.
5. **20 radial segments** — 18 degrees per facet on every silhouette.

## What replaced it

`CandleMeshBuilder` lathes one continuous surface per candle at 64 radial
segments for the front pair and 40 for the back (5.6 and 9 degrees per facet),
along a profile that is a candle rather than a tube:

- **Pooled base** — a concave fillet flowing out of the body, so the spill and
  the candle are one surface with no seam.
- **Tapered body** — a hair narrower toward the top. A dead-parallel tube is
  extruded plastic; poured wax comes out of a tapered mould.
- **Melted shoulder** — the swell below the burn, where wax softened and set
  again. This is what replaces the collar: it *grows out of* the body.
- **Rolled rim** — a quarter arc over the top edge. Melted wax has no sharp
  edge anywhere.
- **Burn crater** — the top dishes down to the wick, steep at the rim and
  flattening toward the middle, the shape a flame actually melts.
- **An uneven burn line** — the rim wanders slightly with angle, because a
  lathe-true circle is itself a tell.
- **Drips as geometry** — nine rivulets, thickening as they climb and beading
  where they cooled.

The wick was re-cut too: tapered toward the tip and curled, instead of a
parallel pin.

### The drips are lathed where the texture paints them

`gen_wax.py` paints nine drips from a seeded sequence. Those exact angles were
extracted from the generator and baked into the mesh builder, so every
geometric rivulet sits under its painted one rather than beside it.

That alignment exposed a real defect in the Phase 46 generator: its
contact-shadow pass re-seeds `Random(7)` but does **not** replay the 90 grain
iterations the drip loop runs after, so it landed on entirely different
columns — every "contact shadow where each drip meets the body" fell in a gap
instead of under a drip. The generator is fixed, and `WaxColor.png` was
regenerated on the owner's authorisation.

Measured, not asserted: only the colour map changes (49,470 pixels — the shadow
layer moving); `WaxEmission.png` is pixel-for-pixel identical and was left
alone rather than rewritten with a no-op diff. In the render the fix moves
**0.25% of pixels with a peak delta of 19/255** — the candle's front is blown
out by its own flame, which washes out exactly this kind of soft shading. It is
a correctness fix that makes the painted drips read as raised on the shadowed
side, not a dramatic change to the look.

## What was deliberately not touched

The candle root's world position, its `Light`, the `Flame`/`Halo` billboards,
and the 0.085 gap between the wax rim and the flame centre. Six phases fought
for this lighting and framing; this is a geometry pass, and a test asserts the
flame still sits where it sat.

## Verification, and one honest limit

- `Phase57CandleModelTests` guards the resolution (a primitive creeping back in
  fails on vertex count), the profile features (pooled base, shoulder swell,
  crater floor below the rim), and that the flame did not move.
- Close-ups before and after are in `Docs/VisualReview/Phase57/`, framed from
  the scene camera's own direction. The HD archive was regenerated on the new
  geometry.
- **Three tuning passes were needed, and the captures caught all three.** The
  first rim wobble was strong enough that the candle read as *cut open*; the
  first drips all started at one height and welded into a bulging ring; the
  first wick leaned nearly twice its own width and read as a bent blade.
- **On the crater interior (corrected after Phase 58).** This phase first
  recorded that "the crater interior blows out to white, geometry cannot fix
  it." A re-check with measurement, not eyeballing, walked that back. The
  crater floor is bright — its own flame sits 0.085 above it, so that surface
  is directly lit — but it is not a flat white disc: the floor's luminance
  standard deviation is 45.5 (min 13, max 255), with the wick casting a
  distinct dip across it (sampled floor columns read 242 / 182 / 247 left to
  right). The bowl's 3D shading survives; the earlier "blows out and eats the
  shape" claim was overstated, written off an unconverged capture before the
  Phase 58 fix. A bright melt pool directly under a flame is also physically
  right. About 21% of the deepest-floor pixels do clip to pure white, which is
  a defensible look rather than a defect; left as is.
- The front menu candles reading as saturated red was **not real** — see
  Phase 58. It was the capture pipeline sampling an unconverged frame, not the
  material. Corrected there; nothing about the modelling caused it.
