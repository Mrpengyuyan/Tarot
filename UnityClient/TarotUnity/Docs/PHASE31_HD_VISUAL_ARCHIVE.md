# Phase 31 HD Visual Archive

Date: 2026-06-15

## Purpose

Produce a complete high-resolution visual archive of the current game UI so the
whole look can be reviewed in one place. Every faithful screen the player passes
through is rendered at 2560x1440 into `Docs/VisualReview/Phase31_HDArchive/`.

## What Is Captured

`Tools/Tarot Unity/Run Phase 31 HD Visual Archive` is read-only (it opens scenes
and renders, never saves) and writes:

- `01_MainMenu.png` - title, tagline, start ritual button.
- `02_ReadingRoom.png` - progress rail, spread select, deck and empty slots on
  the 3D table.
- `03_Result_default.png` - question header, card hero, scrollable reading (short
  copy, Phase 29).
- `04_Result_long.png` - a long backend-style reading filling the scroll panel.
- `phase31_archive_manifest.json` - shot list with descriptions.

Each shot composites the UI (canvases switched to ScreenSpaceCamera for the
render) over the 3D scene with the seated cinematic camera and the Phase 20
post-processing stack.

Note: the Result card hero frame reads empty in the archive because the
`ResultPanelPresenter` is only populated with a drawn card at runtime
(`PresentSession`); a static edit-mode capture has no session, so the slot is
blank by design.

## Flagged Finding: Face-Up Card Sizing (Hero Card - Needs Live Review)

The face-up card reveal is deliberately NOT in the archive. While staging face-up
cards in edit mode (mimicking `DeckController.DealCards`: spawn the card prefab
under the card parent, move to the slot, `SetFaceUp(true)` + `SetFaceArtwork`),
the card faces rendered far larger than the card body and overlapped each other.

`SetFaceArtwork` only assigns the sprite to a `SpriteRenderer` in DrawMode.Simple
and never resizes it, so a sprite's world size is `pixels / pixelsPerUnit`. The
Phase 27 HD sprites import at PPU 100, which is much larger than the low-res art
the card prefab's face slot was originally tuned for. That points to a real
card-face sizing regression from the HD swap - but headless edit-mode staging
could not cleanly reproduce the exact runtime path (diagnostics were
inconsistent), and the card prefab is the hero element that should be tuned with
live feedback rather than altered blind.

Recommendation: flip a card in a real play session. If the face overflows the
card there too, the fix is to normalize the face artwork to the card footprint
(consistent sprite PPU on import, or a fit step in `SetFaceArtwork`) - a focused,
feedback-tuned follow-up.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 31 HD Visual Archive`.

## Exit Criteria

- The four archive PNGs and the manifest are written at 2560x1440.
- EditMode tests confirm the archive artifacts exist.

## Remaining Limitation

Phase 31 is a review archive; it is not final high-end VFX. The face-up card
reveal (and its sizing fix) and any further visual polish remain for a
feedback-tuned pass.
