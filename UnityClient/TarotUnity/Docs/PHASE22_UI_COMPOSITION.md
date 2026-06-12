# Phase 22 UI Composition

Date: 2026-06-12

## Purpose

Phase 21 moved the camera down to a seated, intimate view, but the ReadingRoom
UI was still laid out for the old top-down framing: the question input sat in
the middle of the card stage, and several translucent "fake table" overlays
from the flat-UI era washed out the real 3D table.

Phase 22 re-composes the UI around two researched principles:

- Hearthstone board language: the play area stays clear; the player's
  belongings live in arc/tray zones near the player
  ("information far, actions near" - GDC 2015, Derek Sakamoto;
  Fairtravel Battle UI analysis).
- HUD zone theory (Fagerholt and Lorentzon's classic framework): clickable,
  urgent elements belong to unobstructed screen-space zones; atmosphere
  belongs to the world itself, not to overlay chrome.

## Zone Map (1280x720, center-anchored canvas)

- Top info zone (y >= +100): workflow chips, spread status heading, and the
  flip instruction. Read-only, never clicked.
- Card stage band (y = +95 .. -115): no UI at all. The real 3D table, dealt
  cards, deck, and aura own this band.
- Bottom action tray (y <= -130): one dock plate with the question input row
  and the button row (one card, three cards, draw, reveal result at the right
  end, like an end-turn button). Status lines sit under the tray.

## What Changed

- `QuestionInput` (with its Phase 8 frame and Phase 11 glow) moved from the
  card stage center (y=58) into the action tray (y=-164).
- Spread/draw buttons moved from y=116 to the tray row at y=-232 with
  non-overlapping x positions; `RevealResultButton` anchors the right end of
  the tray at (390, -232).
- `Phase12_RevealInstruction` moved to the top info zone (y=118).
- `Phase11_ActionDock` widened to 1120x148 at (0,-204) as the tray plate.
- Status texts tucked under the tray (-290 / -332).
- Deactivated flat-era overlays that faked a table over the real one:
  `Phase11_TableFocusFrame`, `Phase8_CardSlotsGlow`,
  `Phase12_CardFocusVignette` (deactivated, not deleted; the Phase 20
  post-processing vignette replaces their job).
- `DeckStack` moved from x=-2.9 to x=-2.35 so the deck is always visible in
  the seated default framing; `DeckPose` re-aimed (pitch 37.9, yaw -9.9), and
  the Phase 21 geometric aim test validates the new pairing automatically.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 22 UI Composition Bootstrap`.
Screenshots: `Tools/Tarot Unity/Run Phase 22 Visual Capture` ->
`Docs/VisualReview/Phase22/`.

## Exit Criteria

- All interactive controls sit in the bottom action tray without overlap.
- The card stage band is free of blocking UI.
- Flat-era overlays stay deactivated.
- Deck stack is visible in the default framing and DeckPose still aims at it.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 22 is a layout pass on the existing legacy-Text UI; it is
not final high-end VFX or a typography pass. Later phases can add TextMeshPro
with a proper CJK font, styled tray art (wood/velvet trim), card hover tilt,
and Shader Graph card materials.
