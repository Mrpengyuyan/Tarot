# Phase 18 Ritual Particle Systems Design

Date: 2026-06-05

## Approved Direction

Phase18 adds the first real Unity `ParticleSystem` layer on top of the Phase16 ritual aura anchors and Phase17 runtime aura motion.

The goal is to make the ReadingRoom and Result scenes feel more like a premium 3D card ritual without redesigning the whole interface. The particles should be subtle, readable, and safe: visible enough to improve atmosphere, but never strong enough to hide card artwork, block UI, or break the vertical slice.

## Scope

Phase18 includes:

- adding a null-safe `RitualParticleSystemController` under `Assets/Scripts/Presentation`
- creating an idempotent `Phase18RitualParticleSystemBootstrapper`
- wiring real `ParticleSystem` objects under existing Phase16 particle/focus anchors in `ReadingRoom`
- wiring real `ParticleSystem` objects under existing Phase16 result particle/focus anchors in `Result`
- adding ReadingRoom particles for ambient dust, deck focus, and flip sparkle
- adding Result particles for card-focus motes and interpretation glow
- adding EditMode tests for runtime type, scene wiring, particle settings, prior phase retention, and documentation
- adding PlayMode tests for null safety, play/stop controls, intensity application, and deterministic custom tick behavior
- documenting Phase18 in a phase doc, `README.md`, and the UI completion mainline

Phase18 excludes:

- backend/API changes
- history, settings, profile, admin, or dashboard work
- desktop build or release zip regeneration
- new tarot card artwork
- Shader Graph rune animation
- post-processing
- camera choreography
- broad UI layout redesign
- replacing the Phase15 card/table foundation

## Runtime Component

### `RitualParticleSystemController`

`RitualParticleSystemController` owns optional particle references only.

Serialized references:

- `RitualAuraController auraController`
- `ParticleSystem[] ambientParticles`
- `ParticleSystem[] focusParticles`
- `ParticleSystem[] revealParticles`
- `bool playOnEnable`
- `float baseEmissionMultiplier`
- `float focusEmissionMultiplier`
- `float revealEmissionMultiplier`
- `float intensity`

Public state and methods:

- `bool IsPlaying { get; private set; }`
- `float CurrentIntensity { get; private set; }`
- `void SetParticlesVisible(bool visible)`
- `void SetIntensity(float value)`
- `void PlayAmbient()`
- `void StopAll(bool clear)`
- `void TriggerRevealBurst()`
- `void SimulateTick(float deltaSeconds)`

Runtime behavior:

- `OnEnable()` applies the configured intensity and plays ambient particles only when `playOnEnable` is true.
- `SetParticlesVisible(true)` enables particle GameObjects and resumes ambient/focus particles.
- `SetParticlesVisible(false)` stops all configured particles without destroying them.
- `SetIntensity()` clamps to `[0, 1]` and updates emission rates for all configured systems.
- `PlayAmbient()` plays ambient and focus particle systems.
- `StopAll(clear)` stops every configured particle system and optionally clears them.
- `TriggerRevealBurst()` emits a small burst from reveal particle systems.
- `SimulateTick()` advances all configured particle systems in tests and controlled runtime scenarios.

All arrays and individual references are optional. Missing particle systems must not throw and must not stop card flip, result reveal, or scene navigation.

## Scene Wiring

### ReadingRoom

`Phase18RitualParticleSystemBootstrapper` wires `RitualParticleSystemController` onto:

- `Phase16_RitualAuraRoot`

ReadingRoom particle children:

- `Phase18_AmbientDustParticles`
- `Phase18_DeckFocusParticles`
- `Phase18_FlipSparkParticles`

Preferred parents:

- `Phase18_AmbientDustParticles`: `Phase16_ParticleAnchorNorth`
- `Phase18_DeckFocusParticles`: `Phase16_ParticleAnchorEast`
- `Phase18_FlipSparkParticles`: `Phase16_AuraFocusAnchor`

Suggested ReadingRoom behavior:

- ambient dust loops slowly above the 3D table
- deck focus particles loop near the deck/spread region
- flip spark particles stay ready for reveal bursts
- emission remains low so real tarot art stays readable

### Result

`Phase18RitualParticleSystemBootstrapper` wires `RitualParticleSystemController` onto:

- `Phase16_ResultAuraRoot`

Result particle children:

- `Phase18_ResultCardMotes`
- `Phase18_ResultInterpretationGlow`

Preferred parents:

- `Phase18_ResultCardMotes`: `Phase16_ResultParticleAnchorLeft`
- `Phase18_ResultInterpretationGlow`: `Phase16_ResultAuraFocusAnchor`

Suggested Result behavior:

- result card motes loop around the card-stage region
- interpretation glow loops softly behind the result aura
- emission is lower than ReadingRoom to protect text readability

## Particle Settings

All Phase18 particles use built-in Unity `ParticleSystem` modules and default material support, not custom shaders.

ReadingRoom defaults:

- ambient dust: looping, low emission, small particles, slow upward drift
- deck focus: looping, low emission, tighter shape radius
- flip spark: non-looping, small burst capacity

Result defaults:

- card motes: looping, low emission, small particles around card focus
- interpretation glow: looping, very low emission and larger soft particles

The bootstrapper must remove colliders from generated visual objects and must not add UI `Graphic` components or raycast blockers.

## Data Flow

The existing gameplay flow remains:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

Phase18 does not enter the backend, record, interpretation, or card-selection flow.

Runtime flow:

1. Scene loads.
2. Phase16 aura root enables.
3. `RitualParticleSystemController.OnEnable()` applies intensity and optionally starts ambient particles.
4. Phase17 transform motion continues independently.
5. Card draw, flip, interpretation, and result reveal continue through existing controllers.
6. Existing or future reveal code can call `TriggerRevealBurst()` without requiring any particle reference to exist.

## Error Handling

Phase18 must be safe with missing optional visual references:

- missing `RitualParticleSystemController`: vertical slice still runs
- missing `RitualAuraController`: particle controller still controls particles
- missing ambient particles: focus/reveal particles still work
- missing focus particles: ambient/reveal particles still work
- missing reveal particles: `TriggerRevealBurst()` is a no-op
- empty arrays: no exception
- null entries inside arrays: skipped
- invalid intensity values: clamped to `[0, 1]`

No runtime path should throw because an optional Phase18 visual object is absent.

## Testing

### EditMode

Add `Phase18RitualParticleSystemTests` to verify:

- `RitualParticleSystemController` type exists and exposes the expected public API
- `ReadingRoom` has Phase18 particle wiring on `Phase16_RitualAuraRoot`
- `Result` has Phase18 particle wiring on `Phase16_ResultAuraRoot`
- configured particle systems have safe loop, duration, start size, lifetime, and emission ranges
- Phase16 aura anchors remain available
- Phase17 motion wiring remains available
- Phase13 card artwork catalog remains 78 entries
- Phase14 and Phase15 card prefab wiring remains intact
- Phase18 documentation exists

### PlayMode

Add `Phase18RitualParticleSystemPlayModeTests` to verify:

- `RitualParticleSystemController` is safe with missing references
- `SetIntensity()` clamps values and changes emission rates
- `PlayAmbient()` starts configured ambient/focus particles
- `StopAll(true)` stops and clears configured particles
- `TriggerRevealBurst()` emits reveal particles without requiring ambient particles
- `SimulateTick()` advances assigned particles without relying on scene objects

PlayMode tests should use manually created `GameObject` instances with `ParticleSystem` components.

## Documentation

Create:

- `Docs/PHASE18_RITUAL_PARTICLE_SYSTEMS.md`

Update:

- `Docs/UI_COMPLETION_MAINLINE.md`
- `README.md`

Docs must state that Phase18 is the first true particle-system layer. It is not final high-end VFX, Shader Graph animation, post-processing, camera choreography, release packaging, or broad interface redesign.

## Completion Criteria

Phase18 is complete when:

- `RitualParticleSystemController` is null-safe and test-covered
- ReadingRoom has Phase18 particle systems under existing `Phase16_RitualAuraRoot` anchors
- Result has Phase18 particle systems under existing `Phase16_ResultAuraRoot` anchors
- Phase16 anchors and Phase17 motion wiring remain intact
- Phase13/14/15 visual wiring remains intact
- full EditMode and PlayMode tests pass
- `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passes
- Unity log scans show no project-level compiler errors, exceptions, missing scripts, missing references, or LocalKeyword errors
- known Unity licensing/CDN/batchmode shutdown noise is separated from project errors

## Later Work

Later phases can build on Phase18 by adding:

- card trails during deal and flip
- Shader Graph rune animation
- camera choreography around reveal moments
- post-processing and final lighting
- screenshot-driven visual polish
- final Hearthstone-like 3D presentation pass
