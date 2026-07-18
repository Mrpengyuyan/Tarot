using System.IO;
using NUnit.Framework;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase25ResultCompositionTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DocPath = "Docs/PHASE25_RESULT_COMPOSITION.md";

        private static readonly (string header, string body)[] Sections =
        {
            ("Phase7_ResultSectionSummary", "SummaryText"),
            ("Phase7_ResultSectionOverall", "OverallText"),
            ("Phase7_ResultSectionCards", "CardAnalysisText"),
            ("Phase7_ResultSectionAdvice", "AdviceText"),
        };

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            // Phase 29 drives the reading sections through a VerticalLayoutGroup inside
            // the scroll Content. Rebuild it so world-space geometry reflects the layout.
            var content = FindAny("Content") as RectTransform;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
        }

        [Test]
        public void SectionHeadersStackAboveBodiesWithoutOverlap()
        {
            foreach (var (headerName, bodyName) in Sections)
            {
                var header = FindRect(headerName);
                var body = FindRect(bodyName);
                Assert.That(header, Is.Not.Null, headerName);
                Assert.That(body, Is.Not.Null, bodyName);

                Assert.That(TopEdge(body), Is.LessThanOrEqualTo(BottomEdge(header) + 1f),
                    $"{bodyName} (top {TopEdge(body)}) overlaps {headerName} (bottom {BottomEdge(header)})");
            }
        }

        [Test]
        public void EachSectionHeaderIsGoldAccent()
        {
            foreach (var (headerName, _) in Sections)
            {
                var header = FindRect(headerName);
                Assert.That(header.GetComponent<TarotUiAccentText>(), Is.Not.Null,
                    $"{headerName} should carry the gold accent marker so it stays gold at runtime");
            }
        }

        [Test]
        public void CardHeroSitsLeftOfReadingColumn()
        {
            var card = FindRect("Phase12_ResultCardShowcase");
            var firstHeader = FindRect("Phase7_ResultSectionSummary");
            Assert.That(card, Is.Not.Null);
            Assert.That(firstHeader, Is.Not.Null);

            Assert.That(card.anchoredPosition.x, Is.LessThan(0f), "Card hero should be in the left third");
            Assert.That(RightEdge(card), Is.LessThan(LeftEdge(firstHeader)),
                "Card hero must not overlap the reading column horizontally");
        }

        [Test]
        public void QuestionHeaderSitsAboveTheReadingColumn()
        {
            var question = FindRect("QuestionText");
            var firstHeader = FindRect("Phase7_ResultSectionSummary");
            Assert.That(question, Is.Not.Null);
            Assert.That(BottomEdge(question), Is.GreaterThan(TopEdge(firstHeader)),
                "Question should sit above the first reading section");
        }

        [Test]
        public void RedundantOverlaysDeactivated()
        {
            foreach (var name in new[] { "Phase8_ResultCrest", "Phase11_ResultReadingColumns", "Phase14_ResultTextBridge" })
            {
                var t = FindAny(name);
                Assert.That(t, Is.Not.Null, $"{name} should still exist");
                Assert.That(t.gameObject.activeSelf, Is.False, $"{name} should be deactivated, not deleted");
            }
        }

        [Test]
        public void OverallTextMeetsSizeFloor()
        {
            var overall = FindRect("OverallText").GetComponent<TMP_Text>();
            Assert.That(overall.fontSize, Is.GreaterThanOrEqualTo(19f),
                "OverallText must keep the body size floor other phases assert");
        }

        [Test]
        public void PresenterWiringIntact()
        {
            var presenter = Object.FindObjectsByType<ResultPanelPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(presenter.Length, Is.GreaterThan(0), "Result scene should keep its ResultPanelPresenter");

            var serialized = new SerializedObject(presenter[0]);
            foreach (var field in new[] { "questionText", "spreadNameText", "summaryText", "overallText", "cardAnalysisText", "adviceText" })
            {
                Assert.That(serialized.FindProperty(field)?.objectReferenceValue, Is.Not.Null,
                    $"Repositioning must not have dropped the {field} reference");
            }
        }

        [Test]
        public void Phase25DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase25 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("Result"));
            Assert.That(text, Does.Contain("rule of thirds"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        // World-space edges so comparisons stay valid no matter how deeply a rect is
        // nested (Phase 29 moved the reading sections into the scroll Content). Corner
        // order from GetWorldCorners is bottom-left, top-left, top-right, bottom-right.
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private static float TopEdge(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[1].y;
        }

        private static float BottomEdge(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[0].y;
        }

        private static float LeftEdge(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[0].x;
        }

        private static float RightEdge(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[3].x;
        }

        private static RectTransform FindRect(string name) => FindAny(name) as RectTransform;

        private static Transform FindAny(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
