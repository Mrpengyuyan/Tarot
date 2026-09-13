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
    /// Phase 67 on the real Result scene: clicking a card scrolls to its analysis block, the
    /// bottom fade hides at the end of the reading, and headings, the mode label and the band
    /// fade in with the reveal.
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
