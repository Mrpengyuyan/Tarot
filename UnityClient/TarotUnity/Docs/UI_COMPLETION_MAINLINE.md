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

### Phase 29: Result Reading Scroll Panel

Status: implemented on 2026-06-15.

Purpose:

- Deliver the scrollable reading panel Phase 26 named as the eventual ideal. Phase 26 contained variable-length backend AI text by shrinking it (best-fit); Phase 29 instead renders every section at full size and scrolls when the reading exceeds the viewport, so long copy stays readable instead of shrinking.

What changed:

- The four header/body pairs are reparented, in reading order, into a new `ResultReadingScroll` (vertical `ScrollRect`, `Clamped`, 736x448, centered on the Phase 25 column). A `Viewport` with a `RectMask2D` clips overflow; a `Content` with `VerticalLayoutGroup` + `ContentSizeFitter` (vertical = PreferredSize) grows with the text. The 708px body column keeps the Phase 8/12 reading-width invariants.
- Best-fit is turned off on the four bodies (Phase 26's mechanism is superseded); the redundant `Phase8_ResultScrollPanel` is deactivated, not deleted.
- A few older Result tests (Phase 8/12/25/26) were updated to match the new nested, layout-driven structure while preserving their original intent (recursive find + resolved `rect.width`, world-space geometry, and a scroll-clipping guarantee in place of the best-fit assertion).

Exit criteria:

- `ResultReadingScroll` has a vertical `ScrollRect` wired to a `RectMask2D` viewport and a `VerticalLayoutGroup` + `ContentSizeFitter` Content; all eight sections are children of Content in reading order; bodies render full size; long copy makes Content taller than the viewport; the reading width still clears >= 700 / >= 660; EditMode tests pass.

Remaining limitation:

- Phase 29 is the layout/robustness completion for long readings, not final high-end VFX. A styled scrollbar handle, scroll-edge fades, and a richer card-back reveal remain for a feedback-tuned pass.

### Phase 30: Cross-Screen Secondary Spacing/Alignment

Status: implemented on 2026-06-15.

Purpose:

- Unify the secondary spacing/alignment shared by MainMenu, ReadingRoom, and Result. Composed independently across many phases, they needed an objective audit before any change.

What changed:

- A read-only layout audit (`Phase30LayoutAuditBuilder`) showed the screens are already strongly unified: identical CanvasScaler (1280x720, match 0.5), symmetric centering of every centered heading/footer, and cleanly abutting heading/sub-label pairs. So this phase codifies and guards that consistency instead of moving visually-fine layouts (which would risk the build-blind visual-collapse failure mode).
- New `TarotUiSpacing` source-of-truth constants (reference resolution, match factor, centering tolerance, max heading/sub-label overlap) and new `Phase30CrossScreenConsistencyTests` regression guards. No scene assets were modified.
- The one notable difference - primary-button heights (~32/31/28) - is left for a feedback-tuned visual pass because unifying it risks detaching gold trim / colliding with the reading scroll.

Exit criteria:

- All three core canvases share the 1280x720 / match-0.5 CanvasScaler; centered headings/footers are horizontally symmetric; heading/sub-label pairs do not overlap; EditMode tests pass.

Remaining limitation:

- Phase 30 is an objective consistency guard, not final high-end VFX. The primary-button height unification and any deliberate spacing refresh remain for a feedback-tuned visual pass.

### Phase 31: HD Visual Archive

Status: implemented on 2026-06-15.

Purpose:

- Produce a complete high-resolution archive of the current game UI so the whole look can be reviewed in one place.

What changed:

- `Phase31VisualArchiveBuilder` (read-only editor tool) renders the faithful screens at 2560x1440 into `Docs/VisualReview/Phase31_HDArchive/`: MainMenu, ReadingRoom (spread select), and the Result default and long-reading states, plus a manifest. `Phase31VisualArchiveTests` guards that the artifacts exist.
- The face-up card reveal is intentionally excluded: staging cards in edit mode surfaced a card-face sizing issue (Phase 27 HD sprites import at PPU 100, larger than the face slot was tuned for, so faces overflow the card). That is the hero prefab and should be judged/fixed live with feedback, not approximated blind. Flagged in the Phase 31 doc and below.

Exit criteria:

- The four archive PNGs and manifest are written at 2560x1440; EditMode tests confirm the artifacts exist.

Remaining limitation:

- Phase 31 is a review archive, not final high-end VFX. The card-face sizing fix (normalize HD sprite PPU to the card footprint, or fit in SetFaceArtwork) and the face-up reveal capture remain for a feedback-tuned follow-up.

### Phase 32: Card Face Fit (Oversized Flip Art Bugfix)

Status: implemented on 2026-06-15.

Purpose:

- Fix the bug Phase 31 flagged: after the Phase 27 HD swap, flipping a card showed the face art wildly oversized and overlapping.

What changed:

- Root cause (diagnosed, not guessed): SetFaceArtwork assigned the sprite without sizing it, and HD sprites import at PPU 100, so the face rendered ~3.4x too wide / 7.7x too long vs the card body. Once correctly sized, a second issue showed: the face sat ~0.003 above the opaque cream front planes and was occluded at grazing angles.
- `CardView.FitFaceArtwork` scales the face renderer (preserving aspect) to a target world footprint regardless of sprite resolution/PPU; invoked from SetFaceArtwork and SetFaceUp to cover both deal-then-flip and flip-then-art orders. `Phase32CardFaceFitBootstrapper` sets the prefab footprint and lifts the face renderer clear of the front planes. The Phase 28 holographic sheen is preserved.

Exit criteria:

- `Phase32CardFaceFitTests` confirms the face fits the card footprint (was 2.65 vs 0.78), fills a reasonable portion, and clears the front plane; the Phase 32 capture shows correctly sized, crisp art on the card; EditMode 182/182 and PlayMode 29/29 green.

Remaining limitation:

- Phase 32 fixes correctness (size + visibility), not final high-end VFX. The footprint/lift are tunable, and a richer reveal (animated flip-in, stronger foil) remains for a feedback-tuned pass.

### Phase 33: Ambient Audio

Status: implemented on 2026-06-16.

Purpose:

- The audio system was fully wired (AudioManager + seven presentation cues triggered during play) but silent: cueMap was empty, so cues fell back to single-sine beeps and there was no music. Give the game a real warm "Moonlit Tarot" sound.

What changed:

- Seven original royalty-free synthesized SFX (`Assets/Audio/SFX/`, one per cue) plus a 24s loopable ambient music bed (`Assets/Audio/Music/`), generated from scratch (pure-Python synthesis) so there is no third-party license to track. AudioManager gained an `ambientMusic` clip + `musicVolume` and auto-plays the bed on Awake.
- `Phase33AudioBootstrapper` imports the WAVs (SFX decompress-on-load PCM, music streamed Vorbis) and assigns the seven cue clips (balanced volumes) + ambient bed on the persistent Boot AudioManager. The procedural beep fallback stays enabled as a safety net.

Exit criteria:

- The eight WAVs import as valid AudioClips; the Boot AudioManager binds all seven cues to non-null clips with sane volumes and an ambient music bed; EditMode and PlayMode tests pass.

Remaining limitation:

- Phase 33 is a cohesive baseline sound, not final high-end VFX or a composed soundtrack. Timbres, mix balance, and per-scene music behaviour are taste calls best tuned from a live listen; all volumes and synthesis parameters are easy to adjust (`Docs/Audio/gen_tarot_audio.py`).

### Phase 34: Main-Menu Quit Button

Status: implemented on 2026-06-16.

Purpose:

- Desktop builds had no in-game exit (only OS Cmd+Q / Alt+F4). Add a quit affordance to the main menu - a small objective UX gap fix.

What changed:

- MainMenuController gained a `quitButton` (wired like the start button: runtime AddListener) and a `QuitGame` handler (`Application.Quit`, or stop play mode in editor).
- `Phase34QuitButtonBootstrapper` clones StartReadingButton for identical styling (Phase 30 consistency), relabels it `退出占卜`, places it centered below the start plate + status line, and wires it to the controller. Idempotent.

Exit criteria:

- `Phase34QuitButtonTests`: QuitButton exists, centered, below the start plate without overlap, labeled 退出, wired to the controller which has QuitGame; EditMode/PlayMode green.

Remaining limitation:

- Functional UX fix, not final high-end VFX. The quit button mirrors the primary button's prominence for consistency; de-emphasizing it or adding a confirm step are easy taste tweaks.

### Phase 35: Result Overlay and Menu Polish

Status: implemented on 2026-07-10.

Purpose:

- A fresh visual review of the HD archive surfaced three objective defects: the pre-Phase-25 3D result card stage peeking out from behind the reading panel, the flat-era ResultReadingFrame darkening the hero card slot with a visible seam, and the main-menu status line sitting on the start plate while the quit button competed with the primary action.

What changed:

- The five result stage visuals (pedestal, glow pool, rune ring, two anchor markers) have their MeshRenderers disabled while the GameObjects stay active for the Phase 15-21 Find-based tests and live particle anchors.
- ResultReadingFrame is deactivated (not deleted), matching the Phase 25 convention for redundant flat-era overlays.
- StatusText moved below the start plate; QuitButton became a quiet secondary action (trim clone off, 200x40 plate, 16pt muted label matching the runtime TarotUiTheme rule).

Exit criteria:

- `Phase35UiPolishTests`: stage renderers disabled with objects active, ResultReadingFrame off with the scroll on, status line clears the plate, quit strictly smaller/quieter with start's trim untouched; regenerated HD archive confirms the artifact and seam are gone; EditMode/PlayMode green.

Remaining limitation:

- Defect repair, not final high-end VFX. A faint plate or hover glow on the quit link, and repositioning the hidden stage as a live aura behind the hero card, are feedback-gated follow-ups.

### Phase 36: Performance Pass

Status: implemented on 2026-07-10.

Purpose:

- The framework review surfaced two objective performance items: the card-flip path ran whole-scene scans on every click, and the project had never been measured.

What changed:

- CardFlipController caches the camera choreography and ritual feedback references in lazy self-healing accessors instead of calling FindFirstObjectByType per flip.
- Phase36PerformanceProbeTests (PlayMode) drives the real three-card slice and logs frame times plus per-frame managed allocations per scene state; static audit confirmed all three per-frame loops are allocation-free.

Exit criteria:

- Probe baseline recorded in the Phase 36 doc (steady state 0 B/frame allocations, ~0.1 ms main-thread frames in batch; one 120 ms Result scene-load first frame); Phase36PerformancePassTests guard the caching; EditMode/PlayMode green; macOS player build succeeds.

Remaining limitation:

- CPU/GC only - real GPU frame time needs a played round with the Metal HUD (macOS) or an FPS overlay (Windows); workflow documented in the Phase 36 doc.

### Phase 37: Midnight Parlor Blueprint + Asset Foundation

- Research pass against the Hearthstone benchmark (GDC immersive-UI method: one
  physical seed, skeuomorphic tactility, juice everywhere, the card as hero)
  produced `Docs/PHASE37_VISUAL_REDESIGN_BLUEPRINT.md` - the approved teardown
  of the visual composition layer while flow/tests/audio/camera stay.
- CC0 PBR surfaces (ambientCG felt + espresso wood) and a locally composed gold
  UI kit (celestial card back, nine-slice panels/button, medallion, parchment,
  divider, socket, glow; generators in `Tools/UiKitGenerator/`) imported with
  baked importer settings and URP materials. Provenance in
  `Docs/THIRD_PARTY_ASSETS.md`; Phase37AssetFoundationTests guard it.

### Phase 38: ReadingRoom Table Rebuild

- One Midnight Parlor stage (velvet cloth, walnut rim, parlor backdrop, four
  gold card sockets, staggered deck stack) replaces the Graybox/Phase8/12/14/15
  flat-plane palimpsest; the card prefab wears the composed celestial back and
  drops the primitive ornaments and always-on 2.5D dressing.
- Superseded objects were deleted (listed in `Docs/PHASE38_TABLE_REBUILD.md`)
  and the era tests now guard the deletion; five capture iterations reviewed at
  2560x1440 via Phase38CaptureBuilder; Phase38TableRebuildTests guard the stage.

### Phase 39: ReadingRoom UI Reskin

- HUD plate, progress plates, action dock, question input, and the four flow
  buttons wear the Phase 37 nine-slice gold plaques; button ColorBlocks reset
  to white/gold-hover/dark-press so the sprite tint survives runtime.
- Redundant flat-era frames (Phase8 spread/question frames, Phase11 question
  glow) deactivated per the Phase 22/25 convention; `TarotButton.png`
  regenerated with a heavier frame after capture review;
  Phase39UiReskinTests guard the skin.

### Phase 40: MainMenu + Result Recomposition

- MainMenu: Midnight Parlor stage with a candlelit tabletop vignette (deck
  stack, scattered cards, candle glow quads); the brown disc and green slab
  deleted; start button wears the plaque, quit stays a quiet gold-line link;
  Phase 11 depth fakes deactivated and washes lightened so the velvet reads.
- Result: velvet + backdrop behind the reading (no rim - it striped the UI);
  hero showcase and reading scroll in gold frames, diamond dividers, radial
  candle glow replacing the amber slab. Phase40MenuResultTests guard it all.

### Phase 41: Redesign Close-Out

- HD archive regenerated on the Midnight Parlor look (all four states); 14
  orphaned legacy materials deleted after GUID reference verification, with
  survivors (MoonGold/DeckStack/SpreadSlot/RevealGlow) asserted; era tests
  updated; Phase41FinalPolishTests guard the sweep; fresh macOS build for the
  user playthrough.

### Phase 42: Sharpness + Menu Elevation

- Blurry built text traced (by elimination - two wrong hypotheses recorded in
  the doc) to presentation resolution: a fixed 1280x720 non-resizable window
  stretched to fullscreen. Player now opens FullScreenWindow at native res.
- Menu restaged as a lit room: skybox ambient (a daylight sky over a midnight
  parlor) replaced by a near-black flat floor + one unseen warm fill, candles
  rebuilt with emissive wax at intensity 4.2, camera seated at 27deg/FOV 36
  (was 10.8deg/FOV 60), parlor haze backdrop, deep-oxblood velvet and dark
  walnut. Shared-material change verified to improve ReadingRoom, not regress it.
- Phase42MenuElevationTests guard display settings, lighting contract, camera,
  fill, retired washes, and palette floors.

### Phase 43: TMP SDF Text

- Text rendering method replaced: legacy bitmap Text + 4-copy Outline -> TMP SDF
  with dynamic atlas population (Phase 24's "static atlas can't hold runtime AI
  Chinese" objection is obsolete). Fallback chain added - LXGW has no dingbats
  and the title's stars tofu'd once the OS fallback went away.
- MainMenu migrated (no InputField = clean bridgehead); ReadingRoom/Result stay
  on legacy Text until the TMP_InputField swap. TarotUiTheme drives both.
- Title gilded with a shader gradient + stroke.
- Found: Phase 6's build setup hardcoded 1280x720/Windowed and reverted Phase
  42's sharpness fix on every build - the shipped build never had it.

### Phase 44: Menu Atmosphere

- Fixed the loose-card clipping (both cards were coplanar at localY 0.01).
- Copy rewritten as ritual: 你尚未开口，牌已听见 / 入席问牌 / 离席. Status copy
  must be set in the scene as well as the runtime string.
- One way in: invitation owns the centre, status drops to the foot, quit becomes
  a bare bottom-right link (the Phase 34 function stays - only its place was
  wrong for a main menu).
- Table dressed: arcane circle decal (kept faint - at full strength it out-shouted
  the cards), dust motes, coins and a ring. Phase 7/34/40 tests updated to guard
  the new intent.

### Phase 45: Menu Depth

- Mid-ground band (z 1.5-2.6) filled: drapery folds in the backdrop, scrying orb
  lit from within in cool violet, censer with coals + smoke, two back candles at
  a second depth. All kept dimmer than the cards.
- Type bound into a block: gold rule between title and subtitle, tracked display
  cut, status line receded to a footnote.
- Prop lights now live on children and the rule is asserted: a Light on a prop
  root shares its Transform, and writing light.transform.localPosition dragged
  props to the origin twice (Phase 42's candles, Phase 45's orb/censer).

### Phase 46: Candle Craft

- Candles were perfect cylinders in one flat cream with one flat emission over
  the whole body = plastic tubing. Now: WaxColor (poured grain, drips with
  contact shadow, soot at the rim, grubby base) + WaxEmission (translucency,
  brightest at the flame). The translucency map needs a floor (0.24) - falling
  to black left the bodies unlit, since a candle's flame grazes its own sides.
- CandleFlickerController: two layered Perlin bands drive intensity + flame
  stretch, seeded per candle (unison flicker is its own kind of wrong),
  allocation-free per frame. Only visible in Play/build, not in stills.

### Phase 47: Orb, Button, Deck

- The orb was a matte Lit sphere = a plastic marble. Sourcing a crystal-ball
  asset would not have fixed it: the geometry is just a sphere, the problem was
  shading. TarotScryingOrb.shader adds Fresnel rim (the tell of glass), a
  parallax-sampled equirect interior (nebula behind the surface, not on it), and
  tight per-light speculars. The halo quad that was faking an edge is retired.
- TarotButton regenerated with an inner shadow and a lit/shadowed bevel.
- MP_DeckBody lifted from near-black to aged paper so the stack can be counted.
- A card fan and wax spills were added and removed in the same phase (the fan
  stood inside the front-left candle and repeated the deck; the spills doubled
  the existing WaxPool). The table is full - further elements go in the backdrop.

### Phase 48: The Backdrop's Ceiling

- Measured, not guessed: at y=2.7 / 27deg pitch / 36 FOV, the top of frame is 9deg
  below horizontal, so the highest visible world Y at the backdrop's depth (z=11)
  is 0.34. There is no wall in this shot - the "upper third" is far tabletop plus
  the bottom ~29% of the backdrop. Lamp/window/picture cannot exist here.
- Exposed that Phase 45's drapery was never on screen (folds faded out above the
  ceiling). Backdrop regenerated with folds in the visible band.
- The moonlight cookie rig was attempted and dropped: A/B capture proved it
  rendered but contributed ~3% brightness, and its intensity response stopped
  being explicable. Not shipped rather than shipped unverified.
- Phase48BackdropDiagnostic kept: measure that band before staging in it.

### Phase 49: ReadingRoom Light

- Same defect as the menu pre-Phase-42: skybox ambient (daylight sky over a
  midnight room) = flat, no pools, grey rim bar. Fixed with the same contract.
- Worse defect the menu never had: nothing in the room emitted light - seven
  point lights lit the table from nowhere. Four candles now stand in it (menu
  rig, wax, seeded flicker) plus the unseen warm fill. Phase 12-15 lights
  dimmed, not deleted: later phases reference them by name.
- Candle placement is measured against the live camera: the first pass stood the
  front pair exactly on the frame's cut (x=+-3.34 at the card row's depth) and
  the back pair inside the step tracker. Phase49ReadingRoomLightTests projects
  every candle so it cannot regress.

