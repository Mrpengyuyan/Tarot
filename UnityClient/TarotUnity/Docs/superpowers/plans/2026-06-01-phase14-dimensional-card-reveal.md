# Phase 14 Dimensional Card Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a tested 2.5D dimensional card reveal layer so real tarot card art feels like a physical card object during flip and result reveal.

**Architecture:** Keep gameplay state and backend flow unchanged. Add one narrow runtime presentation component, wire it optionally through `CardView`, and use an idempotent editor bootstrapper to add prefab/scene visual anchors. Tests lock the prefab, scene, and null-safe reveal behavior before implementation.

**Tech Stack:** Unity 6.3 LTS, C#, UnityEngine, UnityEditor, NUnit EditMode tests, existing Unity Test Framework.

---

## File Structure

- Create: `Assets/Scripts/Presentation/DimensionalCardRevealController.cs`
  - Owns only visual lift/settle/glow behavior for one card.
  - Does not own card data, API calls, scene loading, or result state.
- Modify: `Assets/Scripts/Gameplay/CardView.cs`
  - Adds optional serialized `DimensionalCardRevealController`.
  - Notifies the helper when face state changes.
  - Remains safe when the helper is absent.
- Create: `Assets/Editor/Phase14DimensionalCardBootstrapper.cs`
  - Adds card prefab visual anchors.
  - Adds ReadingRoom and Result scene anchors.
  - Creates persistent Phase14 materials.
  - Assigns `CardView.dimensionalRevealController`.
- Create: `Assets/Tests/EditMode/Phase14DimensionalCardRevealTests.cs`
  - Failing tests first for runtime type, prefab anchors, scene anchors, null-safe behavior, and Phase13 retention.
- Create: `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`
  - Documents the pass, bootstrap command, scope limits, and verification commands.
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
  - Adds Phase14 status and remaining limits after implementation.
- Modify: `README.md`
  - Updates current slice status after Phase14.

No git repository is detected under `/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`, so task checkpoints replace commit steps.

---

## Task 1: Write Phase14 Failing Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase14DimensionalCardRevealTests.cs`

- [x] **Step 1: Add the failing test file**

Create `Assets/Tests/EditMode/Phase14DimensionalCardRevealTests.cs` with this content:

```csharp
using System;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase14DimensionalCardRevealTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase14DocPath = "Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md";

        [Test]
        public void DimensionalRevealControllerTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.DimensionalCardRevealController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("PlayReveal", Type.EmptyTypes), Is.Not.Null);
            Assert.That(type.GetMethod("SetGlowVisible", new[] { typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void CardPrefabHasPhase14DimensionalAnchors()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_CardEdge"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_CastShadow"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_FaceRimLight"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_ArtworkGlass"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot/Phase14_RevealGlow"), Is.Not.Null);

            var serializedCard = new SerializedObject(prefab.GetComponent<CardView>());
            var revealController = serializedCard.FindProperty("dimensionalRevealController");
            Assert.That(revealController, Is.Not.Null);
            Assert.That(revealController.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void CardViewAllowsMissingDimensionalRevealController()
        {
            var cardObject = new GameObject("Card");
            try
            {
                var card = cardObject.AddComponent<CardView>();
                Assert.DoesNotThrow(() => card.SetFaceUp(true));
                Assert.DoesNotThrow(() => card.SetFaceUp(false));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void ReadingRoomHasPhase14RevealStageAnchors()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            Assert.That(GameObject.Find("Phase14_TableDepthPlane"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase14_CardRevealPool"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase14_RevealLightWarm"), Is.Not.Null);
        }

        [Test]
        public void ResultHasPhase14CardFocusAnchors()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultCardHalo"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultCardShadow"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase14_ResultTextBridge"), Is.Not.Null);
        }

        [Test]
        public void Phase14KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase14DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(Phase14DocPath), Is.True);
            var text = System.IO.File.ReadAllText(Phase14DocPath);
            Assert.That(text, Does.Contain("2.5D dimensional card reveal"));
            Assert.That(text, Does.Contain("DimensionalCardRevealController"));
            Assert.That(text, Does.Contain("Phase14_DimensionalRoot"));
        }
    }
}
```

