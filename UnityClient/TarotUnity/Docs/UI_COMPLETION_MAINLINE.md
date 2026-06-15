# Unity UI Completion Mainline

Date: 2026-05-26

## Goal

Take the Unity tarot client from a working desktop prototype to a polished downloadable game interface.

The product promise remains:

```text
Download zip -> unzip -> double-click TarotUnity.exe -> play the tarot reading flow
```

The active gameplay path remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

No history, settings, admin, profile, or backend dashboard work belongs on this mainline until the vertical slice feels complete.

## Final Visual Target

The long-term target is a polished 3D card-game presentation, closer to a premium tabletop card game such as Hearthstone than to a flat UI prototype.

This does not mean Phase 9 must solve final art immediately. It means every upcoming UI and presentation pass should move toward:

- a dimensional card table
- cards with physical weight and readable silhouettes
- staged shuffle, draw, flip, and reveal moments
- warm fantasy tabletop lighting
- UI panels that support the card table instead of looking like generic app panels

Future card-art requirement:

- When the visual pass reaches final 3D card presentation, flipped card faces should use real tarot card artwork instead of placeholder text or generic symbols.
- The asset task should cover the full deck used by the game, including image discovery, license/source review, import sizing, texture compression, and a mapping from backend/local card names to Unity assets.
- Do not start this asset-import task before the Phase 11 screenshot review identifies the final card presentation constraints.

Known current issue:

- The current interface is still visually rough and not final-quality. Treat Phases 8-10 as structural, motion, and release-readiness passes, not the final beauty pass.

## Current Baseline

- Phase 1-6 established structure, graybox flow, presentation pass, backend integration, polish checks, and desktop builds.
- Phase 7 selected and implemented `Immersive Ritual Desktop` as the first non-demo visual direction.
- Phase 8 has added the first visual identity pass for table surfaces, candle accents, card ornamentation, result framing, and a richer palette.
- Phase 9 has added motion and audio rhythm scaffolding for shuffle, draw, flip, and reveal moments.
- Phase 10 has added release UX hardening for unzip-and-run desktop distribution.
- Phase 12 added the approved card-first reveal direction: a durable card face-art slot, ReadingRoom reveal staging, and Result card showcase anchors.
- Phase 13 adds the first real tarot artwork pipeline using the RWS1909 public-domain source family, a Unity `CardArtworkCatalog`, and ReadingRoom/Result scene wiring. It solves the immediate placeholder-card problem, while final 3D card-table presentation remains a later visual phase.
- Phase 18 adds the first true ParticleSystem layer on the existing ritual aura anchors, with ReadingRoom ambient/deck/flip particles and restrained Result card motes.
- Phase 19 connects existing shuffle, deal, flip, and result cues to the Phase18 particle layer through action-triggered VFX integration.
- The Windows release zip currently lives at `Builds/Desktop/Release/TarotUnity-Windows-x64.zip`.

## Mainline To Final UI

### Phase 8: Visual Identity Pass

Status: implemented and verified on 2026-05-26.

Purpose:

- Replace remaining demo-like shapes with a coherent tarot table, card, and reading-frame language.
- Establish reusable visual assets and scene anchors that can be improved without breaking gameplay.

Exit criteria:

- Main menu has a premium ritual table composition.
- Reading room has table cloth, deck focus, and framed controls.
- Result screen has a readable oracle-style layout.
- Card prefab has distinct face/back ornamentation.
- Automated EditMode tests prove the visual anchors and theme palette exist.

### Phase 9: Motion And Audio Rhythm

Status: implemented and verified on 2026-05-26.

Purpose:

- Make the interface feel alive through controlled transitions, card timing, hover response, and sound cues.

Exit criteria:

- Shuffle, draw, flip, and result reveal all have clear timing.
- Button, card, and result transitions do not feel abrupt.
- Audio cues are mapped and safe to run without missing references.
- PlayMode vertical-slice tests still pass.

### Phase 10: Release UX Hardening

Status: implemented and verified on 2026-05-27.

