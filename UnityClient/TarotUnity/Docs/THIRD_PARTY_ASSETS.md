# Third-Party Art Assets

All external art in this project is public domain or CC0. This file records
provenance so any future release checklist can verify licensing at a glance.

## ambientCG PBR materials (CC0 1.0)

Source: https://ambientcg.com — all ambientCG assets are published under
Creative Commons CC0 1.0 Universal (no attribution required; attribution given
here anyway).

- `Assets/Art/MidnightParlor/Textures/Fabric034_1K-JPG_*` — "Fabric 034" felt
  (color / NormalGL / roughness, 1K). Used as the velvet table cloth surface.
- `Assets/Art/MidnightParlor/Textures/Wood051_1K-JPG_*` — "Wood 051" espresso
  furniture wood (color / NormalGL / roughness, 1K). Used as the table rim.

## Rider-Waite-Smith 1909 card scans (public domain)

- `Assets/Art/Tarot/RWS1909_HD/` — 78 full-card scans from Wikimedia Commons
  (Pamela Colman Smith, 1909; public domain). Wired in Phase 13/27.

## Locally generated Midnight Parlor UI kit (project-original)

- `Assets/Art/MidnightParlor/Sprites/Tarot*.png` — card back, nine-slice
  panels/button, medallion, parchment, divider, socket decal, radial glow.
  Composed in-house with Python/PIL; generators live in `Tools/UiKitGenerator/`
  so every sprite is reproducible. No third-party imagery embedded.

## Fonts and audio (recorded elsewhere, unchanged)

- LXGW WenKai (SIL OFL 1.1) — Phase 24, see `Docs/PHASE24_TYPOGRAPHY.md`.
- Phase 33 SFX/music — synthesized in-house, see `Docs/PHASE33_AMBIENT_AUDIO.md`.

## Evaluated and not imported

- Kenney "Fantasy UI Borders" (CC0) — downloaded for evaluation during Phase 37
  research; its pixel-art style clashed with the 1909 engraving aesthetic, so
  nothing from the pack entered the project.
