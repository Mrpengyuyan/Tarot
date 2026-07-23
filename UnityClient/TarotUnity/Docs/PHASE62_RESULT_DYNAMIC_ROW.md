# Phase 62 — Result Band: Dynamic N-Card Row

Phase 60 fixed the visible symptom (a three-card spread showing only one card), but
the result band was still built as **exactly three fixed cells**. Any spread with
more than three cards would silently drop every card past the third — the same class
of bug, just latent because the shipping UI only offers one- and three-card spreads.

This audit-and-fix makes the **result screen** robust for any card count.

## What changed

- **`ResultPanelPresenter`** lays the band out at runtime instead of relying on baked
  cell positions. The used cells are centred as one row: cell *i* sits at
  `(i − (used−1)/2) · pitch`. The pitch starts at the 3-card value (348) and shrinks
  (with the cells scaling down to match) once the row would overflow the band width,
  so a larger spread stays on screen rather than dropping cards. A one- or three-card
  reading is pixel-identical to Phase 60. If a spread ever exceeds the pool it logs a
  warning rather than dropping cards unnoticed.
- **The band bootstrapper** now builds a pool of ten cells (enough for a Celtic
  Cross) instead of three, and wires the presenter's layout parameters. Cells beyond
  the ones a reading uses stay hidden.

## Scope note — the table slots are a separate cap

The reading-room **table** still deals through `SpreadLayoutController`, whose
fallback slots are the three three-card slots, and `DeckController.DealCards` deals
`min(draws, slots)`. So a spread larger than three would still under-deal on the
table. That is intentionally left for whenever a bigger spread is actually added
(there is no such spread today — only one- and three-card are reachable), at which
point the table slots, a spread picker, and the camera framing all need to grow
together. This phase hardens only the result screen, which is the piece that would
otherwise drop cards for a backend reading of any size.

## Files

- `Assets/Scripts/UI/ResultPanelPresenter.cs` — runtime centred-row layout, pool cap
  warning.
- `Assets/Editor/Phase60ResultSpreadBandBootstrapper.cs` — builds a ten-cell pool and
  wires the layout params (evolved from the Phase 60 three-cell band).
- `Assets/Editor/Phase62ResultDynamicRowCaptureBuilder.cs` — renders a five-card and a
  three-card reading for review.
- `Assets/Tests/EditMode/Phase62ResultDynamicRowTests.cs` — guards the five-card fill,
  the centred layout, the pool depth, and the unchanged three-card case.
- `Docs/VisualReview/Phase62/Result_fiveCard.png`, `Result_threeCard.png`.