### Phase 50: ReadingRoom Text -> TMP SDF

- Finishes the migration Phase 43 started on the menu. 15 legacy UI.Text (bitmap
  atlas at one size, softens when the canvas scales; outline = four offset mesh
  copies) all convert to TextMeshPro SDF in place, sharp at any resolution.
- The piece the menu never had: the question field. A legacy InputField and a
  TMP_InputField are different components with different child layouts, so the
  field is rebuilt from TMP_DefaultControls (correct masked Text Area) and
  reseated into the old field's RectTransform/background; the old GO is deleted.
  Caret switched from near-black (invisible on the dark ground) to ivory; the
  placeholder reads as an italic muted prompt.
- Source, not just scene: ReadingRoomController's four fields change type
  (InputField -> TMP_InputField, three Text -> TMP_Text; same .text API), and
  TarotUiTheme gains ApplyTmpInputStyle. Four EditMode tests plus two flipped
  legacy tests (Phase7/Phase10 now assert the TMP path) and two PlayMode slice
  tests re-typed to TMP_InputField. EditMode 254/254, PlayMode 30/30. Verified by
  scene capture (dynamic SDF resolves the CJK glyphs).
- Result is the last legacy holdout: 13 legacy Text + the same skybox ambient
  defect - the natural next cut.

