using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase2SceneWiringTests
    {
        [Test]
        public void MainMenuSceneHasStartController()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            var controller = Object.FindFirstObjectByType<MainMenuController>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(GetObjectReference(controller, "startReadingButton"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomSceneHasPlayableGrayboxWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            var layout = Object.FindFirstObjectByType<SpreadLayoutController>();

            Assert.That(room, Is.Not.Null);
            Assert.That(flow, Is.Not.Null);
            Assert.That(deck, Is.Not.Null);
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.GetSlots(1).Count, Is.EqualTo(1));
            Assert.That(layout.GetSlots(3).Count, Is.EqualTo(3));
            Assert.That(GetObjectReference(room, "questionInput"), Is.Not.Null);
            Assert.That(GetObjectReference(room, "drawButton"), Is.Not.Null);
            Assert.That(GetObjectReference(room, "revealResultButton"), Is.Not.Null);
            Assert.That(GetObjectReference(deck, "cardPrefab"), Is.Not.Null);
        }

        [Test]
        public void ResultSceneHasPresenterAndNavigation()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(GetObjectReference(controller, "backToMenuButton"), Is.Not.Null);
            Assert.That(GetObjectReference(presenter, "overallText"), Is.Not.Null);
        }

        private static Object GetObjectReference(Object target, string propertyName)
        {
            var serializedObject = new SerializedObject(target);
            return serializedObject.FindProperty(propertyName).objectReferenceValue;
        }
    }
}

