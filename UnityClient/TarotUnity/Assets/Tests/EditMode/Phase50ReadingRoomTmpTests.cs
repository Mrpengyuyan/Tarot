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
    /// <summary>
    /// Phase 50 finishes the TMP migration the menu started (Phase 43) on the
    /// screen the player sits in. The contract: no legacy Text survives here, the
    /// question field is a real TMP_InputField (not a patched legacy one), and the
    /// controller points at the TMP components rather than the deleted ones.
    /// </summary>
    public sealed class Phase50ReadingRoomTmpTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CanvasName = "ReadingRoomCanvas";

        private static GameObject OpenCanvas()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find(CanvasName);
            Assert.That(canvas, Is.Not.Null, $"{CanvasName} missing");
            return canvas;
        }

        [Test]
        public void NoLegacyTextSurvivesInTheReadingRoom()
        {
            var canvas = OpenCanvas();
            var legacy = canvas.GetComponentsInChildren<Text>(true);
            Assert.That(legacy.Length, Is.EqualTo(0),
                "legacy UI.Text softens at the built resolution - every one must be TMP SDF now");
            Assert.That(canvas.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThan(0),
                "the room's text is TMP now");
        }

        [Test]
        public void TheQuestionFieldIsARealTmpInputField()
        {
            var canvas = OpenCanvas();

            Assert.That(canvas.GetComponentsInChildren<InputField>(true).Length, Is.EqualTo(0),
                "the legacy InputField must be gone, not patched");

            var input = canvas.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(input, Is.Not.Null, "QuestionInput must be a TMP_InputField");
            Assert.That(input.gameObject.name, Is.EqualTo("QuestionInput"));
            Assert.That(input.textComponent, Is.Not.Null, "TMP_InputField needs its TMP text view");
            Assert.That(input.placeholder, Is.InstanceOf<TMP_Text>(), "placeholder must be TMP");
            Assert.That(((TMP_Text)input.placeholder).text, Is.Not.Empty,
                "the empty field must still prompt the reader");
        }

        [Test]
        public void TheControllerPointsAtTheTmpComponents()
        {
            var canvas = OpenCanvas();
            var controller = canvas.GetComponentInChildren<ReadingRoomController>(true);
            Assert.That(controller, Is.Not.Null, "ReadingRoomController missing");

            var so = new SerializedObject(controller);
            Assert.That(so.FindProperty("questionInput").objectReferenceValue,
                Is.InstanceOf<TMP_InputField>(), "questionInput must re-point to the TMP field");

            foreach (var field in new[] { "spreadStatusText", "flowStatusText", "releaseStatusText" })
            {
                var value = so.FindProperty(field).objectReferenceValue;
                Assert.That(value, Is.InstanceOf<TMP_Text>(), $"{field} must re-point to a TMP_Text");
            }
        }

        [Test]
        public void TheThemeCarriesTheSdfFonts()
        {
            var canvas = OpenCanvas();
            var theme = canvas.GetComponent<TarotUiTheme>();
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.TmpBodyFont, Is.Not.Null, "the room theme needs the body SDF cut");
            Assert.That(theme.TmpDisplayFont, Is.Not.Null, "the room theme needs the display SDF cut");
        }

        [Test]
        public void Phase50DocumentationExists()
        {
            const string doc = "Docs/PHASE50_READINGROOM_TMP.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("TMP_InputField"));
        }
    }
}
