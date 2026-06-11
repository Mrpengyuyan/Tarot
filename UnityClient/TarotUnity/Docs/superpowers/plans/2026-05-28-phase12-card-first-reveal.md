# Phase 12 Card-First Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the approved Phase 12 direction, **B. Card-First Reveal**, into a tested Unity implementation pass that makes the revealed tarot cards the visual center of ReadingRoom and Result while keeping the current vertical slice playable.

**Architecture:** Use the existing Unity runtime boundaries. Add a small optional face-art presentation surface to `CardView`, then apply deterministic prefab and scene changes through a Phase 12 editor bootstrapper. Keep backend and gameplay flow untouched except for safe presentation hooks.

**Tech Stack:** Unity 6.3 LTS, C#, NUnit EditMode tests, Unity PlayMode tests, Unity Editor batchmode automation, existing scenes and prefabs under `Assets/`.

---

## Constraints And Scope

- Keep the vertical slice focused on `Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result`.
- Do not add history, settings, profile, admin, or backend dashboard features.
- Do not import real tarot deck images in Phase 12. The real deck task remains source discovery, license review, import settings, and card-name mapping.
- Keep the Phase 11 screenshot artifacts and tests intact.
- Unity Licensing and UnityConnect network noise can be ignored only after confirming there are no compile errors, test failures, missing references, or runtime exceptions caused by this work.
- There is no `.git` directory inside the current Unity project, so commit checkpoints are documented as review checkpoints unless a repository is initialized before execution.

## Files To Create

- `Assets/Tests/EditMode/Phase12CardFirstRevealTests.cs`
- `Assets/Editor/Phase12CardRevealBootstrapper.cs`
- `Docs/PHASE12_CARD_FIRST_REVEAL.md`

## Files To Modify

- `Assets/Scripts/Gameplay/CardView.cs`
- `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- `Assets/Scenes/ReadingRoom.unity`
- `Assets/Scenes/Result.unity`
- `Docs/UI_COMPLETION_MAINLINE.md`
- `README.md`

## Commands

Use this Unity executable:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity
```

Use this project path:

```bash
/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity
```

Run targeted Phase 12 EditMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -runTests -testPlatform EditMode \
  -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase12-editmode.xml \
  -testFilter TarotUnity.Tests.EditMode.Phase12CardFirstRevealTests
```

Run the Phase 12 bootstrapper:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -executeMethod TarotUnity.Editor.Phase12CardRevealBootstrapper.Run \
  -quit
```

Run full EditMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -runTests -testPlatform EditMode \
  -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/editmode.xml
```

Run full PlayMode tests:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity \
  -runTests -testPlatform PlayMode \
  -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/playmode.xml
```

Unity 6.3 Test Framework exits the batchmode editor when the test run completes. Do not add `-quit` to `-runTests` commands, because it can close the editor before tests run.

Run backend tests if any network-facing or backend integration code changes:

```bash
cd /Users/maochuandou/BUPT/Game/Tarot && pytest
```

---

## Task 1: Add Failing Phase 12 EditMode Tests

- [ ] Create `Assets/Tests/EditMode/Phase12CardFirstRevealTests.cs`.
- [ ] Use reflection for new editor-only types so the test file compiles before implementation.
- [ ] Verify the new tests fail for meaningful reasons before implementation.

Test file structure:

