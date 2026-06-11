# Phase 16 Ritual Aura VFX Design

Date: 2026-06-04

## Approved Direction

Phase 16 uses the approved B2 direction: `Ritual Rune Aura`.

The goal is to make the existing Phase15 3D table foundation feel more like an active tarot ritual space. Phase 16 adds a controlled, lightweight aura layer around the card table and result card stage without hiding the real RWS1909 tarot card artwork or blocking the current vertical slice.

The visual priority is atmosphere, not a complete final VFX overhaul.

## Player Experience

The player should feel that the table responds to the reading.

During the ReadingRoom flow:

- the table center has a soft magical glow pool
- a restrained rune ring frames the card reveal area
- small particle anchor points imply motion and ritual energy
- the card remains the highest-contrast subject
- question input, spread controls, and result navigation remain readable and clickable

During the Result scene:

- the primary card area has a matching but calmer aura stage
- result text stays readable
- the aura supports the interpretation moment without turning Result into a pure cinematic screen

## Scope

Phase 16 includes:

- adding a `RitualAuraController` runtime helper under `Assets/Scripts/Presentation`
- adding ReadingRoom Phase16 aura anchors under the Phase15 table area
- adding Result Phase16 aura anchors around the result card stage
- creating persistent Phase16 materials under `Assets/Materials`
- adding an idempotent Unity Editor bootstrapper to create or refresh Phase16 materials, scene anchors, and controller wiring
- adding EditMode tests for Phase16 anchors, materials, non-blocking UI behavior, and Phase13/14/15 retention
- adding PlayMode tests that prove Phase16 helpers are null-safe at runtime
- documenting Phase16 in the UI completion mainline and a phase doc

Phase 16 excludes:

- backend/API changes
- history, settings, profile, admin, or dashboard work
- desktop build regeneration or release zip work
- replacing or re-sourcing the 78 RWS1909 tarot card images
- final Hearthstone-level particle systems, shader graph work, post-processing, depth of field, camera shake, or cinematic camera rails
- broad UI redesign outside the aura support layer

## Scene Design

### ReadingRoom

ReadingRoom gains a named aura root:

- `Phase16_RitualAuraRoot`

The root contains:

- `Phase16_GlowPool`
- `Phase16_RuneRingOuter`
- `Phase16_RuneRingInner`
- `Phase16_ParticleAnchorNorth`
- `Phase16_ParticleAnchorEast`
- `Phase16_ParticleAnchorSouth`
- `Phase16_ParticleAnchorWest`
- `Phase16_AuraFocusAnchor`

The aura root should sit visually above the Phase15 ritual table surface and below the card readability layer. It must not contain colliders or UI raycast targets.

The aura should be centered around the current deck/spread/flip area, not around UI panels.

### Result

Result gains a matching but restrained root:

- `Phase16_ResultAuraRoot`

The root contains:

- `Phase16_ResultGlowPool`
- `Phase16_ResultRuneRing`
- `Phase16_ResultParticleAnchorLeft`
- `Phase16_ResultParticleAnchorRight`
- `Phase16_ResultAuraFocusAnchor`

The Result aura must preserve text readability and should not cover the main interpretation text.

## Prefab Design

Phase 16 does not need a new card prefab.

`PF_TarotCard` may receive a lightweight optional reference to `RitualAuraController` only if the implementation needs card-state-driven aura activation. If the card prefab is touched, the Phase13 face artwork slot, Phase14 dimensional reveal wiring, and Phase15 3D card shell wiring must remain intact.

## Runtime Components

### RitualAuraController

`RitualAuraController` owns optional presentation references only.

It should provide:

- `SetAuraVisible(bool visible)`
- `SetRuneVisible(bool visible)`
- `SetParticlesVisible(bool visible)`
- `SetIntensity(float intensity)`

All references are optional. Missing roots, renderers, particle anchors, or lights must not throw.

The component does not own:

- card draw data
- backend calls
- scene loading
- result interpretation data
- audio playback
- final particle simulation

## Materials

Phase16 materials should use URP-compatible settings and stay lightweight.

Required materials:

- `MAT_Phase16_AuraGlowPool`
- `MAT_Phase16_RuneRing`
- `MAT_Phase16_AuraParticle`

Expected material behavior:

- transparent surface settings
- alpha blending
- no depth write
- render queue in the transparent range
- warm gold and controlled teal-green colors

The materials should not dominate the scene or create a one-note color palette. They should support the existing Phase8/Phase15 gold, green, and warm/cool lighting palette.

## Data Flow

The gameplay flow remains unchanged:

1. Player starts from Main Menu.
2. Player enters ReadingRoom.
3. Player selects a spread and asks a question.
4. Deck and card controllers draw cards.
5. Card flip shows real RWS1909 artwork.
6. ReadingFlowController transitions to Result.
7. ResultPresenter displays interpretation data.

Phase 16 adds visual support around steps 3 through 7. It must not alter backend mode, record creation, card draw mapping, interpretation, scene order, or result data.

## Safety And Error Handling

Phase 16 must be safe with missing visual references:

- missing `RitualAuraController`: vertical slice still runs
- missing aura root: tests catch missing scene structure before completion
- missing renderer: controller method returns without exception
- missing particle anchor: controller method returns without exception
- missing material: EditMode tests catch it before completion

No runtime path should throw because an optional Phase16 visual object is absent.

## Testing Strategy

### EditMode

Add `Phase16RitualAuraVfxTests` to verify:

- `RitualAuraController` type exists and exposes the expected public methods
- ReadingRoom contains `Phase16_RitualAuraRoot` and all required child anchors
- Result contains `Phase16_ResultAuraRoot` and all required child anchors
- Phase16 materials exist and use transparent render settings
- Phase16 scene visual objects do not block UI raycasts
- Phase13 RWS1909 catalog remains available with 78 entries
- `PF_TarotCard` retains Phase13 artwork, Phase14 dimensional reveal, and Phase15 3D shell wiring
- Phase16 documentation exists

### PlayMode

Add `Phase16RitualAuraVfxPlayModeTests` to verify:

- `RitualAuraController` methods are safe when references are absent
- runtime visibility toggles activate and deactivate assigned aura objects
- intensity clamping does not throw and does not require materials to be present

### End Check

The phase is complete only after:

- Unity compile/log check passes
- full EditMode tests pass
- full PlayMode tests pass
- scene/prefab missing reference scan is clean
- vertical slice test `MainMenuToResultVerticalSliceRuns` passes
- scope review confirms no backend/history/settings/profile/admin/dashboard/release package work was added

Known external Unity licensing or shutdown noise can be reported separately if it does not indicate project failure.

## Documentation

Create:

- `Docs/PHASE16_RITUAL_AURA_VFX.md`

Update:

- `Docs/UI_COMPLETION_MAINLINE.md`
- `README.md`

The docs should state that Phase16 is a controlled atmosphere pass. It is not final high-end VFX, shader graph, camera shake, post-processing, or full interface redesign.

## Completion Criteria

Phase 16 is complete when:

- ReadingRoom has a visible ritual aura foundation with glow, rune ring, particle anchors, and an aura focus anchor
- Result has a matching restrained aura support layer
- `RitualAuraController` is null-safe and controls optional aura presentation references
- Phase16 materials are persistent and test-covered
- existing card artwork, dimensional reveal, 3D card shell, backend flow, and vertical slice remain intact
- EditMode and PlayMode tests pass
- the established end-check flow passes

## Future Work

Later phases can build on Phase16 by adding:

- real particle systems
- shader graph rune animation
- card-trail motion during deal and flip
- camera choreography and depth-of-field
- final VFX/audio polish
- broader UI panel integration once the aura layer is stable