### Phase 51: Result Text -> TMP SDF (the migration completes)

- The last screen on legacy UI.Text. 13 components convert with the shared
  Text -> TMP routine; the authored colour carries over, so gold section headers
  stay gold and ivory bodies stay ivory. ResultPanelPresenter's seven readout
  fields (four carrying arbitrary-length backend AI copy) retype Text -> TMP_Text;
  the dynamic SDF atlas resolves whatever Chinese the backend returns. The Phase 29
  scroll still works - TMP_Text is a layout element.
- Ambient: adopts the Flat contract like the other two, but **stated honestly as
  correctness, not a visible change**. This canvas is Screen-Space Overlay over a
  solid near-black camera clear, so the daylight-skybox ambient it shipped with lit
  only off-frame 3D dressing. Before/after captures are byte-identical outside the
  text; the value is that all three scenes' render settings finally match and a
  midnight product no longer carries daylight ambient in its data. The four tuned
  lights are left alone.
- Ten legacy Result tests flipped to guard the TMP state (helpers return TMP_Text;
  alignment reads TextAnchor -> TextAlignmentOptions; best-fit -> enableAutoSizing;
  vertical/horizontal overflow -> overflowMode; the legacy line-spacing floors,
  which have no faithful TMP value and are not guarded on the other screens, become
  the size-floor + no-truncation reading-comfort guarantees). EditMode 259/259,
  PlayMode 30/30.
