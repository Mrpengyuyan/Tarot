using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase21CinematicCameraTests
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Phase21DocPath = "Docs/PROJECT_CHRONICLE.md";

        private static readonly Vector3 SpreadCenter = new(0f, 0.12f, 0.15f);

        [Test]
        public void ChoreographyControllerExposesCinematicApi()
        {
            var type = typeof(CameraChoreographyController);
            Assert.That(type.GetMethod("PunchToward", new[] { typeof(Transform) }), Is.Not.Null);
            Assert.That(type.GetMethod("Kick", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(type.GetProperty("IsPunching"), Is.Not.Null);
            Assert.That(type.GetProperty("CurrentBaseFov"), Is.Not.Null);

            var probe = new GameObject("Phase21_ApiProbe");
            try
            {
                var controller = probe.AddComponent<CameraChoreographyController>();
                var serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("defaultFov"), Is.Not.Null);
                Assert.That(serialized.FindProperty("breathingEnabled"), Is.Not.Null);
                Assert.That(serialized.FindProperty("punchTravelDistance"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void ReadingRoomUsesSeatedCinematicFraming()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("ReadingRoomCameraChoreography");
            Assert.That(root, Is.Not.Null);

            var controller = root.GetComponent<CameraChoreographyController>();
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            AssertFovInCinematicRange(serialized, "defaultFov");
            AssertFovInCinematicRange(serialized, "deckFov");
            AssertFovInCinematicRange(serialized, "oneCardFov");
            AssertFovInCinematicRange(serialized, "threeCardFov");
            AssertFovInCinematicRange(serialized, "resultFov");
            Assert.That(serialized.FindProperty("breathingEnabled")?.boolValue, Is.True);
            Assert.That(serialized.FindProperty("punchTravelDistance")?.floatValue, Is.InRange(0.2f, 1.2f));

            var defaultPose = root.transform.Find("DefaultPose");
            Assert.That(defaultPose, Is.Not.Null);
            Assert.That(defaultPose.position.y, Is.InRange(1.5f, 3.2f), "Default pose should sit at seated height, not surveillance height");

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.fieldOfView, Is.InRange(28f, 45f), "Scene camera must start with cinematic FOV");
        }

        [Test]
        public void ReadingRoomPosesAimAtTheirSubjects()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("ReadingRoomCameraChoreography");
            Assert.That(root, Is.Not.Null);

            AssertPoseAimsAt(root.transform.Find("DefaultPose"), SpreadCenter);
            AssertPoseAimsAt(root.transform.Find("OneCardPose"), SpreadCenter);
            AssertPoseAimsAt(root.transform.Find("ThreeCardPose"), SpreadCenter);
            AssertPoseAimsAt(root.transform.Find("ResultPose"), SpreadCenter);

            var deck = GameObject.Find("DeckStack");
            Assert.That(deck, Is.Not.Null, "ReadingRoom must contain DeckStack");
            AssertPoseAimsAt(root.transform.Find("DeckPose"), deck.transform.position);
        }

        [Test]
        public void ResultKeepsFramingAndAddsSubtleBreathing()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var controller = Object.FindFirstObjectByType<CameraChoreographyController>();
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("resultFov")?.floatValue, Is.EqualTo(60f).Within(0.01f),
                "Result scene framing is aligned with its UI and must not change FOV");
            Assert.That(serialized.FindProperty("breathingEnabled")?.boolValue, Is.True);
            Assert.That(serialized.FindProperty("breathPositionAmplitude")?.floatValue, Is.LessThanOrEqualTo(0.02f),
                "Result breathing must stay subtle to protect text readability");
        }

        [Test]
        public void MainMenuHasBreathingCamera()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);

            var choreography = GameObject.Find("MainMenuCameraChoreography");
            Assert.That(choreography, Is.Not.Null);

            var controller = choreography.GetComponent<CameraChoreographyController>();
            Assert.That(controller, Is.Not.Null);

            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("targetCamera")?.objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("breathingEnabled")?.boolValue, Is.True);
        }

        [Test]
        public void CardFlipControllerTriggersCameraPunch()
        {
            var probe = new GameObject("Phase21_FlipProbe");
            try
            {
                var flip = probe.AddComponent<CardFlipController>();
                var serialized = new SerializedObject(flip);
                var property = serialized.FindProperty("cameraPunchEnabled");
                Assert.That(property, Is.Not.Null, "CardFlipController must expose cameraPunchEnabled");
                Assert.That(property.boolValue, Is.True, "Camera punch should default to enabled");
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void CardPrefabFontlessLabelsStayDeactivated()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(prefab, Is.Not.Null);

            foreach (var labelName in new[] { "TitleLabel", "PositionLabel" })
            {
                var label = FindChildDeep(prefab.transform, labelName);
                Assert.That(label, Is.Not.Null, $"Card prefab should keep {labelName} (deactivated, not deleted)");
                Assert.That(label.gameObject.activeSelf, Is.False,
                    $"{labelName} has no font assigned and renders as giant ribbons; it must stay deactivated");
            }
        }

        [Test]
        public void Phase21KeepsPriorVisualWiring()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            Assert.That(GameObject.Find("Phase16_RitualAuraRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase20_CinematicVolume"), Is.Not.Null);

            EditorSceneManager.OpenScene(ResultScenePath);
            Assert.That(GameObject.Find("Phase16_ResultAuraRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase20_CinematicVolume"), Is.Not.Null);
        }

        [Test]
        public void Phase21DocumentationExists()
        {
            Assert.That(File.Exists(Phase21DocPath), Is.True, $"Missing Phase21 doc at {Phase21DocPath}");

            var text = File.ReadAllText(Phase21DocPath);
            Assert.That(text, Does.Contain("Cinematic Camera"));
            Assert.That(text, Does.Contain("PunchToward"));
            Assert.That(text, Does.Contain("seated"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static void AssertFovInCinematicRange(SerializedObject serialized, string propertyName)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing {propertyName}");
            Assert.That(property.floatValue, Is.InRange(26f, 45f),
                $"{propertyName} should sit in the intimate tabletop range, not wide-angle");
        }

        private static Transform FindChildDeep(Transform root, string name)
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
                var match = FindChildDeep(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void AssertPoseAimsAt(Transform pose, Vector3 target)
        {
            Assert.That(pose, Is.Not.Null);

            var toTarget = (target - pose.position).normalized;
            var angle = Vector3.Angle(pose.forward, toTarget);
            Assert.That(angle, Is.LessThan(8f),
                $"{pose.name} must aim at its subject (off by {angle:F1} degrees)");
        }
    }
}
