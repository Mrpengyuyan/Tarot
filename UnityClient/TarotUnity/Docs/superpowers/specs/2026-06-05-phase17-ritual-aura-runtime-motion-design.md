# Phase 17 Ritual Aura Runtime Motion Design

Date: 2026-06-05

## Approved Direction

Phase 17 uses the approved approach A: a lightweight runtime motion pass for the Phase16 ritual aura layer.

The goal is to make the existing aura anchors feel alive without starting a broad VFX overhaul. Phase17 should add slow rune-ring rotation, subtle glow pulsing, and small particle-anchor drift around the current Phase15 table and Phase16 aura roots.

## Scope

Phase17 includes:

- adding a null-safe `RitualAuraMotionController` under `Assets/Scripts/Presentation`
- wiring the controller onto `Phase16_RitualAuraRoot` in `ReadingRoom`
- wiring the controller onto `Phase16_ResultAuraRoot` in `Result`
- adding an idempotent Unity Editor bootstrapper to wire the controller to existing Phase16 anchors
- adding EditMode tests for runtime type, scene wiring, Phase16 retention, and documentation
- adding PlayMode tests for null safety, deterministic tick behavior, pause behavior, and reset behavior
- documenting Phase17 in a phase doc, `README.md`, and the UI completion mainline

Phase17 excludes:

- backend/API changes
- history, settings, profile, admin, or dashboard work
- desktop build or release zip regeneration
- new tarot card artwork
- final particle systems
- Shader Graph rune animation
- post-processing or camera shake
- broad UI layout redesign
- replacing the Phase15 card/table foundation

## Runtime Component

### `RitualAuraMotionController`

`RitualAuraMotionController` owns optional presentation transforms only.

Serialized references:

- `RitualAuraController auraController`
- `Transform motionRoot`
- `Transform[] glowPulsers`
- `Transform[] runeRings`
- `Transform[] particleAnchors`
- `bool animateOnEnable`
- `float runeRotationSpeedDegrees`
- `float pulseSpeed`
- `float pulseAmplitude`
- `float particleFloatSpeed`
- `float particleFloatAmplitude`

Public state and methods:

- `bool IsAnimating { get; private set; }`
- `float CurrentPulse { get; private set; }`
- `void SetAnimating(bool animating)`
- `void Tick(float deltaSeconds, float elapsedSeconds)`
- `void ResetMotion()`

Runtime behavior:

- `OnEnable()` captures base glow scales and particle positions, then applies `animateOnEnable`.
- `Update()` calls `Tick(Time.deltaTime, Time.time)` only when `IsAnimating` is true.
- `Tick()` rotates non-null rune transforms by a small amount each frame.
- `Tick()` scales glow pulsers around their captured base scale using a sine pulse.
- `Tick()` moves particle anchors around their captured base local position using a subtle vertical sine offset.
- `ResetMotion()` restores captured glow scales and particle positions and resets `CurrentPulse` to `1`.

All arrays and individual references are optional. Missing references must not throw and must not stop the vertical slice.

## Scene Wiring

### ReadingRoom

`Phase17RitualAuraMotionBootstrapper` wires `RitualAuraMotionController` onto:

- `Phase16_RitualAuraRoot`

ReadingRoom references:

- `auraController`: `Phase16_RitualAuraRoot` `RitualAuraController`
- `motionRoot`: `Phase16_RitualAuraRoot.transform`
- `glowPulsers`: `Phase16_GlowPool`
- `runeRings`: `Phase16_RuneRingOuter`, `Phase16_RuneRingInner`
- `particleAnchors`: `Phase16_ParticleAnchorNorth`, `Phase16_ParticleAnchorEast`, `Phase16_ParticleAnchorSouth`, `Phase16_ParticleAnchorWest`

Suggested ReadingRoom values:

- `animateOnEnable`: true
- `runeRotationSpeedDegrees`: 7.5
- `pulseSpeed`: 0.55
- `pulseAmplitude`: 0.08
- `particleFloatSpeed`: 0.75
- `particleFloatAmplitude`: 0.018

