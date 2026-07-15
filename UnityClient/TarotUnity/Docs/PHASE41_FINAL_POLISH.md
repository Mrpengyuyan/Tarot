# Phase 41 Final Polish — Redesign Close-Out

Date: 2026-07-15

## What this phase does

Closes out the Midnight Parlor redesign (Phases 37-40) with verification and
housekeeping; no new visual composition.

- **HD archive regenerated** — `Docs/VisualReview/Phase31_HDArchive/` now shows
  the four Midnight Parlor screens (menu vignette, velvet reading table with
  celestial card backs, gold-framed result in default and long-reading states).
  The long-reading capture confirms the scrollable reading panel carries the
  new gold frame without regression.
- **Orphaned material sweep** — 14 legacy materials whose only users were
  deleted by the rebuild were verified unreferenced (GUID search across all
  scenes and prefabs) and deleted:
  `MAT_Phase8_TableGreen`, `MAT_Phase12_CardRevealStage`,
  `MAT_Phase12_RevealBackdrop`, `MAT_Phase14_TableDepthPlane`,
  `MAT_Phase14_CardRevealPool`, `MAT_Phase14_CardEdge`,
  `MAT_Phase14_CastShadow`, `MAT_Phase14_FaceRimLight`,
  `MAT_Phase14_ArtworkGlass`, `MAT_Phase15_RitualTableSurface`,
  `MAT_Phase15_TableDepthRing`, `MAT_Phase7_DeepVelvet`, `MAT_CardAccent`,
  `MAT_Table`. Survivors verified: `MAT_Phase7_MoonGold` (card title band),
  `MAT_DeckStack` / `MAT_SpreadSlot` (gameplay prefabs),
  `MAT_Phase14_RevealGlow` (flip flash). Era tests updated in the same pass.
- **Full suites green** and a fresh macOS player build produced for the user
  playthrough (real GPU numbers via the Phase 36 Metal HUD workflow).

## What stays feedback-gated

Candle modeling quality, hero-card art tint on the result screen, holographic
intensity, and any motion/rhythm changes wait for the user's played round, per
the standing two-track autonomy rule.
