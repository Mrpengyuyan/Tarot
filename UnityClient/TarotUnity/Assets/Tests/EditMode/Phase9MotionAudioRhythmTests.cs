using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase9MotionAudioRhythmTests
    {
        [Test]
        public void ReadingRoomHasRhythmDirectorAndControllerWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var root = GameObject.Find("Phase9_RhythmDirectorRoot");
            Assert.That(root, Is.Not.Null);

            var director = FindObjectOfTypeName("RitualRhythmDirector");
            Assert.That(director, Is.Not.Null);
            Assert.That(GetFloat(director, "shuffleBreathSeconds"), Is.GreaterThanOrEqualTo(0.55f));
            Assert.That(GetFloat(director, "dealSettleSeconds"), Is.GreaterThanOrEqualTo(0.22f));
            Assert.That(GetFloat(director, "resultBreathSeconds"), Is.GreaterThanOrEqualTo(0.35f));
            Assert.That(GetArraySize(director, "cueOrder"), Is.GreaterThanOrEqualTo(5));

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(GetObjectReference(controller, "rhythmDirector"), Is.SameAs(director));
        }

        [Test]
        public void DeckHasWeightierCardGameDealMotion()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var deck = Object.FindFirstObjectByType<DeckController>();
            Assert.That(deck, Is.Not.Null);
            Assert.That(GetFloat(deck, "dealDuration"), Is.GreaterThanOrEqualTo(0.52f));
            Assert.That(GetFloat(deck, "dealInterval"), Is.GreaterThanOrEqualTo(0.16f));
            Assert.That(GetFloat(deck, "dealArcHeight"), Is.GreaterThanOrEqualTo(0.68f));
            Assert.That(GetFloat(deck, "dealTiltDegrees"), Is.GreaterThanOrEqualTo(8f));
            Assert.That(GetFloat(deck, "postDealSettleSeconds"), Is.GreaterThanOrEqualTo(0.08f));
        }

        [Test]
        public void CardPrefabHasMoreDeliberateRevealPacing()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);

            var flip = card.GetComponent<CardFlipController>();
            Assert.That(flip, Is.Not.Null);
            Assert.That(GetFloat(flip, "flipDuration"), Is.GreaterThanOrEqualTo(0.62f));
            Assert.That(GetFloat(flip, "anticipationPause"), Is.GreaterThanOrEqualTo(0.14f));
            Assert.That(GetFloat(flip, "faceRevealPause"), Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(GetFloat(flip, "liftDuringFlip"), Is.GreaterThanOrEqualTo(0.10f));
        }

        [Test]
        public void ResultRevealHasStagedBreathingRoom()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var reveal = FindObjectOfTypeName("ResultRevealDirector");
            Assert.That(reveal, Is.Not.Null);
            Assert.That(GetFloat(reveal, "firstDelay"), Is.GreaterThanOrEqualTo(0.30f));
            Assert.That(GetFloat(reveal, "groupInterval"), Is.GreaterThanOrEqualTo(0.20f));
            Assert.That(GetFloat(reveal, "fadeDuration"), Is.GreaterThanOrEqualTo(0.36f));
        }

        private static Object GetObjectReference(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName}");
            return property.objectReferenceValue;
        }

        private static Component FindObjectOfTypeName(string typeName)
        {
            foreach (var component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.GetType().Name == typeName)
                {
                    return component;
                }
            }

            return null;
        }

        private static float GetFloat(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName}");
            return property.floatValue;
        }

        private static int GetArraySize(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName}");
            return property.arraySize;
        }
    }
}
