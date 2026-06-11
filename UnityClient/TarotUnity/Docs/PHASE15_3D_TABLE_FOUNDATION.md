# Phase 15 3D Table Foundation

Date: 2026-06-03

## Scope

Phase 15 implements the approved A2 visual direction: a stronger 3D table foundation for the current Unity tarot vertical slice.

It adds card mesh-shell anchors, a 3D ritual table stage, warm/cool light anchors, and restrained Result card-stage support while preserving backend flow, scene order, RWS1909 artwork, and the existing vertical slice.

## Runtime Changes

- `ThreeDCardPresentationController` owns optional card shell visibility.
- `Phase15TableStageController` owns optional table-stage visibility.
- `CardView` can reference the 3D card presentation helper but remains safe when the reference is missing.

## Prefab Changes

`PF_TarotCard` gains:

- `Phase15_CardMeshRoot`
- `Phase15_CardBody`
- `Phase15_CardFacePlane`
- `Phase15_CardBackPlane`
- `Phase15_CardSideEdge`
- `Phase15_CardDropShadow`

## Scene Changes

`ReadingRoom` gains:

- `Phase15_ThreeDTableRoot`
- `Phase15_RitualTableSurface`
- `Phase15_TableDepthRing`
- `Phase15_DeckFocusAnchor`
- `Phase15_SpreadFocusAnchor`
- `Phase15_FlipFocusAnchor`
- `Phase15_WarmKeyLight`
- `Phase15_CoolRimLight`

`Result` gains:

- `Phase15_ResultCardStageRoot`
- `Phase15_ResultCardPedestal`
- `Phase15_ResultFocusAnchor`
- `Phase15_ResultWarmFocusLight`
- `Phase15_ResultCoolEdgeLight`

## Out Of Scope

Phase 15 does not add backend/API changes, history, settings, profile, admin, dashboard work, release zip regeneration, commercial tarot art, or final Hearthstone-level VFX.

## Editor Bootstrap Notes

`Phase15ThreeDTableBootstrapper` is an idempotent Unity Editor utility used to create or refresh Phase15 materials, prefab anchors, and scene anchors. It is not a runtime system and is not included in player builds.

The bootstrapper has already been run for this phase. Rerun it only when Phase15 anchors need to be regenerated, and make sure the Unity Editor is closed before using batchmode.

## Verification

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
