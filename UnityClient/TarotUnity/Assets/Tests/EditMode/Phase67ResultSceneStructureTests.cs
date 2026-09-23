using System.IO;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the Result scene built by Phase67ResultReadingBootstrapper - a viewport that
    /// clears the gold frame, the offline notice and warning section around the Phase 29
    /// sections, scroll affordances, clickable cells, edge-pinned header and buttons, reveal
    /// companions, and a single-card panel wide enough for the Phase 8 reading column.
    /// </summary>
    public sealed class Phase67ResultSceneStructureTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const float FrameInnerGoldLine = 18f; // TarotPanel border rendered at pixelsPerUnitMultiplier 2

        private Transform canvas;

        private RectTransform Scroll => canvas.Find("ResultReadingScroll") as RectTransform;
        private RectTransform Viewport => Scroll.Find("Viewport") as RectTransform;
        private RectTransform Content => Viewport.Find("Content") as RectTransform;

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            canvas = GameObject.Find("ResultCanvas")?.transform;
            Assert.That(canvas, Is.Not.Null, "control: ResultCanvas exists");
        }

        [Test]
        public void ViewportClearsTheFrameBorder()
        {
            var frame = Scroll.GetComponent<Image>();
            Assert.That(frame.sprite?.name, Is.EqualTo("TarotPanel"), "control: the frame art the inset was measured on");
            Assert.That(frame.pixelsPerUnitMultiplier, Is.EqualTo(2f), "control: the border renders at half size");
            Assert.That(Viewport.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(Viewport.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(Viewport.offsetMin.x, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(Viewport.offsetMin.y, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(-Viewport.offsetMax.x, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
            Assert.That(-Viewport.offsetMax.y, Is.GreaterThanOrEqualTo(FrameInnerGoldLine + 6f));
        }

        [Test]
        public void ReadingSectionsKeepTheirOrderWithTheNoticeFirstAndWarningLast()
        {
            var expected = new[]
            {
                "Phase67_OfflineNotice",
                "Phase7_ResultSectionSummary", "SummaryText",
                "Phase7_ResultSectionOverall", "OverallText",
                "Phase7_ResultSectionCards", "CardAnalysisText",
                "Phase7_ResultSectionAdvice", "AdviceText",
                "Phase67_ResultSectionWarning", "WarningText",
            };
            var actual = Enumerable.Range(0, Content.childCount).Select(i => Content.GetChild(i).name).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(Content.Find("Phase67_OfflineNotice").gameObject.activeSelf, Is.False,
                "the notice is shown only for offline readings");
            Assert.That(Content.Find("Phase67_ResultSectionWarning").gameObject.activeSelf, Is.False);
            Assert.That(Content.Find("WarningText").gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("WarningText"), Is.Null, "WarningText has left the footer");
        }

        // Final review V1: the notice is an offline reading's first line, so it needs a section
        // heading's top margin - without it the text starts 2 units from the frame's corner dot.
        [Test]
        public void TheOfflineNoticeClearsTheFrameCornerDot()
        {
            var notice = Content.Find("Phase67_OfflineNotice").GetComponent<TMP_Text>();
            var heading = Content.Find("Phase7_ResultSectionSummary").GetComponent<TMP_Text>();
            Assert.That(heading.margin.y, Is.GreaterThan(0f), "control: a section heading has a top margin");
            Assert.That(notice.margin.y, Is.EqualTo(heading.margin.y), "the notice takes the same top margin");
        }

        [Test]
        public void ThemeKeepsTheOfflineNoticeInk()
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            Assert.That(theme, Is.Not.Null, "control: the Result canvas carries the UI theme");
            var notice = Content.Find("Phase67_OfflineNotice").GetComponent<TMP_Text>();
            var modeLabel = canvas.Find("Phase66_ModeLabel").GetComponent<TMP_Text>();
            var authoredNotice = notice.color;
            var authoredMode = modeLabel.color;

            theme.Apply();

            Assert.That(modeLabel.color, Is.Not.EqualTo(authoredMode), "control: the theme recolours an unmarked small text");
            Assert.That(notice.color, Is.EqualTo(authoredNotice), "the offline notice keeps its dark-gold ink");
            Assert.That(notice.GetComponent<TarotUiPreserveColor>(), Is.Not.Null, "the notice carries the colour-preserving marker");
        }

        [Test]
        public void ScrollbarAndFadeAreWired()
        {
            var scrollRect = Scroll.GetComponent<ScrollRect>();
            Assert.That(scrollRect.verticalScrollbar, Is.Not.Null);
            Assert.That(scrollRect.verticalScrollbar.name, Is.EqualTo("Phase67_ReadingScrollbar"));
            Assert.That(scrollRect.verticalScrollbarVisibility, Is.EqualTo(ScrollRect.ScrollbarVisibility.AutoHide));
            Assert.That(scrollRect.verticalScrollbar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop));

            var fade = Scroll.Find("Phase67_ReadingBottomFade");
            Assert.That(fade, Is.Not.Null);
            Assert.That(fade.GetComponent<ReadingFadeGradient>(), Is.Not.Null);
            Assert.That(fade.GetComponent<Image>().raycastTarget, Is.False, "the fade must not swallow scroll input");
            Assert.That(fade.GetSiblingIndex(), Is.GreaterThan(Viewport.GetSiblingIndex()), "the fade draws over the text");

            var navigator = Scroll.GetComponent<ResultReadingNavigator>();
            Assert.That(navigator, Is.Not.Null);
            var so = new SerializedObject(navigator);
            Assert.That(so.FindProperty("scroll").objectReferenceValue, Is.SameAs(scrollRect));
            Assert.That(so.FindProperty("bottomFade").objectReferenceValue, Is.SameAs(fade.GetComponent<Image>()));
            Assert.That(so.FindProperty("cardAnalysisText").objectReferenceValue,
                Is.SameAs(Content.Find("CardAnalysisText").GetComponent<TMP_Text>()));
            Assert.That(so.FindProperty("cardSectionHeading").objectReferenceValue,
                Is.SameAs(Content.Find("Phase7_ResultSectionCards")));
        }

        [Test]
        public void EverySpreadCellIsAClickTarget()
        {
            var band = canvas.Find("MP_ResultSpreadBand");
            Assert.That(band, Is.Not.Null, "control: the Phase 60 band exists");
            for (var i = 0; i < 10; i++)
            {
                var cell = band.Find($"SpreadCell_{i}");
                var target = cell.GetComponent<ResultSpreadCellTarget>();
                Assert.That(target, Is.Not.Null, $"cell {i} needs a ResultSpreadCellTarget");
                Assert.That(target.CardIndex, Is.EqualTo(i));

                var so = new SerializedObject(target);
                Assert.That(so.FindProperty("navigator").objectReferenceValue, Is.Not.Null);
                Assert.That(so.FindProperty("glow").objectReferenceValue, Is.SameAs(cell.Find("Glow").GetComponent<Image>()));
                Assert.That(cell.Find("Label").GetComponent<TMP_Text>().raycastTarget, Is.True, "the label is clickable too");
            }
        }

        [Test]
        public void HeaderAndButtonsArePinnedToTheCanvasEdges()
        {
            var fit = canvas.GetComponent<ResultCanvasAspectFit>();
            Assert.That(fit, Is.Not.Null);
            Assert.That(canvas.GetComponent<CanvasScaler>().matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f),
                "the saved scene keeps the shared match factor; the fit changes it only at runtime");

            var so = new SerializedObject(fit);
            Assert.That(so.FindProperty("scaler").objectReferenceValue, Is.SameAs(canvas.GetComponent<CanvasScaler>()));
            var pinned = so.FindProperty("pinned");
            var expected = new (string name, int edge)[]
            {
                ("QuestionText", 0), ("SpreadNameText", 0), ("Phase66_ModeLabel", 0), ("Phase8_ResultGoldDividerTop", 0),
                ("BackToMenuButton", 1), ("Phase66_RetryInterpretationButton", 1), ("Phase66_OfflineInterpretationButton", 1),
            };

            Assert.That(pinned.arraySize, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                var element = pinned.GetArrayElementAtIndex(i);
                var target = (RectTransform)element.FindPropertyRelative("target").objectReferenceValue;
                Assert.That(target.name, Is.EqualTo(expected[i].name));
                Assert.That(element.FindPropertyRelative("edge").enumValueIndex, Is.EqualTo(expected[i].edge));
                var y = ResultCanvasAspectFit.PinnedY(720f, (ResultCanvasAspectFit.Edge)expected[i].edge,
                    element.FindPropertyRelative("offsetFromEdge").floatValue);
                Assert.That(y, Is.EqualTo(target.anchoredPosition.y).Within(0.01f), $"{target.name} keeps its saved 16:9 position");
            }
        }

        [Test]
        public void RevealIncludesHeadingsModeLabelAndBand()
        {
            var so = new SerializedObject(canvas.GetComponent<ResultRevealDirector>());
            var groups = so.FindProperty("revealGroups");
            var companions = so.FindProperty("companions");
            var pairs = Enumerable.Range(0, companions.arraySize).Select(i =>
            {
                var element = companions.GetArrayElementAtIndex(i);
                var primary = (CanvasGroup)groups.GetArrayElementAtIndex(element.FindPropertyRelative("groupIndex").intValue)
                    .objectReferenceValue;
                var companion = (CanvasGroup)element.FindPropertyRelative("group").objectReferenceValue;
                return primary.name + ">" + companion.name;
            }).ToArray();

            Assert.That(groups.arraySize, Is.EqualTo(7), "control: the seven Phase 3 reveal groups are unchanged");
            Assert.That(pairs, Is.EquivalentTo(new[]
            {
                "SpreadNameText>Phase66_ModeLabel", "SpreadNameText>MP_ResultSpreadBand",
                "SummaryText>Phase67_OfflineNotice", "SummaryText>Phase7_ResultSectionSummary",
                "OverallText>Phase7_ResultSectionOverall", "CardAnalysisText>Phase7_ResultSectionCards",
                "AdviceText>Phase7_ResultSectionAdvice", "WarningText>Phase67_ResultSectionWarning",
            }));
        }

        [Test]
        public void SingleCardReadingPanelKeepsItsRightEdgeAndPresenterIsWired()
        {
            var so = new SerializedObject(canvas.GetComponent<ResultPanelPresenter>());
            var position = so.FindProperty("singleReadingPos").vector2Value;
            var size = so.FindProperty("singleReadingSize").vector2Value;

            Assert.That(position, Is.EqualTo(new Vector2(160f, 4f)));
            Assert.That(size, Is.EqualTo(new Vector2(772f, 448f)));
            Assert.That(position.x + size.x * 0.5f, Is.EqualTo(546f).Within(0.01f), "the right edge stays where Phase 60 put it");
            Assert.That(Scroll.anchoredPosition, Is.EqualTo(position), "the saved scene shows the single-card layout");
            Assert.That(Scroll.sizeDelta, Is.EqualTo(size));

            Assert.That(so.FindProperty("readingNavigator").objectReferenceValue,
                Is.SameAs(Scroll.GetComponent<ResultReadingNavigator>()));
            Assert.That(so.FindProperty("offlineNoticeText").objectReferenceValue,
                Is.SameAs(Content.Find("Phase67_OfflineNotice").GetComponent<TMP_Text>()));
            Assert.That(so.FindProperty("warningHeading").objectReferenceValue,
                Is.SameAs(Content.Find("Phase67_ResultSectionWarning").gameObject));
            Assert.That(so.FindProperty("bottomDivider").objectReferenceValue,
                Is.SameAs(canvas.Find("Phase8_ResultGoldDividerBottom").gameObject));
            Assert.That(so.FindProperty("cellTargets").arraySize, Is.EqualTo(10));
        }

        [Test]
        public void Phase67DocumentationAndScreenshotsExist()
        {
            const string docPath = "Docs/PHASE67_RESULT_READING.md";
            Assert.That(File.Exists(docPath), Is.True, $"Missing Phase 67 doc at {docPath}");
            var doc = File.ReadAllText(docPath);
            Assert.That(doc, Does.Contain("ResultSpreadLayout"));
            Assert.That(doc, Does.Contain("CardAnalysisParser"));
            Assert.That(doc, Does.Contain("noparse"));
            Assert.That(File.ReadAllText("Docs/PROJECT_CHRONICLE.md"), Does.Contain("### Phase 67"));

            var shots = new[]
            {
                "Result_1card_16x9.png", "Result_3card_16x9.png", "Result_5card_16x9.png", "Result_10card_16x9.png",
                "Result_3card_16x10.png", "Result_10card_16x10.png", "Result_3card_4x3.png",
                "Result_pending20s.png", "Result_failed.png", "Result_offline.png",
            };
            foreach (var file in shots)
            {
                var path = Path.Combine("Docs/VisualReview/Phase67", file);
                Assert.That(File.Exists(path), Is.True, $"Missing review shot {path}");
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096), $"{path} is unexpectedly small");
            }
        }
    }
}
