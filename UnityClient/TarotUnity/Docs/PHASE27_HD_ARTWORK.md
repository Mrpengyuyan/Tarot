# Phase 27 HD Card Artwork

Date: 2026-06-15

## Purpose

Phase 13 wired an authentic Rider-Waite-Smith 1909 deck (public domain) so that
flipping a card already showed real art. But only low-resolution scans (400x666)
were imported, because Wikimedia rate-limited the original batch download. The
faces therefore looked soft. This phase re-sources the full deck in HD and wires
it in, so a flipped card and the result hero render crisply.

## What Changed

- All 78 card faces were re-downloaded from Wikimedia Commons (the public domain
  RWS1909 scans, ~1100x1920 full-card), downscaled to a consistent HD height of
  1280, and saved under the existing filenames in a new folder,
  `Assets/Art/Tarot/RWS1909_HD/`, with a `rws1909_hd_sources.json` manifest for
  traceability.
- `Phase27HdArtworkBootstrapper` imports that folder as Sprites at maxTextureSize
  2048 and rebuilds `RWS1909_CardArtworkCatalog` to point at the HD sprites, then
  re-assigns the catalog to the ReadingRoom `DeckController` and the Result
  `ResultPanelPresenter`. The card-key matching is unchanged, so every draw
  resolves to its HD face.

The old low-resolution folder (`Assets/Art/Tarot/RWS1909/`) was left in place,
untouched, for the user to delete - this phase did not overwrite or remove it.
(Update: the user has since deleted that folder; the catalog points only at
`RWS1909_HD`, and the superseded Phase 13 bootstrap now no-ops gracefully if run.)

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 27 HD Artwork Bootstrap`.

## Exit Criteria

- The catalog resolves all 78 cards, each to an HD sprite (height >= 1000) from
  the HD folder.
- HD textures import as Sprites at HD resolution.
- EditMode tests pass.

## Remaining Limitation

Phase 27 swaps in HD source art; it is not final high-end VFX. The card faces are
authentic flat scans - a later, feedback-tuned pass could add foil/sheen,
view-dependent shimmer, or an animated reveal on the card itself.
