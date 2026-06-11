# Phase 19 Card Action VFX Integration

Date: 2026-06-08

## Scope

Phase 19 connects existing card-action presentation cues to the Phase18 true `ParticleSystem` layer.

It builds on:

- Phase13 real RWS1909 tarot artwork
- Phase14 dimensional card reveal
- Phase15 3D table foundation
- Phase16 ritual aura anchors
- Phase17 runtime aura motion
- Phase18 ritual particle systems

The phase is additive. It does not change backend flow, login, record creation, card selection, interpretation, scene order, release packaging, or broad UI layout.

## Runtime Changes

`RitualActionVfxController` maps existing `PresentationCueId` values to optional Phase18 particle behavior:

- `ShuffleStarted`: set shuffle intensity and optionally play ambient/focus particles
- `CardDealt`: set deal intensity and optionally keep ambient/focus particles alive
- `CardFlipped`: set flip intensity and optionally trigger reveal burst particles
- `ResultReady` and `ResultReveal`: set result intensity, play ambient/focus particles, and optionally trigger reveal burst particles

It exposes null-safe state and methods:

- `PresentationCueId LastCue`
- `Transform LastAnchor`
- `int CueCount`
- `PlayCue(PresentationCueId cue)`
- `PlayCue(PresentationCueId cue, Transform anchor)`

Missing `RitualParticleSystemController` references are safe. The controller still records cue state and must not break card flip, result reveal, or scene navigation.

## Feedback Forwarding

`RitualFeedbackController` remains the single cue entry point for existing gameplay and UI code.

It now includes:

- `RitualActionVfxController actionVfxController`
- `bool forwardCuesToActionVfx`

`PlayCue(cue, anchor)` still updates `LastCue`, increments `CueCount`, plays audio, and plays legacy cue particles. After that, when forwarding is enabled, it forwards the same cue and anchor to `RitualActionVfxController`.

Forwarding is intentionally independent from legacy cue particles. If a legacy particle reference is missing, action VFX can still receive the cue.

## ReadingRoom Wiring

`Phase19CardActionVfxBootstrapper` wires:

- `ReadingRoomRitualFeedback.actionVfxController`
- `ReadingRoomRitualFeedback.forwardCuesToActionVfx = true`
- `RitualActionVfxController.particleSystemController` to `Phase16_RitualAuraRoot`

ReadingRoom action settings:

- shuffle ambient: enabled
- deal ambient: enabled
- flip burst: enabled
- result burst: enabled
- shuffle intensity: `0.76`
- deal intensity: `0.66`
- flip intensity: `0.95`
- result intensity: `0.72`

## Result Wiring

`Phase19CardActionVfxBootstrapper` wires:

- `ResultRitualFeedback.actionVfxController`
- `ResultRitualFeedback.forwardCuesToActionVfx = true`
- `RitualActionVfxController.particleSystemController` to `Phase16_ResultAuraRoot`

Result action settings:

- shuffle ambient: disabled
- deal ambient: disabled
- flip burst: disabled
- result burst: enabled
- shuffle intensity: `0.44`
- deal intensity: `0.44`
- flip intensity: `0.52`
- result intensity: `0.58`

Result currently relies on Phase18 ambient/focus particles and has no separate reveal particle array. Calling `TriggerRevealBurst()` remains safe because empty reveal arrays are no-ops.

## Editor Bootstrap Notes

`Phase19CardActionVfxBootstrapper` is an idempotent Unity Editor utility. It adds or refreshes `RitualActionVfxController` on the ReadingRoom and Result ritual feedback objects.

Run Phase16 and Phase18 bootstrappers first if aura roots or particle controllers are missing.

Menu path:

```text
Tools/Tarot Unity/Run Phase 19 Card Action VFX Bootstrap
```

Batchmode command:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase19CardActionVfxBootstrapper.Run -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-bootstrap.log -quit
```

## Out Of Scope

Phase 19 does not add backend/API changes, history, settings, profile, admin, dashboard work, desktop build regeneration, tarot art replacement, Shader Graph animation, post-processing, camera choreography, or broad interface redesign.

This is action-triggered VFX integration, not final high-end VFX.

## Verification Commands

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase19-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase19-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
