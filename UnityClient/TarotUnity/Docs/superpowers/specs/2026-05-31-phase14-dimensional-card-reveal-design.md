# Phase 14 Dimensional Card Reveal Design

Date: 2026-05-31

## Selected Direction

Phase 14 uses方案 A: a 2.5D dimensional card reveal layer.

The goal is to make the current vertical slice feel more like a premium tabletop card game without destabilizing the working flow. Phase 14 improves the card object, reveal staging, and result payoff around the real RWS1909 card art added in Phase 13.

## Product Goal

The player should feel that the cards have physical presence:

- cards read as objects on a table, not flat UI images
- flip reveal has weight, lift, and a clear face-art payoff
- the result screen visually starts from the card, then supports it with text
- the whole flow still runs through `Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`

## Scope

Phase 14 includes:

- upgrading `PF_TarotCard` with dimensional visual layers
- adding card edge, shadow, highlight, and art-frame anchors
- adding a small runtime component for reveal lift/settle presentation
- adding ReadingRoom reveal-stage polish around the dealt cards
- adding Result card-focus polish around the primary card art
- automated EditMode coverage for prefab and scene anchors
- PlayMode verification of the existing vertical slice

Phase 14 excludes:

- full custom 3D mesh card modeling
- final Hearthstone-like table overhaul
- new backend endpoints
- history, settings, profile, admin, or dashboard work
- new downloadable build generation unless a later release phase asks for it
- replacing the RWS1909 asset source

## Visual Design

`PF_TarotCard` remains compatible with the existing `CardView`, `CardFlipController`, and `DeckController` wiring. The prefab gains named child anchors:

- `Phase14_DimensionalRoot`
- `Phase14_CardEdge`
- `Phase14_CastShadow`
- `Phase14_FaceRimLight`
- `Phase14_ArtworkGlass`
- `Phase14_RevealGlow`

These layers should be subtle. The card art remains readable and is not hidden by decoration. The reveal glow appears only as support around a revealed card, not as a permanent bright overlay.

## Interaction Design

Phase 14 adds a small presentation component, `DimensionalCardRevealController`, that can:

- remember a resting local position and scale
- lift a card slightly during reveal
- settle it back to the table
- toggle a glow renderer safely

The component does not own card draw data, backend calls, scene loading, or result state. It is a visual helper that `CardView` or the flip path can use without changing the gameplay state machine.

If the visual helper is missing, the vertical slice must still run with the existing card flip behavior.

## Scene Design

ReadingRoom gains non-blocking polish anchors:

- `Phase14_TableDepthPlane`
- `Phase14_CardRevealPool`
- `Phase14_RevealLightWarm`

These anchors sit behind or around the card area and must not block buttons, question input, or click targets.

Result gains non-blocking polish anchors:

- `Phase14_ResultCardHalo`
- `Phase14_ResultCardShadow`
- `Phase14_ResultTextBridge`

The text layout remains readable. The primary card image becomes the emotional entry point, but the result copy remains the information payload.

## Architecture

Runtime code stays narrow:

- `DimensionalCardRevealController` lives under `Assets/Scripts/Presentation`.
- `CardView` may hold an optional reference to the controller and notify it when face state changes.
- Existing card art mapping remains in `CardArtworkCatalog`.
- Existing backend and session models remain unchanged.

Editor automation lives in:

- `Assets/Editor/Phase14DimensionalCardBootstrapper.cs`

The bootstrapper may create or update prefab/scene anchors and materials. It must be idempotent so it can be re-run after scene edits.

## Data Flow

The data flow remains:

1. `DeckController` instantiates `PF_TarotCard`.
2. `CardView.Bind` receives `CardDrawData`.
3. `DeckController` resolves the sprite from `CardArtworkCatalog`.
4. `CardView.SetFaceArtwork` stores the sprite.
5. When the card flips face-up, `CardView` shows the sprite and notifies the dimensional reveal helper.
6. Result uses the same catalog and session data to show the first drawn card.

## Error Handling

Phase 14 must be safe with missing visual references:

- missing dimensional helper: card still flips
- missing glow renderer: no exception
- missing art sprite: existing labels remain visible
- missing scene polish anchor: tests catch the missing anchor before completion

No runtime path should throw because an optional Phase14 visual object is absent.

## Testing

Add or update EditMode tests to verify:

- `DimensionalCardRevealController` type exists
- `PF_TarotCard` contains the Phase14 dimensional anchors
- `CardView` keeps optional reveal-helper references null-safe
- `ReadingRoom` contains Phase14 reveal-stage anchors
- `Result` contains Phase14 card-focus anchors
- Phase13 card artwork catalog and scene wiring remain intact

Run final verification:

- Unity batchmode compile/log check
- full EditMode tests
- full PlayMode tests
- missing script/reference scan
- vertical-slice PlayMode test for the main flow

## Acceptance Criteria

Phase 14 is complete only when:

- the card prefab has visible dimensional structure around the real card art
- reveal helper behavior is null-safe and covered by tests
- ReadingRoom and Result contain the Phase14 visual anchors
- Phase13 real card art still appears on card faces and Result showcase
- EditMode and PlayMode tests pass
- Unity logs contain no project compile/runtime errors

Known external Unity Licensing and shutdown noise remains acceptable if it matches prior runs and no project error is present.
