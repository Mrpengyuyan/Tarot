# Phase 19 Card Action VFX Integration Design

Date: 2026-06-08

## Approved Direction

Phase19 uses approved approach A: connect existing card-action presentation cues to the Phase18 true particle-system layer.

The goal is to make player actions feel more physical and responsive without changing the vertical-slice flow or starting a broad final art pass. Shuffle, deal, flip, and result reveal should now drive subtle aura intensity changes, ambient particle playback, and reveal bursts.

## Scope

Phase19 includes:

- adding a null-safe `RitualActionVfxController` under `Assets/Scripts/Presentation`
- extending `RitualFeedbackController` to optionally forward presentation cues into `RitualActionVfxController`
- wiring ReadingRoom `ReadingRoomRitualFeedback` to a `RitualActionVfxController`
- wiring Result `ResultRitualFeedback` to a `RitualActionVfxController`
- connecting ReadingRoom action VFX to `Phase16_RitualAuraRoot` `RitualParticleSystemController`
- connecting Result action VFX to `Phase16_ResultAuraRoot` `RitualParticleSystemController`
- adding EditMode tests for runtime API, scene wiring, cue-forwarding fields, previous phase retention, and docs
- adding PlayMode tests for cue mapping, null safety, reveal burst triggering, and feedback forwarding
- documenting Phase19 in a phase doc, `README.md`, and the UI completion mainline

Phase19 excludes:

- backend/API changes
- history, settings, profile, admin, or dashboard work
- desktop build or release zip regeneration
- new tarot card artwork
- Shader Graph rune animation
- camera choreography
- post-processing
- broad UI layout redesign
- replacing Phase18 particle-system objects

## Runtime Components

### `RitualActionVfxController`

`RitualActionVfxController` owns optional action-to-particle cue mapping.

Serialized references:

- `RitualParticleSystemController particleSystemController`
- `bool playAmbientOnShuffle`
- `bool playAmbientOnDeal`
- `bool burstOnFlip`
- `bool burstOnResult`
- `float shuffleIntensity`
- `float dealIntensity`
- `float flipIntensity`
- `float resultIntensity`

Public state and methods:

- `PresentationCueId LastCue { get; private set; }`
- `Transform LastAnchor { get; private set; }`
- `int CueCount { get; private set; }`
- `void PlayCue(PresentationCueId cue)`
- `void PlayCue(PresentationCueId cue, Transform anchor)`

Runtime behavior:

- `ShuffleStarted` sets shuffle intensity and starts ambient/focus particles.
- `CardDealt` sets deal intensity and keeps ambient/focus particles alive, giving dealt cards a trail context through the existing cue particles and Phase18 layer.
- `CardFlipped` sets flip intensity and calls `TriggerRevealBurst()`.
- `ResultReady` and `ResultReveal` set result intensity, play ambient/focus particles, and trigger reveal bursts when configured.
- `SpreadSelected`, `MenuOpen`, `None`, and unknown future cues are safe no-ops except for recording `LastCue`.

All references are optional. Missing `RitualParticleSystemController` must not throw and must not stop audio, legacy cue particles, card flip, result reveal, or scene navigation.

### `RitualFeedbackController` Extension

`RitualFeedbackController` remains the single cue entry point for existing gameplay/UI code.

New serialized references:

- `RitualActionVfxController actionVfxController`
- `bool forwardCuesToActionVfx`

Behavior:

- `PlayCue(cue, anchor)` continues to update `LastCue`, increment `CueCount`, play audio, and play its legacy cue particle system.
- After legacy cue handling, it forwards the same cue and anchor to `actionVfxController` only when `forwardCuesToActionVfx` is true.
- Missing `actionVfxController` is a no-op.

## Scene Wiring

### ReadingRoom

`Phase19CardActionVfxBootstrapper` wires:

- `ReadingRoomRitualFeedback.actionVfxController`
- `ReadingRoomRitualFeedback.forwardCuesToActionVfx = true`
- `RitualActionVfxController.particleSystemController` to `Phase16_RitualAuraRoot` `RitualParticleSystemController`

Suggested ReadingRoom values:

- `playAmbientOnShuffle`: true
- `playAmbientOnDeal`: true
- `burstOnFlip`: true
- `burstOnResult`: true
- `shuffleIntensity`: 0.76
- `dealIntensity`: 0.66
- `flipIntensity`: 0.95
- `resultIntensity`: 0.72

