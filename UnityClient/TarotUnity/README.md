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

## Current Playable Slice

Open `Assets/Scenes/Boot.unity` and press Play.

Expected local flow:

`Start Reading -> choose spread -> enter question -> Draw -> click cards to flip -> Reveal Result`

The current slice can run with local placeholder data or the Phase 4 backend integration mode. Phase 6 adds desktop runtime config plus generated macOS and Windows prototype build paths. Phase 7 upgrades the vertical slice UI toward an immersive ritual desktop prototype. Phase 8 adds the first visual identity pass for the table, cards, result frame, and theme palette. Phase 9 adds card-game rhythm scaffolding for shuffle, deal, flip, and result reveal. Phase 10 adds release package readme/config files and player-readable offline/backend status copy. Phase 11 adds screenshot-based visual review artifacts and a first layout adjustment pass. Phase 12 adds card-first reveal anchors and a durable card face-art slot. Phase 13 adds the RWS1909 tarot artwork pipeline, default card artwork catalog, and ReadingRoom/Result scene wiring for real card-face sprites. Phase 14 adds a 2.5D dimensional card reveal layer, card edge/shadow/glow anchors, and stage polish around the existing real card art. Phase 15 adds the first 3D table foundation with card mesh-shell anchors, ritual table depth, warm/cool lighting, and Result card-stage support. Phase 16 adds a controlled ritual aura VFX layer with glow pools, rune-ring anchors, particle anchor markers, and Result aura support. Phase 17 adds lightweight runtime aura motion for rune-ring rotation, glow pulsing, and particle-anchor drift. Phase 18 adds the first true ParticleSystem layer for ambient dust, deck focus, flip sparkle, and Result card motes while preserving the vertical slice. Phase 19 connects shuffle, deal, flip, and result cues to that particle layer through action-triggered VFX integration. Phase 20 enables the cinematic rendering pipeline: HDR color grading, ACES tonemapping, bloom, vignette, SMAA, and HDR-boosted glow/emissive materials across MainMenu, ReadingRoom, and Result. Phase 21 replaces the prototype camera with seated cinematic framing (narrow FOV, research-driven poses), idle breathing, an opening settle-in shot, and a flip punch-in reaction. Phase 22 re-composes the ReadingRoom UI for that camera: a clear card stage band, a bottom action tray holding every clickable control, deactivated flat-era table overlays, and a deck stack visible in the default framing. Phase 23 adds card physicality: hover lift/tilt on face-down cards with near-100ms response, plus research-backed rhythm tuning for the flip punch, flip lift, and deal interval. Phase 24 gives the all-Chinese UI a real typographic identity: it bundles the LXGW WenKai (SIL OFL) calligraphic font so text no longer falls back to an inconsistent OS sans-serif, then bakes a body/display type hierarchy with title outlines across MainMenu, ReadingRoom, and Result through a central TarotUiTheme font system. Phase 25 re-composes the Result payoff screen on rule-of-thirds lines: a full-width gold question header, the drawn card's artwork as a hero in the left third, the AI interpretation as aligned gold-header/ivory-body pairs on a backing panel in the right two-thirds, and a centered footer, with redundant flat-era overlays deactivated and a TarotUiAccentText marker keeping headers gold at runtime. The Windows release zip is generated at `Builds/Desktop/Release/TarotUnity-Windows-x64.zip`.
