using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase11VisualReviewBuilder
    {
        public const string ReviewFolder = "Docs/VisualReview/Phase11";
        public const string ReviewDocPath = "Docs/PHASE11_VISUAL_REVIEW.md";
        public const int ScreenshotWidth = 1280;
        public const int ScreenshotHeight = 720;

        private static readonly SceneReviewTarget[] Targets =
        {
            new("Main Menu", "Assets/Scenes/MainMenu.unity", "MainMenu.png"),
            new("Reading Room", "Assets/Scenes/ReadingRoom.unity", "ReadingRoom.png"),
            new("Result", "Assets/Scenes/Result.unity", "Result.png"),
        };

        public static string[] ScreenshotFileNames
        {
            get
            {
                var names = new string[Targets.Length];
                for (var i = 0; i < Targets.Length; i++)
                {
                    names[i] = Targets[i].FileName;
                }

                return names;
            }
        }

        [MenuItem("Tools/Tarot Unity/Run Phase 11 Visual Review Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 11 Visual Review Capture again after Unity returns to Edit Mode.");
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            ApplyFinalAdjustments();

            foreach (var target in Targets)
            {
                CaptureScene(target);
            }

            WriteManifest();
            WriteReviewDoc();

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 11 visual review capture complete.");
        }

        private static void ApplyFinalAdjustments()
        {
            UpgradeMainMenuScene();
            UpgradeReadingRoomScene();
            UpgradeResultScene();
            AssetDatabase.SaveAssets();
        }

        private static void UpgradeMainMenuScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                return;
            }

            EnsurePanel(canvas.transform, "Phase11_MenuDepthFrame", new Vector2(0f, 48f), new Vector2(820f, 312f), new Color(0.022f, 0.017f, 0.034f, 0.52f), 0);
            EnsurePanel(canvas.transform, "Phase11_ActionRail", new Vector2(0f, -82f), new Vector2(390f, 88f), new Color(0.60f, 0.39f, 0.12f, 0.34f), 3);
            EnsurePanel(canvas.transform, "Phase11_TableDepthShadow", new Vector2(0f, -205f), new Vector2(930f, 170f), new Color(0.012f, 0.010f, 0.018f, 0.70f), 1);

            SetText(canvas.transform.Find("TitleText")?.GetComponent<Text>(), "塔罗仪式", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 158f), new Vector2(820f, 86f));
            SetText(canvas.transform.Find("SubtitleText")?.GetComponent<Text>(), "在安静的牌桌前，把问题交给牌面。", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 96f), new Vector2(820f, 44f));
            SetText(canvas.transform.Find("Phase8_MenuCrest")?.GetComponent<Text>(), "月光牌桌", 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 25f), new Vector2(280f, 28f));
            StyleButton(canvas.transform.Find("StartReadingButton")?.GetComponent<Button>(), "开始占卜", new Vector2(0f, -76f), new Vector2(308f, 56f), true);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas == null)
            {
                return;
            }

            EnsurePanel(canvas.transform, "Phase11_TableFocusFrame", new Vector2(0f, -42f), new Vector2(900f, 228f), new Color(0.74f, 0.50f, 0.16f, 0.10f), 5);
            EnsurePanel(canvas.transform, "Phase11_ActionDock", new Vector2(0f, -258f), new Vector2(820f, 84f), new Color(0.018f, 0.015f, 0.026f, 0.72f), 7);
            EnsurePanel(canvas.transform, "Phase11_QuestionGlow", new Vector2(0f, 58f), new Vector2(792f, 64f), new Color(0.05f, 0.11f, 0.08f, 0.42f), 4);

            SetText(canvas.transform.Find("SpreadStatusText")?.GetComponent<Text>(), "选择牌阵，让牌面决定回答的节奏", 23, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 166f), new Vector2(860f, 38f));
            MoveRect(canvas.transform.Find("QuestionInput")?.GetComponent<RectTransform>(), new Vector2(0f, 58f), new Vector2(744f, 50f));
            MoveRect(canvas.transform.Find("Phase8_QuestionPanelFrame")?.GetComponent<RectTransform>(), new Vector2(0f, 58f), new Vector2(796f, 68f));
            MoveRect(canvas.transform.Find("Phase8_CardSlotsGlow")?.GetComponent<RectTransform>(), new Vector2(0f, -54f), new Vector2(880f, 132f));

            StyleButton(canvas.transform.Find("OneCardButton")?.GetComponent<Button>(), "一张牌", new Vector2(-272f, 116f), new Vector2(180f, 42f), false);
            StyleButton(canvas.transform.Find("ThreeCardButton")?.GetComponent<Button>(), "三张牌", new Vector2(-68f, 116f), new Vector2(180f, 42f), false);
            StyleButton(canvas.transform.Find("DrawButton")?.GetComponent<Button>(), "洗牌抽取", new Vector2(184f, 116f), new Vector2(220f, 46f), true);
            StyleButton(canvas.transform.Find("RevealResultButton")?.GetComponent<Button>(), "揭示结果", new Vector2(0f, -224f), new Vector2(254f, 54f), true);

            SetText(canvas.transform.Find("FlowStatusText")?.GetComponent<Text>(), "写下此刻真正想问的问题，然后洗牌抽取。", 17, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -284f), new Vector2(860f, 38f));
            SetText(canvas.transform.Find("Phase10_ReleaseStatusText")?.GetComponent<Text>(), "本地模式已准备好：即使没有启动后端，也可以完成一局塔罗流程。", 13, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, -318f), new Vector2(880f, 28f));

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                return;
            }

            EnsurePanel(canvas.transform, "Phase11_ResultReadingColumns", new Vector2(0f, -20f), new Vector2(920f, 412f), new Color(0.015f, 0.014f, 0.020f, 0.62f), 3);
            EnsurePanel(canvas.transform, "Phase11_ResultCardPresence", new Vector2(-330f, 22f), new Vector2(146f, 224f), new Color(0.80f, 0.62f, 0.30f, 0.22f), 6);
            EnsurePanel(canvas.transform, "Phase11_ResultCardFace", new Vector2(-330f, 22f), new Vector2(118f, 188f), new Color(0.90f, 0.84f, 0.68f, 0.16f), 7);

            SetText(canvas.transform.Find("QuestionText")?.GetComponent<Text>(), "你的问题", 25, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 224f), new Vector2(860f, 36f));
            SetText(canvas.transform.Find("SpreadNameText")?.GetComponent<Text>(), "牌阵", 18, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 190f), new Vector2(860f, 28f));
            SetText(canvas.transform.Find("SummaryText")?.GetComponent<Text>(), "概要", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(150f, 112f), new Vector2(700f, 48f), VerticalWrapMode.Overflow);
            SetText(canvas.transform.Find("OverallText")?.GetComponent<Text>(), "整体解读", 19, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(150f, 28f), new Vector2(700f, 88f), VerticalWrapMode.Overflow);
            SetText(canvas.transform.Find("CardAnalysisText")?.GetComponent<Text>(), "牌面分析", 16, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(150f, -76f), new Vector2(700f, 88f), VerticalWrapMode.Overflow);
            SetText(canvas.transform.Find("AdviceText")?.GetComponent<Text>(), "建议", 17, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(150f, -176f), new Vector2(700f, 66f), VerticalWrapMode.Overflow);
            StyleButton(canvas.transform.Find("BackToMenuButton")?.GetComponent<Button>(), "回到牌桌", new Vector2(0f, -284f), new Vector2(226f, 46f), false);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void CaptureScene(SceneReviewTarget target)
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
            File.WriteAllText(Path.Combine(ReviewFolder, "phase11_visual_review_manifest.json"), builder.ToString());
        }

        private static void WriteReviewDoc()
        {
            File.WriteAllText(
                ReviewDocPath,
                "# Phase 11 Visual Review And Final Adjustment\n" +
                "\n" +
                "Date: 2026-05-27\n" +
                "\n" +
                "## Purpose\n" +
                "\n" +
                "Phase 11 creates a repeatable screenshot baseline before the next beauty pass. The goal is to make visual problems concrete instead of relying on memory from the Unity Editor.\n" +
                "\n" +
                "The long-term target remains a Hearthstone-like 3D card-table presentation, but this phase does not import the final deck art yet.\n" +
                "\n" +
                "## Captured Screens\n" +
                "\n" +
                "- Main Menu: `Docs/VisualReview/Phase11/MainMenu.png`\n" +
                "- Reading Room: `Docs/VisualReview/Phase11/ReadingRoom.png`\n" +
                "- Result: `Docs/VisualReview/Phase11/Result.png`\n" +
                "\n" +
                "Capture note: run `TarotUnity.Editor.Phase11VisualReviewBuilder.Run` in Unity batchmode with graphics enabled. Do not add `-nographics`, because that can produce uniform placeholder screenshots instead of actual scene renders.\n" +
                "\n" +
                "## Review Notes\n" +
                "\n" +
                "- Main Menu must sell the tarot-table fantasy within the first viewport.\n" +
                "- Reading Room must keep spread select, question input, shuffle/draw, flip cards, and result reveal readable at 1280x720.\n" +
                "- Result must feel like a resolved reading, not a plain text report.\n" +
                "- Flipped card faces should later use real tarot card artwork instead of placeholder text or generic symbols.\n" +
                "- The real tarot card artwork task needs source and license review before assets are imported into the release package.\n" +
                "\n" +
                "## Screenshot Review Findings\n" +
                "\n" +
                "- Main Menu has the clearest mood, but the central seal and button overlap into a flat block; later passes should separate title, emblem, table, and primary action with more depth.\n" +
                "- Reading Room is functional, but the top workflow buttons and question panel feel like stacked UI bars above the table; later passes should make the table and deck/card area the visual center.\n" +
                "- Result is readable but still report-like. It needs a stronger result panel composition, card presence, and staged hierarchy before it will feel like a finished game screen.\n" +
                "\n" +
                "## Adjustments Applied\n" +
                "\n" +
                "- Main Menu: separated the title, crest, action rail, and table shadow so the first screen has more depth.\n" +
                "- Reading Room: added a table focus frame and action dock, then lowered result/status copy away from the card table.\n" +
                "- Result: added a left-side card-presence panel and shifted reading text into a clearer right-side column.\n" +
                "\n" +
                "## Deferred Asset Task\n" +
                "\n" +
                "Before importing card art, choose a deck source, verify usage license, define image naming, map backend/local card names to assets, and decide texture import settings for desktop builds.\n" +
                "\n" +
                "## Current Limitations\n" +
                "\n" +
                "- The interface is still a prototype visual pass and is not yet the final 3D card-game look.\n" +
                "- Windows executable smoke testing still needs a Windows machine.\n");
        }

        private static Image EnsurePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, int siblingIndex)
        {
            var existing = parent.Find(name);
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));

            panel.transform.SetParent(parent, false);
            panel.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(panel);
            return image;
        }

        private static void MoveRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

        private static void SetText(
            Text text,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size,
            VerticalWrapMode verticalWrap = VerticalWrapMode.Truncate)
        {
            if (text == null)
            {
                return;
            }

            MoveRect(text.GetComponent<RectTransform>(), position, size);
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.96f, 0.91f, 0.80f, 1f);
            text.lineSpacing = 1.20f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = verticalWrap;
            EditorUtility.SetDirty(text);
        }

        private static void StyleButton(Button button, string label, Vector2 position, Vector2 size, bool primary)
        {
            if (button == null)
            {
                return;
            }

            MoveRect(button.GetComponent<RectTransform>(), position, size);

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = primary
                    ? new Color(0.24f, 0.14f, 0.14f, 0.97f)
                    : new Color(0.070f, 0.050f, 0.092f, 0.94f);
            }

            var colors = button.colors;
            colors.normalColor = button.targetGraphic != null ? button.targetGraphic.color : colors.normalColor;
            colors.highlightedColor = primary
                ? new Color(0.42f, 0.27f, 0.20f, 1f)
                : new Color(0.14f, 0.10f, 0.18f, 1f);
            colors.pressedColor = new Color(0.86f, 0.63f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            SetText(button.transform.Find("Label")?.GetComponent<Text>(), label, primary ? 20 : 17, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, size);
            EditorUtility.SetDirty(button);
        }

        private readonly struct SceneReviewTarget
        {
            public SceneReviewTarget(string displayName, string scenePath, string fileName)
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
