using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase20VisualCaptureBuilder
    {
        public const string ReviewFolder = "Docs/VisualReview/Phase20";
        public const int ScreenshotWidth = 1280;
        public const int ScreenshotHeight = 720;

        private static readonly SceneCaptureTarget[] Targets =
        {
            new("Main Menu", "Assets/Scenes/MainMenu.unity", "MainMenu.png"),
            new("Reading Room", "Assets/Scenes/ReadingRoom.unity", "ReadingRoom.png"),
            new("Result", "Assets/Scenes/Result.unity", "Result.png"),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 20 Visual Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 20 Visual Capture again after Unity returns to Edit Mode.");
                return;
            }

            Directory.CreateDirectory(ReviewFolder);

            foreach (var target in Targets)
            {
                CaptureScene(target);
            }

            WriteManifest();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 20 visual capture complete.");
        }

        private static void CaptureScene(SceneCaptureTarget target)
        {
            EditorSceneManager.OpenScene(target.ScenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException($"Cannot capture {target.DisplayName}; no camera found.");
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
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, ScreenshotWidth, ScreenshotHeight), 0, 0);
                texture.Apply();

                var outputPath = Path.Combine(ReviewFolder, target.FileName);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
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
        }

        private static CanvasState[] PrepareCanvasesForCamera(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var states = new CanvasState[canvases.Length];

            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                states[i] = new CanvasState(
                    canvas,
                    canvas.renderMode,
                    canvas.worldCamera,
                    canvas.planeDistance,
                    canvas.pixelPerfect);

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

        private static void WriteManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"generatedAtUtc\": \"{DateTime.UtcNow:O}\",");
            builder.AppendLine($"  \"resolution\": \"{ScreenshotWidth}x{ScreenshotHeight}\",");
            builder.AppendLine("  \"screenshots\": [");

            for (var i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                builder.AppendLine("    {");
                builder.AppendLine($"      \"scene\": \"{target.DisplayName}\",");
                builder.AppendLine($"      \"scenePath\": \"{target.ScenePath}\",");
                builder.AppendLine($"      \"file\": \"{target.FileName}\"");
                builder.Append("    }");
                builder.AppendLine(i == Targets.Length - 1 ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(ReviewFolder, "phase20_visual_capture_manifest.json"), builder.ToString());
        }

        private readonly struct SceneCaptureTarget
        {
            public SceneCaptureTarget(string displayName, string scenePath, string fileName)
            {
                DisplayName = displayName;
                ScenePath = scenePath;
                FileName = fileName;
            }

            public string DisplayName { get; }
            public string ScenePath { get; }
            public string FileName { get; }
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
