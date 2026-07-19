# Phase 54 — The Deal Lands: Touchdown Weight for Dealt Cards

Date: 2026-07-19

Phase 52 taught the flip to move with weight. The deal still had the old
problem: `DeckController.MoveCardToSlot` flies a lovely arc - lift, tilt,
ease - and then **snaps dead onto the slot** on its final frame. All energy
vanishes instantly; the card arrives weightlessly, exactly the flaw the flip
had before Phase 52. The dust burst and the deal cue already fire around the
touchdown, but nothing in the card's own body acknowledged the contact.

## What landing means here

A card tossed onto velvet doesn't stop like a screenshot. On contact it gives -
a brief compression - and springs back. Phase 54 adds that touchdown as a
squash-and-recover plus a camera kick on the impact frame:

1. **Impact frame** — the arc ends, the card is on its slot, and the camera
   takes a small `Kick` (0.03 - subtler than the flip's reveal shake at 0.05,
   because three deals land in quick succession and the reveal must stay the
   loudest beat).
2. **Squash** — the card arrives compressed: −10% height, +6% width
   (volume-ish preservation, the classic squash), applied instantly on contact.
3. **Recover** — an ease-out spring back over 0.14 s to the **exact** base
   scale. The PlayMode test asserts the final scale to four decimal places, so
   the flip that follows always starts from a clean rest pose.

This is the deal's counterpart to the flip's reveal kick: the same motion
language (anticipation → action → contact → settle), applied to the other half
of the card's journey. The arc itself - duration, height, tilt, interval - is
untouched; those pacing floors belong to Phase 9 and still hold.

## Implementation

- `DeckController` gains a `Phase54 Landing` header with three serialized
  knobs: `landingSquash` (0.1), `landingSeconds` (0.14), `landingCameraKick`
  (0.03). All dialled, not hard-coded.
- A lazy `CameraChoreographyController` lookup, in the same self-healing style
  `CardFlipController` uses - scene-load order never matters, and the deck
  works fine (minus the kick) in a scene with no choreography camera.
- `LandingSettle` runs right after `MoveCardToSlot` inside the per-card deal
  loop, before the highlight and the post-deal settle pause, so the rhythm
  reads: fly → land (squash) → glow → breathe → next card.

## Verification and honesty

- EditMode (`Phase54DealLandingTests`) keeps the three knobs in a tasteful
  envelope - a splat-level squash or a jolting kick fails the suite.
- PlayMode (`Phase54DealLandingTests`) drives a real one-card deal in the
  ReadingRoom scene and watches the scale every frame: it proves the squash
  visibly happens (min scale-Y dips below 97% of rest), recovers to the exact
  rest scale, and the card comes to rest on its slot.
- What no test can judge is whether the touchdown *feels* right - that is
  yours to feel at the table. Every knob is one Inspector edit away.
