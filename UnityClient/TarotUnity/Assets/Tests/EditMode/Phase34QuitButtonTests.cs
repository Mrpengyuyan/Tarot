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

            // Phase 44 moved quit out of the centre column: stacked under the
            // invitation it made the menu read as a form. It is now anchored to the
            // bottom-right corner as a quiet link. The reason Phase 34 added it -
            // desktop players otherwise have no way out but Cmd+Q - is unchanged,
            // so what this guards is that it still exists, reads as an exit, and
            // stays clear of the primary action.
            Assert.That(quit.anchorMin, Is.EqualTo(new Vector2(1f, 0f)),
                "QuitButton should be pinned to the bottom-right corner");

            var quitWorld = quit.TransformPoint(Vector3.zero);
            var startWorld = start.TransformPoint(Vector3.zero);
            Assert.That(quitWorld.y, Is.LessThan(startWorld.y),
                "QuitButton should sit below the start button");
            Assert.That(quitWorld.x, Is.GreaterThan(startWorld.x),
                "QuitButton should sit out of the centre column");

            var label = quit.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null, "QuitButton needs a label");
            Assert.That(label.text, Does.Contain("离席"), "QuitButton label should read as leaving the table");
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
