# Phase 25 Result Composition

Date: 2026-06-14

## Purpose

The Result screen is the payoff of the whole flow and was the weakest screen
after Phase 24. Thirteen phases of additive bootstraps had left it unbalanced:
the gold section labels sat at x -390, inside the card showcase footprint, while
the body text drifted right at x 70 / width 650 into dead space, so labels and
values never read as a pair. A decorative crest ("牌面回声") floated over the same
band. The screen looked like a graybox, not a reading.

This is a composition pass, chosen deliberately for a round with no live
feedback: it is the concrete weakest screen, it is fully verifiable from static
screenshots, and it carries almost no regression risk to hero elements - unlike
foil/sheen shaders or procedural card art, which are motion- and taste-dependent
and unsafe to author blind.

## Research

Game-UI composition guidance (focal points, NN/G card patterns, results-screen
references) and tarot-app layout patterns converge on the same rules:

- One clear focal point; size, colour, and position carry the hierarchy.
- Align elements on shared horizontal/vertical lines; use the rule of thirds.
- Put a card's meaning beside the card.
- Remove dead space and clutter.

## What Changed

`ResultPanelPresenter` fills the left slot with the first drawn card's real
artwork and the right text with the AI interpretation, so the composition
follows that data flow:

- Full-width header: the question in gold, the spread beneath it, a gold divider.
- Card hero in the left third: the layered showcase, plate, and artwork slot
  share one centre and are enlarged so the real card art reads as one framed
  object.
- Reading column in the right two-thirds: each section is a gold accent header
  stacked directly above its body via a top-down cursor, all sharing the
  column's left edge, on a subtle backing panel.
- Footer: a gold divider, the warning line, and the centred return button.

Supporting changes:

- New `TarotUiAccentText` marker: `TarotUiTheme` repaints text by size on Awake,
  which would flatten the gold headers to ivory at runtime. Marked headers keep
  the gold accent, so the running game matches the screenshot.
- Representative placeholder copy is written so the static capture shows a real
  reading; `ResultPanelPresenter` overwrites it at runtime.
- Redundant legacy overlays (`Phase8_ResultCrest`, `Phase7_ResultOracleFrame`,
  early Phase 11 columns, the Phase 14 text bridge) are deactivated, not deleted,
  the same way Phase 22 quieted flat-era overlays. The serialized presenter
  references are untouched - nothing is renamed or removed.

Body text stays at size 19 so the OverallText size floor other phases assert
still holds.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 25 Result Composition Bootstrap`, then
`Tools/Tarot Unity/Run Phase 25 Result Capture` for the screenshot.

## Exit Criteria

- Each section header stacks above its body with no overlap.
- Every section header carries the gold accent marker.
- The card hero sits in the left third, clear of the reading column.
- The redundant legacy overlays are deactivated.
- The presenter's text references are intact and OverallText stays >= 19.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 25 is a composition pass; it is not final high-end VFX. Later phases can
still add Shader Graph foil card materials, view-dependent sheen on the card
hero, a richer reading-panel background, and result-reveal motion - the kind of
spectacle best tuned with live feedback.
