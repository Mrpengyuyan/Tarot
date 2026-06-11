# Phase 17 Ritual Aura Runtime Motion

Date: 2026-06-05

## Scope

Phase 17 implements a lightweight runtime motion pass for the Phase16 ritual aura layer.

It adds `RitualAuraMotionController`, ReadingRoom motion wiring, and Result motion wiring while preserving backend flow, scene order, RWS1909 artwork, Phase14 dimensional reveal behavior, Phase15 3D table foundation, and Phase16 aura anchors.

## Runtime Changes

- `RitualAuraMotionController` rotates rune-ring transforms.
- `RitualAuraMotionController` pulses glow transforms.
- `RitualAuraMotionController` floats particle-anchor transforms.
- All references are optional.
- Missing motion references must not break card flip, result reveal, or scene navigation.

## ReadingRoom Changes

`ReadingRoom` wires `RitualAuraMotionController` on `Phase16_RitualAuraRoot`.

The controller targets:

- `Phase16_GlowPool`
- `Phase16_RuneRingOuter`
- `Phase16_RuneRingInner`
- `Phase16_ParticleAnchorNorth`
- `Phase16_ParticleAnchorEast`
- `Phase16_ParticleAnchorSouth`
- `Phase16_ParticleAnchorWest`

## Result Changes

`Result` wires `RitualAuraMotionController` on `Phase16_ResultAuraRoot`.

The controller targets:

- `Phase16_ResultGlowPool`
- `Phase16_ResultRuneRing`
- `Phase16_ResultParticleAnchorLeft`
- `Phase16_ResultParticleAnchorRight`

## Out Of Scope

Phase 17 does not add backend/API changes, history, settings, profile, admin, dashboard work, release zip regeneration, tarot art replacement, final particle systems, shader graph animation, post-processing, camera shake, or broad UI redesign.

This is not final high-end VFX.

## Editor Bootstrap Notes

`Phase17RitualAuraMotionBootstrapper` is an idempotent Unity Editor utility used to create or refresh Phase17 motion wiring on existing Phase16 aura roots. It is not a runtime system and is not included in player builds.

Run the Phase16 bootstrapper first if Phase16 anchors are missing.

## Verification Commands

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
