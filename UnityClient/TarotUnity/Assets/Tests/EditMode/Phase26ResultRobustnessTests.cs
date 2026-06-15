using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase26ResultRobustnessTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DocPath = "Docs/PHASE26_RESULT_ROBUSTNESS.md";

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
        public void ReadingBodiesUseBestFitWithinSafeBounds()
        {
            foreach (var name in BodyTextNames)
            {
                var text = FindText(name);
                Assert.That(text, Is.Not.Null, $"{name} should exist in the Result scene");
                Assert.That(text.resizeTextForBestFit, Is.True,
                    $"{name} should best-fit so long interpretation never overflows its box");
                Assert.That(text.resizeTextMaxSize, Is.GreaterThanOrEqualTo(19),
                    $"{name} should still render short copy at full size");
                Assert.That(text.resizeTextMinSize, Is.InRange(10, 15),
                    $"{name} should keep a readable floor when copy is long");
                Assert.That(text.resizeTextMinSize, Is.LessThan(text.resizeTextMaxSize),
                    $"{name} best-fit bounds must be a valid range");
            }
        }

        [Test]
        public void OverallTextKeepsSizeInvariant()
        {
            var overall = FindText("OverallText");
            Assert.That(overall, Is.Not.Null);
            Assert.That(overall.fontSize, Is.GreaterThanOrEqualTo(19),
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
    }
}
