using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 35 guards the visual-review fixes: the Result 3D stage leftovers stay
    /// hidden (but findable for the Phase 15-21 suites), the flat-era ResultReadingFrame
    /// stays out of the hero card slot, the main-menu status line clears the start
    /// plate, and the quit button stays a quiet secondary action.
    /// </summary>
    public sealed class Phase35UiPolishTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string DocPath = "Docs/PHASE35_RESULT_MENU_POLISH.md";

        private static readonly string[] StageVisualPaths =
        {
            "Phase15_ResultCardStageRoot/Phase15_ResultCardPedestal",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultGlowPool",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultRuneRing",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultParticleAnchorLeft",
            "Phase15_ResultCardStageRoot/Phase16_ResultAuraRoot/Phase16_ResultParticleAnchorRight",
        };

        [Test]
        public void ResultStageVisualsAreHiddenButObjectsStayActive()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            foreach (var path in StageVisualPaths)
            {
                var root = GameObject.Find(path.Split('/')[0]);
                Assert.That(root, Is.Not.Null, "stage root must stay active for GameObject.Find");
                var t = root.transform.Find(path.Substring(path.IndexOf('/') + 1));
                Assert.That(t, Is.Not.Null, $"missing {path}");
                Assert.That(t.gameObject.activeInHierarchy, Is.True,
                    $"{path} must stay active so Phase 15-21 Find-based tests and particle anchors keep working");
                var renderer = t.GetComponent<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null, $"{path} should carry its MeshRenderer");
                Assert.That(renderer.enabled, Is.False,
                    $"{path} renderer must stay disabled - it pokes out from behind the reading panel");
            }
        }

        [Test]
        public void ResultReadingFrameStaysOutOfTheHeroSlot()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);

            var frame = canvas.transform.Find("ResultReadingFrame");
            Assert.That(frame, Is.Not.Null, "ResultReadingFrame is deactivated, not deleted");
            Assert.That(frame.gameObject.activeSelf, Is.False,
                "ResultReadingFrame darkened the hero card slot with a visible seam and must stay off");

            var scroll = canvas.transform.Find("ResultReadingScroll");
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.gameObject.activeSelf, Is.True, "the reading scroll keeps its own backing");
        }

        [Test]
        public void MainMenuStatusLineClearsTheStartPlate()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);

            var start = canvas.transform.Find("StartReadingButton") as RectTransform;
            var status = canvas.transform.Find("StatusText") as RectTransform;
            Assert.That(start, Is.Not.Null);
            Assert.That(status, Is.Not.Null);

            var plateBottom = start.anchoredPosition.y - start.sizeDelta.y * 0.5f;
            Assert.That(status.anchoredPosition.y, Is.LessThanOrEqualTo(plateBottom - 8f),
                "the status line's center must sit clearly below the start plate instead of on it");
        }

        [Test]
        public void QuitButtonIsAQuietSecondaryAction()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            var start = canvas.transform.Find("StartReadingButton") as RectTransform;
            var quit = canvas.transform.Find("QuitButton") as RectTransform;
            Assert.That(start, Is.Not.Null);
            Assert.That(quit, Is.Not.Null);

            Assert.That(quit.sizeDelta.x, Is.LessThan(start.sizeDelta.x),
                "quit must be narrower than the primary action");
            Assert.That(quit.sizeDelta.y, Is.LessThan(start.sizeDelta.y),
                "quit must be shorter than the primary action");

            var quitTrim = quit.Find("Phase8_StartButtonGoldTrim");
            Assert.That(quitTrim, Is.Not.Null, "the inherited trim clone is deactivated, not deleted");
            Assert.That(quitTrim.gameObject.activeSelf, Is.False,
                "the cloned gold trim rendered a dark notch and full primary prominence");

            var startTrim = start.Find("Phase8_StartButtonGoldTrim");
            Assert.That(startTrim, Is.Not.Null);
            Assert.That(startTrim.gameObject.activeSelf, Is.True, "the primary button keeps its Phase 8 gold look");

            var startLabel = start.GetComponentInChildren<Text>(true);
            var quitLabel = quit.GetComponentInChildren<Text>(true);
            Assert.That(quitLabel.fontSize, Is.LessThan(startLabel.fontSize),
                "quit label should read quieter than the primary label");
        }

        [Test]
        public void Phase35DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(DocPath), Is.True, $"missing {DocPath}");
            var text = System.IO.File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("ResultReadingFrame"));
            Assert.That(text, Does.Contain("quiet secondary action"));
        }
    }
}