### Result

`Phase19CardActionVfxBootstrapper` wires:

- `ResultRitualFeedback.actionVfxController`
- `ResultRitualFeedback.forwardCuesToActionVfx = true`
- `RitualActionVfxController.particleSystemController` to `Phase16_ResultAuraRoot` `RitualParticleSystemController`

Suggested Result values:

- `playAmbientOnShuffle`: false
- `playAmbientOnDeal`: false
- `burstOnFlip`: false
- `burstOnResult`: true
- `shuffleIntensity`: 0.44
- `dealIntensity`: 0.44
- `flipIntensity`: 0.52
- `resultIntensity`: 0.58

## Data Flow

The existing gameplay flow remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

Phase19 does not add new gameplay states.

Runtime cue flow:

1. Existing gameplay/UI code calls `RitualFeedbackController.PlayCue(cue, anchor)`.
2. `RitualFeedbackController` plays audio and legacy cue particles as before.
3. `RitualFeedbackController` forwards the cue to `RitualActionVfxController`.
4. `RitualActionVfxController` adjusts Phase18 particle intensity and playback for the cue.
5. Card draw, flip, interpretation, and scene navigation continue through existing controllers.

## Error Handling

Phase19 must be safe with missing optional visual references:

- missing `RitualActionVfxController`: legacy feedback still works
- missing `RitualParticleSystemController`: action VFX records cues but performs no particle work
- missing Phase18 reveal particles: flip/result burst calls are no-ops
- missing feedback controller scene wiring: vertical slice still runs through existing flow
- null cue anchors: allowed and recorded as null
- invalid intensity fields: clamped by `RitualParticleSystemController.SetIntensity()`

No runtime path should throw because an optional Phase19 visual object is absent.

## Testing

### EditMode

Add `Phase19CardActionVfxIntegrationTests` to verify:

- `RitualActionVfxController` type exists and exposes expected public API
- `RitualFeedbackController` exposes `actionVfxController` and `forwardCuesToActionVfx` serialized fields
- ReadingRoom feedback wires to a `RitualActionVfxController`
- ReadingRoom action VFX references `Phase16_RitualAuraRoot` `RitualParticleSystemController`
- Result feedback wires to a `RitualActionVfxController`
- Result action VFX references `Phase16_ResultAuraRoot` `RitualParticleSystemController`
- Phase18 particle objects and prior Phase13/14/15/16/17 wiring remain intact
- Phase19 documentation exists

### PlayMode

Add `Phase19CardActionVfxPlayModeTests` to verify:

- `RitualActionVfxController` is safe with missing particle controller
- `ShuffleStarted` plays ambient particles
- `CardDealt` keeps ambient/focus particles active
- `CardFlipped` triggers reveal burst
- `ResultReveal` triggers result action VFX safely
- `RitualFeedbackController.PlayCue()` forwards cues to action VFX when enabled
- forwarding can be disabled

PlayMode tests should use manually created `GameObject` instances with `ParticleSystem` components and reflection only for serialized private field setup.

## Documentation

Create:

- `Docs/PHASE19_CARD_ACTION_VFX_INTEGRATION.md`

Update:

- `Docs/UI_COMPLETION_MAINLINE.md`
- `README.md`

Docs must state that Phase19 is action-triggered VFX integration. It is not final high-end VFX, Shader Graph animation, camera choreography, post-processing, release packaging, or broad interface redesign.

## Completion Criteria

Phase19 is complete when:

- `RitualActionVfxController` is null-safe and test-covered
- `RitualFeedbackController` forwards cues without breaking existing audio/legacy particles
- ReadingRoom and Result feedback objects are wired to Phase19 action VFX
- Phase18 particle systems remain intact
- Phase13/14/15/16/17 visual wiring remains intact
- full EditMode and PlayMode tests pass
- `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passes
- Unity log scans show no project-level compiler errors, exceptions, missing scripts, missing references, or LocalKeyword errors
- known Unity licensing/CDN/batchmode shutdown noise is separated from project errors

## Later Work

Later phases can build on Phase19 by adding:

- stronger card trails using dedicated mesh or trail renderers
- camera choreography around reveal moments
- Shader Graph rune animation
- post-processing and final lighting
- screenshot-driven visual polish
- final Hearthstone-like 3D presentation pass
