using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 29 verification: renders the Result scene twice - once with the short
    /// placeholder copy (should look like Phase 25, no scrolling) and once with a
    /// long injected interpretation (should fill the viewport and clip, proving the
    /// reading scrolls instead of overflowing or shrinking).
    /// </summary>
    public static class Phase29ResultScrollCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase29";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const int W = 1280, H = 720;

        private const string LongOverall =
            "过去的牌显示你们之间曾有真实而深的联结，那是一段彼此都认真对待过的关系；但随着时间推移，一些未曾说出口的顾虑与自我保护，像细沙一样慢慢磨蚀了原本的信任。现在的牌停在一种微妙的犹豫里：双方都在等对方先伸手，谁也不愿先暴露脆弱，于是僵局越拖越久，误会也越积越深。牌面提醒你，沉默并不等于答案，回避也不会让问题自己消失。";

        [MenuItem("Tools/Tarot Unity/Run Phase 29 Result Scroll Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            EditorSceneManager.OpenScene(ResultScenePath);

            CaptureOnce("Result_default.png", false);
            CaptureOnce("Result_long.png", true);

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 29 result scroll capture complete.");
        }

        private static void CaptureOnce(string file, bool injectLong)
        {
            if (injectLong)
            {
                var overall = FindText("OverallText");
                if (overall != null)
                {
                    overall.text = LongOverall;
                }

                var content = GameObject.Find("Content")?.GetComponent<RectTransform>();
                if (content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                }
            }

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

        private static Text FindText(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
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