### Result

`Phase17RitualAuraMotionBootstrapper` wires `RitualAuraMotionController` onto:

- `Phase16_ResultAuraRoot`

Result references:

- `auraController`: `Phase16_ResultAuraRoot` `RitualAuraController`
- `motionRoot`: `Phase16_ResultAuraRoot.transform`
- `glowPulsers`: `Phase16_ResultGlowPool`
- `runeRings`: `Phase16_ResultRuneRing`
- `particleAnchors`: `Phase16_ResultParticleAnchorLeft`, `Phase16_ResultParticleAnchorRight`

Suggested Result values:

- `animateOnEnable`: true
- `runeRotationSpeedDegrees`: 4.5
- `pulseSpeed`: 0.45
- `pulseAmplitude`: 0.06
- `particleFloatSpeed`: 0.65
- `particleFloatAmplitude`: 0.012

## Data Flow

The existing gameplay flow remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

Phase17 does not enter the backend or card-selection flow.

Runtime flow:

1. Scene loads.
2. Phase16 aura root enables.
3. `RitualAuraMotionController.OnEnable()` captures base transform state.
4. `Update()` calls `Tick()` while animation is enabled.
5. The aura visual transforms move subtly in place.
6. Card draw, flip, interpretation, and result reveal continue through the existing controllers.

## Error Handling

Phase17 must be safe with missing visual references:

- missing `RitualAuraMotionController`: vertical slice still runs
- missing `RitualAuraController`: motion controller still ticks transforms
- missing glow transforms: no pulse is applied
- missing rune transforms: no rotation is applied
- missing particle anchors: no drift is applied
- empty arrays: no exception
- null entries inside arrays: skipped

No runtime path should throw because an optional Phase17 visual object is absent.

## Testing

### EditMode

Add `Phase17RitualAuraRuntimeMotionTests` to verify:

- `RitualAuraMotionController` type exists and exposes the expected public API
- `ReadingRoom` has motion wiring on `Phase16_RitualAuraRoot`
- `Result` has motion wiring on `Phase16_ResultAuraRoot`
- Phase16 aura anchors and transparent materials remain available
- Phase13 card artwork catalog remains 78 entries
- Phase14 and Phase15 card prefab wiring remains intact
- Phase17 documentation exists

### PlayMode

Add `Phase17RitualAuraRuntimeMotionPlayModeTests` to verify:

- `RitualAuraMotionController` is safe with missing references
- `Tick()` rotates assigned rune transforms
- `Tick()` pulses assigned glow transforms
- `Tick()` floats assigned particle anchors
- `SetAnimating(false)` prevents motion
- `ResetMotion()` restores base glow scale and particle position

PlayMode tests should use manually created `GameObject` and `Transform` instances, not primitives that add colliders.

## Documentation

Create:

- `Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md`

Update:

- `Docs/UI_COMPLETION_MAINLINE.md`
- `README.md`

Docs must state that Phase17 is a controlled runtime motion pass. It is not final high-end VFX, true particle systems, Shader Graph animation, post-processing, camera shake, or broad interface redesign.

## Completion Criteria

Phase17 is complete when:

- `RitualAuraMotionController` is null-safe and test-covered
- ReadingRoom and Result Phase16 roots have Phase17 motion wiring
- Phase16 anchor/material tests still pass
- Phase13/14/15 visual wiring remains intact
- full EditMode and PlayMode tests pass
- `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passes
- Unity log scans show no project-level compiler errors, exceptions, missing scripts, missing references, or LocalKeyword errors
- known Unity licensing/CDN/batchmode shutdown noise is separated from project errors

## Later Work

Later phases can build on Phase17 by adding:

- real ParticleSystem effects
- Shader Graph rune animation
- card trails during deal and flip
- camera choreography around reveal moments
- post-processing and final lighting
- screenshot-driven visual polish

