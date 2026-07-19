using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 54 drives a real single-card deal in the ReadingRoom and watches the
    /// card's scale each frame. It proves the landing is there (the card squashes on
    /// contact) and - the correctness bar - that it recovers to its exact rest scale
    /// and comes to rest on the slot, so the flip that follows starts from a clean
    /// pose.
    /// </summary>
    public sealed class Phase54DealLandingTests
    {
        [UnityTest]
        public IEnumerator DealtCardSquashesOnLandingThenRecoversOnTheSlot()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            var deck = Object.FindFirstObjectByType<DeckController>();
            Assert.That(deck, Is.Not.Null, "ReadingRoom should have a DeckController");

            var slot = new GameObject("Phase54_TestSlot").transform;
            slot.position = deck.transform.position + new Vector3(1.4f, 0f, 0.15f);
            slot.rotation = deck.transform.rotation;

            var draws = new List<CardDrawData> { new CardDrawData() };
            var slots = new List<Transform> { slot };

            CardView dealt = null;
            var minScaleY = float.MaxValue;

            // Run the deal exactly the way production does - as a started coroutine,
            // so its nested MoveCardToSlot/LandingSettle enumerators execute - and
            // sample the card's scale every rendered frame alongside it. Driving the
            // outer enumerator by hand would only observe its outer yield points,
            // and the squash lives (and fully recovers) inside a nested routine.
            var done = false;
            deck.StartCoroutine(RunThenFlag(deck.DealCards(draws, slots), () => done = true));

            var safetySeconds = 0f;
            while (!done && safetySeconds < 30f)
            {
                if (deck.ActiveCards.Count > 0 && deck.ActiveCards[0] != null)
                {
                    dealt = deck.ActiveCards[0];
                    minScaleY = Mathf.Min(minScaleY, dealt.transform.localScale.y);
                }

                safetySeconds += Time.deltaTime;
                yield return null;
            }

            Assert.That(done, Is.True, "the deal should finish within the safety window");
            Assert.That(dealt, Is.Not.Null, "a card should have been dealt");

            var restScaleY = dealt.transform.localScale.y;
            Assert.That(minScaleY, Is.LessThan(restScaleY * 0.97f),
                "the card should visibly squash as it lands");
            Assert.That(dealt.transform.localScale.y, Is.EqualTo(restScaleY).Within(0.0005f),
                "the squash must recover to the exact rest scale");
            Assert.That(Vector3.Distance(dealt.transform.position, slot.position), Is.LessThan(0.02f),
                "the card must come to rest on its slot");

            Object.Destroy(slot.gameObject);
            deck.Clear();
        }

        private static IEnumerator RunThenFlag(IEnumerator inner, System.Action onDone)
        {
            yield return inner;
            onDone();
        }
    }
}
