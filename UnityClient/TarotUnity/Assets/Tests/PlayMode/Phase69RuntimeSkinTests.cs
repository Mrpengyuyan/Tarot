using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 69: in play, TarotUiTheme.Awake must not tint the card stock, and the card stock
    /// follows the chosen spread even while the question is being written.
    /// </summary>
    public sealed class Phase69RuntimeSkinTests
    {
        private const string LongQuestion = "三牌离线验收：我应如何安排下一阶段的学习与生活，兼顾课程、项目开发和休息，并保持稳定的节奏？";

        [UnityTest]
        public IEnumerator LongQuestionStaysOnOneLineAfterThemeReapplication()
        {
            SceneManager.LoadScene("ReadingRoom");
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var canvas = GameObject.Find("ReadingRoomCanvas");
            var theme = canvas.GetComponent<TarotUiTheme>();
            var input = canvas.GetComponentInChildren<TMP_InputField>();
            Assert.That(input.lineType, Is.EqualTo(TMP_InputField.LineType.SingleLine));

            foreach (var width in new[] { 448f, 320f, 224f })
            {
                ((RectTransform)input.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                input.text = LongQuestion;
                theme.Apply();
                yield return null;
                input.ForceLabelUpdate();
                input.textComponent.ForceMeshUpdate();

                Assert.That(input.textComponent.textInfo.lineCount, Is.EqualTo(1),
                    $"A single-line question must scroll rather than wrap at width {width}.");
                Assert.That(input.text, Is.EqualTo(LongQuestion), "Display fitting must not truncate the question.");
            }
        }

        [UnityTest]
        public IEnumerator MultilineInputAndBodyTextKeepWrappingAfterThemeReapplication()
        {
            SceneManager.LoadScene("ReadingRoom");
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var canvas = GameObject.Find("ReadingRoomCanvas");
            var theme = canvas.GetComponent<TarotUiTheme>();
            var input = canvas.GetComponentInChildren<TMP_InputField>();
            var body = new GameObject("ThemeBodyWrapTest", typeof(RectTransform), typeof(TextMeshProUGUI));
            body.transform.SetParent(canvas.transform, false);
            var label = body.GetComponent<TMP_Text>();
            label.font = input.textComponent.font;
            label.fontSize = input.textComponent.fontSize;
            label.rectTransform.sizeDelta = new Vector2(224f, 100f);
            label.text = LongQuestion;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            foreach (var mode in new[] { TMP_InputField.LineType.MultiLineSubmit, TMP_InputField.LineType.MultiLineNewline })
            {
                input.lineType = mode;
                input.text = LongQuestion;
                theme.Apply();
                yield return null;
                input.ForceLabelUpdate();
                input.textComponent.ForceMeshUpdate();
                label.ForceMeshUpdate();

                Assert.That(input.textComponent.textInfo.lineCount, Is.GreaterThan(1), $"{mode} still wraps.");
                Assert.That(label.textInfo.lineCount, Is.GreaterThan(1), "Ordinary body text still wraps.");
                Assert.That(input.text, Is.EqualTo(LongQuestion));
            }

            Object.Destroy(body);
        }

        [UnityTest]
        public IEnumerator CardStockSurvivesTheThemeAndFollowsTheChoice()
        {
            SceneManager.LoadScene("ReadingRoom");
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            var one = canvas.Find("OneCardButton").GetComponent<Button>();
            var three = canvas.Find("ThreeCardButton").GetComponent<Button>();
            var oneSkin = one.GetComponent<UiSkinState>();
            var threeSkin = three.GetComponent<UiSkinState>();

            Assert.That(oneSkin.IsEmphasized, Is.True, "the room opens on one card");
            Assert.That(one.colors.normalColor, Is.EqualTo(Color.white), "the theme left the ColorBlock alone");
            Assert.That(((Image)one.targetGraphic).color, Is.EqualTo(Color.white));
            Assert.That(oneSkin.Label.color, Is.EqualTo(UiSkinState.CardStockLabel), "dark ink on card stock");
            Assert.That(threeSkin.Label.color, Is.EqualTo(UiSkinState.GlassLabel));

            three.onClick.Invoke();
            yield return null;
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.QuestionInput));
            Assert.That(threeSkin.IsEmphasized, Is.True);
            Assert.That(oneSkin.IsEmphasized, Is.False);

            one.onClick.Invoke();   // same state, another spread
            yield return null;
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.QuestionInput));
            Assert.That(oneSkin.IsEmphasized, Is.True);
            Assert.That(threeSkin.IsEmphasized, Is.False);

            var askChip = canvas.Find("Phase7_RitualHudRoot/Phase7_Progress_AskQuestion").GetComponent<UiSkinState>();
            Assert.That(askChip.IsEmphasized, Is.True, "the step bar is on 写问题");
        }

        // Final review: while the draw runs (and after it, when 揭示结果 is the next step) 洗牌抽取
        // cannot be pressed, so it must not keep the card stock that marks the next action.
        [UnityTest]
        public IEnumerator DrawButtonDropsToGlassWhileItCannotBePressed()
        {
            SceneManager.LoadScene("ReadingRoom");
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            var draw = canvas.Find("DrawButton").GetComponent<Button>();
            var drawSkin = draw.GetComponent<UiSkinState>();
            Assert.That(drawSkin.IsEmphasized, Is.True, "control: ready to draw");

            draw.onClick.Invoke();
            yield return null;

            Assert.That(draw.interactable, Is.False, "control: the draw is running");
            Assert.That(drawSkin.IsEmphasized, Is.False);
            Assert.That(canvas.Find("OneCardButton").GetComponent<UiSkinState>().IsEmphasized, Is.True,
                "the chosen spread keeps its card stock");
        }
    }
}