Purpose:

- Make the downloadable package feel reliable for a first public GitHub release.

Exit criteria:

- Windows zip contains a clear executable layout.
- Runtime backend config is easy to edit.
- Offline/backend-error states are readable in-game.
- Build, EditMode, PlayMode, backend tests, zip integrity, and scene reference checks pass.

### Phase 11: Visual Review And Final Adjustment

Status: first screenshot review and layout adjustment pass implemented and verified on 2026-05-28.

Purpose:

- Use actual screenshots and local app runs to catch layout, readability, and first-impression problems that automated tests cannot judge.
- Define concrete visual constraints for the later 3D card-table pass, including where real tarot card faces will appear after flip.

Exit criteria:

- Main Menu, Reading Room, and Result screenshots are reviewed.
- Text fits and is readable at desktop target resolution.
- The interface is less demo-like after the first layout adjustment pass, but it is not yet the final 3D card-game presentation.
- The real-card-art asset task has clear requirements, source/licensing constraints, and import targets before implementation begins.
- Any remaining limitations are documented before release.

### Phase 12: Card-First Reveal

Status: bootstrapper and documentation implemented on 2026-05-28; rerun the Phase 12 bootstrapper in Unity whenever prefab or scene anchors need to be refreshed.

Purpose:

- Move the next UI pass toward approved direction B, where the card reveal is staged first and text interpretation supports that reveal.
- Add a durable `CardView.faceArtworkRenderer` prefab slot so later licensed tarot artwork can be mapped cleanly without changing gameplay wiring.
- Add ReadingRoom and Result anchors for reveal focus, card presence, and future art-driven composition.

Exit criteria:

- `PF_TarotCard` has a centered Phase 12 face-art placeholder assigned to `CardView.faceArtworkRenderer`.
- `ReadingRoom` has non-blocking reveal-stage and instruction anchors while existing controls remain readable and clickable.
- `Result` has card-showcase anchors while preserving the existing result text fields and back button.
- Real tarot artwork import remains deferred until source discovery, license review, texture import settings, and card-name mapping are complete.

### Phase 13: Tarot Artwork Pipeline

Status: implemented and verified on 2026-05-31.

Purpose:

- Replace Phase 12 card-face placeholders with a reusable real-card-art pipeline.
- Stage 78 Rider-Waite-Smith 1909 card images under `Assets/Art/Tarot/RWS1909`.
- Generate `Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset`.
- Resolve backend/local `CardDrawData` to card sprites using suit/number first and English-name fallback.
- Show real card art on dealt cards and in the Result primary-card showcase.

Exit criteria:

- The RWS1909 folder contains 78 imported Sprite assets.
- The default catalog contains 78 card entries.
- `ReadingRoom` and `Result` reference the default catalog.
- EditMode tests prove the artwork pipeline, source documentation, and scene references exist.

Remaining limitation:

- This is the first real-card-art integration pass, not the final Hearthstone-like 3D card table. Later phases should improve card mesh depth, camera staging, lighting, and premium VFX around these mapped sprites.

### Phase 14: Dimensional Card Reveal

Status: implemented and verified on 2026-06-01.

Purpose:

- Move the real card art from a flat sprite reveal toward a physical card-game presentation.
- Add dimensional card prefab anchors for edge, shadow, rim light, glass, and reveal glow.
- Add a null-safe `DimensionalCardRevealController`.
- Add ReadingRoom and Result stage anchors that support card-first reveal without blocking UI.

Exit criteria:

- `PF_TarotCard` contains Phase14 dimensional anchors.
- `CardView` remains safe if the dimensional helper is missing.
- ReadingRoom and Result contain Phase14 visual anchors.
- Phase13 card art catalog remains intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase14 is still a 2.5D pass. A later phase can replace the layered card object with custom 3D meshes, stronger camera motion, and higher-end VFX.

### Phase 15: 3D Table Foundation

Status: implemented on 2026-06-03 and verified on 2026-06-04.

Purpose:

