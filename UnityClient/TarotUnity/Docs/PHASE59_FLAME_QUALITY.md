# Phase 59 — The Flame, Rebuilt

Date: 2026-07-21

A three-screen review (on the Phase 58 converged capture pipeline) found the
scenes in good shape except for one thing in every candlelit frame: the flame.
The candles are the focal light of both the menu and the reading room, and the
flame is the brightest, most-looked-at object in each — and it was the worst.

## The diagnosis

A close-up showed two defects: **horizontal banding** running across the whole
flame like venetian blinds, and a **stepped hard edge** down the top centre.
The source sprite, `CandleFlame.png`, turned out to be a clean 256px soft
gradient with no banding in it at all — so the banding was introduced
downstream. Its importer settings were the cause:

```
textureCompression: 1   (Compressed)
compressionQuality: 50
```

A flame is a pure smooth gradient, which is the single worst case for block
(DXT/BC) compression — the 4x4 blocks quantise a smooth ramp into visible
steps. The banding was the compressor, not the art.

Two secondary problems: at 256px the flame magnified in-frame to a soft blob,
and — unlike the wax maps, which regenerate from `gen_wax.py` — it was a
mystery asset with no generator behind it.

## What replaced it

`Tools/UiKitGenerator/gen_flame.py`, a reproducible generator (matching the wax
pipeline), composes a 512px flame from a real profile rather than a blurred
blob:

- **A true candle-flame silhouette** — a rounded base that hugs the wick, the
  belly widest just above it, tapering the long way to a soft point. Three
  earlier shapes were rejected against previews: a thread-thin jet, a blunt
  bullet, and a symmetric olive that pinched at the base (a flame is full at
  the bottom, not pointed).
- **A heat ramp through the body** — deep amber at the edge through orange and
  gold to a near-white core, so the flame reads as hot in the middle and cooler
  at its skin.
- **A cool blue root** — just above the wick, where a real flame burns bluish.
- **A wide soft outer glow** composited under the core, so it sits in light.

Imported **uncompressed**, so the gradient stays smooth. At 512px RGBA that is
a quarter-megabyte texture — trivial, and the banding is simply gone.

## Verification

- Before/after A/B at the player's framing (`Docs/VisualReview/`): the old flame
  is banded, hard-edged, and reads as a flat smear; the new one is a clean
  tapered teardrop that sits in its glow.
- `Phase59FlameQualityTests` guards the fix at its weak points: the texture is
  512+, imported uncompressed, and has its generator on disk. The next person
  who re-imports it with compression on, or deletes the generator, fails.
- HD archive regenerated on the new flame.

## Honest note

The new flame is a little slimmer than the old banded one — perhaps 30% less
area — because the old blob bled wide. The new proportion is truer to a real
candle and far cleaner, but whether it wants slightly more *presence* (a larger
billboard quad) is a taste call left for a play session. The quad sizing that
six phases tuned was not touched here; this pass is the texture and its import.
