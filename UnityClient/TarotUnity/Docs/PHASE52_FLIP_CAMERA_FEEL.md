# Phase 52 — Flip Weight and a Reveal-Synced Camera

Date: 2026-07-18

A pass on game-feel: the card flip and the camera's reaction to it. No new
mechanics - the same flip, tuned so it lands with weight instead of spinning like
a turnstile. The deliberate pacing floors other phases own (flip duration, lift,
the anticipation and reveal beats) are untouched; this is about the *curves* and
the *secondary motion* between them.

## What the flip did before

It rotated linearly to 90 degrees and back, with a static pause standing in for
anticipation and an ease that crawled right at the edge-on seam - so the reveal,
the one moment that should feel snappy, was the slowest part. No overshoot, no
scale, so the card arrived weightlessly and stopped dead.

## The five beats now

1. **Anticipation** — the card winds back (opposite the flip) and dips, easing
   out so it settles *cocked*, launching from a pose instead of from stillness.
2. **Whip to edge-on** — accelerates (ease-in) so the reveal happens at full
   speed, not at a crawl. The lift rises to meet it.
3. **Reveal** — the face swaps at the seam, the cue sounds, and the camera shake
   fires *on this instant* (see below).
4. **Swing in with a pop** — decelerates past flat into a small overshoot, and a
   scale pop (+6%, fading out) puts a beat of emphasis on the face.
5. **Damped settle** — the overshoot and the residual scale ease back to an
   **exact** rest. A flipped card never drifts from where it was dealt; the
   PlayMode test asserts the final transform to sub-millimetre precision.

## The camera change: land the shake on the reveal

The camera already leaned in toward the flipped card (`PunchToward`) and shook on
arrival. But the arrival shake fired at the *lean-in peak* (~0.26 s), well before
the face actually turned over (~0.49 s) - the impact and the reveal were out of
sync. Now the flip fires a second, slightly stronger `Kick` at the exact reveal
frame, so the punctuation lands with the face. The lean-in still reads as the
camera getting interested; the reveal reads as the hit. (Idle breathing and the
lean-in/return curves are left as they were - they were already right.)

## Tuning

Everything is a serialized knob on `CardFlipController`, so the feel is dialled,
not hard-coded: `windBackAngle`, `windBackDip`, `settleOvershootAngle`,
`settleSeconds`, `revealScalePunch`, `revealCameraShake`. Phase52FlipFeelTests
keeps them in a tasteful envelope; a lurch or a jump-scare pop would fail.

## Verification and honesty

EditMode guards the envelope; a PlayMode test drives a real flip on the prefab
and proves the secondary motion is present (dip, lift, pop) and that it settles
to an exact rest. What a test cannot judge is whether it *feels* good - that is
yours to feel with the card under the cursor. The values here are a principled
starting point (anticipation → fast action → overshoot settle), not a claim that
the number is final. Every knob is one edit away from your preference.
