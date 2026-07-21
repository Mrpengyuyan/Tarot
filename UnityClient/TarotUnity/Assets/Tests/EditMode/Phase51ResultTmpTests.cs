using System.IO;
using NUnit.Framework;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 51 is the last screen to leave legacy UI.Text. The contract: no
    /// legacy Text survives the Result canvas, the presenter's seven readouts point
    /// at TMP components, and the scene now shares the Flat ambient contract the
    /// other two screens carry (correctness/consistency - it is Overlay UI over a
    /// solid-colour clear, so the change is inert on screen but the data is right).
    /// </summary>
    public sealed class Phase51ResultTmpTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const string CanvasName = "ResultCanvas";

        private static readonly string[] PresenterFields =
        {
            "questionText", "spreadNameText", "summaryText", "overallText",
            "cardAnalysisText", "adviceText", "warningText",
        };

        private static GameObject OpenCanvas()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find(CanvasName);
            Assert.That(canvas, Is.Not.Null, $"{CanvasName} missing");
            return canvas;
        }

        [Test]
        public void NoLegacyTextSurvivesInTheResult()
        {
            var canvas = OpenCanvas();
            Assert.That(canvas.GetComponentsInChildren<Text>(true).Length, Is.EqualTo(0),
                "the Result screen was the last legacy UI.Text holdout");
            Assert.That(canvas.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThan(0),
                "its text is TMP SDF now");
        }

        [Test]
        public void ThePresenterPointsAtTheTmpComponents()
        {
            var canvas = OpenCanvas();
            var presenter = canvas.GetComponentInChildren<ResultPanelPresenter>(true);
            Assert.That(presenter, Is.Not.Null, "ResultPanelPresenter missing");

            var so = new SerializedObject(presenter);
            foreach (var field in PresenterFields)
            {
                var value = so.FindProperty(field).objectReferenceValue;
                Assert.That(value, Is.InstanceOf<TMP_Text>(), $"{field} must re-point to a TMP_Text");
            }
        }

        [Test]
        public void TheResultSharesTheFlatAmbientContract()
        {
            OpenCanvas();
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat),
                "a daylight skybox ambient has no place in a midnight product's data");
            Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.2f),
                "ambient is a shadow floor, not a source");
            Assert.That(RenderSettings.reflectionIntensity, Is.LessThan(0.2f));
        }

        [Test]
        public void TheThemeCarriesTheSdfFonts()
        {
            var canvas = OpenCanvas();
            var theme = canvas.GetComponent<TarotUiTheme>();
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.TmpBodyFont, Is.Not.Null);
            Assert.That(theme.TmpDisplayFont, Is.Not.Null);
        }

        [Test]
        public void Phase51DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("Overlay"));
        }
    }
}