- **All three screens are TMP SDF now. No legacy UI.Text or InputField remains in
  any shipping screen.**

### Phase 52: Flip weight + a reveal-synced camera

- Game-feel pass on the card flip and the camera's reaction. No new mechanics, no
  change to the pacing floors Phase 9/23 own (duration, lift, the beats) - this is
  the curves and the secondary motion between them.
- The flip was linear to 90 and back with a static "anticipation" pause and an
  ease that crawled at the edge-on seam - so the reveal, the moment that should
  snap, was the slowest part, and the card arrived weightlessly. It now has five
  beats: wind-up (dip + wind-back so it launches cocked), whip to edge-on
  (accelerate through the reveal), reveal (face swap + cue), swing-in with a scale
  pop (+6%, fading) and a small rotational overshoot, then a damped settle to an
  *exact* rest.
- Camera: the lean-in shake fired at the lean-in peak (~0.26 s), before the face
  actually turned over (~0.49 s). The flip now fires the impact Kick on the exact
  reveal frame, so the punctuation lands with the face. Breathing and the
  lean-in/return curves were already right and are left alone.
- Every new motion is a serialized knob on CardFlipController; the old flipCurve
  (now superseded by the two-phase accelerate/decelerate model) was removed.
  Phase52FlipFeelTests keeps the knobs in a tasteful envelope; a PlayMode test
  drives a real flip and proves the dip/lift/pop are present and that it settles
  to a sub-millimetre exact rest (a flipped card never drifts from where it was
  dealt). EditMode 261/261, PlayMode 31/31.
