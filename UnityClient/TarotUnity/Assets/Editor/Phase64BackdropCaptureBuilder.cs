using System;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 64 review shots: the Result screen (one- and three-card) over the new
    /// parlor backdrop instead of pure black, and the main menu with the restyled
    /// 离席 exit link. READ-ONLY.
    /// </summary>
    public static class Phase64BackdropCaptureBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string OutFolder = "Docs/VisualReview/Phase64";
        private const int W = 2560, H = 1440;

        [MenuItem("Tools/Tarot Unity/Run Phase 64 Backdrop Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(OutFolder);
            CaptureResult("Result_oneCard.png", 1, "单张牌", "我现在最该专注的是什么？");
            CaptureResult("Result_threeCard.png", 3, "三牌阵 · 过去 · 现在 · 建议", "我们之间还会有未来吗？");
            CaptureMenu("MainMenu.png");
            AssetDatabase.Refresh();
            Debug.Log($"Phase 64 backdrop capture complete -> {OutFolder}");
        }

        private static void CaptureResult(string file, int cardCount, string spreadName, string question)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            presenter?.PresentSession(LocalReadingSimulator.CreateSession(2, spreadName, question, "general", draws));

            Canvas.ForceUpdateCanvases();
            var content = GameObject.Find("Content")?.GetComponent<RectTransform>();
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }
            Canvas.ForceUpdateCanvases();

            RenderActiveCamera(file);
        }

        private static void CaptureMenu(string file)
        {
            EditorSceneManager.OpenScene(MenuScenePath);
            Canvas.ForceUpdateCanvases();
            RenderActiveCamera(file);
        }

        private static void RenderActiveCamera(string file)
        {
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in scene.");
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var states = PrepareCanvases(camera);

            try
            {
                camera.aspect = (float)W / H;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(OutFolder, file), tex.EncodeToPNG());
            }
            finally
            {
                RestoreCanvases(states);
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        private static (Canvas c, RenderMode m, Camera cam, float d, bool p)[] PrepareCanvases(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new (Canvas, RenderMode, Camera, float, bool)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                states[i] = (c, c.renderMode, c.worldCamera, c.planeDistance, c.pixelPerfect);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            return states;
        }

        private static void RestoreCanvases((Canvas c, RenderMode m, Camera cam, float d, bool p)[] states)
        {
            foreach (var s in states)
            {
                if (s.c != null)
                {
                    s.c.renderMode = s.m;
                    s.c.worldCamera = s.cam;
                    s.c.planeDistance = s.d;
                    s.c.pixelPerfect = s.p;
                }
            }
        }
    }
}
