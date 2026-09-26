using System;
using System.Linq;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69 restyles the text UI of the reading room, the result page and the menu's
    /// start button: smoked glass for containers and unchosen options, ivory card stock for the
    /// current step, the chosen spread and the next action. Type grows 15%; every framed element
    /// is sized from its TMP preferred size, so no text touches its frame. The hierarchy is
    /// unchanged (tests and older bootstrappers find objects by path); only Phase69_* children
    /// are added. Runs after the Phase 39/40/60/63/67 bootstrappers - re-running any of those
    /// puts the old gold plaques back, so run this one again afterwards. Idempotent.
    /// </summary>
    public static class Phase69UiRestyleBootstrapper
    {
        public const string ReadingRoomPath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultPath = "Assets/Scenes/Result.unity";
        public const string MenuPath = "Assets/Scenes/MainMenu.unity";

        private static readonly Color CornerStar = new Color(232f / 255f, 184f / 255f, 90f / 255f, 1f);   // #e8b85a
        private static readonly Color DotStar = new Color(232f / 255f, 184f / 255f, 90f / 255f, 0.6f);
        private static readonly Color FlankStar = new Color(154f / 255f, 107f / 255f, 30f / 255f, 1f);    // #9a6b1e
        private static readonly Color Underline = new Color(219f / 255f, 161f / 255f, 61f / 255f, 0.6f);  // #dba13d
        private static readonly Color StepUpcoming = new Color(0.961f, 0.910f, 0.800f, 0.55f);
        private static readonly Color StepCompleted = new Color(232f / 255f, 196f / 255f, 122f / 255f, 1f); // #e8c47a

        private static Sprite glass;
        private static Sprite stock;
        private static Sprite sparkle;

        [MenuItem("Tools/Tarot Unity/Run Phase 69 UI Restyle Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            glass = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.GlassPath);
            stock = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.CardStockPath);
            sparkle = AssetDatabase.LoadAssetAtPath<Sprite>(Phase69UiKitGenerator.SparklePath);
            if (glass == null || stock == null || sparkle == null)
            {
                Debug.LogError("Phase 69: UI kit missing; run Tools/Tarot Unity/Generate Phase 69 UI Kit first.");
                return;
            }

            RestyleReadingRoom();
            RestyleResult();
            RestyleMenu();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 69 UI restyle complete.");
        }

        // ---------------------------------------------------------------- reading room

        private static readonly string[] ChipNames =
        {
            "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
            "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
        };

        public static void RestyleReadingRoom()
        {
            var scene = EditorSceneManager.OpenScene(ReadingRoomPath, OpenSceneMode.Single);
            var root = GameObject.Find("ReadingRoomCanvas").transform;

            SetMutedThreshold(root, UiFitLayout.MutedSizeThresholdScaled);
            ScaleType(root, root.gameObject, _ => true);

            BuildStepBar(root);
            BuildDock(root);
            SettleText(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildStepBar(Transform root)
        {
            var hud = (RectTransform)root.Find("Phase7_RitualHudRoot");
            var widths = new float[ChipNames.Length];
            var heights = new float[ChipNames.Length];
            var chips = new RectTransform[ChipNames.Length];
            for (var i = 0; i < ChipNames.Length; i++)
            {
                var chip = (RectTransform)hud.Find(ChipNames[i]);
                var plate = chip.Find("Plate").GetComponent<Image>();
                var label = chip.Find("Label").GetComponent<TMP_Text>();
                plate.transform.SetAsFirstSibling();   // two chips held their Label first; card stock would hide it
                Stretch((RectTransform)plate.transform, 0f);

                var size = FitLabel(chip, label);
                chips[i] = chip;
                widths[i] = size.x;
                heights[i] = size.y;

                var skin = GetOrAdd<UiSkinState>(chip.gameObject);
                skin.Configure(plate, label, null, stock, false);
                EditorUtility.SetDirty(skin);
            }

            var centers = UiFitLayout.RowCenters(widths, UiFitLayout.StepGap);
            for (var i = 0; i < chips.Length; i++)
            {
                chips[i].anchoredPosition = new Vector2(centers[i], 0f);
                EditorUtility.SetDirty(chips[i]);
            }

            for (var i = 0; i < chips.Length - 1; i++)
            {
                var x = (centers[i] + widths[i] / 2f + centers[i + 1] - widths[i + 1] / 2f) / 2f;
                var dot = EnsureImage(hud, $"Phase69_StepDot_{i}", sparkle, DotStar, Vector2.one * UiFitLayout.StepDotSize);
                dot.anchoredPosition = new Vector2(x, 0f);
            }

            var rowWidth = widths.Sum() + UiFitLayout.StepGap * (widths.Length - 1);
            var plateSize = new Vector2(
                rowWidth + 2f * UiFitLayout.HudPad.x + 2f * UiFitLayout.SkinMargin,
                heights.Max() + 2f * UiFitLayout.HudPad.y + 2f * UiFitLayout.SkinMargin);
            hud.sizeDelta = plateSize;
            var hudPlate = (RectTransform)hud.Find("Phase7_HudPlate");
            hudPlate.anchoredPosition = Vector2.zero;
            hudPlate.sizeDelta = plateSize;
            Skin(hudPlate.GetComponent<Image>(), glass);
            AddCornerStars(hudPlate);
            EditorUtility.SetDirty(hud);

            var indicator = UnityEngine.Object.FindFirstObjectByType<RitualStepIndicator>();
            var so = new SerializedObject(indicator);
            so.FindProperty("upcomingLabel").colorValue = StepUpcoming;
            so.FindProperty("completedLabel").colorValue = StepCompleted;
            so.FindProperty("currentLabel").colorValue = UiSkinState.CardStockLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            indicator.SetStep(0);   // SpreadSelect: 选牌阵 is current in the saved scene
        }

        private static void BuildDock(Transform root)
        {
            var spread = new[] { "OneCardButton", "ThreeCardButton", "CelticCrossButton" };
            var actions = new[] { "DrawButton", "RevealResultButton" };
            var names = spread.Concat(actions).ToArray();
            var rects = new RectTransform[names.Length];
            var widths = new float[names.Length];
            var rowHeight = 0f;
            for (var i = 0; i < names.Length; i++)
            {
                rects[i] = (RectTransform)root.Find(names[i]);
                var label = rects[i].Find("Label").GetComponent<TMP_Text>();
                var size = FitLabel(rects[i], label);
                widths[i] = size.x;
                rowHeight = Mathf.Max(rowHeight, size.y);

                var skin = GetOrAdd<UiSkinState>(rects[i].gameObject);
                skin.Configure(rects[i].GetComponent<Image>(), label, glass, stock, true);
                skin.SetEmphasis(names[i] == "OneCardButton" || Array.IndexOf(actions, names[i]) >= 0);
                EditorUtility.SetDirty(skin);
                EditorUtility.SetDirty(rects[i].GetComponent<Button>());
            }

            foreach (var name in actions)
            {
                AddFlanks((RectTransform)root.Find(name));
            }

            // The question: an underline over a clear ground that still takes clicks.
            var input = (RectTransform)root.Find("QuestionInput");
            var field = input.GetComponent<TMP_InputField>();
            var placeholder = (TMP_Text)field.placeholder;
            placeholder.ForceMeshUpdate();
            var inputPreferred = placeholder.GetPreferredValues(placeholder.text);
            var inputHeight = Mathf.Ceil(inputPreferred.y) + 2f * UiFitLayout.Padding(placeholder.fontSize).y;
            var ground = input.GetComponent<Image>();
            ground.sprite = null;
            ground.color = new Color(1f, 1f, 1f, 0f);
            ground.raycastTarget = true;
            GetOrAdd<TarotUiPreserveColor>(input.gameObject);
            var line = EnsureImage(input, "Phase69_InputUnderline", null, Underline, new Vector2(0f, 1f));
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, 1f);

            var rowWidth = widths.Sum() + UiFitLayout.RowGap * (widths.Length - 1);
            var dockWidth = rowWidth + 2f * UiFitLayout.DockPad.x + 2f * UiFitLayout.SkinMargin;
            var dockHeight = 2f * UiFitLayout.SkinMargin + 2f * UiFitLayout.DockPad.y
                             + inputHeight + UiFitLayout.DockRowGap + rowHeight;
            var top = UiFitLayout.DockTop;
            var dock = (RectTransform)root.Find("Phase11_ActionDock");
            dock.sizeDelta = new Vector2(dockWidth, dockHeight);
            dock.anchoredPosition = new Vector2(0f, top - dockHeight / 2f);
            Skin(dock.GetComponent<Image>(), glass);
            AddCornerStars(dock);

            var inputY = top - UiFitLayout.SkinMargin - UiFitLayout.DockPad.y - inputHeight / 2f;
            input.sizeDelta = new Vector2(Mathf.Round(UiFitLayout.InputWidthRatio * dockWidth), inputHeight);
            input.anchoredPosition = new Vector2(0f, inputY);
            EditorUtility.SetDirty(input);

            var rowY = top - UiFitLayout.SkinMargin - UiFitLayout.DockPad.y - inputHeight
                       - UiFitLayout.DockRowGap - rowHeight / 2f;
            var centers = UiFitLayout.RowCenters(widths, UiFitLayout.RowGap);
            for (var i = 0; i < rects.Length; i++)
            {
                rects[i].anchoredPosition = new Vector2(centers[i], rowY);
                EditorUtility.SetDirty(rects[i]);
            }

            // The two status lines stack under the dock.
            var below = top - dockHeight - UiFitLayout.BelowDockGap;
            foreach (var name in new[] { "FlowStatusText", "Phase10_ReleaseStatusText" })
            {
                var rt = (RectTransform)root.Find(name);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, below - rt.sizeDelta.y / 2f);
                below -= rt.sizeDelta.y + UiFitLayout.BelowDockGap;
                EditorUtility.SetDirty(rt);
            }
        }

        // ---------------------------------------------------------------- result

        public static void RestyleResult()
        {
            var scene = EditorSceneManager.OpenScene(ResultPath, OpenSceneMode.Single);
            var root = GameObject.Find("ResultCanvas").transform;

            SetMutedThreshold(root, UiFitLayout.MutedSizeThresholdScaled);
            // Card captions under the spread band are sized by ResultPanelPresenter at runtime.
            ScaleType(root, root.gameObject, t => !t.transform.GetComponentsInParent<Transform>(true)
                .Any(p => p.name.StartsWith("SpreadCell_", StringComparison.Ordinal)));

            Skin(root.Find("ResultReadingScroll").GetComponent<Image>(), glass);
            Skin(root.Find("Phase12_ResultCardShowcase").GetComponent<Image>(), glass);
            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var frame = band.GetChild(i).Find("ReversePivot/Frame")?.GetComponent<Image>();
                if (frame != null)
                {
                    Skin(frame, glass);
                }
            }

            foreach (var (name, emphasized) in new[]
            {
                ("BackToMenuButton", true),
                ("Phase66_RetryInterpretationButton", true),
                ("Phase66_OfflineInterpretationButton", false),
            })
            {
                SkinStandaloneButton((RectTransform)root.Find(name), emphasized);
            }

            SettleText(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- menu

        public static void RestyleMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var root = GameObject.Find("MainMenuCanvas").transform;
            var start = (RectTransform)root.Find("StartReadingButton");
            ScaleType(start, start.gameObject, _ => true);   // only the invitation's text grows
            SkinStandaloneButton(start, true);
            SettleText(start);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SkinStandaloneButton(RectTransform button, bool emphasized)
        {
            var label = button.Find("Label").GetComponent<TMP_Text>();
            FitLabelAtLeast(button, label);
            var skin = GetOrAdd<UiSkinState>(button.gameObject);
            skin.Configure(button.GetComponent<Image>(), label, glass, stock, true);
            skin.SetEmphasis(emphasized);
            EditorUtility.SetDirty(skin);
            EditorUtility.SetDirty(button.GetComponent<Button>());
        }

        // ---------------------------------------------------------------- shared tools

        /// <summary>Sizes a framed element to its label and stretches the label inside the skin margin.</summary>
        private static Vector2 FitLabel(RectTransform frame, TMP_Text label)
        {
            label.alignment = TextAlignmentOptions.Center;
            label.ForceMeshUpdate();
            var size = UiFitLayout.FitSize(label.GetPreferredValues(label.text), label.fontSize);
            frame.sizeDelta = size;
            Stretch(label.rectTransform, UiFitLayout.SkinMargin);
            EditorUtility.SetDirty(label);
            EditorUtility.SetDirty(frame);
            return size;
        }

        /// <summary>Like FitLabel, but never shrinks a standalone button below its authored size.</summary>
        private static void FitLabelAtLeast(RectTransform frame, TMP_Text label)
        {
            var authored = frame.sizeDelta;
            var size = FitLabel(frame, label);
            frame.sizeDelta = Vector2.Max(authored, size);
        }

        /// <summary>Scales every included TMP text under <paramref name="scope"/> once, recorded on the marker.</summary>
        private static void ScaleType(Transform scope, GameObject markerHost, Func<TMP_Text, bool> include)
        {
            var marker = GetOrAdd<UiTypeScale>(markerHost);
            var factor = UiFitLayout.TypeScale / marker.AppliedScale;
            foreach (var text in scope.GetComponentsInChildren<TMP_Text>(true).Where(include))
            {
                text.fontSize = UiFitLayout.ScaledFontSize(text.fontSize, factor);
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = UiFitLayout.ScaledFontSize(text.fontSizeMin, factor);
                    text.fontSizeMax = UiFitLayout.ScaledFontSize(text.fontSizeMax, factor);
                }

                EditorUtility.SetDirty(text);
            }

            marker.AppliedScale = UiFitLayout.TypeScale;
            EditorUtility.SetDirty(marker);
        }

        /// <summary>
        /// TMP caches its colour (m_fontColor32) only when the mesh is rebuilt. Rebuild every text
        /// before saving, or a colour set this run is serialized on the next one and a second run
        /// is not a no-op.
        /// </summary>
        private static void SettleText(Transform scope)
        {
            foreach (var text in scope.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(true, true);
                EditorUtility.SetDirty(text);
            }
        }

        private static void SetMutedThreshold(Transform canvas, float value)
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            var so = new SerializedObject(theme);
            so.FindProperty("mutedSizeThreshold").floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Skin(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }

        private static void AddCornerStars(RectTransform panel)
        {
            var size = Vector2.one * UiFitLayout.CornerStarSize;
            var m = UiFitLayout.SkinMargin;
            var tl = EnsureImage(panel, "Phase69_Star_TL", sparkle, CornerStar, size);
            tl.anchorMin = tl.anchorMax = new Vector2(0f, 1f);
            tl.anchoredPosition = new Vector2(m, -m);
            var br = EnsureImage(panel, "Phase69_Star_BR", sparkle, CornerStar, size);
            br.anchorMin = br.anchorMax = new Vector2(1f, 0f);
            br.anchoredPosition = new Vector2(-m, m);
        }

        private static void AddFlanks(RectTransform button)
        {
            var label = button.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var textWidth = label.GetPreferredValues(label.text).x;
            var star = Mathf.Round(UiFitLayout.FlankStarPerFont * label.fontSize);
            var x = textWidth / 2f + UiFitLayout.FlankGapPerFont * label.fontSize + star / 2f;
            EnsureImage(button, "Phase69_Flank_L", sparkle, FlankStar, Vector2.one * star).anchoredPosition = new Vector2(-x, 0f);
            EnsureImage(button, "Phase69_Flank_R", sparkle, FlankStar, Vector2.one * star).anchoredPosition = new Vector2(x, 0f);
        }

        private static RectTransform EnsureImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            if (existing == null)
            {
                rt.SetParent(parent, false);
            }

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(go);
            return rt;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            EditorUtility.SetDirty(rt);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
