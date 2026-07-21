using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 40 guards the MainMenu/Result recomposition: both screens carry the
    /// Midnight Parlor stage, the menu shows a real tabletop vignette instead of
    /// the deleted disc/slab, and the result frames its hero and reading in gold.
    /// </summary>
    public sealed class Phase40MenuResultTests
    {
        [Test]
        public void MenuHasParlorStageAndTabletopVignette()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            var stage = GameObject.Find("MP_MenuStage");
            Assert.That(stage, Is.Not.Null);
            foreach (var child in new[]
            {
                "MP_MenuCloth", "MP_MenuRimFar", "MP_ParlorBackdrop",
                "MP_MenuDeck", "MP_ScatterCard_A", "MP_ScatterCard_B",
                "MP_CandleGlow_L", "MP_CandleGlow_R",
            })
            {
                Assert.That(stage.transform.Find(child), Is.Not.Null, child);
            }

            Assert.That(stage.transform.Find("MP_MenuDeck").childCount, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void LegacyMenuObjectsStayDeleted()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Assert.That(GameObject.Find("Phase7_ImmersiveMenuRoot"), Is.Null);
            Assert.That(GameObject.Find("Phase8_MenuTableSurface"), Is.Null);
        }

        [Test]
        public void MenuButtonsWearTheKit()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var root = GameObject.Find("MainMenuCanvas").transform;

            var start = root.Find("StartReadingButton").GetComponent<Image>();
            Assert.That(start.sprite?.name, Is.EqualTo("TarotButton"));
            Assert.That(start.type, Is.EqualTo(Image.Type.Sliced));

            // Phase 44: quit became a bare corner link - a plaque there still read
            // as a button competing with the invitation. The graphic stays (Button
            // needs a raycast target) but is fully transparent.
            var quit = root.Find("QuitButton").GetComponent<Image>();
            Assert.That(quit.color.a, Is.EqualTo(0f).Within(0.01f),
                "quit is a link, not a plaque");

            foreach (var wash in new[] { "Phase11_MenuDepthFrame", "Phase11_TableDepthShadow", "Phase11_ActionRail" })
            {
                var overlay = root.Find(wash);
                Assert.That(overlay, Is.Not.Null, wash);
                Assert.That(overlay.gameObject.activeSelf, Is.False, $"{wash} stays deactivated");
            }
        }

        [Test]
        public void ResultHasParlorStageAndGoldChrome()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var stage = GameObject.Find("MP_ResultStage");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.transform.Find("MP_ResultCloth"), Is.Not.Null);
            Assert.That(stage.transform.Find("MP_ParlorBackdrop"), Is.Not.Null);
            Assert.That(stage.transform.Find("MP_ResultRimFar"), Is.Null,
                "the rim beam read as a gray stripe behind the reading UI");

            var root = GameObject.Find("ResultCanvas").transform;
            Assert.That(root.Find("ResultReadingScroll").GetComponent<Image>().sprite?.name,
                Is.EqualTo("TarotPanel"));
            Assert.That(root.Find("Phase12_ResultCardShowcase").GetComponent<Image>().sprite?.name,
                Is.EqualTo("TarotPanel"));
            Assert.That(root.Find("BackToMenuButton").GetComponent<Image>().sprite?.name,
                Is.EqualTo("TarotButton"));
            Assert.That(root.Find("Phase14_ResultCardHalo").GetComponent<Image>().sprite?.name,
                Is.EqualTo("TarotGlow"), "the flat amber rectangle became a radial glow");
            Assert.That(root.Find("Phase8_ResultGoldDividerTop").GetComponent<Image>().sprite?.name,
                Is.EqualTo("TarotDivider"));
        }

        [Test]
        public void Phase40DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            var text = File.ReadAllText(doc);
            Assert.That(text, Does.Contain("MP_MenuStage"));
            Assert.That(text, Does.Contain("deleted"));
        }
    }
}
