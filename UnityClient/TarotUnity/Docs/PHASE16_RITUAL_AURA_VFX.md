# Phase 16 Ritual Aura VFX

Date: 2026-06-04

## Scope

Phase 16 implements the approved B2 direction: a controlled ritual rune aura layer for the current Unity tarot vertical slice.

It adds ReadingRoom and Result aura anchors, transparent aura materials, particle anchor markers, and a null-safe `RitualAuraController` while preserving backend flow, scene order, RWS1909 artwork, Phase14 dimensional reveal behavior, and the Phase15 3D table foundation.

## Runtime Changes

- `RitualAuraController` owns optional aura visibility, rune visibility, particle visibility, and intensity state.
- All references are optional.
- Missing aura references must not break card flip, result reveal, or scene navigation.

## ReadingRoom Changes

`ReadingRoom` gains:

- `Phase16_RitualAuraRoot`
- `Phase16_GlowPool`
- `Phase16_RuneRingOuter`
- `Phase16_RuneRingInner`
- `Phase16_ParticleAnchorNorth`
- `Phase16_ParticleAnchorEast`
- `Phase16_ParticleAnchorSouth`
- `Phase16_ParticleAnchorWest`
- `Phase16_AuraFocusAnchor`

## Result Changes

`Result` gains:

- `Phase16_ResultAuraRoot`
- `Phase16_ResultGlowPool`
- `Phase16_ResultRuneRing`
- `Phase16_ResultParticleAnchorLeft`
- `Phase16_ResultParticleAnchorRight`
- `Phase16_ResultAuraFocusAnchor`

## Materials

Phase16 creates:

- `MAT_Phase16_AuraGlowPool`
- `MAT_Phase16_RuneRing`
- `MAT_Phase16_AuraParticle`

All Phase16 materials are transparent URP-compatible materials intended for light atmosphere, not final high-end VFX.

## Out Of Scope

Phase 16 does not add backend/API changes, history, settings, profile, admin, dashboard work, release zip regeneration, tarot art replacement, final particle systems, shader graph animation, post-processing, camera shake, or broad UI redesign.

## Editor Bootstrap Notes

`Phase16RitualAuraBootstrapper` is an idempotent Unity Editor utility used to create or refresh Phase16 materials and scene anchors. It is not a runtime system and is not included in player builds.

The bootstrapper should be rerun only when Phase16 anchors need to be regenerated. Close the Unity Editor before running it in batchmode.

## Verification

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
