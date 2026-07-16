# Tarot Unity Client

Unity frontend workspace for the Tarot vertical slice.

## Editor

- Unity: `6000.3.16f1`
- Template: URP
- First target platforms: macOS and Windows desktop

## Current Package Baseline

Already present in `Packages/manifest.json`:

- Universal Render Pipeline `17.3.0`
- Input System `1.19.0`
- Unity Test Framework `1.6.0`
- UGUI `2.0.0`

Package Manager follow-up for later phases:

- Cinemachine for reveal and result camera choreography
- TextMeshPro essential resources before final UI work
- Addressables only after card art and audio assets grow

## V1 Flow

The first playable slice stays focused on:

`MainMenu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`

Do not build history, settings, profile, admin, or packaging workflows in this phase.

## Bootstrap Notes

Phase 1 conventions and the first scene, prefab, script, and backend API map live in:

- `Docs/PHASE1_BOOTSTRAP.md`

Phase 2 graybox flow notes live in:

- `Docs/PHASE2_GRAYBOX.md`

Pre-Phase-6 readiness and desktop build notes live in:

- `Docs/PRE_PHASE6_READINESS.md`
- `Docs/PHASE6_DESKTOP_BUILD.md`
- `Docs/PHASE7_IMMERSIVE_UI_DESIGN.md`
- `Docs/UI_COMPLETION_MAINLINE.md`
- `Docs/PHASE8_VISUAL_IDENTITY.md`
- `Docs/PHASE9_MOTION_AUDIO_RHYTHM.md`
- `Docs/PHASE10_RELEASE_UX_HARDENING.md`
- `Docs/PHASE11_VISUAL_REVIEW.md`
- `Docs/PHASE12_CARD_FIRST_REVEAL.md`
- `Docs/PHASE13_TAROT_ARTWORK_PIPELINE.md`
- `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`
- `Docs/PHASE15_3D_TABLE_FOUNDATION.md`
- `Docs/PHASE16_RITUAL_AURA_VFX.md`
- `Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md`
- `Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md`
- `Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md`
- `Docs/PHASE20_CINEMATIC_RENDERING.md`
- `Docs/PHASE21_CINEMATIC_CAMERA.md`
- `Docs/PHASE22_UI_COMPOSITION.md`
- `Docs/PHASE23_CARD_FEEL.md`
- `Docs/PHASE24_TYPOGRAPHY.md`
- `Docs/PHASE25_RESULT_COMPOSITION.md`
- `Docs/PHASE26_RESULT_ROBUSTNESS.md`
- `Docs/PHASE27_HD_ARTWORK.md`
- `Docs/PHASE28_HOLOGRAPHIC_CARD.md`
- `Docs/PHASE29_RESULT_SCROLL.md`
- `Docs/PHASE30_CROSS_SCREEN_CONSISTENCY.md`
- `Docs/PHASE31_HD_VISUAL_ARCHIVE.md`
- `Docs/PHASE32_CARD_FACE_FIT.md`
- `Docs/PHASE33_AMBIENT_AUDIO.md`
- `Docs/PHASE34_QUIT_BUTTON.md`
- `Docs/PHASE35_RESULT_MENU_POLISH.md`
- `Docs/PHASE36_PERFORMANCE_PASS.md`
- `Docs/PHASE37_VISUAL_REDESIGN_BLUEPRINT.md`
- `Docs/PHASE38_TABLE_REBUILD.md`
- `Docs/PHASE39_UI_RESKIN.md`
- `Docs/PHASE40_MENU_RESULT_RECOMPOSITION.md`
- `Docs/PHASE41_FINAL_POLISH.md`
- `Docs/PHASE42_MENU_ELEVATION.md`
- `Docs/THIRD_PARTY_ASSETS.md`

## Current Playable Slice

Open `Assets/Scenes/Boot.unity` and press Play.

Expected local flow:

`Start Reading -> choose spread -> enter question -> Draw -> click cards to flip -> Reveal Result`

