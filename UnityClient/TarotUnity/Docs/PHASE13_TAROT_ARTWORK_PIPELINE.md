# Phase 13: Tarot Artwork Pipeline

Date: 2026-05-31

## Goal

Phase 13 replaces the Phase 12 card-face placeholder with a real tarot artwork pipeline for the current vertical slice.

This phase does not attempt the final Hearthstone-like 3D table. It solves the immediate visual problem that flipped cards still feel like placeholders by making real card art available to:

- `ReadingRoom` card flips
- `Result` primary-card showcase
- future 3D card presentation work

## Deck Source

Deck id:

```text
RWS1909
```

Canonical source family:

- Wikimedia Commons: `Rider-Waite tarot deck (Roses & Lilies)`
- Source category: <https://commons.wikimedia.org/wiki/Category:Rider-Waite_tarot_deck_(Roses_%26_Lilies)>

The Commons category states that it contains images related to the original `Roses & Lilies` edition of the Rider-Waite-Smith tarot deck published by the Rider Company in 1909.

The root `Rider-Waite tarot deck` Commons category notes that the original Rider-Waite tarot deck is public domain in both the United States and the United Kingdom, while warning that later colorized versions can remain copyrighted.

Individual `RWS1909` Commons file pages, such as `RWS1909 - 00 Fool.jpeg`, mark the artwork with the Creative Commons `Public Domain Mark` and state that the work is public domain in the United States because it was published before January 1, 1931.

## Local Asset Staging

Unity assets are staged under:

```text
Assets/Art/Tarot/RWS1909/
```

The staged images were copied from the existing legacy web frontend cache:

```text
/Users/maochuandou/BUPT/Game/Tarot/public/images/tarot-cards/
```

This avoids repeated network pulls during development after Wikimedia Commons returned rate-limit responses during direct batch download. The project keeps `rws1909_sources.json` beside the imported art so each Unity card key remains traceable to the public-domain `RWS1909` source family.

## Runtime Mapping

The default runtime catalog lives at:

```text
Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset
```

The stable card keys are:

- Major Arcana: `major_00` through `major_21`
- Minor Arcana: `cups_01` through `cups_14`
- Minor Arcana: `pentacles_01` through `pentacles_14`
- Minor Arcana: `swords_01` through `swords_14`
- Minor Arcana: `wands_01` through `wands_14`

`CardArtworkCatalog` resolves sprites from backend/local `CardDrawData` using suit and number first, then English name fallback.

## Import Settings

Phase 13 bootstrap configures each staged card image as:

- Texture Type: Sprite
- Sprite Mode: Single
- Max Size: 1024
- Mip Maps: disabled
- sRGB: enabled

This keeps the first desktop prototype lightweight while preserving enough detail for the current card-face reveal.

## Scene Wiring

The Phase 13 bootstrapper assigns the default catalog to:

- `DeckController.artworkCatalog` in `ReadingRoom`
- `ResultPanelPresenter.cardArtworkCatalog` in `Result`
- `ResultPanelPresenter.resultCardArtworkSlot` using `ResultCanvas/Phase12_ResultCardArtworkSlot`

## Out Of Scope

Phase 13 does not:

- model final 3D card meshes
- add card foil, glow, or particle VFX
- redesign the full table composition
- import commercial recolored Rider-Waite variants
- change backend card draw logic

Those belong to later visual phases after the card art mapping is stable.
