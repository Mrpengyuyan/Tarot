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
    /// Phase 60 verification: renders the Result scene with a real three-card
    /// spread (past / present / advice, the third drawn reversed) so the whole band
    /// can be reviewed, plus a one-card reading to prove the original single-hero
    /// layout is untouched.
    /// </summary>
    public static class Phase60ResultSpreadCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase60";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const int W = 2560, H = 1440;

        [MenuItem("Tools/Tarot Unity/Run Phase 60 Result Spread Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            EditorSceneManager.OpenScene(ResultScenePath);

            Capture("Result_threeCard.png", 3, "三牌阵 · 过去 · 现在 · 建议", "我们之间还会有未来吗？");
            Capture("Result_oneCard.png", 1, "单张牌", "我现在最该专注的是什么？");

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 60 result spread capture complete.");
        }

        private static void Capture(string file, int cardCount, string spreadName, string question)
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("No ResultPanelPresenter in Result scene.");
            }

            var draws = LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            var session = LocalReadingSimulator.CreateSession(2, spreadName, question, "relationship", draws);
            presenter.PresentSession(session);

            Canvas.ForceUpdateCanvases();
            var content = GameObject.Find("Content")?.GetComponent<RectTransform>();
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }

            // The presenter resizes the reading scroll; RectMask2D caches its clip
            // rect, so a single-shot edit-mode render would clip at the stale size.
            // Play mode re-clips every frame; here we force it.
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }
            Canvas.ForceUpdateCanvases();

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in Result scene.");
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
                File.WriteAllBytes(Path.Combine(ReviewFolder, file), tex.EncodeToPNG());
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