- Honest note: a test can prove the motion, not that it *feels* good - that is the
  player's to judge with the card under the cursor. The values are a principled
  starting point, one edit away from preference.

### Phase 53: Holographic foil on the Result hero card

- Phase 28 gave the flipped 3D face a foil driven by its real view angle. The
  Result hero card - the drawn card the player admires - never got it because it
  is a UI Image on a Screen-Space Overlay canvas with no view angle for that
  shader to read.
- New UI shader TarotUnity/HolographicCardUI: the same diagonal glare + iridescence
  as Phase 28, but on the standard UI template (honours clip rect + stencil) and
  positioned by a fed-in _Sheen uniform - _Sheen.x sweeps the band, _Sheen.y shifts
  the hue. Both cards shimmer in one material language.
- HolographicHeroCard drives it: a slow figure-of-eight idle drift so the foil is
  always subtly alive, and on hover the band follows the pointer while the card
  tilts a few degrees toward it (X/Y RectTransform rotation reads as a lean on an
  Overlay canvas). Material is instanced at runtime so the per-frame sheen never
  dirties the asset; the hero Image's raycastTarget is turned on for hovers.
- Tuning lesson: the first pass reused Phase 28's (sheen.x - sheen.y) view maths and
  a GlareShift of 2.2, which pushed the band clean off the card at most driver
  values (the capture proved it). Decoupled to _Sheen.x = sweep with GlareShift 0.85
  so a full sweep travels right across the face.
