using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 64: the Result screen gained a parlor backdrop (so a reading no longer
    /// floats on pure black) and the menu's 离席 exit link became legible. These
    /// guard that the backdrop sits behind the reading without blocking clicks, and
    /// that the quit link is not near-invisible again.
    /// </summary>
    public sealed class Phase64ResultBackdropTests
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BackdropName = "MP_ResultBackdrop";
        private const string DocPath = "Docs/PHASE64_RESULT_BACKDROP.md";

        [Test]
        public void BackdropSitsBehindTheReadingAndIgnoresClicks()
        {
            EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);

            var first = canvas.transform.GetChild(0);
            Assert.That(first.name, Is.EqualTo(BackdropName), "the backdrop must be the first (backmost) canvas child");

            var image = first.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, "backdrop needs an Image");
            Assert.That(image.sprite, Is.Not.Null, "backdrop Image has no sprite");
            Assert.That(image.raycastTarget, Is.False, "backdrop must not intercept clicks on the reading");
        }

        [Test]
        public void BackdropFillsTheScreen()
        {
            EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var rt = GameObject.Find(BackdropName).GetComponent<RectTransform>();
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rt.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.offsetMax, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void QuitLinkIsLegible()
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var label = GameObject.Find("QuitButton")?.transform.Find("Label");
            Assert.That(label, Is.Not.Null, "QuitButton/Label missing");

            var graphic = label.GetComponent<Graphic>();
            Assert.That(graphic, Is.Not.Null);
            Assert.That(graphic.color.a, Is.GreaterThan(0.6f), "the exit link must be visible, not near-transparent");
            var c = graphic.color;
            Assert.That(c.r + c.g + c.b, Is.GreaterThan(1.2f), "the exit link ink should read as a light ivory, not a dim grey");
        }

        [Test]
        public void Phase64DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase 64 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("backdrop"));
            Assert.That(text, Does.Contain("Result"));
        }
    }
}
