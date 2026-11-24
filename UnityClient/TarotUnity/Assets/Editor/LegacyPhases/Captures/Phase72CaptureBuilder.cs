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
    /// Phase 72 review shots, written to $PHASE72_CAPTURE_DIR (nothing goes into the repo):
    /// the fan seen from each spread's draw camera at 16:9 and 4:3, and - for the three-card
    /// spread - the hover wave, a picked card hovering with its glow swelled, and a card in
    /// flight trailing light.
    /// </summary>
    public static class Phase72CaptureBuilder
    {
        private const int H = 1440;

        public static void Run()
        {
            var dir = Environment.GetEnvironmentVariable("PHASE72_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Set PHASE72_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(dir);
            foreach (var count in new[] { 1, 3, 10 })
            {
                CaptureFan(dir, count, 2560, "169");
                CaptureFan(dir, count, 1920, "43");
            }

            CaptureMoments(dir);
            Debug.Log($"Phase 72 capture -> {dir}");
        }

        private static List<CardView> Setup(int cardCount, out Camera camera, out SpreadFanController fan)
        {
            EditorSceneManager.OpenScene(Phase72DrawRitualBootstrapper.ScenePath);
            fan = UnityEngine.Object.FindFirstObjectByType<SpreadFanController>();
            var choreography = UnityEngine.Object.FindFirstObjectByType<CameraChoreographyController>();
            choreography.TryGetDrawPose(cardCount, out var pose, out var fov);
            camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            camera.fieldOfView = fov;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            // The spread's sockets as the draw shows them.
            var sockets = UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>();
            if (sockets != null)
            {
                sockets.Apply(cardCount);
            }

            // The fan's light pool, as it is while the player picks.
            var light = (Light)new SerializedObject(fan).FindProperty("fanLight").objectReferenceValue;
            if (light != null)
            {
                light.enabled = true;
                light.intensity = new SerializedObject(fan).FindProperty("fanLightIntensity").floatValue;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<CardView>(Phase72DrawRitualBootstrapper.CardPrefabPath);
            var cards = new List<CardView>();
            for (var i = 0; i < fan.CardCount; i++)
            {
                var card = (CardView)PrefabUtility.InstantiatePrefab(prefab);
                fan.GetFanPose(i, out var p, out var r);
                card.transform.SetPositionAndRotation(p, r);
                card.SetFaceUp(false);
                cards.Add(card);
            }

            return cards;
        }

        private static void CaptureFan(string dir, int cardCount, int width, string aspect)
        {
            Setup(cardCount, out var camera, out _);
            Render(camera, width, Path.Combine(dir, $"Fan_{cardCount}_{aspect}.png"));
        }

        private static void CaptureMoments(string dir)
        {
            // The hover wave: card 10 lifted and slid toward the player, its neighbours rising less.
            var cards = Setup(3, out var camera, out var fan);
            foreach (var (index, lift, slide) in new[] { (10, 0.05f, 0.3f), (9, 0.04f, 0f), (11, 0.04f, 0f), (8, 0.02f, 0f), (12, 0.02f, 0f) })
            {
                fan.GetFanPose(index, out var p, out var r);
                cards[index].transform.position = p + Vector3.up * lift - (r * Vector3.forward) * slide;
            }

            cards[10].SetHovered(true);
            Render(camera, 2560, Path.Combine(dir, "Fan_hover.png"));

            // A picked card hovering above the gap it left, its glow swelled.
            cards = Setup(3, out camera, out fan);
            fan.GetFanPose(12, out var gap, out var gapRotation);
            var picked = cards[12];
            picked.transform.SetPositionAndRotation(gap + Vector3.up * 0.55f - (gapRotation * Vector3.forward) * 0.25f, Quaternion.identity);
            picked.SetHighlighted(true);
            picked.SetHaloBoost(1.6f);
            Render(camera, 2560, Path.Combine(dir, "Pick_hover.png"));

            // The same card halfway to its slot, trailing light.
            var slot = UnityEngine.Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(3)[1];
            var from = gap + Vector3.up * 0.55f;
            picked.SetHaloBoost(1f);
            var trail = picked.GetComponentInChildren<TrailRenderer>(true);
            trail.time = 60f;
            trail.Clear();
            var points = new List<Vector3>();
            for (var k = 0; k <= 8; k++)
            {
                var t = k / 16f;
                points.Add(Vector3.Lerp(from, slot.position, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.72f));
            }

            trail.AddPositions(points.ToArray());
            picked.transform.position = points[points.Count - 1];
            Render(camera, 2560, Path.Combine(dir, "Pick_flight.png"));
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
