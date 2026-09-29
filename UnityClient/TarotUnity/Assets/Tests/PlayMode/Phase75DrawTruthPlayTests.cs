using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 75: the fan is the whole deck, so the deck on the table is gone while the player
    /// picks and back once the fan has gathered; and an offline round deals real cards from it.
    /// </summary>
    public sealed class Phase75DrawTruthPlayTests
    {
        [UnityTest]
        public IEnumerator TheDeckEmptiesIntoTheFanAndFillsAgainAfter()
        {
            yield return LoadRoom();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckShuffleChoreographer>().transform;
            var shown = deck.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToList();
            Assert.That(shown.Count, Is.GreaterThan(8), "control: the deck is on the table");

            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            while (!fan.AcceptingPicks)
            {
                yield return null;
            }

            Assert.That(shown.Count(r => r.enabled), Is.EqualTo(0), "all 78 cards are in the fan - no deck left on the table");
            yield return DrawRitualTestDriver.PickAll();
            while (flow.State != ReadingFlowState.WaitingForFlip)
            {
                yield return null;
            }

            Assert.That(shown.All(r => r.enabled), Is.True, "the rest of the fan gathered back into the deck");
        }

        [UnityTest]
        public IEnumerator AnOfflineRoundDealsRealCardsFromTheDeck()
        {
            yield return LoadRoom();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll();
            while (flow.State != ReadingFlowState.WaitingForFlip)
            {
                yield return null;
            }

            var session = ReadingSessionStore.Current;
            Assert.That(session, Is.Not.Null);
            var byId = TarotDeck.Cards.ToDictionary(c => c.id);
            var ids = new List<int>();
            foreach (var draw in session.cardDraws)
            {
                Assert.That(byId.ContainsKey(draw.tarot_card_id), Is.True);
                var card = byId[draw.tarot_card_id];
                Assert.That(draw.card_meaning.meaning, Is.EqualTo(draw.is_reversed ? card.reversedMeaning : card.uprightMeaning),
                    "a real card's meaning, not the fixed placeholder");
                ids.Add(draw.tarot_card_id);
            }

            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count), "no card twice");
            for (var i = 0; i < deck.ActiveCards.Count; i++)
            {
                Assert.That(deck.ActiveCards[i].DrawData, Is.SameAs(session.cardDraws[i]), "the cards on the table are those cards");
            }
        }

        private static IEnumerator LoadRoom()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
        }
    }
}
