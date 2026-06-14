# Phase 24 Typography

Date: 2026-06-14

## Purpose

Phases 20-23 fixed rendering, camera, composition, and card feel. The biggest
remaining thing dragging the game down to "prototype" was the text itself.

Every visible string in the game is Chinese (86 CJK literals; titles, buttons,
instructions, and the AI interpretation on the result screen). None of it had a
real font. Unity's built-in `LegacyRuntime.ttf` has no CJK glyphs, so each
Chinese character was silently drawn by whatever the operating system happened
to ship: PingFang on the macOS editor, Microsoft YaHei - or tofu boxes - on a
shipped Windows build. That reads as a system dialog, not a game, and it looks
different on every machine.

## Decision

The font is bundled, not borrowed from the OS, so it renders identically
everywhere and survives shipping.

Typography research for tarot is consistent: serif and calligraphic faces carry
the "mystical, spiritual, classical" register, while flat system sans reads as
generic and modern. The chosen face is LXGW WenKai (霞鹜文楷), an elegant
calligraphic Kai derived from Fontworks' Klee, released under SIL OFL so it is
free to bundle in a commercial game. Its hand-inked warmth fits the quiet,
intimate "hand your question to the cards" mood of the reading table.

Two cuts are bundled to give a real type hierarchy:

- LXGWWenKai-Regular.ttf - body text (instructions, captions, interpretation).
- LXGWWenKai-Medium.ttf - display titles and button labels.

Why not a full TextMeshPro migration: the result screen prints arbitrary
AI-generated Chinese at runtime, so a TMP static glyph atlas is impossible and a
full migration of 19 phases of UI.Text would be a high-risk rewrite. A bundled
dynamic font rasterizes any glyph on demand via FreeType, ships consistently,
and is fully verifiable through the existing screenshot pipeline. SDF/TMP can
come later, per screen, once this direction is approved.

### Swapping the typeface

The font is referenced in exactly two places (the `TarotUiTheme` serialized
slots and `Phase24TypographyBootstrapper` constants). To move to a heavier Song
serif - for example Source Han Serif SC / 思源宋体 (also SIL OFL), which gives a
more formal, premium grimoire weight with true Bold/Heavy cuts for titles - drop
the OTFs into `Assets/Fonts/`, repoint the two path constants, and re-run the
bootstrap.

## What Changed

### Bundled fonts

`Assets/Fonts/` now carries `LXGWWenKai-Regular.ttf`, `LXGWWenKai-Medium.ttf`,
and `OFL.txt`. Both faces import as Dynamic fonts with embedded font data and
HintedSmooth rendering, so any runtime glyph stays crisp and ships in the build.

### Central font system

`TarotUiTheme` gained `bodyFont` / `displayFont` slots, a `displaySizeThreshold`
(30), a shared `ClassifyRole` rule, and runtime font application. Roles:

- Display - font size >= 30. Mystical titles. Gets the Medium cut plus a soft
  dark outline so gold titles stay legible over candle-lit backgrounds.
- Emphasis - button labels. Gets the Medium cut.
- Body - everything else. Gets the Regular cut.

`TarotUiTheme` stays the runtime source of truth: its `Apply()` runs on Awake
and also restyles any text that is toggled on later (e.g. the error warning).

### Baking

`Phase24TypographyBootstrapper` bakes the fonts into every active `Text` across
MainMenu, ReadingRoom, and Result, plus the UI prefabs, by role. Baking is
required because the screenshot pipeline renders in edit mode (no Play Mode), so
the serialized assets themselves must carry the font. Inactive text is skipped
so the deliberately deactivated, fontless graybox card labels from Phase 21 are
never touched or revived.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 24 Typography Bootstrap`.

## Exit Criteria

- The two LXGW WenKai TTFs and the OFL license are bundled under `Assets/Fonts/`.
- Each UI scene's `TarotUiTheme` references both bundled fonts.
- Every active scene text uses the bundled font that matches its role.
- Display titles carry a legibility outline.
- The deactivated fontless card labels stay deactivated.
- Title and body size invariants from earlier phases are preserved.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 24 is the typography pass; it is not final high-end VFX. Later phases can
still add Shader Graph foil card materials, view-dependent sheen, motion trails,
a TextMeshPro/SDF upgrade for outlined and gradient text, and result-screen
composition polish.
