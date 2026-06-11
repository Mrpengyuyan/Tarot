# Phase 16 Ritual Aura VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the approved B2 ritual rune aura pass so the Phase15 3D table feels like an active tarot ritual space while preserving the playable vertical slice.

**Architecture:** Add a small null-safe runtime aura helper, then use an idempotent Unity Editor bootstrapper to create Phase16 materials, ReadingRoom aura anchors, Result aura anchors, and scene wiring. Gameplay state, backend calls, RWS1909 artwork mapping, desktop packaging, scene order, and existing card flip behavior remain unchanged.

**Tech Stack:** Unity 6.3 LTS, C#, URP, UnityEditor, NUnit EditMode tests, Unity Test Framework PlayMode tests.

---

## File Structure

- Create: `Assets/Tests/EditMode/Phase16RitualAuraVfxTests.cs`
  - Locks runtime type existence, Phase16 materials, ReadingRoom/Result aura anchors, non-blocking scene objects, Phase13/14/15 retention, and documentation.
- Create: `Assets/Scripts/Presentation/RitualAuraController.cs`
  - Owns only optional aura presentation references and safe visibility/intensity toggles.
- Create: `Assets/Tests/PlayMode/Phase16RitualAuraVfxPlayModeTests.cs`
  - Verifies `RitualAuraController` is null-safe and toggles assigned renderers/lights.
- Create: `Assets/Editor/Phase16RitualAuraBootstrapper.cs`
  - Adds Phase16 materials, ReadingRoom ritual aura anchors, Result aura anchors, and controller wiring.
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create through bootstrapper: `Assets/Materials/MAT_Phase16_*.mat`
- Create: `Docs/PHASE16_RITUAL_AURA_VFX.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`
- Modify after final checks: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify after final checks: `Docs/superpowers/plans/2026-06-04-phase16-ritual-aura-vfx.md`

No git repository is detected under `/Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity`, so task checkpoints replace commit steps.

---

## Task 1: Write Phase16 Failing EditMode Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase16RitualAuraVfxTests.cs`

- [ ] **Step 1: Add the EditMode failing test file**

