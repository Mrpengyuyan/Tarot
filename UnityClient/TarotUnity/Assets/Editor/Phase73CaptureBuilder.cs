using System;
using System.Collections.Generic;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 73 review shots, written to $PHASE73_CAPTURE_DIR (nothing goes into the repo):
    /// a face-up card on the one-card slot; the room's opening view for each spread (where the
    /// deck sits against the slots); the 78-card fan from the draw camera at 16:9 and 4:3 under
    /// its light; and a hovered card picked up with its neighbours parted.
    /// </summary>
    public static class Phase73CaptureBuilder
    {
        private const int H = 1440;

        public static void Run()
        {
            var dir = Environment.GetEnvironmentVariable("PHASE73_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Set PHASE73_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(dir);
            CaptureFaceUp(dir);
            foreach (var count in new[] { 1, 3, 10 })
            {
                CaptureOpening(dir, count);
            }

            foreach (var count in new[] { 3, 10 })
            {
                CaptureFan(dir, count, 2560, "169", hover: false);
                CaptureFan(dir, count, 1920, "43", hover: false);
            }

            CaptureFan(dir, 3, 2560, "hover", hover: true);
            Debug.Log($"Phase 73 capture -> {dir}");
        }

        private static void CaptureFaceUp(string dir)
        {
            OpenRoom(1);
            var slot = UnityEngine.Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(1)[0];
            var card = SpawnCard();
            card.transform.SetPositionAndRotation(slot.position, slot.rotation);
            var catalog = Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
            var draw = LocalReadingSimulator.CreatePlaceholderDraws(1)[0];
            card.Bind(draw);
            card.SetFaceUp(true);
            card.SetFaceArtwork(catalog != null ? catalog.FindSprite(draw) : null);
            Render(PoseCamera("oneCardPose", "oneCardFov"), 2560, Path.Combine(dir, "FaceUp.png"));
        }

        private static void CaptureOpening(string dir, int count)
        {
            OpenRoom(count);
            Render(PoseCamera("defaultPose", "defaultFov"), 2560, Path.Combine(dir, $"Opening_{count}.png"));
        }

        private static void CaptureFan(string dir, int count, int width, string suffix, bool hover)
        {
            OpenRoom(count);
            var fan = UnityEngine.Object.FindFirstObjectByType<SpreadFanController>();
            var fanSo = new SerializedObject(fan);
            var light = (Light)fanSo.FindProperty("fanLight").objectReferenceValue;
            light.enabled = true;
            light.intensity = fanSo.FindProperty("fanLightIntensity").floatValue;

            var cards = new List<CardView>();
            for (var i = 0; i < fan.CardCount; i++)
            {
                var card = SpawnCard();
                fan.GetFanPose(i, out var p, out var r);
                card.transform.SetPositionAndRotation(p, r);
                cards.Add(card);
            }

            if (hover)
            {
                // The runtime pose at rest after the spring settles: card 19 picked up, 16-22 parted.
                var pivot = ((Transform)fanSo.FindProperty("fanCenter").objectReferenceValue).position;
                var radius = fanSo.FindProperty("rows").GetArrayElementAtIndex(0).FindPropertyRelative("radius").floatValue;
                var part = fanSo.FindProperty("partDistance").floatValue;
                var wave = fanSo.FindProperty("waveRadius").floatValue;
                for (var k = -3; k <= 3; k++)
                {
                    if (k == 0)
                    {
                        continue;
                    }

                    var ease = 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (Mathf.Abs(k) - 1f) / wave);
                    var degrees = Mathf.Sign(k) * part * ease / radius * Mathf.Rad2Deg;
                    var t = cards[19 + k].transform;
                    t.RotateAround(pivot, Vector3.up, degrees);
                }

                var hovered = cards[19].transform;
                fan.GetFanPose(19, out var rest, out var restRotation);
                var towardPlayer = -(restRotation * Vector3.forward);
                hovered.SetPositionAndRotation(
                    rest + Vector3.up * fanSo.FindProperty("hoverLift").floatValue + towardPlayer * fanSo.FindProperty("hoverSlide").floatValue,
                    restRotation * Quaternion.AngleAxis(-fanSo.FindProperty("hoverTiltDegrees").floatValue, Vector3.right));
                hovered.localScale *= fanSo.FindProperty("hoverScale").floatValue;
                cards[19].HoverHaloScale = fanSo.FindProperty("fanHoverHaloScale").floatValue;
                cards[19].SetHovered(true);
            }

            var choreography = UnityEngine.Object.FindFirstObjectByType<CameraChoreographyController>();
            choreography.TryGetDrawPose(count, out var pose, out var fov);
            var camera = MainCamera();
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            camera.fieldOfView = fov;
            Render(camera, width, Path.Combine(dir, $"Fan_{count}_{suffix}.png"));
        }

        private static void OpenRoom(int count)
        {
            EditorSceneManager.OpenScene(Phase73DrawPolishBootstrapper.ScenePath);
            var sockets = UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>();
            if (sockets != null)
            {
                sockets.Apply(count);
            }

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }
        }

        private static CardView SpawnCard()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Phase73DrawPolishBootstrapper.CardPrefabPath);
            var card = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<CardView>();
            card.SetFaceUp(false);
            return card;
        }

        private static Camera MainCamera()
        {
            return Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
        }

        private static Camera PoseCamera(string poseField, string fovField)
        {
            var so = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<CameraChoreographyController>());
            var pose = (Transform)so.FindProperty(poseField).objectReferenceValue;
            var camera = MainCamera();
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            camera.fieldOfView = so.FindProperty(fovField).floatValue;
            return camera;
        }

        private static void Render(Camera camera, int width, string path)
        {
            var rt = new RenderTexture(width, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, H, TextureFormat.RGBA32, false);
            camera.aspect = (float)width / H;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            CaptureRig.RenderConverged(camera);
            tex.ReadPixels(new Rect(0, 0, width, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
