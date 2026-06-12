using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase22VisualCaptureBuilder
    {
        public const string ReviewFolder = "Docs/VisualReview/Phase22";
        public const int ScreenshotWidth = 1280;
        public const int ScreenshotHeight = 720;

        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private static readonly string[] FramingSlotNames = { "PastSlot", "PresentSlot", "AdviceSlot" };

        private static readonly SceneCaptureTarget[] Targets =
        {
            new("Main Menu", "Assets/Scenes/MainMenu.unity", "MainMenu.png", placeFramingCards: false),
            new("Reading Room", "Assets/Scenes/ReadingRoom.unity", "ReadingRoom.png", placeFramingCards: true),
            new("Result", "Assets/Scenes/Result.unity", "Result.png", placeFramingCards: false),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 22 Visual Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 22 Visual Capture again after Unity returns to Edit Mode.");
                return;
            }

            Directory.CreateDirectory(ReviewFolder);

            foreach (var target in Targets)
            {
                CaptureScene(target);
            }

            WriteManifest();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 22 visual capture complete.");
        }

        private static void CaptureScene(SceneCaptureTarget target)
        {
            EditorSceneManager.OpenScene(target.ScenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException($"Cannot capture {target.DisplayName}; no camera found.");
            }

            var framingProxies = target.PlaceFramingCards ? PlaceFramingCards() : new List<GameObject>();

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

                foreach (var proxy in framingProxies)
                {
                    if (proxy != null)
                    {
                        UnityEngine.Object.DestroyImmediate(proxy);
                    }
                }
            }
        }

        private static List<GameObject> PlaceFramingCards()
        {
            var proxies = new List<GameObject>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Phase 22 capture could not load card prefab at {CardPrefabPath}.");
                return proxies;
            }

            foreach (var slotName in FramingSlotNames)
            {
                var slot = GameObject.Find(slotName);
                if (slot == null)
                {
                    continue;
                }

                var proxy = UnityEngine.Object.Instantiate(prefab, slot.transform.position, slot.transform.rotation);
                proxy.name = $"Phase22_FramingProxy_{slotName}";
                proxies.Add(proxy);
            }

            return proxies;
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
            File.WriteAllText(Path.Combine(ReviewFolder, "phase22_visual_capture_manifest.json"), builder.ToString());
        }

        private readonly struct SceneCaptureTarget
        {
            public SceneCaptureTarget(string displayName, string scenePath, string fileName, bool placeFramingCards)
            {
                DisplayName = displayName;
                ScenePath = scenePath;
                FileName = fileName;
                PlaceFramingCards = placeFramingCards;
            }

            public string DisplayName { get; }
            public string ScenePath { get; }
            public string FileName { get; }
            public bool PlaceFramingCards { get; }
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