- Move from a 2.5D card reveal layer toward a real 3D tarot table foundation.
- Add a card mesh shell with body, face, back, side edge, and shadow anchors.
- Add ReadingRoom ritual table, depth ring, deck/spread/flip anchors, and warm/cool lights.
- Add restrained Result card-stage support without sacrificing result text readability.

Exit criteria:

- `PF_TarotCard` contains Phase15 3D card shell anchors.
- ReadingRoom contains Phase15 table, depth, deck/spread/flip focus, and light anchors.
- Result contains Phase15 card-stage, result focus, and light anchors.
- Phase13 card art and Phase14 reveal behavior remain intact.
- Targeted Phase15 tests pass; full EditMode/PlayMode verification passed in the end-check flow.

Remaining limitation:

- Phase15 is a foundation pass. Later phases can add custom sculpted card meshes, stronger magical VFX, particle systems, and final camera/audio polish.

### Phase 16: Ritual Aura VFX

Status: implemented and verified on 2026-06-04.

Purpose:

- Add a controlled ritual aura layer around the Phase15 3D tarot table.
- Use glow pool, rune ring, and particle anchors to make ReadingRoom feel more like an active ritual space.
- Add a restrained Result aura support layer without sacrificing text readability.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, and Phase15 3D table foundation intact.

Exit criteria:

- ReadingRoom contains Phase16 aura root, glow, rune, particle, and focus anchors.
- Result contains Phase16 aura root, glow, rune, particle, and focus anchors.
- `RitualAuraController` is null-safe and controls optional aura references.
- Phase16 transparent materials are persistent and test-covered.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase16 is an atmosphere foundation pass. Later phases can add true particle systems, shader graph rune animation, card trails, camera choreography, post-processing, and final VFX/audio polish.

### Phase 17: Ritual Aura Runtime Motion

Status: implemented and verified on 2026-06-05.

Purpose:

- Add lightweight runtime motion to the Phase16 ritual aura layer.
- Rotate rune-ring anchors, pulse glow anchors, and float particle anchors without changing gameplay.
- Keep ReadingRoom and Result more alive while preserving text readability and card art visibility.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, Phase15 3D table foundation, and Phase16 aura anchors intact.

Exit criteria:

- `RitualAuraMotionController` is null-safe and test-covered.
- ReadingRoom wires Phase17 motion on `Phase16_RitualAuraRoot`.
- Result wires Phase17 motion on `Phase16_ResultAuraRoot`.
- Phase16 anchors and materials remain intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase17 is a transform-only runtime motion pass. Later phases can add true particle systems, Shader Graph rune animation, card trails, camera choreography, post-processing, and final VFX/audio polish.

### Phase 18: Ritual Particle Systems

Status: implemented and verified on 2026-06-05.

Purpose:

- Add the first true Unity ParticleSystem layer to the Phase16/17 ritual aura foundation.
- Attach particles to existing Phase16 anchors so Phase17 runtime motion naturally carries them.
- Add ReadingRoom ambient dust, deck focus particles, and flip sparkle particles.
- Add restrained Result card motes and interpretation glow without sacrificing text readability.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, Phase15 3D table foundation, Phase16 aura anchors, and Phase17 motion intact.

Exit criteria:

- `RitualParticleSystemController` is null-safe and test-covered.
- ReadingRoom wires Phase18 particles under `Phase16_RitualAuraRoot` anchors.
- Result wires Phase18 particles under `Phase16_ResultAuraRoot` anchors.
- Phase16 anchors and Phase17 motion wiring remain intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase18 is the first true particle-system layer, not final high-end VFX. Later phases can add card trails, Shader Graph rune animation, camera choreography, post-processing, and screenshot-driven final polish.

### Phase 19: Card Action VFX Integration

Status: implemented and verified on 2026-06-08.

Purpose:

- Connect existing card-action presentation cues to the Phase18 true particle-system layer.
- Let shuffle, deal, flip, and result reveal adjust particle intensity, ambient playback, and reveal bursts.
- Keep `RitualFeedbackController` as the existing gameplay/UI cue entry point while forwarding cues to `RitualActionVfxController`.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, Phase15 3D table foundation, Phase16 aura anchors, Phase17 motion, and Phase18 particles intact.

