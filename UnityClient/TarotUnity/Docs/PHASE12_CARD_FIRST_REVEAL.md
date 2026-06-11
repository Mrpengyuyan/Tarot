# Phase 12 Card-First Reveal

Date: 2026-05-28

## Scope

Phase 12 implements the approved card-first reveal direction for the current Unity tarot vertical slice. It prepares durable scene and prefab anchors for card face artwork, reveal staging, and result-card presence while keeping the existing gameplay and text interpretation flow intact.

Phase 12 adds a durable face-art slot and card-first reveal staging, but it does not import the final tarot deck artwork. The real deck task still requires source discovery, license review, texture import settings, and card-name mapping.

## Approved Direction B

Direction B puts the card face first: the ReadingRoom should stage the reveal around the selected card, and the Result scene should preserve a visible card showcase beside the interpretation text. Text remains important, but it now supports the card moment instead of replacing it.

## Bootstrap Changes

- Card prefab: adds `Front/Phase12_FaceArtworkFrame`, `Front/Phase12_FaceArtworkPlaceholder`, and `Front/Phase12_FaceArtworkPlaceholder/Phase12_FaceArtworkLabel`; assigns `CardView.faceArtworkRenderer` to the placeholder `SpriteRenderer`; leaves `TitleLabel` and `PositionLabel` as fallback labels.
- ReadingRoom: adds `Phase12_CardRevealStage`, `Phase12_RevealBackdrop`, `Phase12_FocusedCardLight`, `Phase12_CardFocusVignette`, and `Phase12_RevealInstruction` with the copy `点击牌面，揭开此刻的讯息`.
- Result: adds `Phase12_ResultCardShowcase`, `Phase12_ResultCardPlaceholder`, and `Phase12_ResultCardArtworkSlot`; preserves result text fields and keeps `OverallText` wide enough for reading copy.

## How To Run The Bootstrapper

In the Unity Editor, run `Tools/Tarot Unity/Run Phase 12 Card Reveal Bootstrap`.

Batchmode example, only when no Unity Editor lock is held:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase12CardRevealBootstrapper.Run -quit
```

## How To Run Tests

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase12CardFirstRevealTests -testResults TestResults/phase12-editmode.xml
```

Do not use `-nographics` for visual screenshot capture workflows; Phase 12 tests themselves are EditMode asset and scene checks.