- [x] **Step 2: Run the targeted test to verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-red.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-red-tests.log
```

Expected:

```text
result=Failed
failed>=1
```

The failures should mention missing `DimensionalCardRevealController`, missing Phase14 anchors, and missing `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`.

- [x] **Step 3: Checkpoint**

Record in the task notes that the test was observed failing for the expected missing Phase14 implementation.

Checkpoint result: `TestResults/phase14-red.xml` recorded `result="Failed(Child)"`, `total="7"`, `passed="2"`, `failed="5"`. The failing cases cover the missing `DimensionalCardRevealController`, missing Phase14 prefab anchors, missing ReadingRoom anchors, missing Result anchors, and missing `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`, which is the expected RED state before Phase14 implementation.

---

## Task 2: Add Runtime Dimensional Reveal Helper

**Files:**
- Create: `Assets/Scripts/Presentation/DimensionalCardRevealController.cs`
- Modify: `Assets/Scripts/Gameplay/CardView.cs`
- Test: `Assets/Tests/EditMode/Phase14DimensionalCardRevealTests.cs`

- [x] **Step 1: Create the runtime helper**

Create `Assets/Scripts/Presentation/DimensionalCardRevealController.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class DimensionalCardRevealController : MonoBehaviour
    {
        [SerializeField] private Transform cardRoot;
        [SerializeField] private Renderer revealGlowRenderer;
        [SerializeField] private float revealLift = 0.08f;
        [SerializeField] private float revealScale = 1.035f;
        [SerializeField] private float settleSeconds = 0.18f;
        [SerializeField] private AnimationCurve settleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 restingLocalPosition;
        private Vector3 restingLocalScale;
        private Coroutine activeRoutine;

        private Transform Target => cardRoot != null ? cardRoot : transform;

        private void Awake()
        {
            CaptureRestingPose();
            SetGlowVisible(false);
        }

        public void CaptureRestingPose()
        {
            restingLocalPosition = Target.localPosition;
            restingLocalScale = Target.localScale;
        }

        public void PlayReveal()
        {
            if (!isActiveAndEnabled)
            {
                SetGlowVisible(true);
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            activeRoutine = StartCoroutine(RevealRoutine());
        }

        public void SetGlowVisible(bool visible)
        {
            if (revealGlowRenderer != null)
            {
                revealGlowRenderer.enabled = visible;
            }
        }

        private IEnumerator RevealRoutine()
        {
            SetGlowVisible(true);

            var target = Target;
            var liftedPosition = restingLocalPosition + Vector3.up * revealLift;
            var liftedScale = restingLocalScale * revealScale;

            target.localPosition = liftedPosition;
            target.localScale = liftedScale;

            var duration = Mathf.Max(0.01f, settleSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = settleCurve.Evaluate(elapsed / duration);
                target.localPosition = Vector3.Lerp(liftedPosition, restingLocalPosition, t);
                target.localScale = Vector3.Lerp(liftedScale, restingLocalScale, t);
                yield return null;
            }

            target.localPosition = restingLocalPosition;
            target.localScale = restingLocalScale;
            activeRoutine = null;
        }
    }
}
```

- [x] **Step 2: Wire `CardView` to the optional helper**

Modify `Assets/Scripts/Gameplay/CardView.cs`:

```csharp
using System;
using TarotUnity.Data;
using TarotUnity.Presentation;
using UnityEngine;
```

Add this serialized field after `faceArtworkRenderer`:

```csharp
[SerializeField] private DimensionalCardRevealController dimensionalRevealController;
```

Inside `SetFaceUp(bool faceUp)`, after the `faceArtworkRenderer.enabled` block, add:

```csharp
if (dimensionalRevealController != null)
{
    dimensionalRevealController.SetGlowVisible(faceUp);
    if (faceUp)
    {
        dimensionalRevealController.PlayReveal();
    }
}
```

- [x] **Step 3: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-runtime.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-runtime-tests.log
```

Expected:

```text
result=Failed
```

The type and null-safety tests should pass. Prefab, scene, and doc tests should still fail.

- [x] **Step 4: Checkpoint**

