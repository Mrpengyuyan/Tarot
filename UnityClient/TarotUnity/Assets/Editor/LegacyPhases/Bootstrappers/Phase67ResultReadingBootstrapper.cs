using System.Collections.Generic;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 67 (spec C): builds the Result reading experience on the existing scene.
    /// - Viewport inset 24 inside the gold frame, new content padding and spacing.
    /// - Offline notice first in the reading; a 提醒 section (WarningText moved in from the footer) last.
    /// - Bottom fade, scrollbar, reading navigator, click targets on every spread cell.
    /// - Edge pinning for the header and button row, and reveal companions.
    /// - Presenter wiring, plus a single-card panel of 772x448 at x 160.
    /// Re-running reuses what it created.
    /// </summary>
    public static class Phase67ResultReadingBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string OfflineNoticeName = "Phase67_OfflineNotice";
        public const string WarningHeadingName = "Phase67_ResultSectionWarning";
        public const string ScrollbarName = "Phase67_ReadingScrollbar";
        public const string BottomFadeName = "Phase67_ReadingBottomFade";

        private const float ViewportInset = 24f;
        private const float ContentSpacing = 8f;
        private const float HeadingTopMargin = 10f;
        private const float BodyLineSpacing = 10f;
        private const float ScrollbarWidth = 4f;
        private const float ScrollbarRightInset = 26f;
        private const float ScrollbarVerticalInset = 30f;
        private const float FadeHeight = 36f;
        private static readonly Vector2 SingleReadingPos = new Vector2(160f, 4f);
        private static readonly Vector2 SingleReadingSize = new Vector2(772f, 448f);

        private static readonly Color NoticeInk = new Color(0.78f, 0.66f, 0.44f, 1f);
        private static readonly Color BodyInk = new Color(0.92f, 0.88f, 0.78f, 1f);
        private static readonly Color FrameFill = new Color(0.118f, 0.059f, 0.102f, 0.92f);
        private static readonly Color ScrollTrack = new Color(0.86f, 0.71f, 0.42f, 0.12f);
        private static readonly Color ScrollHandle = new Color(0.86f, 0.71f, 0.42f, 0.8f);

        private static readonly string[] SectionHeadings =
        {
            "Phase7_ResultSectionSummary", "Phase7_ResultSectionOverall", "Phase7_ResultSectionCards", "Phase7_ResultSectionAdvice",
        };

        private static readonly string[] SectionBodies = { "SummaryText", "OverallText", "CardAnalysisText", "AdviceText" };

        private static readonly (string name, ResultCanvasAspectFit.Edge edge)[] PinnedElements =
        {
            ("QuestionText", ResultCanvasAspectFit.Edge.Top),
            ("SpreadNameText", ResultCanvasAspectFit.Edge.Top),
            ("Phase66_ModeLabel", ResultCanvasAspectFit.Edge.Top),
            ("Phase8_ResultGoldDividerTop", ResultCanvasAspectFit.Edge.Top),
            ("BackToMenuButton", ResultCanvasAspectFit.Edge.Bottom),
            ("Phase66_RetryInterpretationButton", ResultCanvasAspectFit.Edge.Bottom),
            ("Phase66_OfflineInterpretationButton", ResultCanvasAspectFit.Edge.Bottom),
        };

        private static readonly (string group, string companion)[] RevealCompanions =
        {
            ("SpreadNameText", "Phase66_ModeLabel"),
            ("SpreadNameText", "MP_ResultSpreadBand"),
            ("SummaryText", OfflineNoticeName),
            ("SummaryText", "Phase7_ResultSectionSummary"),
            ("OverallText", "Phase7_ResultSectionOverall"),
            ("CardAnalysisText", "Phase7_ResultSectionCards"),
            ("AdviceText", "Phase7_ResultSectionAdvice"),
            ("WarningText", WarningHeadingName),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 67 Result Reading Bootstrap")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvasObject = GameObject.Find("ResultCanvas");
            if (canvasObject == null)
            {
                Debug.LogError("Phase 67: ResultCanvas not found.");
                return;
            }

            var canvas = canvasObject.transform;
            var presenter = canvasObject.GetComponent<ResultPanelPresenter>();
            var reveal = canvasObject.GetComponent<ResultRevealDirector>();
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            var scroll = canvas.Find("ResultReadingScroll") as RectTransform;
            var scrollRect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
            var viewport = scroll != null ? scroll.Find("Viewport") as RectTransform : null;
            var content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            var band = canvas.Find("MP_ResultSpreadBand");
            var warning = FindDeep(canvas, "WarningText");
            var adviceHeading = content != null ? content.Find("Phase7_ResultSectionAdvice") : null;
            var summaryText = content != null ? content.Find("SummaryText")?.GetComponent<TMP_Text>() : null;
            var bottomDivider = canvas.Find("Phase8_ResultGoldDividerBottom");
            if (presenter == null || reveal == null || scaler == null || scrollRect == null || viewport == null
                || content == null || band == null || warning == null || adviceHeading == null || summaryText == null
                || bottomDivider == null)
            {
                Debug.LogError("Phase 67: expected Result objects are missing (presenter, reveal director, scaler, " +
                    "reading scroll, spread band, WarningText, section headings or the bottom divider).");
                return;
            }

            LayOutViewport(viewport, content);
            var notice = EnsureNotice(content, summaryText);
            var warningHeading = EnsureWarningSection(content, adviceHeading, warning);
            var fade = EnsureBottomFade(scroll, viewport);
            EnsureScrollbar(scroll, scrollRect, fade.transform.GetSiblingIndex() + 1);
            var navigator = EnsureNavigator(scroll, scrollRect, fade, content);
            var targets = EnsureCellTargets(band, navigator);
            EnsureAspectFit(canvasObject, scaler);
            WireRevealCompanions(reveal, canvas);
            WirePresenter(presenter, navigator, notice, warningHeading, bottomDivider.gameObject, targets);

            scroll.anchoredPosition = SingleReadingPos;
            scroll.sizeDelta = SingleReadingSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 67: Result reading layout built and wired.");
        }

        private static void LayOutViewport(RectTransform viewport, RectTransform content)
        {
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ViewportInset, ViewportInset);
            viewport.offsetMax = new Vector2(-ViewportInset, -ViewportInset);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 14, 8, 28);
            layout.spacing = ContentSpacing;

            foreach (var name in SectionHeadings)
            {
                var heading = content.Find(name)?.GetComponent<TMP_Text>();
                if (heading != null)
                {
                    heading.margin = new Vector4(0f, HeadingTopMargin, 0f, 0f);
                }
            }

            foreach (var name in SectionBodies)
            {
                var body = content.Find(name)?.GetComponent<TMP_Text>();
                if (body != null)
                {
                    body.lineSpacing = BodyLineSpacing;
                }
            }
        }

        private static TextMeshProUGUI EnsureNotice(RectTransform content, TMP_Text fontSource)
        {
            var notice = EnsureContentText(content, OfflineNoticeName, fontSource);
            EnsureComponent<TarotUiPreserveColor>(notice.gameObject);
            notice.fontSize = 16f;
            notice.color = NoticeInk;
            notice.alignment = TextAlignmentOptions.TopLeft;
            // The notice is the first line of an offline reading, so it needs the same top margin as a
            // section heading - without it the text starts 2 units from the frame's corner dot.
            notice.margin = new Vector4(0f, HeadingTopMargin, 0f, 0f);
            notice.enableWordWrapping = true;
            notice.text = ReleaseUxCopy.OfflineWarning;
            notice.transform.SetSiblingIndex(0);
            notice.gameObject.SetActive(false);
            return notice;
        }

        private static GameObject EnsureWarningSection(RectTransform content, Transform adviceHeading, Transform warning)
        {
            var heading = content.Find(WarningHeadingName);
            if (heading == null)
            {
                // A copy of the 建议 heading keeps its font, gold accent marker and size.
                heading = Object.Instantiate(adviceHeading.gameObject, content).transform;
                heading.name = WarningHeadingName;
            }

            heading.GetComponent<TMP_Text>().text = ReleaseUxCopy.ResultSectionWarning;
            heading.SetAsLastSibling();
            heading.gameObject.SetActive(false);

            warning.SetParent(content, false);
            warning.SetAsLastSibling();
            var warningRect = (RectTransform)warning;
            warningRect.anchorMin = new Vector2(0f, 1f);
            warningRect.anchorMax = new Vector2(1f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.anchoredPosition = Vector2.zero;
            var warningText = warning.GetComponent<TMP_Text>();
            warningText.fontSize = 19f;
            warningText.color = BodyInk;
            warningText.alignment = TextAlignmentOptions.TopLeft;
            warningText.enableWordWrapping = true;
            warningText.lineSpacing = BodyLineSpacing;
            warningText.margin = Vector4.zero;
            warning.gameObject.SetActive(false);
            return heading.gameObject;
        }

        private static Image EnsureBottomFade(RectTransform scroll, RectTransform viewport)
        {
            var fade = EnsureChild(scroll, BottomFadeName);
            fade.anchorMin = new Vector2(0f, 0f);
            fade.anchorMax = new Vector2(1f, 0f);
            fade.pivot = new Vector2(0.5f, 0f);
            fade.anchoredPosition = new Vector2(0f, ViewportInset);
            fade.sizeDelta = new Vector2(-2f * ViewportInset, FadeHeight);

            var image = EnsureComponent<Image>(fade.gameObject);
            image.sprite = null;
            image.color = FrameFill;
            image.raycastTarget = false;
            EnsureComponent<ReadingFadeGradient>(fade.gameObject);
            fade.SetSiblingIndex(viewport.GetSiblingIndex() + 1);
            return image;
        }

        private static void EnsureScrollbar(RectTransform scroll, ScrollRect scrollRect, int siblingIndex)
        {
            var bar = EnsureChild(scroll, ScrollbarName);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.anchoredPosition = new Vector2(-ScrollbarRightInset, 0f);
            bar.sizeDelta = new Vector2(ScrollbarWidth, -2f * ScrollbarVerticalInset);
            var track = EnsureComponent<Image>(bar.gameObject);
            track.sprite = null;
            track.color = ScrollTrack;

            var handle = EnsureChild(bar, "Handle");
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            var handleImage = EnsureComponent<Image>(handle.gameObject);
            handleImage.sprite = null;
            handleImage.color = ScrollHandle;

            var scrollbar = EnsureComponent<Scrollbar>(bar.gameObject);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 0f;
            bar.SetSiblingIndex(siblingIndex);
        }

        private static ResultReadingNavigator EnsureNavigator(
            RectTransform scroll, ScrollRect scrollRect, Image fade, RectTransform content)
        {
            var navigator = EnsureComponent<ResultReadingNavigator>(scroll.gameObject);
            var so = new SerializedObject(navigator);
            so.FindProperty("scroll").objectReferenceValue = scrollRect;
            so.FindProperty("bottomFade").objectReferenceValue = fade;
            so.FindProperty("cardAnalysisText").objectReferenceValue = content.Find("CardAnalysisText").GetComponent<TMP_Text>();
            so.FindProperty("cardSectionHeading").objectReferenceValue = content.Find("Phase7_ResultSectionCards");
            so.ApplyModifiedPropertiesWithoutUndo();
            return navigator;
        }

        private static ResultSpreadCellTarget[] EnsureCellTargets(Transform band, ResultReadingNavigator navigator)
        {
            var targets = new List<ResultSpreadCellTarget>();
            for (var i = 0; ; i++)
            {
                var cell = band.Find($"SpreadCell_{i}");
                if (cell == null)
                {
                    break;
                }

                var target = EnsureComponent<ResultSpreadCellTarget>(cell.gameObject);
                var so = new SerializedObject(target);
                so.FindProperty("cardIndex").intValue = i;
                so.FindProperty("navigator").objectReferenceValue = navigator;
                so.FindProperty("glow").objectReferenceValue = cell.Find("Glow")?.GetComponent<Image>();
                so.ApplyModifiedPropertiesWithoutUndo();

                var label = cell.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.raycastTarget = true;
                }

                targets.Add(target);
            }

            return targets.ToArray();
        }

        private static void EnsureAspectFit(GameObject canvasObject, CanvasScaler scaler)
        {
            var canvas = canvasObject.transform;
            var fit = EnsureComponent<ResultCanvasAspectFit>(canvasObject);
            var so = new SerializedObject(fit);
            so.FindProperty("scaler").objectReferenceValue = scaler;
            so.FindProperty("canvasRect").objectReferenceValue = canvasObject.GetComponent<RectTransform>();
            var pinned = so.FindProperty("pinned");
            pinned.arraySize = PinnedElements.Length;
            var half = TarotUiSpacing.ReferenceHeight * 0.5f;
            for (var i = 0; i < PinnedElements.Length; i++)
            {
                var (name, edge) = PinnedElements[i];
                var target = canvas.Find(name) as RectTransform;
                if (target == null)
                {
                    Debug.LogError($"Phase 67: {name} not found for edge pinning.");
                    continue;
                }

                // The offset is taken from the saved 16:9 position, so pinning at 720 changes nothing.
                var element = pinned.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("target").objectReferenceValue = target;
                element.FindPropertyRelative("edge").enumValueIndex = (int)edge;
                element.FindPropertyRelative("offsetFromEdge").floatValue = edge == ResultCanvasAspectFit.Edge.Top
                    ? half - target.anchoredPosition.y
                    : target.anchoredPosition.y + half;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRevealCompanions(ResultRevealDirector reveal, Transform canvas)
        {
            var so = new SerializedObject(reveal);
            var groups = so.FindProperty("revealGroups");
            var companions = so.FindProperty("companions");
            companions.arraySize = RevealCompanions.Length;
            for (var i = 0; i < RevealCompanions.Length; i++)
            {
                var (groupName, companionName) = RevealCompanions[i];
                var index = -1;
                for (var g = 0; g < groups.arraySize; g++)
                {
                    var group = groups.GetArrayElementAtIndex(g).objectReferenceValue as CanvasGroup;
                    if (group != null && group.name == groupName)
                    {
                        index = g;
                        break;
                    }
                }

                var companion = FindDeep(canvas, companionName);
                if (index < 0 || companion == null)
                {
                    Debug.LogError($"Phase 67: reveal companion {groupName} > {companionName} could not be wired.");
                    continue;
                }

                var element = companions.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("groupIndex").intValue = index;
                element.FindPropertyRelative("group").objectReferenceValue = EnsureComponent<CanvasGroup>(companion.gameObject);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePresenter(
            ResultPanelPresenter presenter, ResultReadingNavigator navigator, TMP_Text notice, GameObject warningHeading,
            GameObject bottomDivider, ResultSpreadCellTarget[] targets)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("readingNavigator").objectReferenceValue = navigator;
            so.FindProperty("offlineNoticeText").objectReferenceValue = notice;
            so.FindProperty("warningHeading").objectReferenceValue = warningHeading;
            so.FindProperty("bottomDivider").objectReferenceValue = bottomDivider;
            var cells = so.FindProperty("cellTargets");
            cells.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++)
            {
                cells.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            }

            so.FindProperty("singleReadingPos").vector2Value = SingleReadingPos;
            so.FindProperty("singleReadingSize").vector2Value = SingleReadingSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMeshProUGUI EnsureContentText(RectTransform content, string name, TMP_Text fontSource)
        {
            var existing = content.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(content, false);
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            // Borrow the body SDF font and material so the font-role guards hold.
            var text = EnsureComponent<TextMeshProUGUI>(go);
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
