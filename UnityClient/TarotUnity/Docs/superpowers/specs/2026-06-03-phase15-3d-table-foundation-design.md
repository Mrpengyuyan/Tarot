# Phase 15 3D Table Foundation Design

Date: 2026-06-03

## Selected Direction

Phase 15 uses the approved A2 direction: a stronger 3D table foundation.

The goal is to make the current Unity tarot vertical slice read more like a premium tabletop card game. Phase 15 should create the reusable 3D stage that later phases can improve with stronger VFX, custom meshes, and final beauty work.

## Product Goal

The player should feel that the reading happens on a physical ritual table:

- the camera sits lower and closer to the table instead of feeling like a flat UI view
- cards have readable thickness, side edges, front art, and shadows
- the table has depth, perspective, and a ritual surface that supports the cards
- warm key light and cool rim light separate card silhouettes from the background
- the existing vertical slice still runs through `Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`

## Scope

Phase 15 includes:

- adding a reusable 3D card mesh shell around the existing `PF_TarotCard` visual structure
- preserving the Phase 13 RWS1909 sprite artwork mapping
- preserving the Phase 14 `DimensionalCardRevealController` behavior and null-safety
- adding a ReadingRoom 3D table foundation with a ritual table surface, depth ring, and deck/spread staging anchors
- adding stronger warm/cool light anchors for card silhouette separation
- refining camera pose anchors for deck, spread, flip, and result focus
- adding an idempotent editor bootstrapper for Phase 15 scene/prefab anchors
- adding EditMode and PlayMode tests for the new 3D anchors, material settings, camera references, and vertical-slice safety
- updating phase documentation and the UI completion mainline

Phase 15 excludes:

- backend/API changes
- history, settings, profile, admin, or dashboard features
- Windows/macOS build or release zip regeneration
- replacing the current scene flow
- commercial tarot deck art or new online asset source work
- final Hearthstone-level VFX polish
- sculpted custom table assets that require external modeling tools
- replacing the Phase 13 RWS1909 card art source

## Visual Design

Phase 15 should push the card table toward a believable 3D space while staying lightweight enough for the current project.

The ReadingRoom should gain a named 3D stage root:

- `Phase15_ThreeDTableRoot`
- `Phase15_RitualTableSurface`
- `Phase15_TableDepthRing`
- `Phase15_DeckFocusAnchor`
- `Phase15_SpreadFocusAnchor`
- `Phase15_WarmKeyLight`
- `Phase15_CoolRimLight`

The card prefab should gain 3D shell anchors:

- `Phase15_CardMeshRoot`
- `Phase15_CardBody`
- `Phase15_CardFacePlane`
- `Phase15_CardBackPlane`
- `Phase15_CardSideEdge`
- `Phase15_CardDropShadow`

The card art remains the visual payload. The 3D shell should frame and support the existing RWS1909 sprite instead of hiding it.

The result scene should gain a restrained 3D focus layer:

- `Phase15_ResultCardStageRoot`
- `Phase15_ResultCardPedestal`
- `Phase15_ResultWarmFocusLight`
- `Phase15_ResultCoolEdgeLight`

The result copy remains readable. Phase 15 should not turn Result into a pure cinematic scene with unreadable text.

## Interaction Design

The existing gameplay state machine remains unchanged.

Card interaction should still work through the existing `CardClickHandler`, `CardFlipController`, `CardView`, `DeckController`, `ReadingFlowController`, and `ResultPanelPresenter` flow.

Phase 15 may improve presentation by:

- giving the card mesh shell a stable resting pose
- letting the Phase 14 reveal lift operate on the 3D card visual root
- refining camera targets when deck/spread/result focus changes
- adding stronger light and shadow cues when cards are revealed

Phase 15 must not:

- add new draw/interpret states
- change backend request order
- require a new result payload
- make UI controls harder to click
- block the question input, draw button, card clicks, or reveal result button

## Architecture

Runtime code should stay narrow.

