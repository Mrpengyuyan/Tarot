using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 29 replaces Phase 26's best-fit shrink with a real scrollable reading
    /// panel: the four interpretation sections live in a ScrollRect whose Content
    /// auto-sizes to the text, so any length renders at full size and scrolls when it
    /// exceeds the viewport instead of overflowing or shrinking.
    /// </summary>
    public sealed class Phase29ResultScrollTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DocPath = "Docs/PHASE29_RESULT_SCROLL.md";

        private static readonly string[] SectionOrder =
        {
            "Phase7_ResultSectionSummary", "SummaryText",
            "Phase7_ResultSectionOverall", "OverallText",
            "Phase7_ResultSectionCards", "CardAnalysisText",
            "Phase7_ResultSectionAdvice", "AdviceText",
        };

        private static readonly string[] BodyTextNames =
        {
            "SummaryText", "OverallText", "CardAnalysisText", "AdviceText",
        };

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
        }

        [Test]
        public void ScrollRectIsWiredForVerticalReading()
        {
            var scroll = FindScroll();
            Assert.That(scroll, Is.Not.Null, "ResultReadingScroll with a ScrollRect should exist");
            Assert.That(scroll.vertical, Is.True, "Reading panel should scroll vertically");
            Assert.That(scroll.horizontal, Is.False, "Reading panel should not scroll horizontally");
            Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped),
                "Clamped movement keeps the reading from rubber-banding past its bounds");
            Assert.That(scroll.viewport, Is.Not.Null, "ScrollRect.viewport must be assigned");
            Assert.That(scroll.content, Is.Not.Null, "ScrollRect.content must be assigned");
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null,
                "Viewport must clip overflowing content with a RectMask2D");
        }

        [Test]
        public void ContentDrivesLayoutBySize()
        {
            var content = FindScroll().content;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            Assert.That(vlg, Is.Not.Null, "Content needs a VerticalLayoutGroup to stack sections");
            Assert.That(vlg.childControlHeight, Is.True);
            Assert.That(vlg.childForceExpandHeight, Is.False,
                "Sections should size to their own copy, not be force-stretched");

            var fitter = content.GetComponent<ContentSizeFitter>();
            Assert.That(fitter, Is.Not.Null, "Content needs a ContentSizeFitter so it grows with the text");
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize),
                "Content height must track the preferred height of the reading");
        }

        [Test]
        public void AllSectionsLiveInContentInReadingOrder()
        {
            var content = FindScroll().content;
            for (var i = 0; i < SectionOrder.Length; i++)
            {
                var child = content.Find(SectionOrder[i]);
                Assert.That(child, Is.Not.Null, $"{SectionOrder[i]} should be parented under the scroll Content");
                Assert.That(child.GetSiblingIndex(), Is.EqualTo(i),
                    $"{SectionOrder[i]} should keep reading order (header then body)");
            }
        }

        [Test]
        public void BodiesRenderAtFullSizeNotBestFit()
        {
            foreach (var name in BodyTextNames)
            {
                var text = FindText(name);
                Assert.That(text, Is.Not.Null, name);
                Assert.That(text.resizeTextForBestFit, Is.False,
                    $"{name} should render full size; the scroll panel handles overflow now");
            }
        }

        [Test]
        public void RedundantBackingPanelDeactivated()
        {
            var panel = FindAny("Phase8_ResultScrollPanel");
            Assert.That(panel, Is.Not.Null, "Phase8_ResultScrollPanel should still exist, not be deleted");
            Assert.That(panel.gameObject.activeSelf, Is.False,
                "The flat backing panel is redundant once the scroll viewport carries the panel colour");
        }

        [Test]
        public void LongReadingMakesContentExceedViewportSoItScrolls()
        {
            var scroll = FindScroll();
            var content = scroll.content;
            var viewport = scroll.viewport;

            const string longCopy =
                "过去的牌显示你们之间曾有真实而深的联结，那是一段彼此都认真对待过的关系；但随着时间推移，一些未曾说出口的顾虑与自我保护，像细沙一样慢慢磨蚀了原本的信任。" +
                "现在的牌停在一种微妙的犹豫里：双方都在等对方先伸手，谁也不愿先暴露脆弱，于是僵局越拖越久，误会也越积越深。牌面提醒你，沉默并不等于答案，回避也不会让问题自己消失。";

            foreach (var name in BodyTextNames)
            {
                var text = FindText(name);
                Assert.That(text, Is.Not.Null, name);
                text.text = longCopy;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Assert.That(content.rect.height, Is.GreaterThan(viewport.rect.height),
                "Long interpretation must make the Content taller than the viewport so it scrolls");
        }

        [Test]
        public void Phase29DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase29 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("Result"));
            Assert.That(text, Does.Contain("scroll"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static ScrollRect FindScroll()
        {
            foreach (var s in Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s.name == "ResultReadingScroll")
                {
                    return s;
                }
            }

            return null;
        }

        private static Text FindText(string name)
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

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