The current slice can run with local placeholder data or the Phase 4 backend integration mode. Phase 6 adds desktop runtime config plus generated macOS and Windows prototype build paths. Phase 7 upgrades the vertical slice UI toward an immersive ritual desktop prototype. Phase 8 adds the first visual identity pass for the table, cards, result frame, and theme palette. Phase 9 adds card-game rhythm scaffolding for shuffle, deal, flip, and result reveal. Phase 10 adds release package readme/config files and player-readable offline/backend status copy. Phase 11 adds screenshot-based visual review artifacts and a first layout adjustment pass. Phase 12 adds card-first reveal anchors and a durable card face-art slot. Phase 13 adds the RWS1909 tarot artwork pipeline, default card artwork catalog, and ReadingRoom/Result scene wiring for real card-face sprites. Phase 14 adds a 2.5D dimensional card reveal layer, card edge/shadow/glow anchors, and stage polish around the existing real card art. Phase 15 adds the first 3D table foundation with card mesh-shell anchors, ritual table depth, warm/cool lighting, and Result card-stage support. Phase 16 adds a controlled ritual aura VFX layer with glow pools, rune-ring anchors, particle anchor markers, and Result aura support. Phase 17 adds lightweight runtime aura motion for rune-ring rotation, glow pulsing, and particle-anchor drift. Phase 18 adds the first true ParticleSystem layer for ambient dust, deck focus, flip sparkle, and Result card motes while preserving the vertical slice. Phase 19 connects shuffle, deal, flip, and result cues to that particle layer through action-triggered VFX integration. Phase 20 enables the cinematic rendering pipeline: HDR color grading, ACES tonemapping, bloom, vignette, SMAA, and HDR-boosted glow/emissive materials across MainMenu, ReadingRoom, and Result. Phase 21 replaces the prototype camera with seated cinematic framing (narrow FOV, research-driven poses), idle breathing, an opening settle-in shot, and a flip punch-in reaction. Phase 22 re-composes the ReadingRoom UI for that camera: a clear card stage band, a bottom action tray holding every clickable control, deactivated flat-era table overlays, and a deck stack visible in the default framing. Phase 23 adds card physicality: hover lift/tilt on face-down cards with near-100ms response, plus research-backed rhythm tuning for the flip punch, flip lift, and deal interval. Phase 24 gives the all-Chinese UI a real typographic identity: it bundles the LXGW WenKai (SIL OFL) calligraphic font so text no longer falls back to an inconsistent OS sans-serif, then bakes a body/display type hierarchy with title outlines across MainMenu, ReadingRoom, and Result through a central TarotUiTheme font system. Phase 25 re-composes the Result payoff screen on rule-of-thirds lines: a full-width gold question header, the drawn card's artwork as a hero in the left third, the AI interpretation as aligned gold-header/ivory-body pairs on a backing panel in the right two-thirds, and a centered footer, with redundant flat-era overlays deactivated and a TarotUiAccentText marker keeping headers gold at runtime. Phase 26 hardens that reading for variable-length backend AI text: each interpretation section uses best-fit text so long copy shrinks to fit its box instead of overflowing into the next section, while short copy stays at full size. Phase 27 upgrades the card faces to HD: all 78 authentic public-domain Rider-Waite-Smith 1909 scans were re-sourced at high resolution (~740x1280), imported as sprites, and wired through the catalog so flipping a card and the result hero show crisp art. Phase 28 gives the flipped card face a holographic foil sheen: a custom URP shader adds a view-angle glare band plus subtle iridescence that sweeps as the card tilts, so the HD face reads as a glossy, dimensional object. Phase 29 replaces Phase 26's best-fit shrink with a true scrollable reading panel on the Result screen: the four interpretation sections are reparented into a vertical ScrollRect (RectMask2D viewport + VerticalLayoutGroup/ContentSizeFitter Content), so any length of backend AI text renders at full size and scrolls instead of shrinking or overflowing. Phase 30 unifies the three screens' secondary spacing/alignment: a read-only layout audit confirmed they already share one CanvasScaler (1280x720, match 0.5) and symmetric centering, so this phase codifies that rhythm in a shared `TarotUiSpacing` source of truth and adds cross-screen regression tests rather than risk speculative repositioning. Phase 31 builds a complete HD visual archive (2560x1440) of the current screens - MainMenu, ReadingRoom, and the Result default/long-reading states - into `Docs/VisualReview/Phase31_HDArchive/` for review, and flags a card-face sizing question on the hero prefab (the HD sprites import larger than the face slot was tuned for) for a feedback-tuned follow-up. Phase 32 fixes that card-face bug: `CardView.FitFaceArtwork` sizes the face artwork to the card footprint regardless of sprite resolution/PPU, and the prefab lifts the face renderer clear of the cream front planes so it is no longer occluded - flipping a card now shows the HD art correctly sized and crisp on the card. Phase 33 gives the game actual sound: the audio system was fully wired but silent (empty cueMap, beep fallback), so this adds seven original royalty-free synthesized SFX (one per presentation cue) plus a looping ambient music bed, assigned on the persistent Boot `AudioManager`. Phase 34 adds a main-menu quit button so desktop players can exit in-game. Phase 35 repairs three visual-review defects: the leftover 3D result card stage no longer peeks out from behind the reading panel, the flat-era ResultReadingFrame no longer darkens the hero card slot with a seam, and the main menu now reads as primary plate / status line / quiet quit link. Phase 36 is the first performance pass: the flip path no longer scans the scene per click, a PlayMode probe measures the real three-card slice (steady state is allocation-free, main-thread frames ~0.1 ms in batch), and the doc records how to read real GPU numbers with the Metal HUD. Phase 37 opens the Midnight Parlor visual redesign (Hearthstone-benchmark research, art direction, CC0 PBR surfaces, and a locally composed gold UI kit with a celestial card back; provenance in `Docs/THIRD_PARTY_ASSETS.md`). Phase 38 rebuilds the ReadingRoom on that foundation: one velvet-and-walnut table stage with gold card sockets, a staggered deck stack, and the real composed card back on the card prefab, with the superseded flat-plane era deleted and guarded against resurrection. Phase 39 reskins the ReadingRoom chrome onto that table: the step tracker, action dock, question input, and all four buttons wear the gold nine-slice plaques, with clean button ColorBlocks and the redundant flat-era frames deactivated. Phase 40 re-composes the MainMenu (candlelit tabletop vignette: deck stack, scattered cards, candle halos replace the deleted disc/slab) and the Result screen (gold-framed hero showcase and reading scroll, diamond dividers, radial candle glow) on the same stage. Phase 41 closes the redesign out: HD archive regenerated on the new look, 14 orphaned legacy materials swept after a GUID reference check, suites green, fresh desktop build. Phase 42 answers the first playthrough: the player now presents as a borderless fullscreen window at native resolution (a stretched fixed 720p window, not render scale or post-processing, was what softened every glyph), and the main menu is restaged as a lit room - stock daylight skybox ambient replaced with a near-black floor so the candles own the lighting, a seated 27deg/36mm framing instead of a 10.8deg wide-angle, a composed parlor haze instead of a black void, real candles with emissive wax, and a deep-oxblood velvet that lets the gold read. The Windows release zip is generated at `Builds/Desktop/Release/TarotUnity-Windows-x64.zip`.
