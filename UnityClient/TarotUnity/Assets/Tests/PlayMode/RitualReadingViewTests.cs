using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualReadingViewTests
    {
        private Transform canvas;
        private Component view;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return null;
            canvas = GameObject.Find("ReadingRoomCanvas").transform;
            var type = typeof(ReadingRoomController).Assembly.GetType("TarotUnity.UI.RitualReadingView");
            Assert.That(type, Is.Not.Null, "the approved stage-based table view must be installed");
            view = Object.FindFirstObjectByType(type) as Component;
            Assert.That(view, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SpreadChoiceDoesNotExposeTheQuestionOrActionDock()
        {
            Assert.That(Stage, Is.EqualTo("Spread"));
            Assert.That(Visible("ThreeCardButton"), Is.True);
            Assert.That(Visible("QuestionInput"), Is.False);
            Assert.That(Visible("DrawButton"), Is.False);
            Assert.That(Visible("Phase11_ActionDock"), Is.False);
            Assert.That(Visible("SpreadStatusText"), Is.False);
            Assert.That(Visible("FlowStatusText"), Is.False);
            var subtitle = canvas.Find("ThreeCardButton").GetComponentsInChildren<TMP_Text>(true)
                .First(t => t.text.Contains("过去"));
            Assert.That(subtitle.text, Is.EqualTo("过去·现在·未来"));
            Assert.That(subtitle.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpreadSymbolsReuseTheActualDeckArtwork()
        {
            var symbol = canvas.Find("OneCardButton/RitualSpreadSymbol");
            var back = symbol.GetComponentInChildren<RawImage>();
            Assert.That(back, Is.Not.Null, "use the real deck back instead of an unsupported decorative glyph");
            Assert.That(back.texture, Is.Not.Null);
            Assert.That(back.texture.name, Is.EqualTo("TarotCardBack"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChoosingASpreadShowsOnlyTheQuestionAndBlankStartIsDisabled()
        {
            canvas.Find("ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(Stage, Is.EqualTo("Question"));
            Assert.That(Visible("ThreeCardButton"), Is.False);
            Assert.That(Visible("QuestionInput"), Is.True);
            Assert.That(Visible("RitualChangeSpread"), Is.True);
            var input = canvas.Find("QuestionInput").GetComponent<TMP_InputField>();
            var start = canvas.Find("DrawButton").GetComponent<Button>();
            foreach (var value in new[] { "", "   ", "\u3000\u00a0" })
            {
                input.text = value;
                yield return null;
                Assert.That(start.interactable, Is.False);
            }
            input.text = "What should I focus on?";
            yield return null;
            Assert.That(start.interactable, Is.True);
            Assert.That(input.lineType, Is.EqualTo(TMP_InputField.LineType.SingleLine));
        }

        [UnityTest]
        public IEnumerator ChangeSpreadReturnsToChoicesWithoutLosingTheQuestion()
        {
            canvas.Find("ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "My question?";
            canvas.Find("RitualChangeSpread").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(Stage, Is.EqualTo("Spread"));
            Assert.That(Visible("OneCardButton"), Is.True);
            Assert.That(canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text, Is.EqualTo("My question?"));
        }

        [UnityTest]
        public IEnumerator ShuffleHasNoFloatingCaptionsOrOldSelectionControls()
        {
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "What comes next?";
            yield return null;
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(Stage, Is.EqualTo("Shuffle"));
            foreach (var name in new[] { "QuestionInput", "OneCardButton", "DrawButton", "FlowStatusText", "SpreadStatusText", "Phase11_ActionDock" })
            {
                Assert.That(Visible(name), Is.False, name);
            }
        }

        [UnityTest]
        public IEnumerator ThemeReapplicationKeepsNavigationBorderless()
        {
            canvas.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            foreach (var name in new[] { "RitualReturnMenu", "RitualChangeSpread" })
            {
                var button = canvas.Find(name).GetComponent<Button>();
                Assert.That(button.targetGraphic.color.a, Is.LessThan(0.001f), name);
            }
        }

        [UnityTest]
        public IEnumerator InterruptedDrawFeedbackRemainsVisible()
        {
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("FlowStatusText").GetComponent<TMP_Text>().text = ReleaseUxCopy.FlowDrawInterrupted;
            yield return null;
            Assert.That(Visible("RitualQuestionFeedback"), Is.True);
            Assert.That(canvas.Find("RitualQuestionFeedback").GetComponent<TMP_Text>().text,
                Is.EqualTo(ReleaseUxCopy.FlowDrawInterrupted));
        }

        [UnityTest]
        public IEnumerator RequiredQuestionFeedbackClearsAfterValidInput()
        {
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "";
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(Visible("RitualQuestionFeedback"), Is.True);
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "What comes next?";
            yield return null;
            Assert.That(Visible("RitualQuestionFeedback"), Is.False);
        }

        [UnityTest]
        public IEnumerator InspectionLabelsUseTheSameReadableFace()
        {
            var overlay = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(c => c.name == "CardInspectionOverlay");
            foreach (var text in overlay.GetComponentsInChildren<TMP_Text>(true))
                Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(text.font.sourceFontFile));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultKeepsItsScrollViewAndUsesTheNewReadableFace()
        {
            SceneManager.LoadScene("Result");
            yield return null;
            yield return null;
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.GetComponentInParent<Canvas>().GetComponentInChildren<ScrollRect>(true), Is.Not.Null);
            var text = presenter.GetComponentInParent<Canvas>().GetComponentsInChildren<TMP_Text>(true)
                .First(t => t.text.Length > 0);
            Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(text.font.sourceFontFile));
        }

        private string Stage => view.GetType().GetProperty("Stage").GetValue(view).ToString();

        private bool Visible(string name)
        {
            var node = canvas.Find(name);
            Assert.That(node, Is.Not.Null, name);
            return node.gameObject.activeInHierarchy && node.GetComponentsInParent<CanvasGroup>(true).All(g => g.alpha > 0.001f);
        }
    }
}
