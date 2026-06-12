using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase23CardFeelTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string Phase23DocPath = "Docs/PHASE23_CARD_FEEL.md";

        [Test]
        public void CardPrefabHasHoverTiltWithResearchBackedTuning()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var hover = prefab.GetComponent<CardHoverTiltController>();
            Assert.That(hover, Is.Not.Null, "Card prefab must carry CardHoverTiltController");

            var serialized = new SerializedObject(hover);
            Assert.That(serialized.FindProperty("hoverLift")?.floatValue, Is.InRange(0.02f, 0.08f),
                "Hover lift should be a subtle physical rise");
            Assert.That(serialized.FindProperty("maxTiltDegrees")?.floatValue, Is.InRange(4f, 10f),
                "Tilt should read as physical without flipping the card visually");
            Assert.That(serialized.FindProperty("responseSeconds")?.floatValue, Is.LessThanOrEqualTo(0.1f),
                "Hover response must settle near 100ms to feel instantaneous");
        }

        [Test]
        public void HoverControllerImplementsPointerInterfaces()
        {
            var type = typeof(CardHoverTiltController);
            Assert.That(typeof(IPointerEnterHandler).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IPointerExitHandler).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IPointerMoveHandler).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetMethod("Suspend"), Is.Not.Null);
            Assert.That(type.GetMethod("ReleaseImmediate"), Is.Not.Null);
        }

        [Test]
        public void FlipControllerSuspendsHoverAndExposesState()
        {
            Assert.That(typeof(CardFlipController).GetProperty("IsFlipping"), Is.Not.Null);

            var probe = new GameObject("Phase23_FlipProbe");
            try
            {
                var flip = probe.AddComponent<CardFlipController>();
                var serialized = new SerializedObject(flip);
                Assert.That(serialized.FindProperty("liftDuringFlip")?.floatValue, Is.InRange(0.12f, 0.2f),
                    "Flip lift should carry a readable emphasis arc");
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void ReadingRoomUsesTunedRhythm()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var choreography = GameObject.Find("ReadingRoomCameraChoreography");
            Assert.That(choreography, Is.Not.Null);

            var controller = choreography.GetComponent<TarotUnity.Presentation.CameraChoreographyController>();
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("punchHoldSeconds")?.floatValue, Is.InRange(0.25f, 0.4f),
                "Punch aftermath should settle with the flip, not linger past it");
            Assert.That(serialized.FindProperty("punchReturnSeconds")?.floatValue, Is.InRange(0.3f, 0.45f),
                "Punch return should ease out slightly faster than the lean-in");

            var deck = GameObject.Find("DeckStack");
            Assert.That(deck, Is.Not.Null);

            var deckController = deck.GetComponent<DeckController>();
            Assert.That(deckController, Is.Not.Null);

            var deckSerialized = new SerializedObject(deckController);
            Assert.That(deckSerialized.FindProperty("dealInterval")?.floatValue, Is.InRange(0.16f, 0.22f),
                "Deal interval should give each landing card its own beat above the Phase 9 floor");
        }

        [Test]
        public void ReadingRoomCameraSupportsPointerEvents()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.GetComponent<PhysicsRaycaster>(), Is.Not.Null,
                "Hover and click on 3D cards require a PhysicsRaycaster on the camera");
        }

        [Test]
        public void Phase23DocumentationExists()
        {
            Assert.That(File.Exists(Phase23DocPath), Is.True, $"Missing Phase23 doc at {Phase23DocPath}");

            var text = File.ReadAllText(Phase23DocPath);
            Assert.That(text, Does.Contain("Card Feel"));
            Assert.That(text, Does.Contain("CardHoverTiltController"));
            Assert.That(text, Does.Contain("100ms"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}