Exit criteria:

- `RitualActionVfxController` is null-safe and test-covered.
- `RitualFeedbackController` forwards cues without breaking audio or legacy cue particles.
- ReadingRoom wires `ReadingRoomRitualFeedback` to `Phase16_RitualAuraRoot` particle control.
- Result wires `ResultRitualFeedback` to `Phase16_ResultAuraRoot` particle control.
- Phase18 particle systems and prior visual wiring remain intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase19 is action-triggered VFX integration, not final high-end VFX. Later phases can add card trails, Shader Graph rune animation, camera choreography, post-processing, and screenshot-driven final polish.

### Phase 20: Cinematic Rendering

Status: implemented on 2026-06-11.

Purpose:

- Turn on the cinematic rendering pipeline that all prior visual layers were missing.
- Add a shared post-processing volume profile with Bloom, ACES Tonemapping, Color Adjustments, White Balance, and Vignette.
- Enable HDR color grading, MSAA, SMAA, and camera post-processing across MainMenu, ReadingRoom, and Result.
- Boost glow materials into HDR range and add candle/moon/edge emission so bloom responds to the existing ritual layers.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, Phase15 3D table foundation, Phase16 aura anchors, Phase17 motion, Phase18 particles, and Phase19 action VFX intact.

Exit criteria:

- `Assets/Settings/Phase20_CinematicVolumeProfile.asset` exists with the five cinematic components.
- Each gameplay scene contains `Phase20_CinematicVolume` and a post-processing camera.
- PC and Mobile pipeline assets use HDR color grading.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase20 is the rendering-pipeline pass, not final high-end VFX. Later phases can add Cinemachine camera choreography, Shader Graph card materials, card trails, depth-of-field result staging, and screenshot-driven final polish.

### Phase 21: Cinematic Camera

Status: implemented on 2026-06-11.

Purpose:

- Replace the wide-angle surveillance framing (vertical FOV 60, 45-degree top-down) with seated, intimate framing computed from measured scene geometry, following Hearthstone physicality and Inscryption seated-table research.
- Add per-pose FOV blending, idle Perlin breathing, an opening settle-in shot, and a flip punch-in with micro-shake.
- Keep Result framing untouched (UI/card-stage alignment) and add only subtle breathing there and in MainMenu.

Exit criteria:

- ReadingRoom poses aim at their subjects (geometry-verified by tests) with FOV in the 28-40 range.
- Flip triggers `PunchToward` and the camera settles back cleanly.
- Prior visual wiring (Phase 16 aura, Phase 20 volume) remains intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase21 is the camera-feel pass, not final high-end VFX. Later phases can add Shader Graph card materials, card trails, depth-of-field staging, and screenshot-driven final polish.

### Phase 22: UI Composition For The Seated Camera

Status: implemented on 2026-06-12.

Purpose:

- Re-compose the ReadingRoom UI around the Phase 21 seated framing using researched zone rules (Hearthstone "information far, actions near"; HUD zone theory).
- Keep the card stage band (y +95..-115) completely free of UI.
- Move every clickable control into a bottom action tray; move the flip instruction to the top info zone.
- Deactivate flat-era fake-table overlays superseded by the real 3D table and Phase 20 vignette.
- Move the deck stack into the default framing and re-aim DeckPose.

Exit criteria:

- Tray controls do not overlap and stay below the card band (test-enforced).
- Flat-era overlays remain deactivated but not deleted.
- Deck visibility and DeckPose aim are geometry-verified.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase22 is a layout pass, not final high-end VFX or typography. Later phases can add TextMeshPro CJK type, styled tray art, card hover tilt, and Shader Graph card materials.

### Phase 23: Card Feel

Status: implemented on 2026-06-12.

Purpose:

