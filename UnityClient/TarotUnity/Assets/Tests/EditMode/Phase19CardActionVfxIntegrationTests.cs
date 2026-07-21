using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase19CardActionVfxIntegrationTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string Phase19DocPath = "Docs/PROJECT_CHRONICLE.md";

        [Test]
        public void RitualActionVfxControllerRuntimeTypeExists()
        {
            var type = Type.GetType("TarotUnity.Presentation.RitualActionVfxController, TarotUnity.Runtime");
            Assert.That(type, Is.Not.Null);
            Assert.That(type.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(type.GetMethod("PlayCue", new[] { typeof(PresentationCueId) }), Is.Not.Null);
            Assert.That(type.GetMethod("PlayCue", new[] { typeof(PresentationCueId), typeof(Transform) }), Is.Not.Null);
            Assert.That(type.GetProperty("LastCue")?.PropertyType, Is.EqualTo(typeof(PresentationCueId)));
            Assert.That(type.GetProperty("LastAnchor")?.PropertyType, Is.EqualTo(typeof(Transform)));
            Assert.That(type.GetProperty("CueCount")?.PropertyType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void RitualFeedbackControllerExposesActionVfxForwardingFields()
        {
            var owner = new GameObject("Phase19 Feedback Field Probe");
            try
            {
                var feedback = owner.AddComponent<RitualFeedbackController>();
                var serialized = new SerializedObject(feedback);

                Assert.That(serialized.FindProperty("actionVfxController"), Is.Not.Null);
                Assert.That(serialized.FindProperty("forwardCuesToActionVfx"), Is.Not.Null);
                Assert.That(serialized.FindProperty("forwardCuesToActionVfx")?.boolValue, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ReadingRoomHasPhase19ActionVfxWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            AssertActionVfxWiring(
                "ReadingRoomRitualFeedback",
                "Phase16_RitualAuraRoot",
                expectedShuffleAmbient: true,
                expectedDealAmbient: true,
                expectedFlipBurst: true,
                expectedResultBurst: true,
                expectedShuffleIntensity: 0.76f,
                expectedDealIntensity: 0.66f,
                expectedFlipIntensity: 0.95f,
                expectedResultIntensity: 0.72f);
        }

        [Test]
        public void ResultHasPhase19ActionVfxWiring()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            AssertActionVfxWiring(
                "ResultRitualFeedback",
                "Phase16_ResultAuraRoot",
                expectedShuffleAmbient: false,
                expectedDealAmbient: false,
                expectedFlipBurst: false,
                expectedResultBurst: true,
                expectedShuffleIntensity: 0.44f,
                expectedDealIntensity: 0.44f,
                expectedFlipIntensity: 0.52f,
                expectedResultIntensity: 0.58f);
        }

        [Test]
        public void Phase19KeepsPhase18ParticlesAndPriorAuraWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            var readingRoot = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(readingRoot, Is.Not.Null);
            Assert.That(readingRoot.GetComponent<RitualAuraController>(), Is.Not.Null);
            Assert.That(readingRoot.GetComponent<RitualAuraMotionController>(), Is.Not.Null);
            Assert.That(readingRoot.GetComponent<RitualParticleSystemController>(), Is.Not.Null);
            Assert.That(FindDeep(readingRoot.transform, "Phase18_AmbientDustParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(FindDeep(readingRoot.transform, "Phase18_DeckFocusParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(FindDeep(readingRoot.transform, "Phase18_FlipSparkParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);

            EditorSceneManager.OpenScene(ResultScenePath);
            var resultRoot = GameObject.Find("Phase16_ResultAuraRoot");
            Assert.That(resultRoot, Is.Not.Null);
            Assert.That(resultRoot.GetComponent<RitualAuraController>(), Is.Not.Null);
            Assert.That(resultRoot.GetComponent<RitualAuraMotionController>(), Is.Not.Null);
            Assert.That(resultRoot.GetComponent<RitualParticleSystemController>(), Is.Not.Null);
            Assert.That(FindDeep(resultRoot.transform, "Phase18_ResultCardMotes")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(FindDeep(resultRoot.transform, "Phase18_ResultInterpretationGlow")?.GetComponent<ParticleSystem>(), Is.Not.Null);
        }

        [Test]
        public void Phase19KeepsCardArtAndPriorVisualWiring()
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
        public void Phase19KeepsPhase13ArtworkCatalogAvailable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var entries = new SerializedObject(catalog).FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));
        }

        [Test]
        public void Phase19DocumentationExists()
        {
            Assert.That(File.Exists(Phase19DocPath), Is.True, $"Missing Phase19 doc at {Phase19DocPath}");

            var text = File.ReadAllText(Phase19DocPath);
            Assert.That(text, Does.Contain("Card Action VFX Integration"));
            Assert.That(text, Does.Contain("RitualActionVfxController"));
            Assert.That(text, Does.Contain("RitualFeedbackController"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static void AssertActionVfxWiring(
            string feedbackName,
            string particleRootName,
            bool expectedShuffleAmbient,
            bool expectedDealAmbient,
            bool expectedFlipBurst,
            bool expectedResultBurst,
            float expectedShuffleIntensity,
            float expectedDealIntensity,
            float expectedFlipIntensity,
            float expectedResultIntensity)
        {
            var feedback = GameObject.Find(feedbackName)?.GetComponent<RitualFeedbackController>();
            Assert.That(feedback, Is.Not.Null, $"Missing {feedbackName}");

            var particleRoot = GameObject.Find(particleRootName);
            Assert.That(particleRoot, Is.Not.Null, $"Missing {particleRootName}");
            var particleController = particleRoot.GetComponent<RitualParticleSystemController>();
            Assert.That(particleController, Is.Not.Null, $"{particleRootName} must have RitualParticleSystemController");

            var serializedFeedback = new SerializedObject(feedback);
            var forwardProperty = serializedFeedback.FindProperty("forwardCuesToActionVfx");
            Assert.That(forwardProperty, Is.Not.Null, $"{feedbackName} missing forwardCuesToActionVfx");
            Assert.That(forwardProperty.boolValue, Is.True, $"{feedbackName} must forward cues to Phase19 action VFX");

            var actionProperty = serializedFeedback.FindProperty("actionVfxController");
            Assert.That(actionProperty, Is.Not.Null, $"{feedbackName} missing actionVfxController");
            Assert.That(actionProperty.objectReferenceValue, Is.Not.Null, $"{feedbackName} action VFX is not assigned");

            var actionType = Type.GetType("TarotUnity.Presentation.RitualActionVfxController, TarotUnity.Runtime");
            Assert.That(actionType, Is.Not.Null);
            Assert.That(actionProperty.objectReferenceValue.GetType(), Is.EqualTo(actionType));

            var serializedAction = new SerializedObject(actionProperty.objectReferenceValue);
            Assert.That(serializedAction.FindProperty("particleSystemController")?.objectReferenceValue, Is.SameAs(particleController));
            Assert.That(serializedAction.FindProperty("playAmbientOnShuffle")?.boolValue, Is.EqualTo(expectedShuffleAmbient));
            Assert.That(serializedAction.FindProperty("playAmbientOnDeal")?.boolValue, Is.EqualTo(expectedDealAmbient));
            Assert.That(serializedAction.FindProperty("burstOnFlip")?.boolValue, Is.EqualTo(expectedFlipBurst));
            Assert.That(serializedAction.FindProperty("burstOnResult")?.boolValue, Is.EqualTo(expectedResultBurst));
            Assert.That(serializedAction.FindProperty("shuffleIntensity")?.floatValue, Is.EqualTo(expectedShuffleIntensity).Within(0.001f));
            Assert.That(serializedAction.FindProperty("dealIntensity")?.floatValue, Is.EqualTo(expectedDealIntensity).Within(0.001f));
            Assert.That(serializedAction.FindProperty("flipIntensity")?.floatValue, Is.EqualTo(expectedFlipIntensity).Within(0.001f));
            Assert.That(serializedAction.FindProperty("resultIntensity")?.floatValue, Is.EqualTo(expectedResultIntensity).Within(0.001f));
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
