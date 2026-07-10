# Phase 35 Result Overlay and Menu Polish

Date: 2026-07-10

## Purpose

A fresh review of the Phase 31 HD archive (after the Phase 34 quit button landed)
surfaced three objective defects. Phase 35 fixes exactly those; it makes no
taste-driven composition changes.

## What Was Wrong

1. **Result: stray 3D plate behind the reading copy.** The pre-Phase-25 result card
   stage - `Phase15_ResultCardPedestal` plus the Phase 16 glow pool, rune ring, and
   two particle anchor marker cubes - projects into the reading band (viewport
   ~0.39, 0.44) and its lit edges showed through the translucent reading panel next
   to the 建议 body copy. The stage belongs to the old composition; since Phase 25
   the hero card is a UI Image in the left third.
2. **Result: hero slot darkened with a seam.** `ResultReadingFrame`, a Phase 5
   flat-era overlay the Phase 25 cleanup missed, reached left into the hero card
   slot: its edge cut a visible brightness seam across the slot (and would sit on
   top of real card art at runtime too).
3. **MainMenu: status line on the plate, quit competing with start.** The status
   line's rendered text sat on the gold start plate touching its bottom edge, and
   the Phase 34 quit button was a full-prominence clone whose inherited gold trim
   also rendered a dark notch at its top edge.

## What Changed

- The five stage visuals' MeshRenderers are disabled. The GameObjects stay ACTIVE:
  Phase 15-21 tests locate them with `GameObject.Find` (which only finds active
  objects) and the particle anchors still parent live systems. No runtime code
  toggles these renderers back on (verified: no caller of the aura visibility API).
- `ResultReadingFrame` is deactivated, not deleted, matching the Phase 25
  convention for redundant flat-era overlays. The reading column keeps its own
  scroll backing, oracle frame, and backdrops.
- `StatusText` moved from y -96 to y -122 so its rendered line clears the start
  plate's bottom edge (-104) instead of touching it.
- `QuitButton` became a quiet secondary action: the cloned gold trim is off, the
  plate shrank from 308x56 to 200x40 (y -182, still centered below the status
  line), and the label dropped to 16pt regular in the theme's muted color - the
  same color `TarotUiTheme` applies to 16pt-and-under text at runtime, so the
  editor bake and play mode match.

## Verification

- `Phase35UiPolishTests`: stage renderers stay disabled while their objects stay
  active; ResultReadingFrame stays off while the scroll stays on; the status line
  clears the plate; the quit button is strictly smaller and quieter than start with
  its trim clone off and the start button's trim untouched.
- Visual: regenerated `Docs/VisualReview/Phase31_HDArchive/` - the stray plate next
  to 建议 is gone, the hero slot is a single uniform tone, and the menu reads as
  primary plate / status line / quiet quit link.
- Full EditMode and PlayMode suites green.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 35 UI Polish Bootstrap`.
Diagnostic (read-only): `Tools/Tarot Unity/Run Phase 35 Result Artifact Diagnostic`.

## Remaining Limitation

Phase 35 is defect repair, not final high-end VFX. The quit button now reads as a
quiet text link; if it should carry a faint plate or hover glow, that is a small
taste tweak on top. The hidden result stage could later be repositioned behind the
hero card as a live aura, but that is a feedback-gated composition change.
