using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the draw ritual end to end.</summary>
    public sealed class Phase72DrawFlowTests
    {
        private ReadingRoomController room;
        private ReadingFlowController flow;
        private DeckController deck;
        private SpreadFanController fan;

        private IEnumerator LoadRoom()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            room = Object.FindFirstObjectByType<ReadingRoomController>();
            flow = Object.FindFirstObjectByType<ReadingFlowController>();
            deck = Object.FindFirstObjectByType<DeckController>();
            fan = Object.FindFirstObjectByType<SpreadFanController>();
        }

        [UnityTest]
        public IEnumerator ThreeCardRitualPicksLandsBindsAndFlips()
        {
            yield return LoadRoom();
            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();

            yield return Until(() => fan.AcceptingPicks, "the fan opens after the shuffle");
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.Drawing));
            Assert.That(fan.FanCards.Count, Is.EqualTo(78), "Phase 73: the whole deck");
            Assert.That(Get<CanvasGroup[]>("pickHiddenUi").All(g => g.alpha < 0.01f && !g.blocksRaycasts), Is.True,
                "the dock steps aside while picking");
            Assert.That(Get<TMP_Text>("flowStatusText").text, Is.EqualTo(ReleaseUxCopy.FlowPickPrompt(3, 3)));

            var picked = new[] { fan.FanCards[2], fan.FanCards[10], fan.FanCards[19] };
            foreach (var card in picked)
            {
                fan.RequestPick(card);
                yield return Until(() => !fan.FanCards.Contains(card) && deck.ActiveCards.Contains(card), "the card lands");
            }

            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the flip phase starts");
            Assert.That(deck.ActiveCards, Is.EqualTo(picked), "the cards picked are the cards on the table");
            Assert.That(picked.All(c => c.DrawData != null), Is.True, "bound after the picks");
            Assert.That(fan.FanCards.Count, Is.EqualTo(0), "the fan is gathered");
            Assert.That(Get<CanvasGroup[]>("pickHiddenUi").All(g => g.alpha > 0.99f), Is.True, "the dock returns");

            var flipper = Object.FindFirstObjectByType<CardFlipController>();
            foreach (var card in picked)
            {
                card.GetComponent<CardClickHandler>().OnPointerClick(
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                yield return Until(() => card.IsFaceUp && (flipper == null || !flipper.IsFlipping), "the card flips");
            }

            yield return Until(() => flow.State == ReadingFlowState.ResultReady, "the reading is ready");
        }

        [UnityTest]
        public IEnumerator CardsOnTheTableDoNotFlipBeforeTheyAreBound()
        {
            yield return LoadRoom();
            Get<Button>("threeCardButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return Until(() => fan.AcceptingPicks, "the fan opens");

            var first = fan.FanCards[5];
            fan.RequestPick(first);
            yield return Until(() => deck.ActiveCards.Contains(first), "the first card lands");
            first.GetComponent<CardClickHandler>().OnPointerClick(
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            yield return new WaitForSeconds(0.8f);
            Assert.That(first.IsFaceUp, Is.False, "no flipping mid-pick");
        }

        [UnityTest]
        public IEnumerator CelticCrossPicksAllTenCards()
        {
            yield return LoadRoom();
            Get<Button>("celticCrossButton").onClick.Invoke();
            Get<Button>("drawButton").onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll(60f);
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the flip phase starts", 60f);
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(10));
            Assert.That(deck.ActiveCards.All(c => c.DrawData != null), Is.True);
        }

        private T Get<T>(string name) where T : class
        {
            var field = typeof(ReadingRoomController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(room) as T;
        }

        private static IEnumerator Until(System.Func<bool> condition, string message, float seconds = 20f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }
    }
}
