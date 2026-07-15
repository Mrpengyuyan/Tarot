# Phase 39 UI Reskin — Gold Plaques on the Velvet

Date: 2026-07-15

## What changed

The ReadingRoom canvas chrome moved from flat translucent boxes to the
Midnight Parlor nine-slice kit (Phase 37):

- **Step tracker** — the HUD plate and its five progress plates wear the subtle
  gold-framed panel; the strip now reads as a mounted brass rail rather than
  debug boxes.
- **Action dock** — the bottom band is a full `TarotPanel` plaque (gold double
  frame, corner ticks) that visually holds every interactive control.
- **Buttons** — `OneCardButton`, `ThreeCardButton`, `DrawButton`,
  `RevealResultButton` wear the `TarotButton` plaque (regenerated with a
  heavier outer frame and brighter fill gradient after the first capture read
  underweight). Their ColorBlocks were reset: the flat-era normal colors (dark
  red/gray fills) would have overwritten the sprite tint at runtime; hover now
  brightens toward gold and press sinks darker.
- **Question input** — wears the subtle panel so it reads as an inset slot in
  the dock.
- **Deactivated** (kept in scene, Phase 22/25 convention):
  `Phase8_SpreadChoiceFrame`, `Phase8_QuestionPanelFrame`,
  `Phase11_QuestionGlow` — redundant flat-era frames behind the new plaques.

`TarotButton.png` was regenerated in place via `Tools/UiKitGenerator/gen_uikit.py`
(the generator file is the source of truth; the sprite is reproducible).

## Verification

- Two HD capture iterations via `Phase38CaptureBuilder`
  (`Docs/VisualReview/Phase38/ReadingRoom.png`) reviewed for frame weight,
  fill contrast, and label legibility.
- `Phase39UiReskinTests` guard the sprite assignments, sliced mode, clean
  button ColorBlocks, and the deactivated flat-era frames.