Record the current pass/fail split from `TestResults/phase14-runtime.xml`.

Checkpoint result: `TestResults/phase14-runtime.xml` recorded `result="Failed(Child)"`, `total="7"`, `passed="3"`, `failed="4"`. The passing tests are `DimensionalRevealControllerTypeExists`, `CardViewAllowsMissingDimensionalRevealController`, and `Phase14KeepsPhase13ArtworkCatalogAvailable`. The remaining failures are the expected Task 3/4 gaps: Phase14 prefab anchors, ReadingRoom anchors, Result anchors, and `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`.

---

## Task 3: Add Phase14 Bootstrapper And Visual Anchors

**Files:**
- Create: `Assets/Editor/Phase14DimensionalCardBootstrapper.cs`
- Modify through bootstrapper: `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create through bootstrapper: `Assets/Materials/MAT_Phase14_*.mat`

- [x] **Step 1: Create the editor bootstrapper**

Create `Assets/Editor/Phase14DimensionalCardBootstrapper.cs` with:

```csharp
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase14DimensionalCardBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 14 Dimensional Card Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 14 Dimensional Card Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 14 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 14 dimensional card bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var dimensionalRoot = EnsureChild(root.transform, "Phase14_DimensionalRoot");
                dimensionalRoot.transform.localPosition = Vector3.zero;
                dimensionalRoot.transform.localRotation = Quaternion.identity;
                dimensionalRoot.transform.localScale = Vector3.one;

                var edge = EnsureQuad(dimensionalRoot.transform, "Phase14_CardEdge", new Vector3(0f, 0.058f, -0.041f), new Vector3(0.74f, 1.05f, 1f), new Color(0.16f, 0.095f, 0.050f, 1f), 0);
                var shadow = EnsureQuad(dimensionalRoot.transform, "Phase14_CastShadow", new Vector3(0.030f, -0.014f, 0.030f), new Vector3(0.82f, 1.12f, 1f), new Color(0f, 0f, 0f, 0.36f), 0);
                var rim = EnsureQuad(dimensionalRoot.transform, "Phase14_FaceRimLight", new Vector3(0f, 0.088f, -0.044f), new Vector3(0.62f, 0.84f, 1f), new Color(1f, 0.80f, 0.43f, 0.30f), 0);
                var glass = EnsureQuad(dimensionalRoot.transform, "Phase14_ArtworkGlass", new Vector3(0f, 0.091f, -0.046f), new Vector3(0.48f, 0.64f, 1f), new Color(1f, 0.96f, 0.82f, 0.10f), 0);
                var glow = EnsureQuad(dimensionalRoot.transform, "Phase14_RevealGlow", new Vector3(0f, 0.094f, -0.050f), new Vector3(0.70f, 0.94f, 1f), new Color(1f, 0.70f, 0.26f, 0.22f), 0);

                var glowRenderer = glow.GetComponent<MeshRenderer>();
                if (glowRenderer != null)
                {
                    glowRenderer.enabled = false;
                }

                var controller = root.GetComponent<DimensionalCardRevealController>();
                if (controller == null)
                {
                    controller = root.AddComponent<DimensionalCardRevealController>();
                }

                SetSerializedReference(controller, "cardRoot", root.transform);
                SetSerializedReference(controller, "revealGlowRenderer", glowRenderer);

                var cardView = root.GetComponent<CardView>();
                if (cardView != null)
                {
                    SetSerializedReference(cardView, "dimensionalRevealController", controller);
                }

                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var table = EnsureQuad(null, "Phase14_TableDepthPlane", new Vector3(0f, -0.54f, 0.03f), new Vector3(5.65f, 2.12f, 1f), new Color(0.026f, 0.016f, 0.034f, 0.62f), 0);
            table.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var pool = EnsureQuad(null, "Phase14_CardRevealPool", new Vector3(0f, -0.51f, 0.16f), new Vector3(4.20f, 1.30f, 1f), new Color(0.91f, 0.62f, 0.26f, 0.16f), 0);
            pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var lightObject = GameObject.Find("Phase14_RevealLightWarm") ?? new GameObject("Phase14_RevealLightWarm");
            lightObject.transform.position = new Vector3(0f, 1.92f, -1.38f);
            lightObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            var light = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.38f, 1f);
            light.intensity = 0.58f;
            light.range = 4.8f;
            EditorUtility.SetDirty(lightObject);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                EnsurePanel(canvas.transform, "Phase14_ResultCardShadow", new Vector2(-330f, 10f), new Vector2(172f, 256f), new Color(0f, 0f, 0f, 0.34f), 4);
                EnsurePanel(canvas.transform, "Phase14_ResultCardHalo", new Vector2(-330f, 22f), new Vector2(204f, 304f), new Color(1f, 0.72f, 0.28f, 0.14f), 5);
                EnsurePanel(canvas.transform, "Phase14_ResultTextBridge", new Vector2(-72f, 10f), new Vector2(116f, 324f), new Color(0.80f, 0.54f, 0.20f, 0.10f), 5);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static GameObject EnsureQuad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int siblingIndex)
        {
            var existing = parent != null ? parent.Find(name)?.gameObject : GameObject.Find(name);
            var quad = existing ?? GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;

            if (parent != null)
            {
                quad.transform.SetParent(parent, false);
                quad.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            }

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = localScale;

            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = EnsurePreviewMaterial(name, color);
            }

            EditorUtility.SetDirty(quad);
            return quad;
        }

        private static Image EnsurePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, int siblingIndex)
        {
            var existing = parent.Find(name);
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));

            panel.transform.SetParent(parent, false);
            panel.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(image);
            return image;
        }

        private static Material EnsurePreviewMaterial(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            var materialPath = $"{MaterialFolder}/MAT_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
```

- [x] **Step 2: Run the bootstrapper**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase14DimensionalCardBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-bootstrap.log
```

Expected:

```text
exit code 0
Tarot Unity Phase 14 dimensional card bootstrap complete.
```

Known acceptable noise:

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
```

- [x] **Step 3: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-anchors.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-anchors-tests.log
```

Expected:

```text
result=Failed
```

Only the documentation test should remain failing.

- [x] **Step 4: Checkpoint**

Record prefab, ReadingRoom, and Result anchors that now pass.

Checkpoint result: `Logs/phase14-bootstrap.log` recorded `Tarot Unity Phase 14 dimensional card bootstrap complete.` with only the known Unity licensing token noise. `TestResults/phase14-anchors.xml` recorded `result="Failed(Child)"`, `total="7"`, `passed="6"`, `failed="1"`. The passing cases prove the `PF_TarotCard` dimensional anchors, optional `CardView` reveal-helper safety, ReadingRoom anchors, Result anchors, and Phase13 artwork catalog retention. The only remaining failure is `Phase14DocumentationExists`, which is intentionally deferred to Task 4. Code-quality re-review approved the transparent Phase14 material setup and confirmed `DimensionalCardRevealController.cardRoot` targets `Phase14_DimensionalRoot` instead of the prefab root.

---

## Task 4: Add Phase14 Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [x] **Step 1: Add Phase14 phase document**

Create `Docs/PHASE14_DIMENSIONAL_CARD_REVEAL.md`:

```markdown
# Phase 14 Dimensional Card Reveal

Date: 2026-06-01

## Scope

Phase 14 implements the selected方案 A: a 2.5D dimensional card reveal layer for the current Unity tarot vertical slice.

It improves the presentation around the real RWS1909 card art from Phase 13 without changing backend flow, scene order, desktop packaging, history, settings, profile, admin, or dashboard features.

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

## How To Run Bootstrap

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase14DimensionalCardBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-bootstrap.log
```

## Verification

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
```

- [x] **Step 2: Update UI mainline**

Add a Phase14 section to `Docs/UI_COMPLETION_MAINLINE.md`:

```markdown
### Phase 14: Dimensional Card Reveal

Status: implemented and verified on 2026-06-01.

Purpose:

- Move the real card art from a flat sprite reveal toward a physical card-game presentation.
- Add dimensional card prefab anchors for edge, shadow, rim light, glass, and reveal glow.
- Add a null-safe `DimensionalCardRevealController`.
- Add ReadingRoom and Result stage anchors that support card-first reveal without blocking UI.

Exit criteria:

- `PF_TarotCard` contains Phase14 dimensional anchors.
- `CardView` remains safe if the dimensional helper is missing.
- ReadingRoom and Result contain Phase14 visual anchors.
- Phase13 card art catalog remains intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase14 is still a 2.5D pass. A later phase can replace the layered card object with custom 3D meshes, stronger camera motion, and higher-end VFX.
```

- [x] **Step 3: Update README status paragraph**

Modify the current status paragraph in `README.md` so it includes:

```text
Phase 14 adds a 2.5D dimensional card reveal layer, card edge/shadow/glow anchors, and stage polish around the existing real card art.
```

- [x] **Step 4: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-green.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-green-tests.log
```

Expected:

```text
result=Passed
total=7
failed=0
```

- [x] **Step 5: Checkpoint**

Checkpoint result: `TestResults/phase14-green.xml` recorded `result="Passed"`, `total="7"`, `passed="7"`, `failed="0"`. The targeted `TarotUnity.Tests.EditMode.Phase14DimensionalCardRevealTests` run exited with code 0, and `Logs/phase14-green-tests.log` contained only the known Unity licensing token noise.

---

## Task 5: Final Verification And Review

**Files:**
- Read/check: `Logs/phase14-final-unity-check.log`
- Read/check: `TestResults/phase14-final-editmode.xml`
- Read/check: `TestResults/phase14-final-playmode.xml`

- [x] **Step 1: Confirm no Unity Editor process is active**

Run:

```bash
pgrep -fl "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity|Unity.app/Contents/MacOS/Unity" || true
```

Expected:

```text
no output
```

- [x] **Step 2: Unity compile/log check**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-unity-check.log -quit
```

Expected:

```text
exit code 0
```

- [x] **Step 3: Scan Unity logs for project errors**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode" Logs/phase14-final-unity-check.log ~/Library/Logs/Unity/Editor.log || true
```

Expected:

```text
no output
```

Acceptable known external noise can be listed separately with:

```bash
rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized" Logs/phase14-final-unity-check.log || true
```

- [x] **Step 4: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [x] **Step 5: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase14-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase14-final-playmode.log
```

Expected:

```text
result=Passed
failed=0
VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns Passed
```

- [x] **Step 6: Scan scene and prefab references**

Run:

```bash
rg -n "m_Script: \\{fileID: 0\\}|m_MissingScript|Missing Script|MissingReferenceException|MissingReference" Assets/Scenes Assets/Prefabs || true
```

Expected:

```text
no output
```

- [x] **Step 7: Final self-review checklist**

Verify:

```text
No backend files changed.
No history/settings/profile/admin/dashboard work added.
No desktop build or release zip regenerated.
Phase13 catalog still has 78 entries.
CardView is null-safe if DimensionalCardRevealController is absent.
ReadingRoom and Result anchors are non-blocking UI support.
```

- [x] **Step 8: Final report**

Report:

```text
状态：完成 / 部分完成 / 阻塞中
本次实际改动
错误检查结果
测试结果
垂直切片验证
仍存在的问题或下一步建议
```

If any project error, failed test, or unverified vertical slice remains, report `部分完成` or `阻塞中`, not `完成`.

Checkpoint result: Unity Editor was not active before final checks. `Logs/phase14-final-unity-check.log` exited with code 0 and the project-error scan found no compile errors, exceptions, missing scripts, or missing references. Known external Unity noise remained limited to licensing-token and shutdown thread messages. `TestResults/phase14-final-editmode.xml` recorded `result="Passed"`, `total="66"`, `passed="66"`, `failed="0"`, `skipped="0"`. `TestResults/phase14-final-playmode.xml` recorded `result="Passed"`, `total="2"`, `passed="2"`, `failed="0"`, `skipped="0"`, including `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns`. Scene/prefab reference scan returned no missing scripts or missing references. Self-review confirmed no backend/history/settings/profile/admin/dashboard scope was added, no desktop build or release zip was regenerated, the Phase13 catalog still has 78 entries, `CardView` remains null-safe without `DimensionalCardRevealController`, and ReadingRoom/Result Phase14 anchors are non-blocking visual support.