Preferred additions:

- `ThreeDCardPresentationController` under `Assets/Scripts/Presentation`
  - owns optional card mesh/root references and visual state only
  - does not own card data, backend calls, or scene loading
- `Phase15TableStageController` under `Assets/Scripts/Presentation`
  - owns optional table stage light/camera support references only
  - can be absent without breaking the vertical slice

Editor automation should live in:

- `Assets/Editor/Phase15ThreeDTableBootstrapper.cs`

The bootstrapper should:

- add or refresh card prefab mesh anchors
- add or refresh ReadingRoom 3D table anchors
- add or refresh Result 3D focus anchors
- create persistent Phase 15 materials under `Assets/Materials`
- avoid duplicate anchors when rerun
- remove generated colliders from visual primitives unless a click target explicitly needs one
- keep UI image raycast targets disabled for purely decorative support layers

## Data Flow

The data flow remains:

1. `DeckController` instantiates `PF_TarotCard`.
2. `CardView.Bind` receives `CardDrawData`.
3. `DeckController` resolves the sprite from `CardArtworkCatalog`.
4. `CardView.SetFaceArtwork` stores the sprite.
5. When the card flips face-up, `CardView` shows the sprite and notifies the optional visual helpers.
6. Result uses the same catalog and session data to show the primary card art.

Phase 15 adds only visual anchors around that flow.

## Error Handling

Phase 15 must be safe with missing visual references:

- missing 3D card presentation helper: card still flips
- missing table stage controller: scene still plays
- missing light anchors: scene still plays
- missing card sprite: existing labels and fallback behavior remain
- missing Phase 15 anchors: tests catch the issue before completion

No runtime path should throw because an optional Phase 15 visual object is absent.

## Testing

Add or update EditMode tests to verify:

- `ThreeDCardPresentationController` type exists if runtime helper is added
- `Phase15TableStageController` type exists if runtime helper is added
- `PF_TarotCard` contains Phase 15 3D card shell anchors
- the Phase 15 card shell keeps Phase 13 face-art wiring intact
- Phase 14 `DimensionalCardRevealController.cardRoot` remains targeted at the intended card visual root
- Phase 15 materials use appropriate URP transparent/opaque settings for their purpose
- ReadingRoom contains Phase 15 table stage anchors and lights
- Result contains Phase 15 result card focus anchors and lights
- decorative UI layers do not block raycasts
- the Phase 13 artwork catalog still has 78 entries

Add or update PlayMode tests to verify:

- `MainMenuToResultVerticalSliceRuns` still passes
- the card reveal path still flips and reaches Result
- Phase 15 optional presentation helpers do not throw when references are absent
- the Phase 14 reveal behavior still triggers only on face-up transition

Final verification must include:

- Unity batchmode compile/log check
- project error scan
- full EditMode tests
- full PlayMode tests
- scene/prefab missing script and missing reference scan
- vertical-slice verification

## Acceptance Criteria

Phase 15 is complete only when:

- ReadingRoom has a visibly stronger 3D table foundation with named table, ring, lighting, deck, and spread anchors
- `PF_TarotCard` has a 3D card shell with body, face, back, side edge, and shadow anchors
- the existing RWS1909 artwork remains visible on revealed cards
- the Phase 14 reveal behavior remains intact
- Result has a restrained 3D card focus layer without sacrificing text readability
- no backend, release, history, settings, profile, admin, or dashboard work is added
- EditMode and PlayMode tests pass
- Unity logs contain no project compile/runtime errors

Known external Unity Licensing and shutdown noise remains acceptable if it matches prior runs and no project error is present.

## Remaining Limitations

Phase 15 is a foundation pass, not the final premium card-game presentation.

Later phases can add:

- custom sculpted card meshes
- higher-end magical VFX
- particle systems
- cinematic camera shake or depth-of-field
- final audio/lighting polish
- a wider full-interface redesign once the 3D table foundation is stable
