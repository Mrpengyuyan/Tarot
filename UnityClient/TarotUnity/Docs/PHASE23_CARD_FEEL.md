# Phase 23 Card Feel

Date: 2026-06-12

## Purpose

Phases 20-22 fixed rendering, camera, and composition. Phase 23 makes the
cards themselves feel physical, which research identifies as the core of
premium card-game feel:

- Hearthstone's GDC-documented "physicality" principle: every touch gets an
  immediate physical reaction; cards tilt and lift under the cursor.
- Interaction-feedback research (NN/G animation duration; Val Head's duration
  guidance): feedback must settle near 100ms to feel instantaneous; ease-out
  for entering motion; exits slightly faster than entries.
- Hearthstone's three-phase feedback structure: Preparation, Emphasis,
  Aftermath - the aftermath should only last long enough to process the
  result.

## What Changed

### Hover physicality

`CardHoverTiltController` (new, on `PF_TarotCard`): hovering a face-down card
lifts it by 0.045 world units and tilts it up to 7 degrees toward the pointer,
with an exponential response of about 60ms so the card settles near 100ms.
It rides the existing EventSystem pointer pipeline (PhysicsRaycaster +
`IPointerEnter/Exit/Move`), the same one `CardClickHandler` already uses.

Safety rules:

- Only face-down cards react; flipped cards stay still.
- `CardFlipController` calls `Suspend()` at flip start, which restores the
  rest pose synchronously so the flip animation never starts from a tilted
  position and the two systems never fight over the transform.

### Rhythm tuning (research-backed)

- Flip punch aftermath: hold 0.5 to 0.32 seconds, return 0.5 to 0.4 seconds,
  so the camera settles together with the flip instead of lingering past it,
  and the return (an exit) eases out slightly faster than the lean-in.
- Flip lift raised from 0.12 to 0.16 for a more readable emphasis arc.
- Deal interval widened to 0.18 seconds, above the deliberate Phase 9
  "weightier deal" floor of 0.16, so each landing card reads as its own beat.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 23 Card Feel Bootstrap`.

Hover feel is runtime-only and cannot be screenshot-verified; PlayMode tests
cover lift, tilt, settle-back, and suspend behavior instead.

## Exit Criteria

- `PF_TarotCard` carries a tuned `CardHoverTiltController`.
- Hover lifts and tilts; exit settles back; suspend restores rest pose and
  blocks further hover.
- Punch aftermath, flip lift, and deal interval sit in the tuned ranges.
- The ReadingRoom camera still carries the PhysicsRaycaster pointer pipeline.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 23 is the interaction-feel pass; it is not final high-end VFX. Later
phases can add Shader Graph card materials (foil, view-dependent sheen), card
motion trails, TextMeshPro CJK typography, and screenshot-driven final polish.