- Verified: Phase53HeroHolographicTests guards the wiring; a read-only capture loads
  a card into the empty hero and renders the foil at two sheen positions
  (Docs/VisualReview/Phase53/) - the band is on the face and travels. EditMode
  266/266, PlayMode 31/31. The idle/hover feel is the player's to judge.

### Phase 54: The deal lands - touchdown weight for dealt cards

- Phase 52 gave the flip weight; the deal still ended by snapping dead onto the
  slot after its arc - all energy vanishing in one frame, the exact flaw the
  flip used to have. Dust and sound already marked the touchdown; the card's own
  body did not.
- Now the card lands: on contact it squashes (−10% height, +6% width) and
  springs back ease-out to its exact base scale over 0.14 s, and the camera
  takes a small Kick (0.03) on the impact frame - subtler than the flip's reveal
  shake because three deals land in quick succession and the reveal stays the
  loudest beat.
- Three serialized knobs on DeckController (landingSquash / landingSeconds /
  landingCameraKick) under a Phase54 Landing header; camera found by the same
  lazy self-healing lookup CardFlipController uses. The Phase 9 arc pacing
  floors (duration, height, tilt, interval) are untouched.
- Verified: EditMode guards the tuning envelope; a PlayMode test drives a real
  one-card deal in the ReadingRoom and proves the squash happens, recovers to
  the exact rest scale, and the card rests on its slot. The feel itself is the
  player's to judge at the table.