```csharp
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase12CardFirstRevealTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string Phase11ReviewFolder = "Docs/VisualReview/Phase11";

        [Test]
        public void Phase12CardRevealTypesExist()
        {
            var builderType = System.Type.GetType("TarotUnity.Editor.Phase12CardRevealBootstrapper, Assembly-CSharp-Editor");
            Assert.That(builderType, Is.Not.Null);
            Assert.That(builderType.GetField("CardPrefabPath")?.GetValue(null), Is.EqualTo(CardPrefabPath));
            Assert.That(builderType.GetField("ReadingRoomScenePath")?.GetValue(null), Is.EqualTo(ReadingRoomScenePath));
            Assert.That(builderType.GetField("ResultScenePath")?.GetValue(null), Is.EqualTo(ResultScenePath));
        }

        [Test]
        public void CardPrefabHasFaceArtworkPlaceholder()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var cardView = prefab.GetComponent<CardView>();
            Assert.That(cardView, Is.Not.Null);

            var serialized = new SerializedObject(cardView);
            var faceArtworkRenderer = serialized.FindProperty("faceArtworkRenderer");
            Assert.That(faceArtworkRenderer, Is.Not.Null);
            Assert.That(faceArtworkRenderer.objectReferenceValue, Is.Not.Null);

            Assert.That(prefab.transform.Find("Front/Phase12_FaceArtworkFrame"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Front/Phase12_FaceArtworkPlaceholder"), Is.Not.Null);

            var label = prefab.transform.Find("Front/Phase12_FaceArtworkPlaceholder/Phase12_FaceArtworkLabel")?.GetComponent<TextMesh>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Does.Contain("Tarot Art"));
        }

        [Test]
        public void ReadingRoomHasCardFirstRevealStage()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            Assert.That(GameObject.Find("Phase12_CardRevealStage"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase12_RevealBackdrop"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase12_FocusedCardLight"), Is.Not.Null);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_CardFocusVignette"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_RevealInstruction"), Is.Not.Null);
        }

        [Test]
        public void ResultSceneHasCardShowcase()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardShowcase"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardPlaceholder"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase12_ResultCardArtworkSlot"), Is.Not.Null);

            var overallText = canvas.transform.Find("OverallText")?.GetComponent<Text>();
            Assert.That(overallText, Is.Not.Null);
            Assert.That(overallText.GetComponent<RectTransform>().sizeDelta.x, Is.GreaterThanOrEqualTo(660f));
        }

        [Test]
        public void Phase12DoesNotBreakPhase11ScreenshotReviewArtifacts()
        {
            Assert.That(File.Exists(Path.Combine(Phase11ReviewFolder, "MainMenu.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Phase11ReviewFolder, "ReadingRoom.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Phase11ReviewFolder, "Result.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Phase11ReviewFolder, "phase11_visual_review_manifest.json")), Is.True);
        }
    }
}
```

Expected result before implementation:

- `Phase12CardRevealTypesExist` fails because `Phase12CardRevealBootstrapper` does not exist.
- `CardPrefabHasFaceArtworkPlaceholder` fails because `CardView.faceArtworkRenderer` and Phase 12 prefab children do not exist.
- `ReadingRoomHasCardFirstRevealStage` fails because Phase 12 scene anchors do not exist.
- `ResultSceneHasCardShowcase` fails because Phase 12 result showcase anchors do not exist.
- Phase 11 artifact test should pass if the previous baseline artifacts are still present.

Review checkpoint:

- Confirm no production file was changed in this task.
- Confirm the failing tests describe the approved B direction rather than broad visual taste.

---

## Task 2: Extend CardView With Optional Face Artwork Support

- [ ] Modify `Assets/Scripts/Gameplay/CardView.cs`.
- [ ] Add an optional serialized `SpriteRenderer faceArtworkRenderer` field.
- [ ] Add `SetFaceArtwork(Sprite sprite)` for future real tarot artwork import.
- [ ] Keep `Bind(CardDrawData drawData)` and `SetFaceUp(bool faceUp)` safe when the new renderer is missing.
- [ ] Do not change the draw, flip, result, or backend flow.

Implementation shape:

```csharp
[SerializeField] private SpriteRenderer faceArtworkRenderer;

public void SetFaceArtwork(Sprite sprite)
{
    if (faceArtworkRenderer == null)
    {
        return;
    }

    faceArtworkRenderer.sprite = sprite;
    faceArtworkRenderer.enabled = sprite != null && IsFaceUp;
}
```

Update `Bind` after labels are assigned:

```csharp
SetFaceArtwork(null);
SetFaceUp(false);
```

Update `SetFaceUp` after `frontRenderer` handling:

```csharp
if (faceArtworkRenderer != null)
{
    faceArtworkRenderer.enabled = faceUp && faceArtworkRenderer.sprite != null;
}
```

Acceptance for this task:

- Existing `CardView` tests and PlayMode flow continue to compile.
- Cards without imported artwork still show their existing title and position fallback labels.

Review checkpoint:

- Confirm the new field is optional and cannot create a null reference.
- Confirm no backend or data contract changes were introduced.

---

## Task 3: Implement Phase12CardRevealBootstrapper

- [ ] Create `Assets/Editor/Phase12CardRevealBootstrapper.cs`.
- [ ] Add constants:
  - `public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";`
  - `public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";`
  - `public const string ResultScenePath = "Assets/Scenes/Result.unity";`
  - `public const string Phase12DocPath = "Docs/PHASE12_CARD_FIRST_REVEAL.md";`
- [ ] Add menu item `Tools/Tarot Unity/Run Phase 12 Card Reveal Bootstrap`.
- [ ] Exit early with a warning if Unity is in Play Mode.
- [ ] Upgrade the card prefab, ReadingRoom scene, Result scene, and documentation in one deterministic run.
- [ ] Save assets and scenes, then refresh the AssetDatabase.

File skeleton:

```csharp
using System.IO;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase12CardRevealBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string Phase12DocPath = "Docs/PHASE12_CARD_FIRST_REVEAL.md";

        [MenuItem("Tools/Tarot Unity/Run Phase 12 Card Reveal Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 12 Card Reveal Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();
            WritePhase12Doc();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 12 card-first reveal bootstrap complete.");
        }
    }
}
```

Helper methods required in the file:

- `UpgradeCardPrefab()`
- `UpgradeReadingRoomScene()`
- `UpgradeResultScene()`
- `WritePhase12Doc()`
- `EnsureQuad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int siblingIndex)`
- `EnsureTextMesh(Transform parent, string name, string text, Vector3 localPosition, int fontSize, Color color)`
- `EnsurePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, int siblingIndex)`
- `EnsureText(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, FontStyle fontStyle)`
- `SetSerializedReference(Object target, string propertyName, Object value)`

Review checkpoint:

- Confirm the bootstrapper creates deterministic names used by tests.
- Confirm it does not call Phase11 screenshot capture, because screenshot capture must run with graphics enabled.
- Confirm repeated runs are idempotent and do not duplicate Phase 12 objects.

---

## Task 4: Upgrade PF_TarotCard For Card-First Reveal

- [ ] In `UpgradeCardPrefab()`, load `Assets/Prefabs/Cards/PF_TarotCard.prefab`.
- [ ] Find the existing `Front` transform.
- [ ] Create or update `Front/Phase12_FaceArtworkFrame`.
- [ ] Create or update `Front/Phase12_FaceArtworkPlaceholder`.
- [ ] Create or update `Front/Phase12_FaceArtworkPlaceholder/Phase12_FaceArtworkLabel`.
- [ ] Assign `CardView.faceArtworkRenderer` to the `SpriteRenderer` on `Phase12_FaceArtworkPlaceholder`.
- [ ] Save the prefab.

Required visual structure:

```text
PF_TarotCard
└── Front
    ├── Phase12_FaceArtworkFrame
    └── Phase12_FaceArtworkPlaceholder
        └── Phase12_FaceArtworkLabel
```

Required implementation details:

- Use `SpriteRenderer` or equivalent renderer for `Phase12_FaceArtworkPlaceholder`.
- Use a warm, readable temporary color such as parchment gold for the artwork slot.
- Keep the slot centered on the front face and smaller than the existing card border.
- Keep the existing `TitleLabel` and `PositionLabel` as fallback text.
- Mark changed objects dirty with `EditorUtility.SetDirty`.
- Save through `PrefabUtility.SavePrefabAsset(prefab)`.

Review checkpoint:

- Confirm the existing card root, front root, back root, highlight root, title label, and position label are still referenced.
- Confirm no missing script entries were introduced in the prefab YAML.

