using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 31 builds a high-resolution visual archive of the current game UI: each
    /// faithful screen the player sees, rendered at 2560x1440 into one folder for review.
    /// It is READ-ONLY - it opens scenes and renders them but never saves a scene.
    ///
    /// Note: the face-up card reveal is intentionally NOT staged here. Real flips happen
    /// through gameplay that cannot be driven headlessly, and staging cards in edit mode
    /// surfaced an unresolved card-face sizing question on the hero prefab (flagged in the
    /// Phase 31 doc) that should be judged live with feedback, not approximated blind.
    /// </summary>
    public static class Phase31VisualArchiveBuilder
    {
        private const string ArchiveFolder = "Docs/VisualReview/Phase31_HDArchive";
        private const int W = 2560, H = 1440;

        private const string LongOverall =
            "过去的牌显示你们之间曾有真实而深的联结，那是一段彼此都认真对待过的关系；但随着时间推移，一些未曾说出口的顾虑与自我保护，像细沙一样慢慢磨蚀了原本的信任。现在的牌停在一种微妙的犹豫里：双方都在等对方先伸手，谁也不愿先暴露脆弱，于是僵局越拖越久，误会也越积越深。牌面提醒你，沉默并不等于答案，回避也不会让问题自己消失。";

        [MenuItem("Tools/Tarot Unity/Run Phase 31 HD Visual Archive")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(ArchiveFolder);
            var shots = new List<string>();

            CaptureScene("Assets/Scenes/MainMenu.unity", "01_MainMenu.png", false);
            shots.Add(Shot("01_MainMenu.png", "Main menu - title, tagline, start ritual button"));

            CaptureScene("Assets/Scenes/ReadingRoom.unity", "02_ReadingRoom.png", false);
            shots.Add(Shot("02_ReadingRoom.png", "Reading room - progress rail, spread select, deck and empty slots on the 3D table"));

            CaptureScene("Assets/Scenes/Result.unity", "03_Result_default.png", false);
            shots.Add(Shot("03_Result_default.png", "Result - question header, card hero, scrollable reading (short copy)"));

            CaptureScene("Assets/Scenes/Result.unity", "04_Result_long.png", true);
            shots.Add(Shot("04_Result_long.png", "Result - long backend-style reading filling the scroll panel"));

            var manifest = new StringBuilder();
            manifest.AppendLine("{");
            manifest.AppendLine($"  \"resolution\": \"{W}x{H}\",");
            manifest.AppendLine("  \"shots\": [");
            manifest.AppendLine(string.Join(",\n", shots));
            manifest.AppendLine("  ]");
            manifest.AppendLine("}");
            File.WriteAllText(Path.Combine(ArchiveFolder, "phase31_archive_manifest.json"), manifest.ToString());

            AssetDatabase.Refresh();
            Debug.Log($"Tarot Unity Phase 31 HD visual archive complete ({W}x{H}, {shots.Count} shots) -> {ArchiveFolder}");
        }

        private static string Shot(string file, string desc) =>
            $"    {{ \"file\": \"{file}\", \"description\": \"{desc}\" }}";

        private static void CaptureScene(string scenePath, string file, bool injectLongReading)
        {
            EditorSceneManager.OpenScene(scenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException($"No camera in {scenePath}.");
            }

            if (injectLongReading)
            {
                InjectLongReading();
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
                File.WriteAllBytes(Path.Combine(ArchiveFolder, file), tex.EncodeToPNG());
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

        private static void InjectLongReading()
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "OverallText")
                {
                    t.text = LongOverall;
                }
            }

            var content = GameObject.Find("Content")?.GetComponent<RectTransform>();
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
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
