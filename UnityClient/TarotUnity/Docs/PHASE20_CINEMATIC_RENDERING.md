# Phase 20 Cinematic Rendering

Date: 2026-06-11

## Purpose

Phase 20 is the first true rendering-quality pass. Phases 15-19 built the 3D table,
aura anchors, runtime motion, and particle systems, but the frame itself was still
rendered flat: no post-processing, LDR color grading, and no HDR glow response.
Phase 20 turns on the cinematic rendering pipeline so every existing visual layer
immediately reads richer, closer to a premium card-game presentation.

## What Changed

### Post-Processing Volume

A shared cinematic volume profile lives at
`Assets/Settings/Phase20_CinematicVolumeProfile.asset` and contains:

- Bloom: threshold 0.85, intensity 0.8, warm gold tint, high-quality filtering.
  HDR-boosted glow materials and candle emission now halo softly.
- Tonemapping: ACES filmic response, the same curve family used by premium 3D
  card games. Highlights roll off instead of clipping.
- Color Adjustments: +0.12 exposure, +12 contrast, +8 saturation for a deeper
  candlelit mood.
- White Balance: +10 temperature toward warm candlelight.
- Vignette: 0.32 intensity to focus the eye on the table center.

Each gameplay scene (`MainMenu`, `ReadingRoom`, `Result`) contains a
`Phase20_CinematicVolume` global volume object referencing this shared profile.

### Camera Upgrades

Every scene camera now renders with:

- post-processing enabled
- HDR output allowed
- SMAA (high quality) anti-aliasing
- dithering to reduce banding in the dark table mood

### Render Pipeline Assets

- `PC_RPAsset`: HDR color grading mode, 32-point LUT, MSAA 4x.
- `Mobile_RPAsset`: HDR color grading mode kept consistent so a future mobile
  target inherits the same look.

### HDR Glow Materials

Transparent glow materials (`MAT_CardGlow`, `MAT_RitualGlow`, `MAT_RitualSpark`,
`MAT_Phase14_RevealGlow`, `MAT_Phase14_FaceRimLight`, `MAT_Phase16_AuraGlowPool`,
`MAT_Phase16_AuraParticle`, `MAT_Phase16_RuneRing`, `MAT_Phase18_RitualParticles`)
were boosted into HDR range (x2.2) so bloom picks them up. The boost is
idempotent: colors already above LDR range are left untouched.

Emissive lit materials now feed bloom directly:

- `MAT_Phase8_CandleAmber`: warm amber emission for the candle bodies.
- `MAT_Phase7_MoonGold`: soft gold emission for the menu moon disc.
- `MAT_Phase8_EdgeGold`: subtle gold edge emission.

## How To Run

Editor menu: `Tools/Tarot Unity/Run Phase 20 Cinematic Rendering Bootstrap`.

Batch mode:

```bash
Unity -projectPath UnityClient/TarotUnity -batchmode \
  -executeMethod TarotUnity.Editor.Phase20CinematicRenderingBootstrapper.Run -quit
```

Screenshot capture (graphics required, do not add `-nographics`):

```bash
Unity -projectPath UnityClient/TarotUnity -batchmode \
  -executeMethod TarotUnity.Editor.Phase20VisualCaptureBuilder.Run -quit
```

Captured screens land in `Docs/VisualReview/Phase20/`.

## Exit Criteria

- `Phase20_CinematicVolumeProfile.asset` exists with Bloom, ACES Tonemapping,
  Color Adjustments, White Balance, and Vignette.
- `MainMenu`, `ReadingRoom`, and `Result` each contain `Phase20_CinematicVolume`
  and a camera with post-processing, HDR, and SMAA enabled.
- PC and Mobile pipeline assets use HDR color grading.
- Key glow materials sit above LDR range; candle/moon/edge materials emit.
- Phase 13 card art, Phase 16 aura anchors, and prior wiring remain intact.
- EditMode and PlayMode tests pass.

## Remaining Limitation

Phase 20 is the rendering-pipeline pass, not final high-end VFX. Later phases can
add Cinemachine camera choreography, Shader Graph card materials (foil, rim,
dissolve), card motion trails, depth-of-field staging for the Result scene, and
screenshot-driven final polish.
