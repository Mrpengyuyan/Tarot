using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Focused capture for Phase 25: only the Result scene changed, so this
    /// renders just that scene (edit-mode camera.Render, same canvas handling as
    /// the earlier capture builders) into its own folder. The Phase 24 Result
    /// screenshot stays the "before" for comparison.
    /// </summary>
    public static class Phase25ResultCaptureBuilder
    {
        public const string ReviewFolder = "Docs/VisualReview/Phase25";
        public const int ScreenshotWidth = 1280;
        public const int ScreenshotHeight = 720;
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 25 Result Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 25 Result Capture again after Unity returns to Edit Mode.");
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            EditorSceneManager.OpenScene(ResultScenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("Cannot capture Result; no camera found.");
            }

            var renderTexture = new RenderTexture(ScreenshotWidth, ScreenshotHeight, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(ScreenshotWidth, ScreenshotHeight, TextureFormat.RGBA32, false);
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var previousAspect = camera.aspect;
            var canvasStates = PrepareCanvasesForCamera(camera);

            try
            {
                camera.aspect = (float)ScreenshotWidth / ScreenshotHeight;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                texture.ReadPixels(new Rect(0f, 0f, ScreenshotWidth, ScreenshotHeight), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(ReviewFolder, "Result.png"), texture.EncodeToPNG());
            }
            finally
            {
                RestoreCanvases(canvasStates);
                camera.targetTexture = previousTargetTexture;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 25 result capture complete.");
        }

        private static CanvasState[] PrepareCanvasesForCamera(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new CanvasState[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                states[i] = new CanvasState(canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance, canvas.pixelPerfect);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.pixelPerfect = false;
            }

            return states;
        }

        private static void RestoreCanvases(CanvasState[] states)
        {
            foreach (var state in states)
            {
                if (state.Canvas == null)
                {
                    continue;
                }

                state.Canvas.renderMode = state.RenderMode;
                state.Canvas.worldCamera = state.WorldCamera;
                state.Canvas.planeDistance = state.PlaneDistance;
                state.Canvas.pixelPerfect = state.PixelPerfect;
            }
        }

        private readonly struct CanvasState
        {
            public CanvasState(Canvas canvas, RenderMode renderMode, Camera worldCamera, float planeDistance, bool pixelPerfect)
            {
                Canvas = canvas;
                RenderMode = renderMode;
                WorldCamera = worldCamera;
                PlaneDistance = planeDistance;
                PixelPerfect = pixelPerfect;
            }

            public Canvas Canvas { get; }
            public RenderMode RenderMode { get; }
            public Camera WorldCamera { get; }
            public float PlaneDistance { get; }
            public bool PixelPerfect { get; }
        }
    }
}
