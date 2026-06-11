using System;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase3PresentationTests
    {
        [Test]
        public void PresentationRuntimeTypesExist()
        {
            Assert.That(FindType("TarotUnity.Presentation.CameraChoreographyController"), Is.Not.Null);
            Assert.That(FindType("TarotUnity.Presentation.RitualFeedbackController"), Is.Not.Null);
            Assert.That(FindType("TarotUnity.Presentation.ResultRevealDirector"), Is.Not.Null);
        }

        [Test]
        public void ReadingRoomSceneHasPresentationPassWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var room = UnityEngine.Object.FindFirstObjectByType<ReadingRoomController>();
            Assert.That(room, Is.Not.Null);
            Assert.That(FindObjectOfTypeName("CameraChoreographyController"), Is.Not.Null);
            Assert.That(FindObjectOfTypeName("RitualFeedbackController"), Is.Not.Null);
            Assert.That(GetObjectReference(room, "cameraChoreography"), Is.Not.Null);
            Assert.That(GetObjectReference(room, "ritualFeedback"), Is.Not.Null);

            var particleSystems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(particleSystems.Length, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void ResultSceneHasStagedRevealWiring()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var controller = UnityEngine.Object.FindFirstObjectByType<ResultSceneController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(FindObjectOfTypeName("ResultRevealDirector"), Is.Not.Null);
            Assert.That(GetObjectReference(controller, "revealDirector"), Is.Not.Null);

            var revealDirector = FindObjectOfTypeName("ResultRevealDirector");
            var groups = GetArraySize(revealDirector, "revealGroups");
            Assert.That(groups, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void CardPrefabHasPresentationFlipPacing()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);

            var flipController = card.GetComponent<CardFlipController>();
            Assert.That(flipController, Is.Not.Null);
            Assert.That(GetFloat(flipController, "flipDuration"), Is.GreaterThan(0.45f));
            Assert.That(GetFloat(flipController, "anticipationPause"), Is.GreaterThan(0f));
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }

        private static Component FindObjectOfTypeName(string typeName)
        {
            return UnityEngine.Object.FindObjectsByType<Component>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(component => component.GetType().Name == typeName);
        }

        private static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.objectReferenceValue;
        }

        private static int GetArraySize(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.arraySize;
        }

        private static float GetFloat(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.floatValue;
        }
    }
}
