using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 67 on the real Result scene: clicking a card scrolls to its analysis block (or to the
    /// 牌面分析 heading when the analysis did not split, and nowhere while the reading is not
    /// interactive), the bottom fade hides at the end of the reading, headings, the mode label and
    /// the band fade in with the reveal, and the layout holds at the runtime canvas size.
    /// </summary>
    public sealed class Phase67ResultReadingPlayTests
    {
        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ReadingSessionStore.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickingACardScrollsItsBlockToTheTopAndHighlightsIt()
        {
            yield return LoadResultWith(Offline(10));
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            Canvas.ForceUpdateCanvases();

            Assert.That(navigator.Blocks.Count, Is.EqualTo(10), "control: the analysis was split into ten blocks");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height), "control: the reading scrolls");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "control: it starts at the top");

            var cell = GameObject.Find("MP_ResultSpreadBand").transform.Find("SpreadCell_4").GetComponent<ResultSpreadCellTarget>();
            cell.Activate();
            Assert.That(navigator.HighlightedCard, Is.EqualTo(4), "the heading glow starts at once");
            yield return new WaitForSecondsRealtime(0.6f);

            var text = GetField<TMP_Text>(presenter, "cardAnalysisText");
            text.ForceMeshUpdate();
            var block = navigator.Blocks[4];
            var info = text.textInfo;
            var line = info.characterInfo[block.HeadingStart].lineNumber;
            var world = text.rectTransform.TransformPoint(new Vector3(0f, info.lineInfo[line].ascender, 0f));
            var viewport = scroll.viewport;
            var fromTop = viewport.rect.yMax - viewport.InverseTransformPoint(world).y;

            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(0.999f), "control: the reading moved");
            Assert.That(fromTop, Is.InRange(-1f, viewport.rect.height * 0.2f),
                "the fifth card's heading sits near the top of the viewport");
        }

        [UnityTest]
        public IEnumerator FadeHidesAtTheBottomOfTheReading()
        {
            yield return LoadResultWith(Offline(10));
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            yield return null;

            Assert.That(navigator.IsFadeVisible, Is.True, "long text at the top shows the fade");
            Assert.That(scroll.verticalScrollbar.gameObject.activeInHierarchy, Is.True, "the scrollbar shows for long text");

            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            yield return null;

            Assert.That(navigator.IsFadeVisible, Is.False, "the fade hides at the bottom");
        }

        [UnityTest]
        public IEnumerator RevealBringsInTheHeadingsModeLabelAndBand()
        {
            yield return LoadResultWith(Offline(3));
            var director = Object.FindFirstObjectByType<ResultRevealDirector>();
            var summaryHeading = GameObject.Find("Phase7_ResultSectionSummary").GetComponent<CanvasGroup>();

            Assert.That(director.IsRevealComplete, Is.False, "control: the reveal is still running");
            Assert.That(summaryHeading.alpha, Is.LessThan(1f), "a heading waits for its section");

            yield return WaitUntil(() => director.IsRevealComplete, 10f, "expected the reveal to finish");

            var names = new[]
            {
                "Phase66_ModeLabel", "MP_ResultSpreadBand", "Phase67_OfflineNotice", "Phase7_ResultSectionSummary",
                "Phase7_ResultSectionOverall", "Phase7_ResultSectionCards", "Phase7_ResultSectionAdvice",
            };
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                Assert.That(go, Is.Not.Null, $"{name} should be active for an offline three-card reading");
                Assert.That(go.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f), $"{name} is fully shown after the reveal");
            }
        }

        // Final review I2: when the card analysis did not split, a card click scrolls to the 牌面分析 heading.
        [UnityTest]
        public IEnumerator ClickingACardWhenTheAnalysisDidNotSplitScrollsToTheCardSection()
        {
            var session = Offline(3);
            session.summary = Repeat("旧的节奏正在松动，新的方向需要你亲手确认。", 10);
            session.overallInterpretation = Repeat("眼下的犹豫来自选择太多，把注意力收回到一件事上。", 20);
            session.cardAnalysis = Repeat("整组牌讲的是节奏与专注，先收拢精力再往前走。", 10);
            session.advice = Repeat("这一周只定一个目标，每天为它做一件小事。", 60);
            yield return LoadResultWith(session);
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            Canvas.ForceUpdateCanvases();

            var viewport = scroll.viewport;
            var heading = (RectTransform)scroll.content.Find("Phase7_ResultSectionCards");
            Assert.That(session.cardAnalysis, Does.Not.Contain("\n"), "control: one paragraph for three cards");
            Assert.That(navigator.Blocks.Count, Is.EqualTo(0), "control: the analysis did not split");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(viewport.rect.height), "control: the reading scrolls");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "control: it starts at the top");
            Assert.That(TopFromViewportTop(heading, viewport), Is.GreaterThan(viewport.rect.height * 0.2f),
                "control: the 牌面分析 heading starts below the top 20% of the viewport");

            var cell = GameObject.Find("MP_ResultSpreadBand").transform.Find("SpreadCell_1").GetComponent<ResultSpreadCellTarget>();
            cell.Activate();
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(0.999f), "the reading moved");
            Assert.That(TopFromViewportTop(heading, viewport), Is.InRange(-1f, viewport.rect.height * 0.2f),
                "the 牌面分析 heading sits near the top of the viewport");
            Assert.That(navigator.HighlightedCard, Is.EqualTo(-1), "there is no block to glow");
        }

        // Final review I2 (Ruling 15l): the interactive gate keeps a click from scrolling a reading that is
        // not shown, such as one still generating.
        [UnityTest]
        public IEnumerator ClickingACardWhileNotInteractiveDoesNotScroll()
        {
            yield return LoadResultWith(Offline(10));
            var navigator = Object.FindFirstObjectByType<ResultReadingNavigator>();
            var scroll = navigator.GetComponent<ScrollRect>();
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            Canvas.ForceUpdateCanvases();

            Assert.That(navigator.Blocks.Count, Is.EqualTo(10), "control: the analysis was split into ten blocks");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height), "control: the reading scrolls");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "control: it starts at the top");
            Assert.That(navigator.IsInteractive, Is.True, "control: the reading is interactive until the gate closes");

            navigator.SetInteractive(false); // the call ShowPending makes for a generating reading
            var before = scroll.verticalNormalizedPosition;
            var cell = GameObject.Find("MP_ResultSpreadBand").transform.Find("SpreadCell_4").GetComponent<ResultSpreadCellTarget>();
            cell.Activate();
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(before).Within(0.001f),
                "a reading that is not interactive does not scroll");
            Assert.That(navigator.HighlightedCard, Is.EqualTo(-1), "a reading that is not interactive glows no heading");
        }

        // Final review M6 (Ruling 15e): the aspect fit, the edge pins and the relayout hold at whatever canvas
        // size this run has; batch PlayMode cannot force a Game view aspect.
        [UnityTest]
        public IEnumerator ResultLayoutHoldsAtTheRuntimeCanvas()
        {
            yield return LoadResultWith(Offline(3));
            yield return WaitUntil(() => Object.FindFirstObjectByType<ResultRevealDirector>().IsRevealComplete, 10f,
                "expected the reveal to finish");
            yield return null;
            yield return null;

            var canvas = (RectTransform)GameObject.Find("ResultCanvas").transform;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var where = $"screen {Screen.width}x{Screen.height}, canvas {canvas.rect.width:0.##}x{canvas.rect.height:0.##}";
            Debug.Log($"Phase 67 runtime layout check: {where}");

            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(ResultCanvasAspectFit.MatchFor(Screen.width, Screen.height)),
                $"the match factor is the one ResultCanvasAspectFit picks for this aspect ({where})");

            var reading = RectIn(canvas, (RectTransform)canvas.Find("ResultReadingScroll"));
            var buttonTop = float.MinValue;
            foreach (var name in new[] { "BackToMenuButton", "Phase66_RetryInterpretationButton", "Phase66_OfflineInterpretationButton" })
            {
                var button = (RectTransform)canvas.Find(name);
                if (button != null && button.gameObject.activeInHierarchy)
                {
                    buttonTop = Mathf.Max(buttonTop, RectIn(canvas, button).yMax);
                }
            }

            Assert.That(buttonTop, Is.GreaterThan(float.MinValue), $"control: the button row is showing ({where})");
            Assert.That(reading.yMin - buttonTop, Is.GreaterThanOrEqualTo(10f - 0.01f),
                $"the reading panel ends at least 10 above the button row: reading bottom {reading.yMin:0.##}, " +
                $"button top {buttonTop:0.##} ({where})");

            var band = canvas.Find("MP_ResultSpreadBand");
            var bandBottom = float.MaxValue;
            var shownCells = 0;
            for (var i = 0; i < band.childCount; i++)
            {
                var cell = band.GetChild(i);
                if (!cell.gameObject.activeInHierarchy)
                {
                    continue;
                }

                shownCells++;
                foreach (var part in new[] { "ReversePivot/Frame", "Label" })
                {
                    var rect = cell.Find(part) as RectTransform;
                    if (rect != null)
                    {
                        bandBottom = Mathf.Min(bandBottom, RectIn(canvas, rect).yMin);
                    }
                }
            }

            Assert.That(shownCells, Is.EqualTo(3), $"control: the band shows the three cards ({where})");
            Assert.That(bandBottom, Is.GreaterThan(reading.yMax),
                $"the card band sits above the reading panel: band bottom {bandBottom:0.##}, reading top {reading.yMax:0.##} ({where})");
        }

        private static string Repeat(string sentence, int times)
        {
            return string.Concat(System.Linq.Enumerable.Repeat(sentence, times));
        }

        private static float TopFromViewportTop(RectTransform target, RectTransform viewport)
        {
            var world = target.TransformPoint(new Vector3(0f, target.rect.yMax, 0f));
            return viewport.rect.yMax - viewport.InverseTransformPoint(world).y;
        }

        // A rect's bounds in an ancestor's space, built from local transforms so the canvas scale factor
        // does not enter.
        private static Rect RectIn(Transform ancestor, RectTransform rect)
        {
            var toAncestor = Matrix4x4.identity;
            for (Transform t = rect; t != null && t != ancestor; t = t.parent)
            {
                toAncestor = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * toAncestor;
            }

            var r = rect.rect;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax) })
            {
                var point = (Vector2)toAncestor.MultiplyPoint3x4(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static ReadingSessionSnapshot Offline(int cardCount)
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            return LocalReadingSimulator.CreateSession(2, "牌阵", "此刻我最需要留意什么？", "general", draws);
        }

        private static IEnumerator LoadResultWith(ReadingSessionSnapshot session)
        {
            ReadingSessionStore.Save(session);
            SceneManager.LoadScene("Result");
            yield return null;
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == "Result"
                    && Object.FindFirstObjectByType<ResultSceneController>() != null,
                10f,
                "expected the Result scene");
            yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float seconds, string message)
        {
            var timeoutAt = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, message);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");

            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"Field {fieldName} on {target.GetType().Name} is null");
            return value;
        }
    }
}
