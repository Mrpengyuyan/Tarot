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
    /// Phase 63 review shots: the reading room with the Celtic Cross sockets lit at
    /// their ten table positions (framed by the Celtic camera pose), and the result
    /// screen showing a full ten-card Celtic reading with its position names. READ-ONLY.
    /// </summary>
    public static class Phase63CelticCaptureBuilder
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string OutFolder = "Docs/VisualReview/Phase63";
        private const int W = 2560, H = 1440;
        private const float CelticFov = 50f;

        [MenuItem("Tools/Tarot Unity/Run Phase 63 Celtic Cross Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(OutFolder);
            CaptureReadingRoom();
            CaptureResult();
            AssetDatabase.Refresh();
            Debug.Log($"Phase 63 Celtic capture complete -> {OutFolder}");
        }

        private static void CaptureReadingRoom()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var indicator = UnityEngine.Object.FindFirstObjectByType<RitualStepIndicator>();
            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            flow?.SelectSpread(3, 10);
            indicator?.ApplyFlowState(ReadingFlowState.Shuffling);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in reading room.");
            }

            // Preview the Celtic camera pose so its framing can be judged.
            var pose = GameObject.Find("MP_CelticCrossPose");
            var prevPos = camera.transform.position;
            var prevRot = camera.transform.rotation;
            var prevFov = camera.fieldOfView;
            if (pose != null)
            {
                camera.transform.SetPositionAndRotation(pose.transform.position, pose.transform.rotation);
                camera.fieldOfView = CelticFov;
            }

            RenderToFile(camera, "ReadingRoom_celtic.png");

            camera.transform.SetPositionAndRotation(prevPos, prevRot);
            camera.fieldOfView = prevFov;
        }

        private static void CaptureResult()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            var celtic = Resources.Load<SpreadCatalog>(SpreadCatalog.ResourcePath)?.GetByCardCount(10);
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(10,
                celtic?.positionNames, celtic?.positionMeanings);
            var session = LocalReadingSimulator.CreateSession(3, "凯尔特十字", "我这段关系的整体走向？", "relationship", draws);
            presenter?.PresentSession(session);

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

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in Result scene.");
            }

            RenderToFile(camera, "Result_celtic.png");
        }

        private static void RenderToFile(Camera camera, string file)
        {
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
