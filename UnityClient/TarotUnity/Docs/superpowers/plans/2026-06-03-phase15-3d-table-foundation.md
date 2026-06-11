# Phase 15 3D Table Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the approved A2 3D table foundation so the tarot reading feels like it happens on a physical card table while preserving the current playable vertical slice.

**Architecture:** Add optional presentation-only runtime helpers, then use an idempotent Unity editor bootstrapper to add prefab, scene, material, light, and camera-stage anchors. Gameplay state, backend calls, RWS1909 artwork mapping, desktop packaging, and scene order remain unchanged.

**Tech Stack:** Unity 6.3 LTS, C#, URP, UnityEditor, NUnit EditMode tests, Unity Test Framework PlayMode tests.

---

## File Structure

- Create: `Assets/Tests/EditMode/Phase15ThreeDTableFoundationTests.cs`
  - Locks Phase15 runtime type existence, card prefab anchors, scene anchors, materials, documentation, and Phase13/14 retention.
- Create: `Assets/Tests/PlayMode/Phase15ThreeDTableFoundationPlayModeTests.cs`
  - Verifies optional Phase15 presentation helpers are null-safe at runtime.
- Create: `Assets/Scripts/Presentation/ThreeDCardPresentationController.cs`
  - Owns only optional 3D card shell renderer/root presentation state.
- Create: `Assets/Scripts/Presentation/Phase15TableStageController.cs`
  - Owns only optional 3D table/lights/stage visibility references.
- Modify: `Assets/Scripts/Gameplay/CardView.cs`
  - Adds optional `ThreeDCardPresentationController` reference and notifies it when face state changes.
- Create: `Assets/Editor/Phase15ThreeDTableBootstrapper.cs`
  - Adds card prefab shell anchors, ReadingRoom table anchors/lights, Result card-stage anchors/lights, and Phase15 materials.
- Modify through bootstrapper: `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create through bootstrapper: `Assets/Materials/MAT_Phase15_*.mat`
- Create: `Docs/PHASE15_3D_TABLE_FOUNDATION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

No git repository is detected under `/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`, so task checkpoints replace commit steps.

---

## Task 1: Write Phase15 Failing Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase15ThreeDTableFoundationTests.cs`

- [ ] **Step 1: Add the EditMode failing test file**

Create `Assets/Tests/EditMode/Phase15ThreeDTableFoundationTests.cs` with tests for:

- `ThreeDCardPresentationController` type and public methods.
- `Phase15TableStageController` type and public methods.
- `CardView` serialized optional `threeDPresentationController` field.
- `PF_TarotCard` Phase15 card shell anchors.
- Phase13 `faceArtworkRenderer` and Phase14 `dimensionalRevealController` are still wired.
- ReadingRoom Phase15 table anchors and lights.
- Result Phase15 card-stage anchors and lights.
- Phase15 material render settings.
- Phase15 documentation exists.
- Phase13 catalog still has 78 entries.

Use this exact structure:

```csharp
using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase15ThreeDTableFoundationTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase15DocPath = "Docs/PHASE15_3D_TABLE_FOUNDATION.md";

        private static readonly string[] OpaqueMaterialPaths =
        {
            "Assets/Materials/MAT_Phase15_CardBody.mat",
            "Assets/Materials/MAT_Phase15_CardFacePlane.mat",
            "Assets/Materials/MAT_Phase15_CardBackPlane.mat",
            "Assets/Materials/MAT_Phase15_CardSideEdge.mat",
            "Assets/Materials/MAT_Phase15_RitualTableSurface.mat",
            "Assets/Materials/MAT_Phase15_ResultPedestal.mat",
        };

        private static readonly string[] TransparentMaterialPaths =
        {
            "Assets/Materials/MAT_Phase15_CardDropShadow.mat",
            "Assets/Materials/MAT_Phase15_TableDepthRing.mat",
        };

        [Test]
        public void Phase15RuntimeTypesExist()
        {
            var cardType = Type.GetType("TarotUnity.Presentation.ThreeDCardPresentationController, TarotUnity.Runtime");
            Assert.That(cardType, Is.Not.Null);
            Assert.That(cardType.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(cardType.GetMethod("SetFaceVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(cardType.GetMethod("SetDropShadowVisible", new[] { typeof(bool) }), Is.Not.Null);

            var stageType = Type.GetType("TarotUnity.Presentation.Phase15TableStageController, TarotUnity.Runtime");
            Assert.That(stageType, Is.Not.Null);
            Assert.That(stageType.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(stageType.GetMethod("SetStageVisible", new[] { typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void CardViewHasOptionalPhase15PresentationReference()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            var property = serializedCard.FindProperty("threeDPresentationController");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void CardPrefabHasPhase15CardMeshShell()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardBody"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardFacePlane"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardBackPlane"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardSideEdge"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot/Phase15_CardDropShadow"), Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.ThreeDCardPresentationController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var controller = prefab.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);
        }

        [Test]
        public void CardPrefabRetainsPhase13ArtworkAndPhase14RevealWiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);
            var serializedCard = new SerializedObject(card);

            Assert.That(serializedCard.FindProperty("faceArtworkRenderer")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedCard.FindProperty("dimensionalRevealController")?.objectReferenceValue, Is.Not.Null);

            var phase14 = prefab.GetComponent<DimensionalCardRevealController>();
            Assert.That(phase14, Is.Not.Null);
            var serializedPhase14 = new SerializedObject(phase14);
            Assert.That(serializedPhase14.FindProperty("cardRoot")?.objectReferenceValue, Is.SameAs(prefab.transform.Find("Phase14_DimensionalRoot")));
        }

        [Test]
        public void Phase15MaterialsUseExpectedRenderSettings()
        {
            foreach (var path in OpaqueMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                AssertMaterialFloat(material, "_Surface", 0f, path);
                Assert.That(material.renderQueue, Is.LessThan((int)RenderQueue.Transparent), path);
            }

            foreach (var path in TransparentMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.Transparent), path);
                AssertMaterialFloat(material, "_Surface", 1f, path);
                AssertMaterialFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha, path);
                AssertMaterialFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha, path);
                AssertMaterialFloat(material, "_ZWrite", 0f, path);
            }
        }

        [Test]
        public void ReadingRoomHasPhase15ThreeDTableFoundation()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase15_ThreeDTableRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_RitualTableSurface"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_TableDepthRing"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_DeckFocusAnchor"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_SpreadFocusAnchor"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_FlipFocusAnchor"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_WarmKeyLight")?.GetComponent<Light>(), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_CoolRimLight")?.GetComponent<Light>(), Is.Not.Null);
            var stageType = Type.GetType("TarotUnity.Presentation.Phase15TableStageController, TarotUnity.Runtime");
            Assert.That(stageType, Is.Not.Null);
            Assert.That(root.GetComponent(stageType), Is.Not.Null);
        }

        [Test]
        public void ResultHasPhase15CardStage()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase15_ResultCardStageRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_ResultCardPedestal"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase15_ResultFocusAnchor"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_ResultWarmFocusLight")?.GetComponent<Light>(), Is.Not.Null);
            Assert.That(GameObject.Find("Phase15_ResultCoolEdgeLight")?.GetComponent<Light>(), Is.Not.Null);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            foreach (var image in canvas.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("Phase15_", StringComparison.Ordinal))
                {
                    Assert.That(image.raycastTarget, Is.False, image.name);
                }
            }
        }

        [Test]
        public void Phase15KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase15DocumentationExists()
        {
            Assert.That(File.Exists(Phase15DocPath), Is.True, $"Missing Phase15 doc at {Phase15DocPath}");
            var text = File.ReadAllText(Phase15DocPath);
            Assert.That(text, Does.Contain("3D table foundation"));
            Assert.That(text, Does.Contain("ThreeDCardPresentationController"));
            Assert.That(text, Does.Contain("Phase15_ThreeDTableRoot"));
        }

        private static void AssertMaterialFloat(Material material, string propertyName, float expected, string path)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{path} missing {propertyName}");
            Assert.That(material.GetFloat(propertyName), Is.EqualTo(expected), $"{path} {propertyName}");
        }
    }
}
```

