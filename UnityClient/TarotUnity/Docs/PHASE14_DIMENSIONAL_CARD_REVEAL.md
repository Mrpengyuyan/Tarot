# Phase 14 Dimensional Card Reveal

Date: 2026-06-01

## Scope

Phase 14 implements the selected approach: a 2.5D dimensional card reveal layer for the current Unity tarot vertical slice.

It improves presentation around the real RWS1909 card art from Phase 13 without changing backend flow, scene order, desktop packaging, history, settings, profile, admin, or dashboard features.

## Runtime Changes

- `DimensionalCardRevealController` adds card lift, settle, and glow toggling.
- `CardView` can reference that controller, but remains safe when the reference is missing.
- `DeckController` and `CardArtworkCatalog` keep the Phase 13 card-art flow unchanged.

## Prefab Changes

`PF_TarotCard` gains:

- `Phase14_DimensionalRoot`
- `Phase14_CardEdge`
- `Phase14_CastShadow`
- `Phase14_FaceRimLight`
- `Phase14_ArtworkGlass`
- `Phase14_RevealGlow`

These anchors create visual depth around the existing card art slot.

## Scene Changes

`ReadingRoom` gains:

- `Phase14_TableDepthPlane`
- `Phase14_CardRevealPool`
- `Phase14_RevealLightWarm`

`Result` gains:

- `Phase14_ResultCardHalo`
- `Phase14_ResultCardShadow`
- `Phase14_ResultTextBridge`

## Bootstrap Command

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase14DimensionalCardBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-bootstrap.log
```

## Verification Commands

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-green.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-green-tests.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