---

## Task 5: Upgrade ReadingRoom Into A Reveal Stage

- [ ] In `UpgradeReadingRoomScene()`, open `Assets/Scenes/ReadingRoom.unity`.
- [ ] Add or update world anchors:
  - `Phase12_CardRevealStage`
  - `Phase12_RevealBackdrop`
  - `Phase12_FocusedCardLight`
- [ ] Add or update UI anchors under `ReadingRoomCanvas`:
  - `Phase12_CardFocusVignette`
  - `Phase12_RevealInstruction`
- [ ] Keep `OneCardButton`, `ThreeCardButton`, `QuestionInput`, `DrawButton`, `RevealResultButton`, `FlowStatusText`, and `Phase10_ReleaseStatusText` readable at 1280x720.
- [ ] Save the scene.

Required layout intent:

- `Phase12_CardRevealStage` sits behind the card slots, not over the UI controls.
- `Phase12_FocusedCardLight` is a scene light with low intensity and warm color.
- `Phase12_CardFocusVignette` is non-interactive and does not block UI clicks.
- `Phase12_RevealInstruction` uses concise Chinese copy:

```text
点击牌面，揭开此刻的讯息
```

Review checkpoint:

- Confirm `ReadingRoomController` serialized references still exist.
- Confirm spread selection and draw controls remain interactable.
- Confirm UI overlays have `raycastTarget = false`.

---

## Task 6: Upgrade Result Into A Card Showcase

- [ ] In `UpgradeResultScene()`, open `Assets/Scenes/Result.unity`.
- [ ] Add or update UI anchors under `ResultCanvas`:
  - `Phase12_ResultCardShowcase`
  - `Phase12_ResultCardPlaceholder`
  - `Phase12_ResultCardArtworkSlot`
- [ ] Preserve existing result text fields:
  - `QuestionText`
  - `SpreadNameText`
  - `SummaryText`
  - `OverallText`
  - `CardAnalysisText`
  - `AdviceText`
  - `BackToMenuButton`
- [ ] Keep `OverallText` width at least `660`.
- [ ] Save the scene.

Required layout intent:

- The showcase sits on the left side where Phase 11 already introduced card presence.
- The text column remains on the right and keeps enough width for Chinese readings.
- The card slot visually says "the answer came from these cards" without needing real deck art in this phase.

Review checkpoint:

- Confirm `ResultPanelPresenter` serialized text references still exist.
- Confirm `ResultRevealDirector` still has reveal groups and completes in PlayMode.
- Confirm the back-to-menu control remains visible.

---

## Task 7: Write Phase 12 Documentation

- [ ] Create `Docs/PHASE12_CARD_FIRST_REVEAL.md`.
- [ ] Update `Docs/UI_COMPLETION_MAINLINE.md`.
- [ ] Update `README.md`.

`Docs/PHASE12_CARD_FIRST_REVEAL.md` must include:

- Phase 12 scope.
- Approved B direction.
- What changed in card prefab, ReadingRoom, and Result.
- How to run the bootstrapper.
- How to run tests.
- Explicit statement that real tarot art import remains a future licensed asset task.

Documentation wording to include:

```text
Phase 12 adds a durable face-art slot and card-first reveal staging, but it does not import the final tarot deck artwork. The real deck task still requires source discovery, license review, texture import settings, and card-name mapping.
```

Review checkpoint:

- Confirm documentation matches actual implementation.
- Confirm documentation does not imply the real deck art is already imported.

---

## Task 8: Run Phase 12 Bootstrapper And Targeted Tests

- [ ] Run targeted Phase 12 EditMode tests before the bootstrapper and confirm expected failures.
- [ ] Run `TarotUnity.Editor.Phase12CardRevealBootstrapper.Run`.
- [ ] Run targeted Phase 12 EditMode tests again.
- [ ] Fix failures and rerun until targeted tests pass.

Expected final targeted result:

```text
Phase12CardFirstRevealTests: Passed 5 / Failed 0 / Skipped 0
```