- [ ] **Step 2: Run EditMode test to verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase15ThreeDTableFoundationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-red.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-red-tests.log
```

Expected:

```text
result=Failed
failed>=1
```

The failures should mention missing Phase15 runtime types, card prefab anchors, ReadingRoom anchors, Result anchors, materials, and docs.

- [ ] **Step 3: Add PlayMode test file after runtime type task is ready**

Do not add the PlayMode file before Task 2 creates compileable runtime types. The PlayMode test references `ThreeDCardPresentationController` directly and would create a compile error in the initial RED step.

Checkpoint: record the RED failure split from `TestResults/phase15-red.xml`.

---

## Task 2: Add Runtime Phase15 Presentation Helpers

**Files:**
- Create: `Assets/Scripts/Presentation/ThreeDCardPresentationController.cs`
- Create: `Assets/Scripts/Presentation/Phase15TableStageController.cs`
- Modify: `Assets/Scripts/Gameplay/CardView.cs`
- Create: `Assets/Tests/PlayMode/Phase15ThreeDTableFoundationPlayModeTests.cs`

- [ ] **Step 1: Create `ThreeDCardPresentationController`**

Create `Assets/Scripts/Presentation/ThreeDCardPresentationController.cs`:

```csharp
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ThreeDCardPresentationController : MonoBehaviour
    {
        [SerializeField] private Transform cardMeshRoot;
        [SerializeField] private Renderer cardFaceRenderer;
        [SerializeField] private Renderer cardBackRenderer;
        [SerializeField] private Renderer cardDropShadowRenderer;

        public void SetFaceVisible(bool faceVisible)
        {
            if (cardFaceRenderer != null)
            {
                cardFaceRenderer.enabled = faceVisible;
            }

            if (cardBackRenderer != null)
            {
                cardBackRenderer.enabled = !faceVisible;
            }
        }

        public void SetDropShadowVisible(bool visible)
        {
            if (cardDropShadowRenderer != null)
            {
                cardDropShadowRenderer.enabled = visible;
            }
        }

        public void SetShellVisible(bool visible)
        {
            if (cardMeshRoot != null)
            {
                cardMeshRoot.gameObject.SetActive(visible);
            }
        }
    }
}
```

- [ ] **Step 2: Create `Phase15TableStageController`**

Create `Assets/Scripts/Presentation/Phase15TableStageController.cs`:

```csharp
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class Phase15TableStageController : MonoBehaviour
    {
        [SerializeField] private GameObject tableRoot;
        [SerializeField] private Light warmKeyLight;
        [SerializeField] private Light coolRimLight;

        public void SetStageVisible(bool visible)
        {
            if (tableRoot != null)
            {
                tableRoot.SetActive(visible);
            }

            if (warmKeyLight != null)
            {
                warmKeyLight.enabled = visible;
            }

            if (coolRimLight != null)
            {
                coolRimLight.enabled = visible;
            }
        }
    }
}
```

- [ ] **Step 3: Wire `CardView` to the optional Phase15 helper**

Modify `Assets/Scripts/Gameplay/CardView.cs`:

```csharp
[SerializeField] private ThreeDCardPresentationController threeDPresentationController;
```

Add the field near the existing `dimensionalRevealController` field.

Inside `SetFaceUp`, after front/back roots are updated and before `FaceChanged?.Invoke`, add:

```csharp
if (threeDPresentationController != null)
{
    threeDPresentationController.SetFaceVisible(faceUp);
    threeDPresentationController.SetDropShadowVisible(true);
}
```

The existing `dimensionalRevealController` block remains unchanged.

- [ ] **Step 4: Add PlayMode null-safety test**

Create `Assets/Tests/PlayMode/Phase15ThreeDTableFoundationPlayModeTests.cs`:

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase15ThreeDTableFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator OptionalPhase15PresentationHelpersAreSafeAtRuntime()
        {
            var owner = new GameObject("Phase15 Runtime Safety Card");
            try
            {
                var card = owner.AddComponent<CardView>();
                Assert.DoesNotThrow(() => card.SetFaceUp(true));
                Assert.DoesNotThrow(() => card.SetFaceUp(false));

                var face = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var back = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                face.transform.SetParent(owner.transform, false);
                back.transform.SetParent(owner.transform, false);
                shadow.transform.SetParent(owner.transform, false);

                var controller = owner.AddComponent<ThreeDCardPresentationController>();
                SetPrivateField(controller, "cardFaceRenderer", face);
                SetPrivateField(controller, "cardBackRenderer", back);
                SetPrivateField(controller, "cardDropShadowRenderer", shadow);
                SetPrivateField(card, "threeDPresentationController", controller);

                card.SetFaceUp(true);
                yield return null;
                Assert.That(face.enabled, Is.True);
                Assert.That(back.enabled, Is.False);
                Assert.That(shadow.enabled, Is.True);

                card.SetFaceUp(false);
                yield return null;
                Assert.That(face.enabled, Is.False);
                Assert.That(back.enabled, Is.True);
            }
            finally
            {
                Object.Destroy(owner);
            }
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
```

