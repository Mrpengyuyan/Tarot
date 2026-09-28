using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 70: 揭示结果 shares 洗牌抽取's slot, so when the reading is ready the reveal button
    /// appears and the draw button (which cannot be pressed again this round) goes away.
    /// </summary>
    public sealed class Phase70RevealReplacesDrawTests
    {
        [UnityTest]
        public IEnumerator RevealTakesTheDrawButtonsPlaceWhenTheReadingIsReady()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ReadingRoom", "Expected ReadingRoom to load.");
            yield return null;

            var room = UnityEngine.Object.FindFirstObjectByType<TarotUnity.UI.ReadingRoomController>();
            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            var draw = GetField<Button>(room, "drawButton");
            var reveal = GetField<Button>(room, "revealResultButton");

            Assert.That(draw.gameObject.activeSelf, Is.True, "control: 洗牌抽取 shows before the draw");
            Assert.That(reveal.gameObject.activeSelf, Is.False);

            draw.onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll();   // Phase 72: the player picks from the fan
            yield return WaitUntil(() => flow.State == ReadingFlowState.WaitingForFlip, "Expected the deal to finish.");
            Assert.That(draw.gameObject.activeSelf, Is.True, "洗牌抽取 stays (as glass) until the reading is ready");

            var flipController = UnityEngine.Object.FindFirstObjectByType<CardFlipController>();
            foreach (var card in deck.ActiveCards.ToArray())
            {
                card.GetComponent<CardClickHandler>().OnPointerClick(
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                yield return WaitUntil(() => !flipController.IsFlipping, "Expected the flip to finish.");
            }

            yield return WaitUntil(() => flow.State == ReadingFlowState.ResultReady, $"Expected ResultReady, but was {flow.State}.");
            yield return WaitUntil(() => reveal.gameObject.activeSelf, "Expected 揭示结果 to appear.");

            Assert.That(draw.gameObject.activeSelf, Is.False, "揭示结果 takes 洗牌抽取's place");
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(target) as T;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string message)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(message);
                }

                yield return null;
            }
        }
    }
}
