# Phase 17 Ritual Aura Runtime Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add lightweight runtime motion to the Phase16 ritual aura layer without changing gameplay, backend behavior, release packaging, or broad UI layout.

**Architecture:** Add a null-safe `RitualAuraMotionController` that animates existing Phase16 transforms through deterministic `Tick()` calls and normal `Update()` runtime flow. Use an idempotent Unity Editor bootstrapper to wire the controller onto existing ReadingRoom and Result Phase16 roots. Keep the motion layer transform-only so it remains easy to test and safe if references are missing.

**Tech Stack:** Unity 6.3 LTS, C#, URP, UnityEditor, NUnit EditMode tests, Unity Test Framework PlayMode tests.

---

## File Structure

- Create: `Assets/Tests/EditMode/Phase17RitualAuraRuntimeMotionTests.cs`
  - Locks the Phase17 runtime type, scene wiring, Phase16/13/14/15 retention, and documentation.
- Create: `Assets/Scripts/Presentation/RitualAuraMotionController.cs`
  - Owns optional runtime aura motion transforms and deterministic motion methods.
- Create: `Assets/Tests/PlayMode/Phase17RitualAuraRuntimeMotionPlayModeTests.cs`
  - Verifies null safety, transform motion, pause behavior, and reset behavior.
- Create: `Assets/Editor/Phase17RitualAuraMotionBootstrapper.cs`
  - Wires `RitualAuraMotionController` to existing Phase16 anchors in ReadingRoom and Result.
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`
- Create: `Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`
- Modify after final checks: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify after final checks: `Docs/superpowers/plans/2026-06-05-phase17-ritual-aura-runtime-motion.md`

No backend files, release build files, tarot artwork assets, history/settings/profile/admin/dashboard code, or desktop release zip files are in scope.

---

## Task 1: Write Phase17 Failing EditMode Tests

**Files:**
- Create: `Assets/Tests/EditMode/Phase17RitualAuraRuntimeMotionTests.cs`

- [ ] **Step 1: Add the EditMode failing test file**

Create `Assets/Tests/EditMode/Phase17RitualAuraRuntimeMotionTests.cs`:

```csharp
using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase17RitualAuraRuntimeMotionTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase17DocPath = "Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md";

        [Test]
        public void RitualAuraMotionControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.RitualAuraMotionController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetAnimating", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("Tick", new[] { typeof(float), typeof(float) }), Is.Not.Null);
            Assert.That(type.GetMethod("ResetMotion", Type.EmptyTypes), Is.Not.Null);
            Assert.That(type.GetProperty("IsAnimating"), Is.Not.Null);
            Assert.That(type.GetProperty("CurrentPulse"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomHasPhase17MotionWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(root, Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraMotionController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var motion = root.GetComponent(controllerType);
            Assert.That(motion, Is.Not.Null);

            var serialized = new SerializedObject(motion);
            Assert.That(serialized.FindProperty("auraController")?.objectReferenceValue, Is.SameAs(root.GetComponent<RitualAuraController>()));
            Assert.That(serialized.FindProperty("motionRoot")?.objectReferenceValue, Is.SameAs(root.transform));
            Assert.That(serialized.FindProperty("glowPulsers")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("runeRings")?.arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(serialized.FindProperty("particleAnchors")?.arraySize, Is.GreaterThanOrEqualTo(4));
            Assert.That(serialized.FindProperty("animateOnEnable")?.boolValue, Is.True);
        }

        [Test]
        public void ResultHasPhase17MotionWiring()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(root, Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualAuraMotionController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var motion = root.GetComponent(controllerType);
            Assert.That(motion, Is.Not.Null);

            var serialized = new SerializedObject(motion);
            Assert.That(serialized.FindProperty("auraController")?.objectReferenceValue, Is.SameAs(root.GetComponent<RitualAuraController>()));
            Assert.That(serialized.FindProperty("motionRoot")?.objectReferenceValue, Is.SameAs(root.transform));
            Assert.That(serialized.FindProperty("glowPulsers")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("runeRings")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("particleAnchors")?.arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(serialized.FindProperty("animateOnEnable")?.boolValue, Is.True);
        }

        [Test]
        public void Phase17KeepsPhase16AuraAnchorsAvailable()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            Assert.That(GameObject.Find("Phase16_RitualAuraRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase16_GlowPool"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase16_RuneRingOuter"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase16_RuneRingInner"), Is.Not.Null);

            EditorSceneManager.OpenScene(ResultScenePath);
            Assert.That(GameObject.Find("Phase16_ResultAuraRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase16_ResultGlowPool"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase16_ResultRuneRing"), Is.Not.Null);
        }

        [Test]
        public void Phase17KeepsCardArtAndPriorVisualWiring()
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
        public void Phase17KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase17DocumentationExists()
        {
            Assert.That(File.Exists(Phase17DocPath), Is.True, $"Missing Phase17 doc at {Phase17DocPath}");

            var text = File.ReadAllText(Phase17DocPath);
            Assert.That(text, Does.Contain("Ritual Aura Runtime Motion"));
            Assert.That(text, Does.Contain("RitualAuraMotionController"));
            Assert.That(text, Does.Contain("Phase16_RitualAuraRoot"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}
```

- [ ] **Step 2: Run the targeted EditMode tests and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase17RitualAuraRuntimeMotionTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-red-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-red-editmode.log
```

Expected:

```text
result=Failed
failed > 0
```

The failures should mention missing `RitualAuraMotionController`, missing Phase17 scene wiring, and missing Phase17 documentation. If the command fails because of a C# compile error, fix the test file before proceeding.

---

## Task 2: Implement RitualAuraMotionController

**Files:**
- Create: `Assets/Scripts/Presentation/RitualAuraMotionController.cs`

- [ ] **Step 1: Add the runtime controller**

Create `Assets/Scripts/Presentation/RitualAuraMotionController.cs`:

```csharp
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualAuraMotionController : MonoBehaviour
    {
        private const float TwoPi = Mathf.PI * 2f;

        [SerializeField] private RitualAuraController auraController;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private Transform[] glowPulsers;
        [SerializeField] private Transform[] runeRings;
        [SerializeField] private Transform[] particleAnchors;
        [SerializeField] private bool animateOnEnable = true;
        [SerializeField] private float runeRotationSpeedDegrees = 7.5f;
        [SerializeField] private float pulseSpeed = 0.55f;
        [SerializeField] private float pulseAmplitude = 0.08f;
        [SerializeField] private float particleFloatSpeed = 0.75f;
        [SerializeField] private float particleFloatAmplitude = 0.018f;

        private Vector3[] baseGlowScales;
        private Vector3[] baseParticlePositions;

        public bool IsAnimating { get; private set; }
        public float CurrentPulse { get; private set; } = 1f;

        private void OnEnable()
        {
            CaptureBaseState();
            SetAnimating(animateOnEnable);
        }

        private void Update()
        {
            if (IsAnimating)
            {
                Tick(Time.deltaTime, Time.time);
            }
        }

        public void SetAnimating(bool animating)
        {
            IsAnimating = animating;
        }

        public void Tick(float deltaSeconds, float elapsedSeconds)
        {
            CaptureBaseState();

            if (!IsAnimating)
            {
                return;
            }

            RotateRuneRings(deltaSeconds);
            PulseGlowPulsers(elapsedSeconds);
            FloatParticleAnchors(elapsedSeconds);
        }

        public void ResetMotion()
        {
            CaptureBaseState();
            CurrentPulse = 1f;

            if (glowPulsers != null && baseGlowScales != null)
            {
                for (var i = 0; i < glowPulsers.Length; i++)
                {
                    if (glowPulsers[i] != null && i < baseGlowScales.Length)
                    {
                        glowPulsers[i].localScale = baseGlowScales[i];
                    }
                }
            }

            if (particleAnchors != null && baseParticlePositions != null)
            {
                for (var i = 0; i < particleAnchors.Length; i++)
                {
                    if (particleAnchors[i] != null && i < baseParticlePositions.Length)
                    {
                        particleAnchors[i].localPosition = baseParticlePositions[i];
                    }
                }
            }
        }

        private void RotateRuneRings(float deltaSeconds)
        {
            if (runeRings == null)
            {
                return;
            }

            for (var i = 0; i < runeRings.Length; i++)
            {
                var runeRing = runeRings[i];
                if (runeRing == null)
                {
                    continue;
                }

                var direction = i % 2 == 0 ? 1f : -1f;
                runeRing.localRotation *= Quaternion.Euler(0f, runeRotationSpeedDegrees * direction * deltaSeconds, 0f);
            }
        }

        private void PulseGlowPulsers(float elapsedSeconds)
        {
            CurrentPulse = Mathf.Max(0f, 1f + Mathf.Sin(elapsedSeconds * pulseSpeed * TwoPi) * Mathf.Max(0f, pulseAmplitude));

            if (glowPulsers == null || baseGlowScales == null)
            {
                return;
            }

            for (var i = 0; i < glowPulsers.Length; i++)
            {
                if (glowPulsers[i] != null && i < baseGlowScales.Length)
                {
                    glowPulsers[i].localScale = baseGlowScales[i] * CurrentPulse;
                }
            }
        }

        private void FloatParticleAnchors(float elapsedSeconds)
        {
            if (particleAnchors == null || baseParticlePositions == null)
            {
                return;
            }

            var amplitude = Mathf.Max(0f, particleFloatAmplitude);
            for (var i = 0; i < particleAnchors.Length; i++)
            {
                if (particleAnchors[i] == null || i >= baseParticlePositions.Length)
                {
                    continue;
                }

                var phase = i * 0.73f;
                var offset = Mathf.Sin(elapsedSeconds * particleFloatSpeed * TwoPi + phase) * amplitude;
                particleAnchors[i].localPosition = baseParticlePositions[i] + Vector3.up * offset;
            }
        }

        private void CaptureBaseState()
        {
            if (glowPulsers != null && (baseGlowScales == null || baseGlowScales.Length != glowPulsers.Length))
            {
                baseGlowScales = new Vector3[glowPulsers.Length];
                for (var i = 0; i < glowPulsers.Length; i++)
                {
                    baseGlowScales[i] = glowPulsers[i] != null ? glowPulsers[i].localScale : Vector3.one;
                }
            }

            if (particleAnchors != null && (baseParticlePositions == null || baseParticlePositions.Length != particleAnchors.Length))
            {
                baseParticlePositions = new Vector3[particleAnchors.Length];
                for (var i = 0; i < particleAnchors.Length; i++)
                {
                    baseParticlePositions[i] = particleAnchors[i] != null ? particleAnchors[i].localPosition : Vector3.zero;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Run targeted EditMode and verify partial GREEN**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase17RitualAuraRuntimeMotionTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-controller-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-controller-editmode.log
```

Expected:

```text
result=Failed
```

The runtime type test should pass now. Failures should remain for missing Phase17 scene wiring and docs.

---

## Task 3: Add Phase17 PlayMode Runtime Motion Tests

**Files:**
- Create: `Assets/Tests/PlayMode/Phase17RitualAuraRuntimeMotionPlayModeTests.cs`

- [ ] **Step 1: Add PlayMode tests after the runtime type exists**

Create `Assets/Tests/PlayMode/Phase17RitualAuraRuntimeMotionPlayModeTests.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase17RitualAuraRuntimeMotionPlayModeTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDownCreatedObjects()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.Destroy(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraMotionControllerIsSafeWithMissingReferences()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion Safety"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            Assert.DoesNotThrow(() => controller.SetAnimating(true));
            Assert.DoesNotThrow(() => controller.Tick(0.1f, 0.25f));
            Assert.DoesNotThrow(() => controller.SetAnimating(false));
            Assert.DoesNotThrow(() => controller.Tick(0.1f, 0.5f));
            Assert.DoesNotThrow(controller.ResetMotion);
            Assert.That(controller.CurrentPulse, Is.EqualTo(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraMotionControllerAnimatesAssignedTransforms()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            var glow = CreateChild("Phase17 Glow", owner.transform);
            glow.localScale = new Vector3(2f, 1f, 1f);
            var rune = CreateChild("Phase17 Rune", owner.transform);
            var particle = CreateChild("Phase17 Particle", owner.transform);
            particle.localPosition = new Vector3(0f, 0.5f, 0f);

            SetPrivateField(controller, "glowPulsers", new[] { glow });
            SetPrivateField(controller, "runeRings", new[] { rune });
            SetPrivateField(controller, "particleAnchors", new[] { particle });
            SetPrivateField(controller, "pulseSpeed", 0.25f);
            SetPrivateField(controller, "pulseAmplitude", 0.2f);
            SetPrivateField(controller, "particleFloatSpeed", 0.25f);
            SetPrivateField(controller, "particleFloatAmplitude", 0.1f);
            SetPrivateField(controller, "runeRotationSpeedDegrees", 10f);

            var baseGlowScale = glow.localScale;
            var baseRuneRotation = rune.localRotation;
            var baseParticlePosition = particle.localPosition;

            controller.SetAnimating(true);
            controller.Tick(0.5f, 1f);

            Assert.That(rune.localRotation, Is.Not.EqualTo(baseRuneRotation));
            Assert.That(glow.localScale, Is.Not.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.Not.EqualTo(baseParticlePosition));
            Assert.That(controller.CurrentPulse, Is.GreaterThan(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraMotionControllerPauseAndResetAreDeterministic()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion Reset"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            var glow = CreateChild("Phase17 Reset Glow", owner.transform);
            glow.localScale = new Vector3(1.5f, 0.75f, 1f);
            var rune = CreateChild("Phase17 Reset Rune", owner.transform);
            var particle = CreateChild("Phase17 Reset Particle", owner.transform);
            particle.localPosition = new Vector3(0.1f, 0.2f, 0.3f);

            SetPrivateField(controller, "glowPulsers", new[] { glow });
            SetPrivateField(controller, "runeRings", new[] { rune });
            SetPrivateField(controller, "particleAnchors", new[] { particle });
            SetPrivateField(controller, "pulseSpeed", 0.25f);
            SetPrivateField(controller, "pulseAmplitude", 0.2f);
            SetPrivateField(controller, "particleFloatSpeed", 0.25f);
            SetPrivateField(controller, "particleFloatAmplitude", 0.1f);

            var baseGlowScale = glow.localScale;
            var baseParticlePosition = particle.localPosition;

            controller.SetAnimating(false);
            controller.Tick(1f, 1f);
            Assert.That(glow.localScale, Is.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.EqualTo(baseParticlePosition));

            controller.SetAnimating(true);
            controller.Tick(1f, 1f);
            Assert.That(glow.localScale, Is.Not.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.Not.EqualTo(baseParticlePosition));

            controller.ResetMotion();
            Assert.That(glow.localScale, Is.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.EqualTo(baseParticlePosition));
            Assert.That(controller.CurrentPulse, Is.EqualTo(1f));

            yield return null;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private GameObject Track(GameObject gameObject)
        {
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Transform CreateChild(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }
    }
}
```

- [ ] **Step 2: Run targeted PlayMode tests and verify GREEN**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testFilter TarotUnity.Tests.PlayMode.Phase17RitualAuraRuntimeMotionPlayModeTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-playmode-runtime.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-playmode-runtime.log
```

Expected:

```text
result=Passed
failed=0
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.

---

## Task 4: Add Phase17 Editor Bootstrapper And Scene Wiring

**Files:**
- Create: `Assets/Editor/Phase17RitualAuraMotionBootstrapper.cs`
- Modify through bootstrapper: `Assets/Scenes/ReadingRoom.unity`
- Modify through bootstrapper: `Assets/Scenes/Result.unity`

- [ ] **Step 1: Create the editor bootstrapper**

Create `Assets/Editor/Phase17RitualAuraMotionBootstrapper.cs`:

```csharp
using System.Linq;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase17RitualAuraMotionBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 17 Ritual Aura Motion Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 17 Ritual Aura Motion Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 17 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 17 ritual aura motion bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = FindSceneObject("Phase16_RitualAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 17 bootstrap skipped ReadingRoom because Phase16_RitualAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var controller = EnsureMotionController(root);
            SetSerializedReference(controller, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedReference(controller, "motionRoot", root.transform);
            SetSerializedArrayReferences(controller, "glowPulsers", FindChild(root, "Phase16_GlowPool"));
            SetSerializedArrayReferences(controller, "runeRings", FindChild(root, "Phase16_RuneRingOuter"), FindChild(root, "Phase16_RuneRingInner"));
            SetSerializedArrayReferences(
                controller,
                "particleAnchors",
                FindChild(root, "Phase16_ParticleAnchorNorth"),
                FindChild(root, "Phase16_ParticleAnchorEast"),
                FindChild(root, "Phase16_ParticleAnchorSouth"),
                FindChild(root, "Phase16_ParticleAnchorWest"));
            SetSerializedBool(controller, "animateOnEnable", true);
            SetSerializedFloat(controller, "runeRotationSpeedDegrees", 7.5f);
            SetSerializedFloat(controller, "pulseSpeed", 0.55f);
            SetSerializedFloat(controller, "pulseAmplitude", 0.08f);
            SetSerializedFloat(controller, "particleFloatSpeed", 0.75f);
            SetSerializedFloat(controller, "particleFloatAmplitude", 0.018f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = FindSceneObject("Phase16_ResultAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 17 bootstrap skipped Result because Phase16_ResultAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var controller = EnsureMotionController(root);
            SetSerializedReference(controller, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedReference(controller, "motionRoot", root.transform);
            SetSerializedArrayReferences(controller, "glowPulsers", FindChild(root, "Phase16_ResultGlowPool"));
            SetSerializedArrayReferences(controller, "runeRings", FindChild(root, "Phase16_ResultRuneRing"));
            SetSerializedArrayReferences(controller, "particleAnchors", FindChild(root, "Phase16_ResultParticleAnchorLeft"), FindChild(root, "Phase16_ResultParticleAnchorRight"));
            SetSerializedBool(controller, "animateOnEnable", true);
            SetSerializedFloat(controller, "runeRotationSpeedDegrees", 4.5f);
            SetSerializedFloat(controller, "pulseSpeed", 0.45f);
            SetSerializedFloat(controller, "pulseAmplitude", 0.06f);
            SetSerializedFloat(controller, "particleFloatSpeed", 0.65f);
            SetSerializedFloat(controller, "particleFloatAmplitude", 0.012f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static RitualAuraMotionController EnsureMotionController(GameObject root)
        {
            var controller = root.GetComponent<RitualAuraMotionController>();
            if (controller == null)
            {
                controller = root.AddComponent<RitualAuraMotionController>();
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Transform FindChild(GameObject root, string childName)
        {
            return root != null ? root.transform.Find(childName) : null;
        }

        private static GameObject FindSceneObject(string name)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == name && candidate.scene == activeScene && !EditorUtility.IsPersistent(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
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

        private static void SetSerializedArrayReferences(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"Missing serialized array property {propertyName} on {target.name}");
                return;
            }

            var nonNullValues = values.Where(value => value != null).ToArray();
            property.arraySize = nonNullValues.Length;
            for (var i = 0; i < nonNullValues.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = nonNullValues[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized bool property {propertyName} on {target.name}");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized float property {propertyName} on {target.name}");
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
```

- [ ] **Step 2: Run the bootstrapper**

First confirm Unity is closed:

```bash
pgrep -fl "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity|Unity.app/Contents/MacOS/Unity" || true
```

If there is no output, run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase17RitualAuraMotionBootstrapper.Run -quit -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-bootstrap.log
```

Expected:

```text
exit code 0
Tarot Unity Phase 17 ritual aura motion bootstrap complete.
```

Known Unity licensing-token and shutdown-thread noise can appear, but project compile errors, exceptions, missing script errors, or LocalKeyword errors are not acceptable.

- [ ] **Step 3: Run targeted EditMode and verify GREEN except docs**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase17RitualAuraRuntimeMotionTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-wiring-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-wiring-editmode.log
```

Expected:

```text
result=Failed
```

The only remaining failure should be `Phase17DocumentationExists`. If runtime type, scene wiring, Phase16 retention, Phase13 catalog, or Phase14/15 card wiring tests fail, fix the controller/bootstrapper and rerun.

---

## Task 5: Add Phase17 Documentation And Mainline Updates

**Files:**
- Create: `Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md`
- Modify: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify: `README.md`

- [ ] **Step 1: Add the Phase17 phase document**

Create `Docs/PHASE17_RITUAL_AURA_RUNTIME_MOTION.md`:

```markdown
# Phase 17 Ritual Aura Runtime Motion

Date: 2026-06-05

## Scope

Phase 17 implements a lightweight runtime motion pass for the Phase16 ritual aura layer.

It adds `RitualAuraMotionController`, ReadingRoom motion wiring, and Result motion wiring while preserving backend flow, scene order, RWS1909 artwork, Phase14 dimensional reveal behavior, Phase15 3D table foundation, and Phase16 aura anchors.

## Runtime Changes

- `RitualAuraMotionController` rotates rune-ring transforms.
- `RitualAuraMotionController` pulses glow transforms.
- `RitualAuraMotionController` floats particle-anchor transforms.
- All references are optional.
- Missing motion references must not break card flip, result reveal, or scene navigation.

## ReadingRoom Changes

`ReadingRoom` wires `RitualAuraMotionController` on `Phase16_RitualAuraRoot`.

The controller targets:

- `Phase16_GlowPool`
- `Phase16_RuneRingOuter`
- `Phase16_RuneRingInner`
- `Phase16_ParticleAnchorNorth`
- `Phase16_ParticleAnchorEast`
- `Phase16_ParticleAnchorSouth`
- `Phase16_ParticleAnchorWest`

## Result Changes

`Result` wires `RitualAuraMotionController` on `Phase16_ResultAuraRoot`.

The controller targets:

- `Phase16_ResultGlowPool`
- `Phase16_ResultRuneRing`
- `Phase16_ResultParticleAnchorLeft`
- `Phase16_ResultParticleAnchorRight`

## Out Of Scope

Phase 17 does not add backend/API changes, history, settings, profile, admin, dashboard work, release zip regeneration, tarot art replacement, final particle systems, shader graph animation, post-processing, camera shake, or broad UI redesign.

This is not final high-end VFX.

## Editor Bootstrap Notes

`Phase17RitualAuraMotionBootstrapper` is an idempotent Unity Editor utility used to create or refresh Phase17 motion wiring on existing Phase16 aura roots. It is not a runtime system and is not included in player builds.

Run the Phase16 bootstrapper first if Phase16 anchors are missing.

## Verification

Run full EditMode and PlayMode tests after implementation:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-playmode.log
```

Do not add `-quit` to `-runTests` commands in Unity 6.3.
```

- [ ] **Step 2: Update UI completion mainline**

Add a Phase17 section after Phase16 in `Docs/UI_COMPLETION_MAINLINE.md`:

```markdown
### Phase 17: Ritual Aura Runtime Motion

Status: implementation and targeted Phase17 checks pending final verification.

Purpose:

- Add lightweight runtime motion to the Phase16 ritual aura layer.
- Rotate rune-ring anchors, pulse glow anchors, and float particle anchors without changing gameplay.
- Keep ReadingRoom and Result more alive while preserving text readability and card art visibility.
- Keep the real RWS1909 card artwork, Phase14 dimensional reveal, Phase15 3D table foundation, and Phase16 aura anchors intact.

Exit criteria:

- `RitualAuraMotionController` is null-safe and test-covered.
- ReadingRoom wires Phase17 motion on `Phase16_RitualAuraRoot`.
- Result wires Phase17 motion on `Phase16_ResultAuraRoot`.
- Phase16 anchors and materials remain intact.
- EditMode and PlayMode tests pass.

Remaining limitation:

- Phase17 is a transform-only runtime motion pass. Later phases can add true particle systems, Shader Graph rune animation, card trails, camera choreography, post-processing, and final VFX/audio polish.
```

- [ ] **Step 3: Update README status paragraph**

Modify the current status paragraph in `README.md` so it includes:

```text
Phase 17 adds lightweight runtime aura motion for rune-ring rotation, glow pulsing, and particle-anchor drift.
```

- [ ] **Step 4: Run targeted EditMode and verify GREEN**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase17RitualAuraRuntimeMotionTests -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-green-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-green-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

---

## Task 6: Final Verification And Review

**Files:**
- Read/check: `Logs/phase17-final-unity-check.log`
- Read/check: `TestResults/phase17-final-editmode.xml`
- Read/check: `TestResults/phase17-final-playmode.xml`
- Modify after checks: `Docs/UI_COMPLETION_MAINLINE.md`
- Modify after checks: `Docs/superpowers/plans/2026-06-05-phase17-ritual-aura-runtime-motion.md`

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
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-unity-check.log -quit
```

Expected:

```text
exit code 0
```

- [ ] **Step 3: Scan Unity logs for project errors**

Run:

```bash
rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" Logs/phase17-final-unity-check.log || true
tail -n 500 "$HOME/Library/Logs/Unity/Editor.log" | rg -n "error CS|CompilerError|Compilation failed|Exception:|NullReferenceException|MissingReferenceException|Missing Script|m_Script: \\{fileID: 0\\}|Fatal|Aborting batchmode|Local keyword" || true
```

Expected:

```text
no output
```

Acceptable known external noise can be listed separately:

```bash
rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized|Licensing" Logs/phase17-final-unity-check.log || true
tail -n 500 "$HOME/Library/Logs/Unity/Editor.log" | rg -n "Error: Access token|UnityConnect|Request timed out|Curl error|Shader warning|Ray Tracing|TraceVirtualOffset|Callback aborted|prematurely finalized|Licensing" || true
```

- [ ] **Step 4: Run full EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-editmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-editmode.log
```

Expected:

```text
result=Passed
failed=0
```

- [ ] **Step 5: Run full PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform PlayMode -testResults /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/TestResults/phase17-final-playmode.xml -logFile /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity/Logs/phase17-final-playmode.log
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
CardView Phase13, Phase14, Phase15, and Phase16 wiring remain intact.
ReadingRoom and Result Phase17 motion wiring is visual support only.
```

- [ ] **Step 8: Update final status docs**

After all checks pass, update `Docs/UI_COMPLETION_MAINLINE.md` Phase17 status from:

```text
implementation and targeted Phase17 checks pending final verification.
```

to:

```text
implemented and verified on 2026-06-05.
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

Date: 2026-06-05

- Unity Editor process check: no active Unity Editor process before batchmode verification.
- Unity compile/import check: `Logs/phase17-final-unity-check.log` completed with exit code 0.
- Project error scan: no `error CS`, compiler errors, compilation failures, project exceptions, missing scripts, missing references, fatal batchmode errors, or LocalKeyword errors found in the final Unity log scan.
- Known external Unity noise: licensing access-token refresh, UnityConnect config fallback, Curl callback abort on shutdown, debugger-agent listen failure on shutdown, and prematurely finalized batchmode shutdown threads.
- Full EditMode: `TestResults/phase17-final-editmode.xml` reported 96 passed, 0 failed, 0 skipped.
- Full PlayMode: `TestResults/phase17-final-playmode.xml` reported 9 passed, 0 failed, 0 skipped.
- Vertical slice: `VerticalSliceFlowTests.MainMenuToResultVerticalSliceRuns` passed, covering Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result.
- Scene/prefab reference scan: no missing script or missing reference markers found under `Assets/Scenes` or `Assets/Prefabs`.
- Scope review: no backend files changed, no desktop build or release zip regenerated, RWS image count remained 78, and the RWS catalog remained 78 entries.

---

## Plan Self-Review

- Spec coverage: covered runtime helper, ReadingRoom wiring, Result wiring, docs, EditMode tests, PlayMode tests, and end-check flow.
- Scope check: plan excludes backend/API, history, settings, profile, admin, dashboard, release zip, tarot art replacement, final particle systems, shader graph, camera shake, post-processing, and broad UI redesign.
- Type consistency: `RitualAuraMotionController`, `Phase17RitualAuraMotionBootstrapper`, `Phase17RitualAuraRuntimeMotionTests`, and `Phase17RitualAuraRuntimeMotionPlayModeTests` names are consistent across tasks.
- Test flow: Task 1 establishes RED, Task 2 makes runtime type partially green, Task 3 verifies runtime behavior, Task 4 wires scenes, Task 5 makes targeted EditMode green, and Task 6 performs the required end-check flow.