- [ ] **Step 5: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase15ThreeDTableFoundationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-runtime.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-runtime-editmode.log
```

Expected:

```text
result=Failed
```

Only runtime type and `CardView` field checks should now pass; prefab, scene, material, and documentation checks should still fail.

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testFilter TarotUnity.Tests.PlayMode.Phase15ThreeDTableFoundationPlayModeTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-runtime-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-runtime-playmode.log
```

Expected:

```text
result=Passed
failed=0
```

Checkpoint: record pass/fail split.

---

## Task 3: Add Phase15 Bootstrapper And Visual Anchors

**Files:**
- Create: `Assets/Editor/Phase15ThreeDTableBootstrapper.cs`
- Modify through bootstrapper: `Assets/Prefabs/Cards/PF_TarotCard.prefab`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create through bootstrapper: `Assets/Materials/MAT_Phase15_*.mat`

- [ ] **Step 1: Create the editor bootstrapper**

Create `Assets/Editor/Phase15ThreeDTableBootstrapper.cs` with this code:

```csharp
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase15ThreeDTableBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 15 3D Table Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 15 3D Table Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 15 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 15 3D table bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var bodyMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardBody", new Color(0.20f, 0.12f, 0.055f, 1f));
                var faceMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardFacePlane", new Color(0.92f, 0.82f, 0.62f, 1f));
                var backMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardBackPlane", new Color(0.10f, 0.09f, 0.18f, 1f));
                var edgeMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardSideEdge", new Color(0.12f, 0.075f, 0.038f, 1f));
                var shadowMaterial = EnsureTransparentMaterial("MAT_Phase15_CardDropShadow", new Color(0f, 0f, 0f, 0.38f));

                var meshRoot = EnsureChild(root.transform, "Phase15_CardMeshRoot");
                meshRoot.transform.localPosition = new Vector3(0f, -0.018f, 0f);
                meshRoot.transform.localRotation = Quaternion.identity;
                meshRoot.transform.localScale = Vector3.one;

                var body = EnsureCube(meshRoot.transform, "Phase15_CardBody", Vector3.zero, new Vector3(0.74f, 0.035f, 1.05f), bodyMaterial);
                var face = EnsureQuad(meshRoot.transform, "Phase15_CardFacePlane", new Vector3(0f, 0.022f, -0.002f), new Vector3(0.64f, 0.90f, 1f), faceMaterial);
                var back = EnsureQuad(meshRoot.transform, "Phase15_CardBackPlane", new Vector3(0f, -0.022f, 0.002f), new Vector3(0.70f, 0.98f, 1f), backMaterial);
                back.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var edge = EnsureCube(meshRoot.transform, "Phase15_CardSideEdge", new Vector3(0.385f, 0f, 0f), new Vector3(0.035f, 0.042f, 1.05f), edgeMaterial);
                var shadow = EnsureQuad(meshRoot.transform, "Phase15_CardDropShadow", new Vector3(0.045f, -0.036f, 0.045f), new Vector3(0.82f, 1.14f, 1f), shadowMaterial);
                shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                RemoveCollider(body);
                RemoveCollider(face);
                RemoveCollider(back);
                RemoveCollider(edge);
                RemoveCollider(shadow);

                var controller = root.GetComponent<ThreeDCardPresentationController>();
                if (controller == null)
                {
                    controller = root.AddComponent<ThreeDCardPresentationController>();
                }

                SetSerializedReference(controller, "cardMeshRoot", meshRoot.transform);
                SetSerializedReference(controller, "cardFaceRenderer", face.GetComponent<MeshRenderer>());
                SetSerializedReference(controller, "cardBackRenderer", back.GetComponent<MeshRenderer>());
                SetSerializedReference(controller, "cardDropShadowRenderer", shadow.GetComponent<MeshRenderer>());

                var cardView = root.GetComponent<CardView>();
                if (cardView != null)
                {
                    SetSerializedReference(cardView, "threeDPresentationController", controller);
                }

                var phase14 = root.GetComponent<DimensionalCardRevealController>();
                var phase14Root = root.transform.Find("Phase14_DimensionalRoot");
                if (phase14 != null && phase14Root != null)
                {
                    SetSerializedReference(phase14, "cardRoot", phase14Root);
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

            var root = GameObject.Find("Phase15_ThreeDTableRoot") ?? new GameObject("Phase15_ThreeDTableRoot");
            root.transform.position = new Vector3(0f, -0.56f, 0.08f);
            root.transform.rotation = Quaternion.identity;

            var table = EnsureQuad(root.transform, "Phase15_RitualTableSurface", Vector3.zero, new Vector3(5.90f, 2.45f, 1f), EnsureOpaqueMaterial("MAT_Phase15_RitualTableSurface", new Color(0.045f, 0.028f, 0.045f, 1f)));
            table.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ring = EnsureQuad(root.transform, "Phase15_TableDepthRing", new Vector3(0f, 0.012f, 0.08f), new Vector3(4.65f, 1.74f, 1f), EnsureTransparentMaterial("MAT_Phase15_TableDepthRing", new Color(1f, 0.68f, 0.26f, 0.16f)));
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            EnsureEmptyChild(root.transform, "Phase15_DeckFocusAnchor", new Vector3(-1.72f, 0.10f, -0.30f));
            EnsureEmptyChild(root.transform, "Phase15_SpreadFocusAnchor", new Vector3(0f, 0.12f, 0.08f));
            EnsureEmptyChild(root.transform, "Phase15_FlipFocusAnchor", new Vector3(0.42f, 0.20f, -0.10f));

            var warm = EnsureLight("Phase15_WarmKeyLight", new Vector3(-1.25f, 1.70f, -1.10f), Quaternion.Euler(57f, 28f, 0f), new Color(1f, 0.70f, 0.36f, 1f), 1.02f, 5.4f);
            var cool = EnsureLight("Phase15_CoolRimLight", new Vector3(1.70f, 1.32f, 0.78f), Quaternion.Euler(52f, -35f, 0f), new Color(0.38f, 0.58f, 1f, 1f), 0.64f, 4.2f);

            var controller = root.GetComponent<Phase15TableStageController>();
            if (controller == null)
            {
                controller = root.AddComponent<Phase15TableStageController>();
            }

            SetSerializedReference(controller, "tableRoot", root);
            SetSerializedReference(controller, "warmKeyLight", warm);
            SetSerializedReference(controller, "coolRimLight", cool);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase15_ResultCardStageRoot") ?? new GameObject("Phase15_ResultCardStageRoot");
            root.transform.position = new Vector3(-1.74f, -0.42f, 0.06f);
            root.transform.rotation = Quaternion.identity;

            EnsureCube(root.transform, "Phase15_ResultCardPedestal", Vector3.zero, new Vector3(1.18f, 0.055f, 1.62f), EnsureOpaqueMaterial("MAT_Phase15_ResultPedestal", new Color(0.12f, 0.075f, 0.052f, 1f)));
            EnsureEmptyChild(root.transform, "Phase15_ResultFocusAnchor", new Vector3(0f, 0.28f, -0.10f));

            EnsureLight("Phase15_ResultWarmFocusLight", new Vector3(-2.24f, 1.28f, -0.82f), Quaternion.Euler(58f, 18f, 0f), new Color(1f, 0.68f, 0.32f, 1f), 0.82f, 4.3f);
            EnsureLight("Phase15_ResultCoolEdgeLight", new Vector3(-0.82f, 1.06f, 0.82f), Quaternion.Euler(54f, -30f, 0f), new Color(0.42f, 0.62f, 1f, 1f), 0.48f, 3.2f);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                foreach (var image in canvas.GetComponentsInChildren<Image>(true))
                {
                    if (image.name.StartsWith("Phase15_", System.StringComparison.Ordinal))
                    {
                        image.raycastTarget = false;
                        EditorUtility.SetDirty(image);
                    }
                }
            }

            EditorUtility.SetDirty(root);
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

        private static GameObject EnsureEmptyChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = EnsureChild(parent, name);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
            return child;
        }

        private static GameObject EnsureCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cube = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            SetMaterial(cube, material);
            return cube;
        }

        private static GameObject EnsureQuad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var quad = EnsurePrimitive(parent, name, PrimitiveType.Quad);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = localScale;
            SetMaterial(quad, material);
            return quad;
        }

        private static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType type)
        {
            var existingTransform = parent != null ? parent.Find(name) : null;
            var target = existingTransform != null ? existingTransform.gameObject : null;

            if (target == null || target.GetComponent<MeshFilter>() == null || target.GetComponent<MeshRenderer>() == null)
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }

                target = GameObject.CreatePrimitive(type);
                target.name = name;
                target.transform.SetParent(parent, false);
            }

            RemoveCollider(target);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static Light EnsureLight(string name, Vector3 position, Quaternion rotation, Color color, float intensity, float range)
        {
            var lightObject = GameObject.Find(name) ?? new GameObject(name);
            lightObject.transform.position = position;
            lightObject.transform.rotation = rotation;

            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;

            EditorUtility.SetDirty(lightObject);
            EditorUtility.SetDirty(light);
            return light;
        }

        private static Material EnsureOpaqueMaterial(string materialName, Color color)
        {
            var material = EnsureMaterial(materialName, color);
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");

            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);
            SetFloatIfPresent(material, "_ZWrite", 1f);

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureTransparentMaterial(string materialName, Color color)
        {
            var material = EnsureMaterial(materialName, color);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");

            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMaterial(string materialName, Color color)
        {
            Directory.CreateDirectory(MaterialFolder);
            var materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
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
            EditorUtility.SetDirty(target);
        }
    }
}
```

