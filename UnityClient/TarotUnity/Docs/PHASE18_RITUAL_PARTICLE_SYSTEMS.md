# Phase 18 Ritual Particle Systems

Date: 2026-06-05

## Scope

Phase 18 adds the first true Unity `ParticleSystem` layer to the existing ritual aura presentation.

It builds on:

- Phase13 real RWS1909 tarot artwork
- Phase14 dimensional card reveal
- Phase15 3D table foundation
- Phase16 ritual aura anchors
- Phase17 runtime aura motion

The phase is additive. It does not change backend flow, record creation, card selection, interpretation, scene order, release packaging, or broad UI layout.

## Runtime Changes

`RitualParticleSystemController` controls optional particle arrays:

- `ambientParticles`
- `focusParticles`
- `revealParticles`

It exposes null-safe methods:

- `SetParticlesVisible(bool visible)`
- `SetIntensity(float value)`
- `PlayAmbient()`
- `StopAll(bool clear)`
- `TriggerRevealBurst()`
- `SimulateTick(float deltaSeconds)`

Missing arrays, empty arrays, and null entries are skipped. Optional particle references must never break card flip, result reveal, or scene navigation.

## ReadingRoom Changes

`Phase18RitualParticleSystemBootstrapper` wires `RitualParticleSystemController` onto:

- `Phase16_RitualAuraRoot`

It creates real `ParticleSystem` children under existing Phase16 anchors:

- `Phase18_AmbientDustParticles` under `Phase16_ParticleAnchorNorth`
- `Phase18_DeckFocusParticles` under `Phase16_ParticleAnchorEast`
- `Phase18_FlipSparkParticles` under `Phase16_AuraFocusAnchor`

These effects are intentionally low-emission so the table, controls, and real tarot artwork remain readable.

## Result Changes

`Phase18RitualParticleSystemBootstrapper` wires `RitualParticleSystemController` onto:

- `Phase16_ResultAuraRoot`

It creates real `ParticleSystem` children under existing Phase16 result anchors:

- `Phase18_ResultCardMotes` under `Phase16_ResultParticleAnchorLeft`
- `Phase18_ResultInterpretationGlow` under `Phase16_ResultAuraFocusAnchor`

Result particles are quieter than ReadingRoom particles to protect interpretation text readability.

## Out Of Scope

Phase 18 does not add backend/API changes, history, settings, profile, admin, dashboard work, desktop build regeneration, tarot art replacement, Shader Graph animation, post-processing, camera choreography, or broad interface redesign.

This is the first true particle-system layer, not final high-end VFX.

## Editor Bootstrap Notes

`Phase18RitualParticleSystemBootstrapper` is an idempotent Unity Editor utility. It creates or refreshes Phase18 particle wiring on existing Phase16 aura roots.

Run Phase16 and Phase17 bootstrappers first if their anchors or motion controllers are missing.

Menu path:

```text
Tools/Tarot Unity/Run Phase 18 Ritual Particle Systems Bootstrap
```

## Verification Commands

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase18-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase18-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
