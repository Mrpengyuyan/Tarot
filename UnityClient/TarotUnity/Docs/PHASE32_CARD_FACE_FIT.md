# Phase 32 Card Face Fit (Oversized Flip Art Bugfix)

Date: 2026-06-15

## The Bug

After Phase 27 swapped the card faces to HD sprites, flipping a card showed the
art wildly oversized and overlapping its neighbours. Phase 31 flagged this for a
feedback-tuned follow-up; this phase is that fix.

## Root Cause (Investigated, Not Guessed)

A definitive diagnostic (place a card exactly like `DeckController.DealCards`,
flip it, read the real `faceArtworkRenderer` field) showed:

- `SetFaceArtwork` only assigned the sprite to the face `SpriteRenderer` and never
  sized it. A sprite's world size is `pixels / pixelsPerUnit`.
- The HD sprites import at PPU 100, so `major_01` (739x1280 px) rendered at
  **2.65 x 9.06 world units** while the card body is only **0.78 x 1.18** - about
  3.4x too wide and 7.7x too long. The prefab's face slot had been tuned for the
  old low-res art's much smaller world size.

A second issue surfaced once the art was correctly sized: the face sprite sat only
~0.003 units above the opaque cream front planes, so at the seated grazing angle
the front plane occluded the (now small) art - it had only been visible before
because the oversized quad stuck out far past the card. Rendering the sprite in
isolation confirmed the art itself was perfect; it was buried in the face layer
stack.

## The Fix

Resolution-independent, so it holds for any sprite/PPU and future art:

1. `CardView.FitFaceArtwork` (runtime): after assigning the sprite, scale the face
   renderer (preserving aspect) so the art fits a target world footprint
   (`faceArtworkWorldSize`, default 0.64 x 1.0). Bounds are only valid once the
   front is active, so it is invoked from both `SetFaceArtwork` and `SetFaceUp`
   (after the front activates) to cover deal-then-flip and flip-then-art orders.
2. `Phase32CardFaceFitBootstrapper` (data): sets `faceArtworkWorldSize` on the
   prefab and lifts the face renderer ~0.027 world units along the card thickness
   axis so it clears the cream front planes and is no longer occluded.

The holographic foil sheen (Phase 28) is preserved - it does not wash the art.

## Verification

- `Phase32CardFaceFitTests` (EditMode): instantiates the real prefab, flips it,
  assigns the HD sprite, and asserts the face fits the card footprint (was
  2.65 > 0.78), fills a reasonable portion of it, and clears the front plane.
  Confirmed failing before the fix, passing after.
- `Tools/Tarot Unity/Run Phase 32 Card Face Capture` renders three face-up cards
  in the seated view: `Docs/VisualReview/Phase32/ReadingRoom_cards_faceup.png`
  shows The Fool / The Magician / The Star at correct size with crisp art.
- EditMode 182/182 and PlayMode 29/29 green.

## How To Run

1. `Tools/Tarot Unity/Run Phase 32 Card Face Fit Bootstrap` (sets prefab footprint
   + lift; the runtime sizing lives in `CardView`).
2. `Tools/Tarot Unity/Run Phase 32 Card Face Capture` (verification render).

`Phase32CardFaceDiagnostic` is a read-only investigation tool kept for future
card-face debugging; it logs the renderer/stacking state and never saves a scene.

## Remaining Limitation

Phase 32 fixes correctness (size + visibility); it is not final high-end VFX. The
target footprint and lift are single tunable values, and a richer reveal (animated
flip-in, stronger foil) remains for a feedback-tuned pass.