- [ ] **Step 2: Run the bootstrapper**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase15ThreeDTableBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-bootstrap.log
```

Expected:

```text
exit code 0
Tarot Unity Phase 15 3D table bootstrap complete.
```

Known acceptable noise:

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
```

- [ ] **Step 3: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase15ThreeDTableFoundationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-anchors.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-anchors-tests.log
```

Expected:

```text
result=Failed
```

Only the documentation test should remain failing.

Checkpoint: record prefab, ReadingRoom, Result, material, and Phase13/14 retention tests that now pass.

---

## Task 4: Add Phase15 Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE15_3D_TABLE_FOUNDATION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [ ] **Step 1: Add Phase15 phase document**

Create `Docs/PHASE15_3D_TABLE_FOUNDATION.md` with:

```markdown
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

## Bootstrap Command

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase15ThreeDTableBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-bootstrap.log
```

## Verification

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
```

- [ ] **Step 2: Update UI mainline**

Add to `Docs/UI_COMPLETION_MAINLINE.md` after Phase 14:

```markdown
### Phase 15: 3D Table Foundation

Status: implementation and targeted Phase15 checks completed on 2026-06-03; full phase verification pending final checks.

Purpose:

- Move from a 2.5D card reveal layer toward a real 3D tarot table foundation.
- Add a card mesh shell with body, face, back, side edge, and shadow anchors.
- Add ReadingRoom ritual table, depth ring, deck/spread/flip anchors, and warm/cool lights.
- Add restrained Result card-stage support without sacrificing result text readability.

Exit criteria:

- `PF_TarotCard` contains Phase15 3D card shell anchors.
- ReadingRoom contains Phase15 table, depth, deck/spread/flip focus, and light anchors.
- Result contains Phase15 card-stage, result focus, and light anchors.
- Phase13 card art and Phase14 reveal behavior remain intact.
- Targeted Phase15 tests pass; full EditMode/PlayMode verification is part of the end-check flow.

Remaining limitation:

- Phase15 is a foundation pass. Later phases can add custom sculpted card meshes, stronger magical VFX, particle systems, and final camera/audio polish.
```

