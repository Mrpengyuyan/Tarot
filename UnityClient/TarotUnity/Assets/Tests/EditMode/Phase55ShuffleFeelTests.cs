using System.IO;
using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 55 plays the shuffle on the deck stack itself - the last silent beat in
    /// the card's motion chain (flip: Phase 52, deal landing: Phase 54). These keep
    /// the choreography tuning in a tasteful envelope and prove the scene is wired:
    /// the Midnight Parlor stack carries the choreographer and the draw flow points
    /// at it. The shuffle pacing floor (shuffleBreathSeconds) stays with Phase 9.
    /// </summary>
    public sealed class Phase55ShuffleFeelTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string DocPath = "Docs/PHASE55_SHUFFLE_FEEL.md";

        [Test]
        public void ShuffleTuningStaysInATastefulEnvelope()
        {
            var probe = new GameObject("Phase55_StackProbe");
            try
            {
                var choreographer = probe.AddComponent<DeckShuffleChoreographer>();
                var so = new SerializedObject(choreographer);

                Assert.That(so.FindProperty("anticipationDip")?.floatValue, Is.InRange(0.005f, 0.04f),
                    "the press-down is a hand squaring the deck, not the deck sinking into the table");
                Assert.That(so.FindProperty("anticipationSeconds")?.floatValue, Is.InRange(0.06f, 0.2f),
                    "the anticipation is a beat, not a wait");
                Assert.That(so.FindProperty("riffleLift")?.floatValue, Is.InRange(0.02f, 0.12f),
                    "each pop clears its neighbours without the stack exploding");
                Assert.That(so.FindProperty("riffleCardSeconds")?.floatValue, Is.InRange(0.08f, 0.3f),
                    "one card's pop is quick - a riffle, not a juggle");
                Assert.That(so.FindProperty("riffleStagger")?.floatValue, Is.InRange(0.01f, 0.06f),
                    "the ripple reads as one gesture running up the stack");
                Assert.That(so.FindProperty("riffleYawDegrees")?.floatValue, Is.InRange(1f, 10f),
                    "the twist is a shiver, not a spin");
                Assert.That(so.FindProperty("contactSquash")?.floatValue, Is.InRange(0.02f, 0.12f),
                    "the square-up squash reads as weight, not a splat");
                Assert.That(so.FindProperty("settleSeconds")?.floatValue, Is.InRange(0.08f, 0.25f),
                    "the settle springs back quickly");
                Assert.That(so.FindProperty("contactCameraKick")?.floatValue, Is.InRange(0f, 0.08f),
                    "the shuffle kick stays quieter than the flip's reveal");
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void TheDeckStackCarriesTheChoreographerAndTheDrawFlowPointsAtIt()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var stack = GameObject.Find("DeckStack/MP_DeckStack");
            Assert.That(stack, Is.Not.Null, "the Midnight Parlor deck stack is missing");

            var choreographer = stack.GetComponent<DeckShuffleChoreographer>();
            Assert.That(choreographer, Is.Not.Null,
                "MP_DeckStack must carry the Phase 55 shuffle choreographer");

            var room = Object.FindFirstObjectByType<ReadingRoomController>(FindObjectsInactive.Include);
            Assert.That(room, Is.Not.Null);

            var so = new SerializedObject(room);
            Assert.That(so.FindProperty("deckShuffle")?.objectReferenceValue, Is.EqualTo(choreographer),
                "the draw flow must point at the stack's choreographer, or the shuffle stays still");
        }

        [Test]
        public void Phase55DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase55 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("shuffle"));
            Assert.That(text, Does.Contain("riffle"));
        }
    }
}
