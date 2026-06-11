# Phase 8 Visual Identity

Date: 2026-05-26

## Direction

Phase 8 continues from the Phase 7 `Immersive Ritual Desktop` direction.

The goal is to make the current Unity interface stop reading as generated UI panels and start reading as a tarot game table with a coherent visual identity.

## Scope

Included:

- Main Menu visual composition
- Reading Room table and control framing
- Result reading frame
- Card face and back ornamentation
- Theme palette refinement
- Automated EditMode coverage for the visual identity anchors

Excluded:

- history page
- settings page
- account/profile page
- admin/backend tools
- new backend API scope

## Visual Language

- Deep velvet shadows for the room.
- Dark green table cloth to avoid a one-hue purple interface.
- Moon-gold accents for borders, crests, rings, and primary calls to action.
- Warm candle amber for the table atmosphere.
- Ivory card faces with ink and gold framing.

## Acceptance Criteria

- `MainMenu` contains Phase 8 visual identity anchors and a stronger first impression.
- `ReadingRoom` contains table, deck focus, question panel, spread controls, and card slot visual anchors.
- `Result` contains a scroll-like oracle reading panel and gold dividers.
- `PF_TarotCard` has readable Phase 8 face and back details.
- `TarotUiTheme` exposes and applies a richer palette instead of a single purple family.
- Full EditMode and PlayMode checks still pass after the bootstrapper runs.

## Implementation

Implemented in:

- `Assets/Editor/Phase8VisualIdentityBootstrapper.cs`
- `Assets/Tests/EditMode/Phase8VisualIdentityTests.cs`
- `Assets/Scripts/UI/TarotUiTheme.cs`
- `Assets/Art/UI/TX_Phase8_TableWeave.png`
- `Assets/Materials/MAT_Phase8_CardIvory.mat`
- `Assets/Materials/MAT_Phase8_CardInk.mat`
- `Assets/Materials/MAT_Phase8_EdgeGold.mat`
- `Assets/Materials/MAT_Phase8_TableGreen.mat`
- `Assets/Materials/MAT_Phase8_CandleAmber.mat`
- `Assets/Materials/MAT_Phase8_ShadowGlass.mat`

Regenerate the Phase 8 scene and prefab pass with:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -enableUnityConnectPrefs false \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase8VisualIdentityBootstrapper.Run \
  -quit
```

## Latest Verification

- Phase 8 RED check: 5 failed before implementation.
- Phase 8 GREEN check: 5 passed after implementation.
- Full EditMode: 34 passed, 0 failed.
- Full PlayMode: 2 passed, 0 failed.
- Backend tests: 161 passed, 1 third-party warning.
- Windows desktop build: passed.
- macOS desktop build: passed.
- Windows release zip integrity: passed.