Create `Assets/Tests/EditMode/Phase16RitualAuraVfxTests.cs`:

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
    public sealed class Phase16RitualAuraVfxTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase16DocPath = "Docs/PHASE16_RITUAL_AURA_VFX.md";

        private static readonly string[] TransparentMaterialPaths =
        {
            "Assets/Materials/MAT_Phase16_AuraGlowPool.mat",
            "Assets/Materials/MAT_Phase16_RuneRing.mat",
            "Assets/Materials/MAT_Phase16_AuraParticle.mat",
        };

        [Test]
        public void RitualAuraControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetAuraVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetRuneVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetParticlesVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetIntensity", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(type.GetProperty("CurrentIntensity"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomHasPhase16RitualAuraAnchors()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_GlowPool"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_RuneRingOuter"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_RuneRingInner"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorNorth"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorEast"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorSouth"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ParticleAnchorWest"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_AuraFocusAnchor"), Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var controller = root.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("auraRoot")?.objectReferenceValue, Is.SameAs(root));
            Assert.That(serialized.FindProperty("glowRenderers")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("runeRenderers")?.arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(serialized.FindProperty("particleRenderers")?.arraySize, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void ResultHasPhase16RitualAuraAnchors()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultGlowPool"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultRuneRing"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultParticleAnchorLeft"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultParticleAnchorRight"), Is.Not.Null);
            Assert.That(root.transform.Find("Phase16_ResultAuraFocusAnchor"), Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(root.GetComponent(controllerType), Is.Not.Null);
        }

        [Test]
        public void Phase16MaterialsUseTransparentRenderSettings()
        {
            foreach (var materialPath in TransparentMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Assert.That(material, Is.Not.Null, $"Missing material at {materialPath}");
                Assert.That(material.renderQueue, Is.EqualTo((int)RenderQueue.Transparent), materialPath);
                Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Transparent"), materialPath);
                AssertMaterialFloat(material, "_Surface", 1f, materialPath);
                AssertMaterialFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha, materialPath);
                AssertMaterialFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha, materialPath);
                AssertMaterialFloat(material, "_ZWrite", 0f, materialPath);
                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True, materialPath);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False, materialPath);
            }
        }

        [Test]
        public void Phase16AuraObjectsDoNotBlockInput()
        {
            AssertNoBlockingObjects(ReadingRoomScenePath, "Phase16_RitualAuraRoot");
            AssertNoBlockingObjects(ResultScenePath, "Phase16_ResultAuraRoot");
        }

        [Test]
        public void Phase16KeepsCardArtAndPriorVisualWiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var card = prefab.GetComponent<CardView>();
            Assert.That(card, Is.Not.Null);

            var serializedCard = new SerializedObject(card);
            Assert.That(serializedCard.FindProperty("faceArtworkRenderer")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedCard.FindProperty("dimensionalRevealController")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedCard.FindProperty("threeDPresentationController")?.objectReferenceValue, Is.Not.Null);

            Assert.That(prefab.transform.Find("Phase14_DimensionalRoot"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Phase15_CardMeshRoot"), Is.Not.Null);
        }

        [Test]
        public void Phase16KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase16DocumentationExists()
        {
            Assert.That(File.Exists(Phase16DocPath), Is.True, $"Missing Phase16 doc at {Phase16DocPath}");

            var text = File.ReadAllText(Phase16DocPath);
            Assert.That(text, Does.Contain("Ritual Aura"));
            Assert.That(text, Does.Contain("RitualAuraController"));
            Assert.That(text, Does.Contain("Phase16_RitualAuraRoot"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static void AssertNoBlockingObjects(string scenePath, string rootName)
        {
            EditorSceneManager.OpenScene(scenePath);

            var root = GameObject.Find(rootName);
            Assert.That(root, Is.Not.Null, rootName);

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert.Fail($"{collider.name} under {rootName} should not have a Collider.");
            }

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, $"{graphic.name} under {rootName} should not block raycasts.");
            }
        }

        private static void AssertMaterialFloat(Material material, string propertyName, float expected, string path)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{path} missing {propertyName}");
            Assert.That(material.GetFloat(propertyName), Is.EqualTo(expected), $"{path} {propertyName}");
        }
    }
}
```

- [ ] **Step 2: Run the targeted EditMode tests and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase16RitualAuraVfxTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-red-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-red-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

The failures should mention missing `RitualAuraController`, missing Phase16 materials, missing Phase16 scene anchors, and missing Phase16 documentation. If the command fails because of a C# compile error, fix the test file before proceeding.

---

## Task 2: Implement RitualAuraController

**Files:**
- Create: `Assets/Scripts/Presentation/RitualAuraController.cs`

- [ ] **Step 1: Add the runtime controller**

Create `Assets/Scripts/Presentation/RitualAuraController.cs`:

```csharp
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualAuraController : MonoBehaviour
    {
        [SerializeField] private GameObject auraRoot;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Renderer[] runeRenderers;
        [SerializeField] private Renderer[] particleRenderers;
        [SerializeField] private Light[] auraLights;

        public float CurrentIntensity { get; private set; } = 1f;

        public void SetAuraVisible(bool visible)
        {
            if (auraRoot != null)
            {
                auraRoot.SetActive(visible);
            }

            SetRenderersVisible(glowRenderers, visible);
            SetRenderersVisible(runeRenderers, visible);
            SetRenderersVisible(particleRenderers, visible);
            SetLightsVisible(auraLights, visible);
        }

        public void SetRuneVisible(bool visible)
        {
            SetRenderersVisible(runeRenderers, visible);
        }

        public void SetParticlesVisible(bool visible)
        {
            SetRenderersVisible(particleRenderers, visible);
        }

        public void SetIntensity(float intensity)
        {
            CurrentIntensity = Mathf.Clamp01(intensity);

            if (auraLights == null)
            {
                return;
            }

            foreach (var auraLight in auraLights)
            {
                if (auraLight != null)
                {
                    auraLight.intensity = CurrentIntensity;
                }
            }
        }

        private static void SetRenderersVisible(Renderer[] renderers, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private static void SetLightsVisible(Light[] lights, bool visible)
        {
            if (lights == null)
            {
                return;
            }

            foreach (var light in lights)
            {
                if (light != null)
                {
                    light.enabled = visible;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Run targeted EditMode and verify partial GREEN**

Run the same command:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase16RitualAuraVfxTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-controller-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-controller-editmode.log
```

Expected:

```text
result=Failed
```

The runtime type test should pass now. Failures should remain for missing scene anchors, materials, and docs.

---

## Task 3: Add Phase16 PlayMode Runtime Safety Tests

**Files:**
- Create: `Assets/Tests/PlayMode/Phase16RitualAuraVfxPlayModeTests.cs`

- [ ] **Step 1: Add PlayMode tests after the runtime type exists**

Create `Assets/Tests/PlayMode/Phase16RitualAuraVfxPlayModeTests.cs`:

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase16RitualAuraVfxPlayModeTests
    {
        [UnityTest]
        public IEnumerator RitualAuraControllerIsSafeWithMissingReferences()
        {
            var owner = new GameObject("Phase16 Runtime Aura Safety");
            try
            {
                var controller = owner.AddComponent<RitualAuraController>();

                Assert.DoesNotThrow(() => controller.SetAuraVisible(true));
                Assert.DoesNotThrow(() => controller.SetRuneVisible(false));
                Assert.DoesNotThrow(() => controller.SetParticlesVisible(false));
                Assert.DoesNotThrow(() => controller.SetIntensity(-1f));
                Assert.That(controller.CurrentIntensity, Is.EqualTo(0f));
                Assert.DoesNotThrow(() => controller.SetIntensity(2f));
                Assert.That(controller.CurrentIntensity, Is.EqualTo(1f));

                yield return null;
            }
            finally
            {
                Object.Destroy(owner);
            }
        }

        [UnityTest]
        public IEnumerator RitualAuraControllerTogglesAssignedVisuals()
        {
            var owner = new GameObject("Phase16 Runtime Aura Toggle");
            try
            {
                var controller = owner.AddComponent<RitualAuraController>();
                var auraRoot = new GameObject("Phase16 Runtime Aura Root");
                auraRoot.transform.SetParent(owner.transform, false);

                var glow = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var rune = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var particle = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                glow.transform.SetParent(owner.transform, false);
                rune.transform.SetParent(owner.transform, false);
                particle.transform.SetParent(owner.transform, false);

                var auraLight = new GameObject("Phase16 Runtime Aura Light").AddComponent<Light>();
                auraLight.transform.SetParent(owner.transform, false);

                SetPrivateField(controller, "auraRoot", auraRoot);
                SetPrivateField(controller, "glowRenderers", new Renderer[] { glow });
                SetPrivateField(controller, "runeRenderers", new Renderer[] { rune });
                SetPrivateField(controller, "particleRenderers", new Renderer[] { particle });
                SetPrivateField(controller, "auraLights", new[] { auraLight });

                controller.SetAuraVisible(false);
                yield return null;
                Assert.That(auraRoot.activeSelf, Is.False);
                Assert.That(glow.enabled, Is.False);
                Assert.That(rune.enabled, Is.False);
                Assert.That(particle.enabled, Is.False);
                Assert.That(auraLight.enabled, Is.False);

                controller.SetAuraVisible(true);
                yield return null;
                Assert.That(auraRoot.activeSelf, Is.True);
                Assert.That(glow.enabled, Is.True);
                Assert.That(rune.enabled, Is.True);
                Assert.That(particle.enabled, Is.True);
                Assert.That(auraLight.enabled, Is.True);

                controller.SetRuneVisible(false);
                Assert.That(rune.enabled, Is.False);
                Assert.That(glow.enabled, Is.True);
                Assert.That(particle.enabled, Is.True);

                controller.SetParticlesVisible(false);
                Assert.That(particle.enabled, Is.False);

                controller.SetIntensity(0.42f);
                Assert.That(controller.CurrentIntensity, Is.EqualTo(0.42f).Within(0.001f));
                Assert.That(auraLight.intensity, Is.EqualTo(0.42f).Within(0.001f));
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

- [ ] **Step 2: Run targeted PlayMode tests and verify GREEN**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testFilter TarotUnity.Tests.PlayMode.Phase16RitualAuraVfxPlayModeTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-playmode-runtime.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-playmode-runtime.log
```

Expected:

```text
result=Passed
failed=0
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.

---

## Task 4: Add Phase16 Editor Bootstrapper And Scene/Material Anchors

**Files:**
- Create: `Assets/Editor/Phase16RitualAuraBootstrapper.cs`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create through bootstrapper: `Assets/Materials/MAT_Phase16_AuraGlowPool.mat`
- Create through bootstrapper: `Assets/Materials/MAT_Phase16_RuneRing.mat`
- Create through bootstrapper: `Assets/Materials/MAT_Phase16_AuraParticle.mat`

- [ ] **Step 1: Create the editor bootstrapper**

Create `Assets/Editor/Phase16RitualAuraBootstrapper.cs`.

Implementation requirements:

```text
namespace: TarotUnity.Editor
class: Phase16RitualAuraBootstrapper
menu item: Tools/Tarot Unity/Run Phase 16 Ritual Aura Bootstrap
public method: Run()
```

`Run()` must:

```csharp
if (EditorApplication.isPlaying)
{
    EditorApplication.isPlaying = false;
    Debug.LogWarning("Exited Play Mode. Run Phase 16 Ritual Aura Bootstrap again after Unity returns to Edit Mode.");
    return;
}

if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
{
    Debug.LogWarning("Phase 16 bootstrap cancelled because current scene changes were not saved.");
    return;
}

Directory.CreateDirectory(MaterialFolder);
UpgradeReadingRoomScene();
UpgradeResultScene();

AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
Debug.Log("Tarot Unity Phase 16 ritual aura bootstrap complete.");
```

Use `using` directives:

```csharp
using System.IO;
using System.Linq;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
```

- [ ] **Step 2: Add persistent material helpers**

Implement transparent material helpers equivalent to Phase15:

```csharp
private static Material EnsureTransparentMaterial(string materialName, Color color)
{
    var material = EnsureMaterial(materialName, color, Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
    material.renderQueue = (int)RenderQueue.Transparent;
    material.SetOverrideTag("RenderType", "Transparent");

    SetFloatIfPresent(material, "_Surface", 1f);
    SetFloatIfPresent(material, "_Blend", 0f);
    SetFloatIfPresent(material, "_AlphaClip", 0f);
    SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
    SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
    SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
    SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
    SetFloatIfPresent(material, "_ZWrite", 0f);

    DisableKeywords(material, "_ALPHAPREMULTIPLY_ON", "_ALPHAMODULATE_ON", "_ALPHATEST_ON");
    EnableKeyword(material, "_SURFACE_TYPE_TRANSPARENT");
    SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
    SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
    EditorUtility.SetDirty(material);
    return material;
}
```

Required material colors:

```csharp
var glowMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraGlowPool", new Color(0.42f, 0.95f, 0.78f, 0.22f));
var runeMaterial = EnsureTransparentMaterial("MAT_Phase16_RuneRing", new Color(1.0f, 0.78f, 0.32f, 0.34f));
var particleMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraParticle", new Color(1.0f, 0.86f, 0.46f, 0.58f));
```

- [ ] **Step 3: Add ReadingRoom aura anchors**

`UpgradeReadingRoomScene()` must:

```csharp
EditorSceneManager.OpenScene(ReadingRoomScenePath);

var phase15Root = FindSceneObject("Phase15_ThreeDTableRoot");
var parent = phase15Root != null ? phase15Root.transform : null;
var root = FindSceneObject("Phase16_RitualAuraRoot") ?? new GameObject("Phase16_RitualAuraRoot");
root.transform.SetParent(parent, false);
root.transform.localPosition = new Vector3(0f, 0.038f, 0.08f);
root.transform.localRotation = Quaternion.identity;
root.transform.localScale = Vector3.one;
```

Create these children under `root`:

```text
Phase16_GlowPool: quad, localPosition (0, 0.012, 0.02), localScale (2.75, 0.86, 1)
Phase16_RuneRingOuter: quad, localPosition (0, 0.018, 0.02), localScale (3.25, 1.02, 1)
Phase16_RuneRingInner: quad, localPosition (0, 0.024, 0.02), localScale (2.35, 0.72, 1)
Phase16_ParticleAnchorNorth: sphere, localPosition (0, 0.06, 0.68), localScale (0.055, 0.055, 0.055)
Phase16_ParticleAnchorEast: sphere, localPosition (1.24, 0.055, 0.02), localScale (0.048, 0.048, 0.048)
Phase16_ParticleAnchorSouth: sphere, localPosition (0, 0.052, -0.62), localScale (0.05, 0.05, 0.05)
Phase16_ParticleAnchorWest: sphere, localPosition (-1.24, 0.055, 0.02), localScale (0.048, 0.048, 0.048)
Phase16_AuraFocusAnchor: empty, localPosition (0, 0.16, 0.02)
```

All quads must use `Quaternion.Euler(90f, 0f, 0f)`.

All primitives must have colliders removed.

Add `RitualAuraController` to `Phase16_RitualAuraRoot` and set:

```text
auraRoot = root
glowRenderers = [Phase16_GlowPool MeshRenderer]
runeRenderers = [Phase16_RuneRingOuter MeshRenderer, Phase16_RuneRingInner MeshRenderer]
particleRenderers = the four particle anchor MeshRenderers
auraLights = empty array
```

- [ ] **Step 4: Add Result aura anchors**

`UpgradeResultScene()` must:

```csharp
EditorSceneManager.OpenScene(ResultScenePath);

var phase15ResultRoot = FindSceneObject("Phase15_ResultCardStageRoot");
var parent = phase15ResultRoot != null ? phase15ResultRoot.transform : null;
var root = FindSceneObject("Phase16_ResultAuraRoot") ?? new GameObject("Phase16_ResultAuraRoot");
root.transform.SetParent(parent, false);
root.transform.localPosition = new Vector3(0f, 0.08f, -0.02f);
root.transform.localRotation = Quaternion.identity;
root.transform.localScale = Vector3.one;
```

Create these children under `root`:

```text
Phase16_ResultGlowPool: quad, localPosition (0, 0.012, 0), localScale (1.28, 0.52, 1)
Phase16_ResultRuneRing: quad, localPosition (0, 0.018, 0), localScale (1.45, 0.58, 1)
Phase16_ResultParticleAnchorLeft: sphere, localPosition (-0.62, 0.06, 0), localScale (0.042, 0.042, 0.042)
Phase16_ResultParticleAnchorRight: sphere, localPosition (0.62, 0.06, 0), localScale (0.042, 0.042, 0.042)
Phase16_ResultAuraFocusAnchor: empty, localPosition (0, 0.20, 0)
```

Set `RitualAuraController` references:

```text
auraRoot = root
glowRenderers = [Phase16_ResultGlowPool MeshRenderer]
runeRenderers = [Phase16_ResultRuneRing MeshRenderer]
particleRenderers = left/right particle MeshRenderers
auraLights = empty array
```

- [ ] **Step 5: Run the bootstrapper**

First confirm Unity is closed:

```bash
pgrep -fl "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity|Unity.app/Contents/MacOS/Unity" || true
```

If there is no output, run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase16RitualAuraBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-bootstrap.log
```

Expected:

```text
exit code 0
Tarot Unity Phase 16 ritual aura bootstrap complete.
```

Known Unity licensing-token and shutdown-thread noise can appear, but project compile errors, exceptions, `Local keyword` errors, or missing script errors are not acceptable.

- [ ] **Step 6: Run targeted EditMode and verify GREEN except docs**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase16RitualAuraVfxTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-anchors-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-anchors-editmode.log
```

Expected:

```text
result=Failed
```

The only remaining failure should be `Phase16DocumentationExists`.

If material, anchor, controller wiring, raycast, or prior phase retention tests fail, fix the bootstrapper and rerun it before moving on.

---

## Task 5: Add Phase16 Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE16_RITUAL_AURA_VFX.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [ ] **Step 1: Add the Phase16 phase document**

Create `Docs/PHASE16_RITUAL_AURA_VFX.md`:

```markdown
# Phase 16 Ritual Aura VFX

Date: 2026-06-04

## Scope

Phase 16 implements the approved B2 direction: a controlled ritual rune aura layer for the current Unity tarot vertical slice.

It adds ReadingRoom and Result aura anchors, transparent aura materials, particle anchor markers, and a null-safe `RitualAuraController` while preserving backend flow, scene order, RWS1909 artwork, Phase14 dimensional reveal behavior, and the Phase15 3D table foundation.

## Runtime Changes

- `RitualAuraController` owns optional aura visibility, rune visibility, particle visibility, and intensity state.
- All references are optional.
- Missing aura references must not break card flip, result reveal, or scene navigation.

## ReadingRoom Changes

`ReadingRoom` gains:

- `Phase16_RitualAuraRoot`
- `Phase16_GlowPool`
- `Phase16_RuneRingOuter`
- `Phase16_RuneRingInner`
- `Phase16_ParticleAnchorNorth`
- `Phase16_ParticleAnchorEast`
- `Phase16_ParticleAnchorSouth`
- `Phase16_ParticleAnchorWest`
- `Phase16_AuraFocusAnchor`

## Result Changes

`Result` gains:

- `Phase16_ResultAuraRoot`
- `Phase16_ResultGlowPool`
- `Phase16_ResultRuneRing`
- `Phase16_ResultParticleAnchorLeft`
- `Phase16_ResultParticleAnchorRight`
- `Phase16_ResultAuraFocusAnchor`

## Materials

Phase16 creates:

- `MAT_Phase16_AuraGlowPool`
- `MAT_Phase16_RuneRing`
- `MAT_Phase16_AuraParticle`

All Phase16 materials are transparent URP-compatible materials intended for light atmosphere, not final high-end VFX.

## Out Of Scope

Phase 16 does not add backend/API changes, history, settings, profile, admin, dashboard work, release zip regeneration, tarot art replacement, final particle systems, shader graph animation, post-processing, camera shake, or broad UI redesign.

## Editor Bootstrap Notes

`Phase16RitualAuraBootstrapper` is an idempotent Unity Editor utility used to create or refresh Phase16 materials and scene anchors. It is not a runtime system and is not included in player builds.

The bootstrapper should be rerun only when Phase16 anchors need to be regenerated. Close the Unity Editor before running it in batchmode.

## Verification

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
```

- [ ] **Step 2: Update UI completion mainline**

Add a Phase16 section after Phase15 in `Docs/UI_COMPLETION_MAINLINE.md`:

```markdown
### Phase 16: Ritual Aura VFX

Status: implementation and targeted Phase16 checks pending final verification.

Purpose:

- Add a controlled ritual aura layer around the Phase15 3D tarot table.
- Use glow pool, rune ring, and particle anchors to make ReadingRoom feel more like an active ritual space.
- Add a restrained Result aura support layer without sacrificing text readability.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, and Phase15 3D table foundation intact.

Exit criteria:

- ReadingRoom contains Phase16 aura root, glow, rune, particle, and focus anchors.
- Result contains Phase16 aura root, glow, rune, particle, and focus anchors.
- `RitualAuraController` is null-safe and controls optional aura references.
- Phase16 transparent materials are persistent and test-covered.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase16 is an atmosphere foundation pass. Later phases can add true particle systems, shader graph rune animation, card trails, camera choreography, post-processing, and final VFX/audio polish.
```

- [ ] **Step 3: Update README status paragraph**

Modify the current status paragraph in `README.md` so it includes:

```text
Phase 16 adds a controlled ritual aura VFX layer with glow pools, rune-ring anchors, particle anchor markers, and Result aura support.
```

- [ ] **Step 4: Run targeted EditMode and verify GREEN**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase16RitualAuraVfxTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-green-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-green-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

---

## Task 6: Final Verification And Review

**Files:**
- Read/check: `Logs/phase16-final-unity-check.log`
- Read/check: `TestResults/phase16-final-editmode.xml`
- Read/check: `TestResults/phase16-final-playmode.xml`
- Modify after checks: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify after checks: `Docs/superpowers/plans/2026-06-04-phase16-ritual-aura-vfx.md`

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
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-unity-check.log -quit
```

Expected:

```text
exit code 0
```

- [ ] **Step 3: Scan Unity logs for project errors**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" Logs/phase16-final-unity-check.log || true
tail -n 500 "$HOME/Library/Logs/Unity/Editor.log" | rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" || true
```

Expected:

```text
no output
```

Acceptable known external noise can be listed separately:

```bash
rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized|Licensing" Logs/phase16-final-unity-check.log || true
tail -n 500 "$HOME/Library/Logs/Unity/Editor.log" | rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized|Licensing" || true
```

- [ ] **Step 4: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [ ] **Step 5: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase16-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase16-final-playmode.log
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
CardView Phase13, Phase14, and Phase15 wiring remain intact.
ReadingRoom and Result Phase16 anchors are visual support only.
```

- [ ] **Step 8: Update final status docs**

After all checks pass, update `Docs/UI_COMPLETION_MAINLINE.md` Phase16 status from:

```text
implementation and targeted Phase16 checks pending final verification.
```

to:

```text
implemented and verified on 2026-06-04.
```

Update this plan's Task 6 checkpoint with actual results.

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

---

## Task 6 Actual Results

Date: 2026-06-04

- Unity Editor process check: no active Unity Editor process before batchmode verification.
- Unity compile/import check: `Logs/phase16-final-unity-check.log` completed with exit code 0.
- Project error scan: no `error CS`, compiler errors, compilation failures, project exceptions, missing scripts, missing references, fatal batchmode errors, or LocalKeyword errors found in the final Unity log scan.
- Known external Unity noise: licensing access-token refresh, UnityConnect config fallback, Curl callback abort on shutdown, and prematurely finalized batchmode shutdown threads.
- Full EditMode: `TestResults/phase16-final-editmode.xml` reported 89 passed, 0 failed, 0 skipped.
- Full PlayMode: `TestResults/phase16-final-playmode.xml` reported 6 passed, 0 failed, 0 skipped.
- Vertical slice: `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed, covering Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result.
- Scene/prefab reference scan: no missing script or missing reference markers found under `Assets/Scenes` or `Assets/Prefabs`.
- Scope review: no backend files changed, no desktop build or release zip regenerated, RWS image count remained 78, and the RWS catalog remained 78 entries.

---

## Plan Self-Review

- Spec coverage: covered runtime helper, ReadingRoom anchors, Result anchors, materials, docs, EditMode tests, PlayMode tests, and end-check flow.
- Open-marker scan: no unresolved marker tokens or unspecified implementation markers remain in the plan.
- Scope check: plan excludes backend/API, history, settings, profile, admin, dashboard, release zip, tarot art replacement, final particle systems, shader graph, camera shake, post-processing, and broad UI redesign.
- Type consistency: `RitualAuraController`, `Phase16RitualAuraBootstrapper`, `Phase16RitualAuraVfxTests`, and `Phase16RitualAuraVfxPlayModeTests` names are consistent across tasks.
