using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
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
    }
}
