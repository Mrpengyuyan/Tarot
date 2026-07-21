using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 39 guards the ReadingRoom UI reskin: the chrome wears the Phase 37
    /// nine-slice plaques, buttons carry a clean white ColorBlock (so the sprite
    /// tint survives runtime transitions), and the redundant flat-era frames stay
    /// deactivated.
    /// </summary>
    public sealed class Phase39UiReskinTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";

        private static Transform OpenCanvas()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);
            return canvas.transform;
        }

        [Test]
        public void ChromeWearsTheNineSlicePlaques()
        {
            var root = OpenCanvas();
            foreach (var (path, sprite) in new[]
            {
                ("Phase7_RitualHudRoot/Phase7_HudPlate", "TarotPanelSubtle"),
                ("Phase11_ActionDock", "TarotPanel"),
                ("QuestionInput", "TarotPanelSubtle"),
            })
            {
                var image = root.Find(path)?.GetComponent<Image>();
                Assert.That(image, Is.Not.Null, path);
                Assert.That(image.sprite, Is.Not.Null, path);
                Assert.That(image.sprite.name, Is.EqualTo(sprite), path);
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), path);
            }
        }

        [Test]
        public void ButtonsAreGoldPlaquesWithCleanColorBlocks()
        {
            var root = OpenCanvas();
            foreach (var name in new[] { "OneCardButton", "ThreeCardButton", "DrawButton", "RevealResultButton" })
            {
                var button = root.Find(name)?.GetComponent<Button>();
                Assert.That(button, Is.Not.Null, name);

                var image = button.GetComponent<Image>();
                Assert.That(image.sprite?.name, Is.EqualTo("TarotButton"), name);
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), name);
                Assert.That(button.colors.normalColor, Is.EqualTo(Color.white), name);
            }
        }

        [Test]
        public void ProgressPlatesShareTheSubtlePanel()
        {
            var root = OpenCanvas();
            foreach (var step in new[]
            {
                "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
                "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
            })
            {
                var image = root.Find($"Phase7_RitualHudRoot/{step}/Plate")?.GetComponent<Image>();
                Assert.That(image, Is.Not.Null, step);
                Assert.That(image.sprite?.name, Is.EqualTo("TarotPanelSubtle"), step);
            }
        }

        [Test]
        public void FlatEraFramesStayDeactivated()
        {
            var root = OpenCanvas();
            foreach (var name in new[]
            {
                "Phase8_SpreadChoiceFrame", "Phase8_QuestionPanelFrame", "Phase11_QuestionGlow",
            })
            {
                var chrome = root.Find(name);
                Assert.That(chrome, Is.Not.Null, $"{name} must stay in the scene (deactivated, not deleted)");
                Assert.That(chrome.gameObject.activeSelf, Is.False, name);
            }
        }

        [Test]
        public void Phase39DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("nine-slice"));
        }
    }
}
