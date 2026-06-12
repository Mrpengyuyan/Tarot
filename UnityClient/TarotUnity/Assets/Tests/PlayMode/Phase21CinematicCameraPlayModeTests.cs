using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase21CinematicCameraPlayModeTests
    {
        private GameObject cameraObject;
        private GameObject controllerObject;
        private GameObject poseObject;
        private GameObject focusObject;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { controllerObject, cameraObject, poseObject, focusObject })
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }

        [UnityTest]
        public IEnumerator FocusDeckBlendsPositionRotationAndFov()
        {
            var camera = CreateCamera(new Vector3(0f, 3f, -4f), Quaternion.Euler(35f, 0f, 0f), 40f);
            var deckPose = CreatePose("Phase21_TestDeckPose", new Vector3(-2f, 1.7f, -1.9f), Quaternion.Euler(36f, -24f, 0f));

            var controller = CreateController(camera);
            SetField(controller, "deckPose", deckPose.transform);
            SetField(controller, "deckFov", 30f);
            SetField(controller, "transitionDuration", 0.25f);
            SetField(controller, "breathingEnabled", false);

            controller.FocusDeck();
            yield return new WaitForSeconds(0.6f);

            Assert.That(Vector3.Distance(camera.transform.position, deckPose.transform.position), Is.LessThan(0.05f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, deckPose.transform.rotation), Is.LessThan(1f));
            Assert.That(camera.fieldOfView, Is.EqualTo(30f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator PunchTowardLeansInThenReturns()
        {
            var startPosition = new Vector3(0f, 2.45f, -3.55f);
            var camera = CreateCamera(startPosition, Quaternion.Euler(32f, 0f, 0f), 36f);

            focusObject = new GameObject("Phase21_TestFlipFocus");
            focusObject.transform.position = new Vector3(0f, 0.12f, 0.15f);

            var controller = CreateController(camera);
            SetField(controller, "breathingEnabled", false);
            SetField(controller, "punchTravelDistance", 0.55f);
            SetField(controller, "punchInSeconds", 0.15f);
            SetField(controller, "punchHoldSeconds", 0.2f);
            SetField(controller, "punchReturnSeconds", 0.2f);
            SetField(controller, "punchShakeAmplitude", 0f);

            controller.PunchToward(focusObject.transform);
            yield return new WaitForSeconds(0.25f);

            var leanDistance = Vector3.Distance(camera.transform.position, startPosition);
            Assert.That(leanDistance, Is.GreaterThan(0.3f), "Camera should lean toward the flipped card");
            Assert.That(camera.fieldOfView, Is.LessThan(36f), "Punch should tighten the FOV");

            yield return new WaitForSeconds(0.6f);

            Assert.That(Vector3.Distance(camera.transform.position, startPosition), Is.LessThan(0.05f),
                "Camera should settle back to its base pose after the punch");
            Assert.That(camera.fieldOfView, Is.EqualTo(36f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator BreathingDriftsWithinAmplitudeBounds()
        {
            var startPosition = new Vector3(0f, 2.45f, -3.55f);
            var camera = CreateCamera(startPosition, Quaternion.Euler(32f, 0f, 0f), 36f);

            var controller = CreateController(camera);
            SetField(controller, "breathingEnabled", true);
            SetField(controller, "breathPositionAmplitude", 0.03f);
            SetField(controller, "breathRotationAmplitude", 0.3f);
            SetField(controller, "breathFrequency", 3f);

            var drifted = false;
            for (var elapsed = 0f; elapsed < 1.2f; elapsed += Time.deltaTime)
            {
                var offset = Vector3.Distance(camera.transform.position, startPosition);
                Assert.That(offset, Is.LessThan(0.12f), "Breathing must stay subtle");
                if (offset > 0.002f)
                {
                    drifted = true;
                }

                yield return null;
            }

            Assert.That(drifted, Is.True, "Breathing should produce perceptible micro-motion");
        }

        private Camera CreateCamera(Vector3 position, Quaternion rotation, float fov)
        {
            cameraObject = new GameObject("Phase21_TestCamera");
            cameraObject.transform.SetPositionAndRotation(position, rotation);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = fov;
            return camera;
        }

        private CameraChoreographyController CreateController(Camera camera)
        {
            controllerObject = new GameObject("Phase21_TestChoreography");
            var controller = controllerObject.AddComponent<CameraChoreographyController>();
            SetField(controller, "targetCamera", camera);
            InvokePrivate(controller, "CaptureBaseFromCamera");
            return controller;
        }

        private GameObject CreatePose(string name, Vector3 position, Quaternion rotation)
        {
            poseObject = new GameObject(name);
            poseObject.transform.SetPositionAndRotation(position, rotation);
            return poseObject;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
            method.Invoke(target, null);
        }
    }
}