- Add Hearthstone-style card physicality: face-down cards lift and tilt toward the pointer with near-100ms response via `CardHoverTiltController` on the existing EventSystem pointer pipeline.
- Guarantee the hover layer and the flip animation never fight: flip suspends hover and restores the rest pose synchronously.
- Apply research-backed rhythm tuning: punch aftermath 0.32/0.4, flip lift 0.16, deal interval 0.15.

Exit criteria:

- `PF_TarotCard` carries the tuned hover controller.
- PlayMode tests cover lift, tilt, settle-back, and suspend.
- Tuned rhythm values are test-enforced in ReadingRoom.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase23 is the interaction-feel pass, not final high-end VFX. Later phases can add Shader Graph card materials, card trails, TextMeshPro CJK typography, and screenshot-driven final polish.

### Phase 24: Typography

Status: implemented on 2026-06-14.

Purpose:

- Replace the built-in `LegacyRuntime` font (no CJK glyphs; silent OS fallback to PingFang or YaHei or tofu) with a bundled, designed Chinese typeface so the all-Chinese UI reads as a game and ships identically on every machine.
- Chosen face: LXGW WenKai (SIL OFL), an elegant calligraphic Kai that fits the quiet reading-table mood; bundled as a body cut (Regular) and a display cut (Medium).

What changed:

- `Assets/Fonts/` bundles `LXGWWenKai-Regular.ttf`, `LXGWWenKai-Medium.ttf`, and `OFL.txt`, imported as Dynamic fonts with embedded data so arbitrary runtime glyphs (AI interpretation text) rasterize on demand.
- `TarotUiTheme` gained body/display font slots, a shared `ClassifyRole` rule (Display >= size 30, Emphasis = button labels, Body = the rest), and runtime font application; it stays the runtime source of truth.
- `Phase24TypographyBootstrapper` bakes the fonts into every active scene/prefab Text by role and adds a legibility outline to display titles, skipping inactive text so the deactivated fontless card labels are never revived.

Exit criteria:

- The two LXGW WenKai TTFs and the OFL license ship under `Assets/Fonts/`.
- Each UI scene theme references both fonts; every active scene text uses the bundled font for its role; display titles carry an outline.
- Earlier-phase size invariants are preserved; EditMode and PlayMode tests pass.

Remaining limitation:

- Phase24 is the typography pass, not final high-end VFX. A heavier Song serif (Source Han Serif SC) can be swapped in via the two font path references, and a later TextMeshPro/SDF upgrade can add gradient and outlined glyph effects.

### Phase 25: Result Composition

Status: implemented on 2026-06-14.

Purpose:

- Re-compose the Result payoff screen, the weakest screen after Phase 24: gold labels overlapped the card showcase, body text drifted into dead space, and a decorative crest floated over the reading band.
- Apply game-UI composition rules (focal point, rule of thirds, aligned lines, card meaning beside the card, no dead space).

What changed:

- Layout follows the data flow (ResultPanelPresenter shows the drawn card's real artwork left, AI interpretation right): full-width gold question header, card hero in the left third, reading column of aligned gold-header/ivory-body pairs on a backing panel in the right two-thirds, centered footer.
- New `TarotUiAccentText` marker keeps section headers gold through TarotUiTheme's Awake repaint, so runtime matches the screenshot.
- Redundant flat-era overlays (`Phase8_ResultCrest`, `Phase7_ResultOracleFrame`, early Phase 11 columns, Phase 14 text bridge) are deactivated, not deleted; presenter references are untouched.
- Representative placeholder copy makes the static capture show a real reading; the body size stays >= 19.

Exit criteria:

- Each section header stacks above its body without overlap and carries the gold accent marker.
- The card hero sits in the left third, clear of the reading column; redundant overlays are deactivated.
- Presenter text references stay intact; EditMode and PlayMode tests pass.

Remaining limitation:

- Phase25 is a composition pass, not final high-end VFX. Later phases can add Shader Graph foil card materials, view-dependent card sheen, a richer panel background, and result-reveal motion.

### Phase 26: Result Text Robustness

Status: implemented on 2026-06-15.

Purpose:

- Harden the Result reading for variable-length backend AI interpretation. Phase 25 used fixed-height boxes sized for short local placeholder copy; long real copy would overflow and overlap the next section - an invisible production risk, since local-mode text is short.

What changed:

- The four reading body texts use Unity Text best-fit (max 19, min 13): short copy renders at full size, long copy shrinks to fit its box so sections never overlap. fontSize stays at the design size, preserving the OverallText >= 19 invariant.
- A materials-only "moonlit glow" on the card back and deck was attempted first and reverted: screenshot verification showed the card-back emission had no visible effect and the deck blew out into a garish red under ACES tonemapping. Lesson: emissive on flat back planes does not make a tasteful arcane back; that needs pattern geometry, a feedback-gated hero-element change.

Exit criteria:

- The four reading body texts use best-fit within safe bounds; OverallText keeps its size floor; EditMode tests pass.

Remaining limitation:

- Phase26 is a robustness floor, not final high-end VFX. A scrollable reading panel is the eventual ideal for very long readings, and card/back visual "wow" remains for a feedback-tuned pass.

### Phase 27: HD Card Artwork

Status: implemented on 2026-06-15.

Purpose:

- Phase 13 wired authentic public-domain RWS1909 card faces, but only low-resolution scans (400x666) imported because Wikimedia rate-limited the batch, so faces looked soft. Re-source the full deck in HD and wire it in so a flipped card and the result hero render crisply.

What changed:

- All 78 faces were re-downloaded from Wikimedia Commons (public domain RWS1909, ~1100x1920 full-card), downscaled to a consistent HD height of 1280, and saved under the existing filenames in a new folder `Assets/Art/Tarot/RWS1909_HD/` with a sources manifest.
- `Phase27HdArtworkBootstrapper` imports the folder as Sprites at maxTextureSize 2048 and rebuilds the catalog to point at the HD sprites, re-assigning it to the ReadingRoom DeckController and the Result ResultPanelPresenter. Card-key matching is unchanged.
- The old low-res folder is left untouched for the user to delete (no overwrite/remove).

Exit criteria:

- The catalog resolves all 78 cards, each to an HD sprite (height >= 1000) from the HD folder; HD textures import as Sprites; EditMode tests pass.

Remaining limitation:

- Phase27 swaps in HD source art, not final high-end VFX. Foil/sheen, view-dependent shimmer, or an animated card reveal remain for a feedback-tuned pass.

### Phase 28: Holographic Card Face

Status: implemented on 2026-06-15.

Purpose:

- Make the flipped card face read as a glossy, dimensional object. The cards were already a tilting 3D mesh (Phase 15/23); this adds a holographic foil sheen that sweeps as the card tilts, like a premium foil trading card.

What changed:

- New URP sprite shader `TarotUnity/HolographicCard`: samples the card art and adds a diagonal glare band plus subtle iridescence, positioned by the object-space view direction so it sweeps automatically on tilt (Phase 23) and camera breathing (Phase 21). Sheen is masked by the card alpha. Declares `Fallback "Sprites/Default"` so art never breaks.
- `MAT_HolographicCardFace` built from it with a tuned, tasteful glare (intensity 0.5, narrow band) that shines without washing out the art.
- `Phase28HolographicCardBootstrapper` assigns the material to the card prefab's face SpriteRenderer - a single reversible swap. The sheen shows only on a face-up card.

Exit criteria:

- The shader compiles and is supported; the material uses it with a tuned glare; the card face renderer uses the material; EditMode tests pass.

Remaining limitation:

- The sweep is a view-angle effect best judged live (hover to tilt). Intensity/width/iridescence are single material properties, easy to tune from feedback. The Result hero card (a UI image) is not yet holographic.

## Operating Rule

Every phase ends with the established end-check flow:

- Unity error and log scan
- EditMode / PlayMode / custom tests
- code and asset reference review
- vertical-slice verification
- build or zip verification when the phase touches release output
