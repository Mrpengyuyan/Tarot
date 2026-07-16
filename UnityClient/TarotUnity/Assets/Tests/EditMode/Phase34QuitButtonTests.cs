using System.Reflection;
using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 34 guards the main-menu quit affordance: a QuitButton exists, is centered and
    /// stacked below the start plate without overlapping it or the status line, carries a
    /// quit label, and is wired to MainMenuController (which has the QuitGame handler).
    /// </summary>
    public sealed class Phase34QuitButtonTests
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string DocPath = "Docs/PHASE34_QUIT_BUTTON.md";

        [Test]
        public void QuitButtonExistsCenteredBelowStartWithoutOverlap()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);

            var quit = canvas.transform.Find("QuitButton") as RectTransform;
            var start = canvas.transform.Find("StartReadingButton") as RectTransform;
            Assert.That(quit, Is.Not.Null, "QuitButton should exist under the menu canvas");
            Assert.That(start, Is.Not.Null);
            Assert.That(quit.GetComponent<Button>(), Is.Not.Null, "QuitButton needs a Button");

            // Horizontally centered like the primary button.
            Assert.That(quit.anchoredPosition.x, Is.EqualTo(start.anchoredPosition.x).Within(1.5f),
                "QuitButton should share the start button's horizontal centering");

            // Stacked below the start plate.
            Assert.That(quit.anchoredPosition.y, Is.LessThan(start.anchoredPosition.y),
                "QuitButton should sit below the start button");

            // No overlap with the start plate or the status line beneath it.
            var quitTop = quit.anchoredPosition.y + quit.sizeDelta.y * 0.5f;
            var startBottom = start.anchoredPosition.y - start.sizeDelta.y * 0.5f;
            Assert.That(quitTop, Is.LessThanOrEqualTo(startBottom),
                "QuitButton must not overlap the start button");

            var status = canvas.transform.Find("StatusText") as RectTransform;
            if (status != null)
            {
                var statusBottom = status.anchoredPosition.y - status.sizeDelta.y * 0.5f;
                Assert.That(quitTop, Is.LessThanOrEqualTo(statusBottom + 1f),
                    "QuitButton must not overlap the status line under the start plate");
            }

            var label = quit.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null, "QuitButton needs a label");
            Assert.That(label.text, Does.Contain("退出"), "QuitButton label should read as a quit action");
        }

        [Test]
        public void MainMenuControllerWiresQuitButtonAndHandler()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(controller, Is.Not.Null);

            var quitProp = new SerializedObject(controller).FindProperty("quitButton");
            Assert.That(quitProp, Is.Not.Null, "MainMenuController should have a quitButton field");
            Assert.That(quitProp.objectReferenceValue, Is.Not.Null, "quitButton must be assigned");

            var method = typeof(MainMenuController).GetMethod("QuitGame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "MainMenuController should have a QuitGame handler");
        }

        [Test]
        public void Phase34DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(DocPath), Is.True, $"missing {DocPath}");
            var text = System.IO.File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("quit"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}