### Phase 55: The shuffle moves - riffle choreography on the deck stack

- The card's motion chain had one silent beat left: flip weight came in Phase
  52, the deal landing in Phase 54, but the shuffle was still sound and dust
  over a perfectly still Midnight Parlor stack.
- DeckShuffleChoreographer (on MP_DeckStack) plays the established language as
  a hand shuffling a deck: press-down anticipation (0.015), a riffle ripple
  bottom-to-top (each card pops 0.055 with a 4-degree yaw shiver, rising
  ease-out and dropping ease-in, neighbours 0.025 s apart), a square-up contact
  with camera Kick 0.03 and a -6% squash, and an ease-out settle to the exact
  authored stagger. Total ~0.65 s, matching the Phase 9 shuffle breath (floor
  untouched).
- ReadingRoomController gains a deckShuffle field and fires Play() on the same
  line as the ShuffleStarted cue, so motion, dust, and sound land together.
  Nine serialized knobs; lazy self-healing camera lookup; re-entrant Play()
  calls are ignored while a shuffle runs.
- Verified: EditMode guards the tuning envelope and the scene wiring; PlayMode
  plays a real shuffle via the production StartCoroutine path (the Phase 54
  sampling lesson) and proves cards pop, the stack squashes on contact, and
  every card restores to its rest pose to sub-millimetre precision. EditMode
  271/271, PlayMode 33/33. The riffle's feel is the player's to judge.

### Phase 56: Release 0.9.0 - the presentation-complete build

- The version had not moved since Phase 6 set it: Phases 7-55 (the whole visual
  identity, the Midnight Parlor redesign, HD artwork, audio, the TMP migration,
  and the full card motion chain) all shipped as `0.6.0`, which no longer said
  anything true about the build.
- Bumped to `0.9.0` at its single source of truth
  (`Phase6DesktopBuildBuilder.BundleVersion`, written into PlayerSettings on
  every build), with the Phase 6 test updated to assert the shipped value.
  Held below 1.0 deliberately: the backend reading path has not been verified
  end-to-end in a playthrough, and the Phase 52-55 feel is still unjudged.
- Corrected a stale line in PHASE6_DESKTOP_BUILD.md that still described the
  window as 1280x720 windowed - Phase 42/43 changed it to 1920x1080
  FullScreenWindow, and that exact setting was the blurry-text root cause.
- Verified: EditMode 271/271, PlayMode 33/33. Fresh macOS build (375.7 MB) and
  Windows build (358.7 MB) both report 0.9.0; the Windows release zip
  (203 MB, Builds/Desktop/Release/TarotUnity-Windows-x64.zip) passes an
  integrity test and excludes Unity's BurstDebugInformation_DoNotShip folder.

### Phase 57: The candles, modelled

- Phase 46 gave the candles wax surfaces but never touched the geometry. A
  read-only close-up diagnosis measured what was under them: four stacked Unity
  primitives at 20 radial segments (18 degrees per facet), whose `Lip` is a disc
  *wider* than the body with two hard 90-degree edges - on screen, a cap screwed
  onto a pill bottle - over a flat lid, a dead-parallel tube, and a base disc
  butted against the body with a visible seam.
- CandleMeshBuilder lathes one continuous surface per candle (64 segments front,
  40 back) along a real profile: pooled base flowing out of the body, tapered
  body, melted shoulder swell replacing the collar, rolled rim, a burn crater
  dishing to the wick, an uneven burn line, and the nine drips as geometry. The
  wick is re-cut tapered and curled.
