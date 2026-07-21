using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase26ResultRobustnessTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

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
        public void ReadingBodiesStayContainedAtFullSize()
        {
            // Phase 26 contained long copy by shrinking it (best-fit). Phase 29 supersedes
            // that mechanism with a scroll panel: bodies render at full size and any
            // overflow is clipped by the viewport RectMask2D instead of bleeding into the
            // next section. The Phase 26 guarantee (no overflow) still holds, the better way.
            foreach (var name in BodyTextNames)
            {
                var text = FindText(name);
                Assert.That(text, Is.Not.Null, $"{name} should exist in the Result scene");
                // Phase 51: TMP's autosizing is the equivalent of legacy best-fit; it
                // must stay off so bodies render full size inside the scroll panel.
                Assert.That(text.enableAutoSizing, Is.False,
                    $"{name} no longer best-fits; Phase 29 renders it full size inside the scroll panel");
                Assert.That(text.GetComponentInParent<RectMask2D>(), Is.Not.Null,
                    $"{name} must live under the scroll viewport mask so long copy is clipped, not overflowed");
            }
        }

        [Test]
        public void OverallTextKeepsSizeInvariant()
        {
            var overall = FindText("OverallText");
            Assert.That(overall, Is.Not.Null);
            Assert.That(overall.fontSize, Is.GreaterThanOrEqualTo(19f),
                "OverallText must keep the size floor earlier phases assert");
        }

        [Test]
        public void Phase26DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase26 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("Result"));
            Assert.That(text, Does.Contain("best-fit"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static TMP_Text FindText(string name)
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
