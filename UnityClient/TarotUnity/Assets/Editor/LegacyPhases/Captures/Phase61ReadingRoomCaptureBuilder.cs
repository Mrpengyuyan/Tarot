using System;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 61 review shots of the reading room: the recessed glowing sockets at
    /// rest, and the draw step with the selected spread's sockets lit and the step
    /// bar highlighting the current step. READ-ONLY — drives the indicator in memory
    /// and renders; never saves the scene.
    /// </summary>
    public static class Phase61ReadingRoomCaptureBuilder
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string OutFolder = "Docs/VisualReview/Phase61";
        private const int W = 2560, H = 1440;

        [MenuItem("Tools/Tarot Unity/Run Phase 61 Reading Room Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(OutFolder);

            // Idle: the recessed sockets on the cloth, step bar at the choose-spread step.
            Capture("ReadingRoom_idle.png", indicator =>
            {
                indicator.ApplyFlowState(ReadingFlowState.SpreadSelect);
            });

            // Draw: three-card spread selected, sockets lit, "抽牌" chip highlighted.
            Capture("ReadingRoom_draw_three.png", indicator =>
            {
                var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
                flow?.SelectSpread(2, 3);
                indicator.ApplyFlowState(ReadingFlowState.Shuffling);
            });

            AssetDatabase.Refresh();
            Debug.Log($"Phase 61 reading room capture complete -> {OutFolder}");
        }

        private static void Capture(string file, Action<RitualStepIndicator> drive)
        {
            EditorSceneManager.OpenScene(ScenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException($"No camera in {ScenePath}.");
            }

            var indicator = UnityEngine.Object.FindFirstObjectByType<RitualStepIndicator>();
            if (indicator == null)
            {
                throw new InvalidOperationException("RitualStepIndicator missing - run the Phase 61 bootstrapper.");
            }

            drive(indicator);

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