- The drips are lathed at the exact angles gen_wax.py paints them, extracted
  from its seeded sequence. That alignment exposed a Phase 46 defect: the
  contact-shadow pass re-seeds Random(7) without replaying the 90 grain
  iterations, so every shadow fell beside a drip instead of under one. The
  generator is fixed and WaxColor.png regenerated on the owner's authorisation;
  WaxEmission.png is pixel-identical and was left untouched. Measured honestly:
  the fix moves 0.25% of rendered pixels at a peak delta of 19/255, since the
  candle's lit face is blown out by its own flame - a correctness fix, not a
  visible overhaul.
- Untouched on purpose: candle root positions, Lights, Flame/Halo billboards,
  and the 0.085 wax-rim-to-flame gap - a test asserts the flame did not move.
- Verified: EditMode 275/275, PlayMode 33/33, before/after close-ups in
  Docs/VisualReview/Phase57/. Three tuning passes were needed and the captures
  caught all three (a rim wobble that read as cut open, drips welded into a
  bulging ring, a wick like a bent blade). Phase 57 first recorded the crater
  interior as blowing out to flat white; a Phase 58 re-check with measurement
  walked that back - the floor is bright (directly lit) but keeps its 3D
  shading (luminance stdev 45.5, the wick casting a visible dip), so the "eats
  the shape" claim was overstated off an unconverged capture. ~21% clips to
  white, a defensible melt-pool look, left as is.

### Phase 58: The candles were never red - the capture pipeline was

- Opened to fix the menu's front candles rendering as saturated red plastic
  (RGB 246,74,25 at saturation 0.90) against the room's cream wax (213,149,59).
  Ends without touching a light, colour, or material.
- A read-only probe rendered nine candidate fixes and seven landed on the same
  number - the signature of a broken instrument, not a result. Two control rows
  (candle lights off, fill off) were added: killing the candle lights *produced*
  the red look, inverting the hypothesis, and the plain baseline disagreed with
  the previous run's baseline. Same scene, two answers.
- Cause: the wax is RealtimeEmissive and realtime GI does not resolve on the
  frame a scene loads. Measured on one loaded scene, render 1 gives
  RGB(246,77,23) saturation 0.91 and render 2 onward gives RGB(233,177,78)
  saturation 0.66, flat through 24 renders. All fifteen capture builders
  rendered exactly once after opening a scene - every visual review this
  project has run was looking at unsettled lighting. The player never saw it.
- CaptureRig.RenderConverged (4 warm-up renders, twice the measured convergence
  point) now fronts all fifteen builders; Phase58CaptureConvergenceTests scans
  Assets/Editor for a direct Camera.Render() and fails on the next one written.
  HD archive regenerated: the menu candle now reads RGB(233,179,76) at
  saturation 0.67 - less saturated than the room's, which was the reference.
- EditMode 278/278, PlayMode 33/33. Lesson kept: when several different changes
  produce the same result, doubt the instrument before the hypothesis.

### Phase 59: The flame, rebuilt

- A three-screen review on the Phase 58 converged pipeline found the scenes good
  except the flame - the focal light's brightest, most-looked-at object, and the
  worst-looking one. Close-up: horizontal banding across the whole flame plus a
  stepped hard edge down the top centre.
- Root cause was not the art: the source sprite is a clean 256px gradient, but
  it imported Compressed (textureCompression 1), and a pure gradient is the
  worst case for block compression - the 4x4 blocks quantise the ramp into
  venetian blinds. It was also a 256px mystery asset with no generator.
- Replaced with Tools/UiKitGenerator/gen_flame.py (reproducible, matching the
  wax pipeline): a 512px flame with a true candle silhouette (rounded base
  hugging the wick, belly low, tapering the long way to a point - three earlier
  shapes rejected against previews), a heat ramp from amber edge to white core,
  a cool blue root, and a soft outer glow. Imported uncompressed.
- Verified: before/after A/B at player framing (old banded and hard-edged, new a
  clean teardrop in its glow); Phase59FlameQualityTests guards 512+/uncompressed/
  generator-present; HD archive regenerated. Honest note: the new flame is ~30%
  slimmer (the old blob bled wide) - truer and cleaner, but whether it wants more
  presence via a larger quad is a taste call left for a play session.

## Operating Rule

Every phase ends with the established end-check flow:

- Unity error and log scan
- EditMode / PlayMode / custom tests
- code and asset reference review
- vertical-slice verification
- build or zip verification when the phase touches release output