Review checkpoint:

- Confirm Unity was not in Play Mode when the bootstrapper ran.
- Confirm the test result XML is stored under `TestResults/phase12-editmode.xml`.

---

## Task 9: Run Full Unity And Backend Verification

- [ ] Run full EditMode tests.
- [ ] Run full PlayMode tests.
- [ ] Run backend pytest only if this implementation touched network-facing code or backend integration behavior.
- [ ] If backend pytest is skipped, record the reason in the final report.

Expected Unity verification:

```text
EditMode: Passed, Failed 0
PlayMode: Passed, Failed 0
```

Expected PlayMode coverage:

```text
Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result
```

Review checkpoint:

- Confirm any failed test is fixed before final reporting.
- Confirm any unverified part of the vertical slice is explicitly listed as unverified.

---

## Task 10: Run Missing Reference And Log Checks

- [ ] Scan scenes and prefabs for missing scripts and missing serialized references using an existing project tool if one exists.
- [ ] If no existing scanner is available, add a narrow editor validation method or run a Unity asset scan script before final report.
- [ ] Check the newest Unity `Editor.log` for compile errors, exceptions, and Console errors.
- [ ] Ignore only known external noise after recording it separately:
  - Unity Licensing token or handshake noise.
  - UnityConnect or public CDN timeouts.
  - URP ray tracing shader warning on unsupported platform.

Search commands:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}" ~/Library/Logs/Unity/Editor.log
```

```bash
rg -n "m_Script: \\{fileID: 0\\}|m_MissingScript|Missing" Assets/Scenes Assets/Prefabs
```

Review checkpoint:

- Confirm compile errors are zero.
- Confirm missing script references are zero.
- Confirm no new null reference or missing reference exception was introduced by Phase 12.

---

## Task 11: Manual Unity Editor Verification

- [ ] Open Unity Editor after automated tests pass.
- [ ] Confirm Unity is not in Play Mode before saving.
- [ ] Open `ReadingRoom.unity`.
- [ ] Verify cards have a visible Phase 12 face-art region after draw and flip.
- [ ] Verify the vertical slice reaches `Result.unity`.
- [ ] Verify Result has a card showcase and readable interpretation text.

Manual validation notes to record:

- Whether ReadingRoom card reveal staging is visible.
- Whether Result card showcase is visible.
- Whether Unity Editor display blur remains an editor UI issue rather than a game scene rendering issue.

Review checkpoint:

- If manual validation is not run, final report must say automated tests passed but manual visual check is unverified.
- If manual validation finds layout overlap, fix scene/bootstrapper layout and rerun tests.

---

## Task 12: Final Self-Review And Report

- [ ] Review changed files with `git status --short` if a repository exists, otherwise use `find`/timestamps and the planned file list.
- [ ] Confirm no unrelated files were modified.
- [ ] Confirm no Phase 6 packaging work or real tarot deck import slipped into Phase 12.
- [ ] Confirm null-reference risks from optional fields are guarded.
- [ ] Confirm docs state that real tarot art remains a later licensed asset task.
- [ ] Produce the required user-facing ending report.

Final report format:

```text
状态：完成 / 部分完成 / 阻塞中

本次实际改动：
- Created Phase 12 tests, bootstrapper, docs, and card-first visual anchors.

错误检查结果：
- Unity compile/Console/Editor.log: state the checked result and quote any non-project external noise separately.
- Missing refs: state the scan command and result.
- Known external noise: list Unity Licensing, UnityConnect, or URP ray tracing warnings only when present.

测试结果：
- EditMode: Passed X / Failed 0 / Skipped Y
- PlayMode: Passed X / Failed 0 / Skipped Y
- Backend pytest: Passed X or Skipped with reason

垂直切片验证：
- Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result: verified / partially verified

仍存在的问题或下一步：
- Real tarot artwork import remains the licensed asset sourcing task.
```

Completion rule:

- If any compile error, Console error, failed test, missing reference, or unverified main flow remains, report `部分完成` or `阻塞中`, not `完成`.
