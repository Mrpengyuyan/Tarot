using System;
using System.IO;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 66 review shots: the Result screen for a three-card online reading that is
    /// generating, ready (real-model text), failed (retryable) and switched to offline
    /// text. READ-ONLY: it presents sample snapshots and renders; the scene is not saved.
    /// Set PHASE66_CAPTURE_DIR to render a review round somewhere other than Docs.
    /// </summary>
    public static class Phase66InterpretationStateCaptureBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DefaultOutFolder = "Docs/VisualReview/Phase66";
        private const int W = 2560, H = 1440;

        private static readonly string[] Files =
        {
            "Result_pending.png", "Result_ready.png", "Result_failed.png", "Result_offline.png",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 66 Interpretation State Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            var outFolder = Environment.GetEnvironmentVariable("PHASE66_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = DefaultOutFolder;
            }

            Directory.CreateDirectory(outFolder);
            foreach (var file in Files)
            {
                if (File.Exists(Path.Combine(outFolder, file)))
                {
                    throw new InvalidOperationException($"{outFolder}/{file} already exists; captures never overwrite.");
                }
            }

            Capture(outFolder, Files[0], BuildOnline());

            var ready = BuildOnline();
            ReadingSessionMapper.ApplyInterpretation(ready, SampleInterpretation());
            Capture(outFolder, Files[1], ready);

            var failed = BuildOnline();
            InterpretationPoller.ApplyFailure(failed, InterpretationFailure.ConnectionLost);
            Capture(outFolder, Files[2], failed);

            var offline = BuildOnline();
            InterpretationPoller.ApplyOffline(offline);
            Capture(outFolder, Files[3], offline);

            Debug.Log($"Phase 66 interpretation state capture complete -> {outFolder}");
        }

        private static ReadingSessionSnapshot BuildOnline()
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = 66,
                    spread_type_id = 2,
                    question = "我接下来最该把力气放在哪里？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            session.spreadName = "过去现在未来";
            return session;
        }

        private static InterpretationResponse SampleInterpretation()
        {
            return new InterpretationResponse
            {
                id = 1,
                summary = "旧的节奏正在松动，新的方向需要你亲手确认。",
                overall_interpretation = "过去的积累给了你底气，眼下的犹豫来自选择太多。把注意力收回到一件真正重要的事上，局面会比想象中更快清晰。",
                card_analysis = "过去：愚者 — 敢于开始的勇气仍在。\n现在：魔术师 — 资源齐备，关键在于专注。\n建议：女祭司（逆位） — 别只听外界的声音。",
                advice = "这一周只定一个目标，每天为它做一件小事。",
                warning = "解读仅供参考，重要决定请结合现实情况。",
                model_used = "deepseek-chat",
            };
        }

        private static void Capture(string outFolder, string file, ReadingSessionSnapshot session)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("ResultPanelPresenter not found.");
            }

            presenter.PresentSession(session);

            Canvas.ForceUpdateCanvases();
            var contentObject = GameObject.Find("Content");
            if (contentObject != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentObject.GetComponent<RectTransform>());
            }

            // RectMask2D caches its clip rect; toggle it so the capture clips to the
            // layout the presenter just applied (Phase 60 lesson).
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }

            Canvas.ForceUpdateCanvases();
            RenderActiveCamera(Path.Combine(outFolder, file));
        }

        private static void RenderActiveCamera(string path)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

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
                File.WriteAllBytes(path, tex.EncodeToPNG());
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
