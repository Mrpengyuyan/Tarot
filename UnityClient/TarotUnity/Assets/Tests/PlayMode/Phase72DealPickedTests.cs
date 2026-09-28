using System.Collections;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: a picked card is pulled, hovers glowing, flies trailing light, and lands.</summary>
    public sealed class Phase72DealPickedTests
    {
        private DeckController deck;
        private SpreadLayoutController layout;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            deck = Object.FindFirstObjectByType<DeckController>();
            layout = Object.FindFirstObjectByType<SpreadLayoutController>();
        }

        private static CardView SpawnFaceDown(Vector3 at)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var card = Object.Instantiate(prefab, at, Quaternion.identity);
            card.SetFaceUp(false);
            var tilt = card.GetComponent<CardHoverTiltController>();
            if (tilt != null)
            {
                tilt.Suspend();
            }

            return card;
#else
            Assert.Ignore("needs the editor to load the card prefab");
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator PickedCardHoversGlowingThenLandsOnItsSlot()
        {
            var slot = layout.GetSlots(3)[1];
            var card = SpawnFaceDown(new Vector3(0f, 0.13f, -2.4f));
            var halo = card.GetComponentsInChildren<Transform>(true).First(t => t.name == "Highlight");
            var trail = card.GetComponentInChildren<TrailRenderer>(true);
            var hovering = false;
            deck.CardHovering += _ => hovering = true;

            var haloRest = 0f;
            var maxHaloScale = 0f;
            var maxHeight = 0f;
            var trailSeen = false;
            deck.StartCoroutine(deck.DealPickedCard(card, slot));
            var started = Time.time;
            while (!deck.ActiveCards.Contains(card) && Time.time - started < 5f)
            {
                if (haloRest <= 0f && halo.gameObject.activeSelf)
                {
                    haloRest = halo.localScale.x;
                }

                maxHaloScale = Mathf.Max(maxHaloScale, halo.localScale.x);
                maxHeight = Mathf.Max(maxHeight, card.transform.position.y);
                trailSeen |= trail != null && trail.emitting;
                yield return null;
            }

            var seconds = Time.time - started;
            Assert.That(deck.ActiveCards, Does.Contain(card));
            Assert.That(hovering, Is.True, "the hover beat is announced");
            Assert.That(seconds, Is.InRange(0.9f, 1.6f), "pull + hover + flight + landing is about 1.1 s");
            Assert.That(maxHeight, Is.GreaterThan(0.4f), "the card rises and arcs");
            Assert.That(maxHaloScale, Is.GreaterThan(haloRest * 1.3f), "the glow swells while it hovers");
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(card.transform.position, slot.position), Is.LessThan(0.002f));
            Assert.That(halo.gameObject.activeSelf, Is.True, "a landed card waits to be flipped");
            Assert.That(card.GetComponent<CardHoverTiltController>().IsSuspended, Is.False, "the tilt comes back");

            Assume.That(trail, Is.Not.Null, "the flight trail arrives with the Phase 72 bootstrap");
            Assert.That(trailSeen, Is.True, "a light trail follows the flight");
            Assert.That(trail.emitting, Is.False);
        }

        [UnityTest]
        public IEnumerator BindGivesLandedCardsTheirDrawsInSlotOrder()
        {
            var slots = layout.GetSlots(3);
            var cards = Enumerable.Range(0, 3).Select(i => SpawnFaceDown(new Vector3(i, 0.13f, -2.4f))).ToArray();
            for (var i = 0; i < 3; i++)
            {
                yield return deck.DealPickedCard(cards[i], slots[i]);
            }

            var draws = LocalReadingSimulator.CreatePlaceholderDraws(3);
            deck.BindDealtCards(draws);
            for (var i = 0; i < 3; i++)
            {
                Assert.That(deck.ActiveCards[i], Is.SameAs(cards[i]));
                Assert.That(cards[i].DrawData, Is.SameAs(draws[i]));
                Assert.That(cards[i].IsFaceUp, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ReturnFliesTheCardsBackAndClears()
        {
            var slot = layout.GetSlots(1)[0];
            var card = SpawnFaceDown(new Vector3(0f, 0.13f, -2.4f));
            yield return deck.DealPickedCard(card, slot);
            yield return deck.ReturnDealtCards();
            yield return null;
            Assert.That(deck.ActiveCards.Count, Is.EqualTo(0));
            Assert.That(card == null, Is.True, "returned cards are destroyed");
        }
    }
}
