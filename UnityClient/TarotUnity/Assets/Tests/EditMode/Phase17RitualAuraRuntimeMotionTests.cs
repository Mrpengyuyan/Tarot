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