During Task 5, update the status to `implemented and verified on 2026-06-03` only after final checks pass.

- [ ] **Step 3: Update README status paragraph**

Modify the current status paragraph in `README.md` so it includes:

```text
Phase 15 adds the first 3D table foundation with card mesh-shell anchors, ritual table depth, warm/cool lighting, and Result card-stage support.
```

- [ ] **Step 4: Run targeted tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase15ThreeDTableFoundationTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-green.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-green-tests.log
```

Expected:

```text
result=Passed
failed=0
```

Checkpoint: record targeted test totals.

---

## Task 5: Final Verification And Review

**Files:**
- Read/check: `Logs/phase15-final-unity-check.log`
- Read/check: `TestResults/phase15-final-editmode.xml`
- Read/check: `TestResults/phase15-final-playmode.xml`
- Modify after checks: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify after checks: `Docs/superpowers/plans/2026-06-03-phase15-3d-table-foundation.md`

- [ ] **Step 1: Confirm no Unity Editor process is active**

Run:

```bash
pgrep -fl "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity|Unity.app/Contents/MacOS/Unity" || true
```

Expected:

```text
no output
```

- [ ] **Step 2: Unity compile/log check**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-unity-check.log -quit
```

Expected:

```text
exit code 0
```

- [ ] **Step 3: Scan Unity logs for project errors**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode" Logs/phase15-final-unity-check.log ~/Library/Logs/Unity/Editor.log || true
```

Expected:

```text
no output
```

Acceptable known external noise can be listed separately:

```bash
rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized" Logs/phase15-final-unity-check.log || true
```

- [ ] **Step 4: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [ ] **Step 5: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase15-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase15-final-playmode.log
```

