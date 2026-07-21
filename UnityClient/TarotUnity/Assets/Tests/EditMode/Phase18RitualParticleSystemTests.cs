using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase18RitualParticleSystemTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase18DocPath = "Docs/PROJECT_CHRONICLE.md";

        [Test]
        public void RitualParticleSystemControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.RitualParticleSystemController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("SetParticlesVisible", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("SetIntensity", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(type.GetMethod("PlayAmbient", Type.EmptyTypes), Is.Not.Null);
            Assert.That(type.GetMethod("StopAll", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(type.GetMethod("TriggerRevealBurst", Type.EmptyTypes), Is.Not.Null);
            Assert.That(type.GetMethod("SimulateTick", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(type.GetProperty("IsPlaying"), Is.Not.Null);
            Assert.That(type.GetProperty("CurrentIntensity"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomHasPhase18ParticleWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(root, Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualParticleSystemController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var controller = root.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);

            var ambient = AssertParticleUnder(root, "Phase16_ParticleAnchorNorth", "Phase18_AmbientDustParticles");
            var focus = AssertParticleUnder(root, "Phase16_ParticleAnchorEast", "Phase18_DeckFocusParticles");
            var reveal = AssertParticleUnder(root, "Phase16_AuraFocusAnchor", "Phase18_FlipSparkParticles");

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("auraController")?.objectReferenceValue, Is.SameAs(root.GetComponent<RitualAuraController>()));
            Assert.That(serialized.FindProperty("ambientParticles")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("focusParticles")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("revealParticles")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("playOnEnable")?.boolValue, Is.True);
            Assert.That(serialized.FindProperty("intensity")?.floatValue, Is.InRange(0.45f, 0.8f));

            Assert.That(ArrayContains(serialized.FindProperty("ambientParticles"), ambient), Is.True);
            Assert.That(ArrayContains(serialized.FindProperty("focusParticles"), focus), Is.True);
            Assert.That(ArrayContains(serialized.FindProperty("revealParticles"), reveal), Is.True);
        }

        [Test]
        public void ResultHasPhase18ParticleWiring()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(root, Is.Not.Null);

            var controllerType = Type.GetType("TarotUnity.Presentation.RitualParticleSystemController, TarotUnity.Runtime");
            Assert.That(controllerType, Is.Not.Null);

            var controller = root.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);

            var motes = AssertParticleUnder(root, "Phase16_ResultParticleAnchorLeft", "Phase18_ResultCardMotes");
            var glow = AssertParticleUnder(root, "Phase16_ResultAuraFocusAnchor", "Phase18_ResultInterpretationGlow");

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("auraController")?.objectReferenceValue, Is.SameAs(root.GetComponent<RitualAuraController>()));
            Assert.That(serialized.FindProperty("ambientParticles")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("focusParticles")?.arraySize, Is.GreaterThanOrEqualTo(1));
            Assert.That(serialized.FindProperty("revealParticles")?.arraySize, Is.EqualTo(0));
            Assert.That(serialized.FindProperty("intensity")?.floatValue, Is.InRange(0.25f, 0.65f));

            Assert.That(ArrayContains(serialized.FindProperty("ambientParticles"), motes), Is.True);
            Assert.That(ArrayContains(serialized.FindProperty("focusParticles"), glow), Is.True);
        }

        [Test]
        public void Phase18ParticleSystemsUseSafeVisualSettings()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            AssertParticleSettings("Phase18_AmbientDustParticles", shouldLoop: true, maxRate: 12f);
            AssertParticleSettings("Phase18_DeckFocusParticles", shouldLoop: true, maxRate: 10f);
            AssertParticleSettings("Phase18_FlipSparkParticles", shouldLoop: false, maxRate: 1f);
            AssertNoBlockingObjects("Phase16_RitualAuraRoot");

            EditorSceneManager.OpenScene(ResultScenePath);
            AssertParticleSettings("Phase18_ResultCardMotes", shouldLoop: true, maxRate: 8f);
            AssertParticleSettings("Phase18_ResultInterpretationGlow", shouldLoop: true, maxRate: 6f);
            AssertNoBlockingObjects("Phase16_ResultAuraRoot");
        }

        [Test]
        public void Phase18KeepsPhase16AndPhase17AuraWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var readingRoot = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(readingRoot, Is.Not.Null);
            Assert.That(readingRoot.GetComponent<RitualAuraController>(), Is.Not.Null);
            Assert.That(readingRoot.GetComponent<RitualAuraMotionController>(), Is.Not.Null);
            Assert.That(readingRoot.transform.Find("Phase16_ParticleAnchorNorth"), Is.Not.Null);
            Assert.That(readingRoot.transform.Find("Phase16_ParticleAnchorEast"), Is.Not.Null);
            Assert.That(readingRoot.transform.Find("Phase16_ParticleAnchorSouth"), Is.Not.Null);
            Assert.That(readingRoot.transform.Find("Phase16_ParticleAnchorWest"), Is.Not.Null);

            EditorSceneManager.OpenScene(ResultScenePath);

            var resultRoot = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(resultRoot, Is.Not.Null);
            Assert.That(resultRoot.GetComponent<RitualAuraController>(), Is.Not.Null);
            Assert.That(resultRoot.GetComponent<RitualAuraMotionController>(), Is.Not.Null);
            Assert.That(resultRoot.transform.Find("Phase16_ResultParticleAnchorLeft"), Is.Not.Null);
            Assert.That(resultRoot.transform.Find("Phase16_ResultParticleAnchorRight"), Is.Not.Null);
        }

        [Test]
        public void Phase18KeepsCardArtAndPriorVisualWiring()
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
        public void Phase18KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase18DocumentationExists()
        {
            Assert.That(File.Exists(Phase18DocPath), Is.True, $"Missing Phase18 doc at {Phase18DocPath}");

            var text = File.ReadAllText(Phase18DocPath);
            Assert.That(text, Does.Contain("Ritual Particle Systems"));
            Assert.That(text, Does.Contain("RitualParticleSystemController"));
            Assert.That(text, Does.Contain("Phase18_AmbientDustParticles"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static ParticleSystem AssertParticleUnder(GameObject root, string parentName, string particleName)
        {
            var parent = FindDeep(root.transform, parentName);
            Assert.That(parent, Is.Not.Null, $"Missing parent {parentName}");

            var child = parent.Find(particleName);
            Assert.That(child, Is.Not.Null, $"Missing {particleName} under {parentName}");

            var particles = child.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null, $"{particleName} must have ParticleSystem");
            return particles;
        }

        private static void AssertParticleSettings(string particleName, bool shouldLoop, float maxRate)
        {
            var particleObject = GameObject.Find(particleName);
            Assert.That(particleObject, Is.Not.Null, particleName);

            var particles = particleObject.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null, particleName);

            var main = particles.main;
            Assert.That(main.loop, Is.EqualTo(shouldLoop), particleName);
            Assert.That(main.playOnAwake, Is.False, particleName);
            Assert.That(main.duration, Is.InRange(0.4f, 6f), particleName);
            Assert.That(main.maxParticles, Is.InRange(12, 160), particleName);

            var emission = particles.emission;
            Assert.That(emission.enabled, Is.True, particleName);
            Assert.That(emission.rateOverTime.constantMax, Is.LessThanOrEqualTo(maxRate), particleName);

            var shape = particles.shape;
            Assert.That(shape.enabled, Is.True, particleName);

            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer, Is.Not.Null, particleName);
            Assert.That(renderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Billboard), particleName);
        }

        private static void AssertNoBlockingObjects(string rootName)
        {
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

        private static bool ArrayContains(SerializedProperty arrayProperty, UnityEngine.Object expected)
        {
            if (arrayProperty == null || !arrayProperty.isArray)
            {
                return false;
            }

            for (var index = 0; index < arrayProperty.arraySize; index++)
            {
                if (arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindDeep(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
