using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69: renders the main menu (its start button is now card stock) to
    /// $PHASE69_CAPTURE_DIR/MainMenu.png for review. Same canvas-to-camera treatment as the
    /// Phase 64 capture; nothing is written into the repo.
    /// </summary>
    public static class Phase69MenuCaptureBuilder
    {
        private const int W = 2560, H = 1440;

        public static void Run()
        {
            var dir = Environment.GetEnvironmentVariable("PHASE69_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Set PHASE69_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            camera.aspect = (float)W / H;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            Canvas.ForceUpdateCanvases();
            CaptureRig.RenderConverged(camera);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, "MainMenu.png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"Phase 69 menu capture -> {dir}");
        }
    }
}