Expected:

```text
result=Passed
failed=0
VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns Passed
```

- [ ] **Step 6: Scan scene and prefab references**

Run:

```bash
rg -n "m_Script: \\{fileID: 0\\}|m_MissingScript|Missing Script|MissingReferenceException|MissingReference" Assets/Scenes Assets/Prefabs || true
```

Expected:

```text
no output
```

- [ ] **Step 7: Scope and asset self-review**

Run:

```bash
find /Users/maochuandou/BUPT/Game/Tarot -type f -mmin -360 2>/dev/null | sed -n '1,80p'
find Builds -type f -mmin -360 2>/dev/null | sed -n '1,120p'
printf 'RWS image files: '; find Assets/Art/Tarot/RWS1909 -maxdepth 1 \( -name '*.png' -o -name '*.jpg' -o -name '*.jpeg' \) | wc -l
printf 'RWS catalog entries: '; rg -n -- '  - key:' Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset | wc -l
```

Verify:

```text
No backend files changed.
No history/settings/profile/admin/dashboard work added.
No desktop build or release zip regenerated.
RWS image files = 78.
RWS catalog entries = 78.
CardView remains null-safe if Phase15 helper is absent.
Phase14 reveal behavior remains intact.
ReadingRoom and Result Phase15 anchors are visual support only.
```

- [ ] **Step 8: Update final status docs**

After all checks pass, update `Docs/UI_COMPLETION_MAINLINE.md` Phase15 status from:

```text
implementation and targeted Phase15 checks completed on 2026-06-03; full phase verification pending final checks.
```

to:

```text
implemented on 2026-06-03 and verified on 2026-06-04.
```

Update this plan's Task 5 checkpoint with actual results.

- [ ] **Step 9: Final report**

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

## Task 5 Actual Results

Completed on 2026-06-04.

- Unity process check: no active Unity Editor process before final batchmode checks.
- Unity compile/log check: `Logs/phase15-final-unity-check.log` exited 0.
- Project error scan: no compile errors, project exceptions, missing script references, or local keyword errors in final Unity/EditMode/PlayMode logs.
- Known external noise only: Unity licensing access-token update message, UnityConnect config fallback, and batchmode shutdown thread/curl messages.
- Full EditMode: `TestResults/phase15-final-editmode.xml` passed 81/81, failed 0, skipped 0.
- Full PlayMode: `TestResults/phase15-final-playmode.xml` passed 4/4, failed 0, skipped 0.
- Vertical slice: `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed.
- Scene/prefab reference scan: no missing script or missing reference hits under `Assets/Scenes` and `Assets/Prefabs`.
- Scope review: no recent files changed under the old `/Users/maochuandou/BUPT/Game/Tarot` backend/frontend repo, and no new files generated under `Builds`.
- RWS deck integrity: 78 image files and 78 catalog entries.
- Code/document review: Phase15 implementation and documentation reviews approved after the editor bootstrap notes fix.
