# Phase 40 Menu + Result Recomposition — the Parlor at Rest and at Answer

Date: 2026-07-15

## MainMenu

The menu becomes a candlelit tabletop vignette on the shared Midnight Parlor
stage (`MP_MenuStage`: velvet cloth, walnut far rim, parlor backdrop):

- **Deleted**: `Phase7_ImmersiveMenuRoot` (the brown disc "table" halo) and
  `Phase8_MenuTableSurface` (the green slab).
- **Added**: a ten-card staggered deck beside the left candle, two scattered
  face-down cards near the right candle (all wearing the composed celestial
  back), and a soft `MP_WarmGlow` halo quad behind each candle flame.
- **UI**: the start button wears the `TarotButton` plaque (its Phase 8 gold
  trim is now deactivated); the quit button becomes a quiet gold-line link
  (`TarotPanelSubtle` at 0.66 alpha — keeps the Phase 35 hierarchy decision).
  The Phase 11 depth fakes (`Phase11_MenuDepthFrame`, `Phase11_TableDepthShadow`,
  `Phase11_ActionRail`) are deactivated and the remaining full-screen washes
  drop to a light grade (0.22/0.15/0.25) so the velvet reads.

## Result

- **Stage**: `MP_ResultStage` puts velvet and the parlor backdrop behind the
  reading (no rim beam — at this shallow camera pitch it read as a gray stripe
  across the UI, so the result stays cloth + backdrop only).
- **UI**: the reading scroll and the hero card showcase wear `TarotPanel`
  frames; the back-to-menu button wears the plaque; the two gold dividers
  become `TarotDivider` sprites (center diamond); and the flat amber halo
  rectangle behind the hero card becomes a real radial `TarotGlow` candle glow.
  Backdrop washes drop to 0.40/0.24.

## Verification

- Two HD capture iterations per screen (`Docs/VisualReview/Phase38/MainMenu.png`,
  `Result.png`). Iteration 1 exposed the buried velvet (stacked washes), the
  stripe-reading rim, and the amber slab halo; iteration 2 resolved all three.
- Era tests updated: Phase 7 (menu halo deleted), Phase 8 (menu slab deleted),
  Phase 35 (the plaque supersedes the start button's flat trim).
- `Phase40MenuResultTests` guard the stages, the deletions, the chrome sprites,
  and this document.
