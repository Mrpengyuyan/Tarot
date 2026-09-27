using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 71: a dealt, face-down card glows softly under its edges until it is flipped (the
    /// deal's "flip me"), hovering widens the glow, and leaving the card no longer puts it out. The
    /// glow is the socket-glow material, not the opaque graybox slab that washed the card out.
    /// </summary>
    public sealed class Phase71CardHoverTests
    {
        [UnityTest]
        public IEnumerator DealtCardsGlowUntilFlippedAndHoverWidensTheGlow()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "ReadingRoom", "Expected ReadingRoom to load.");
            yield return null;

            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => flow.State == ReadingFlowState.WaitingForFlip, "Expected the deal to finish.");

            var card = deck.ActiveCards.First();
            var halo = FindDeep(card.transform, "Highlight");
            Assert.That(halo, Is.Not.Null);
            Assert.That(halo.gameObject.activeInHierarchy, Is.True, "a dealt card waiting to be flipped glows softly");
            Assert.That(halo.GetComponent<MeshRenderer>().sharedMaterial.name, Does.StartWith("MP_WarmGlow"));
            Assert.That(card.transform.InverseTransformPoint(halo.position).y, Is.LessThan(0f), "under the card");
            var restScale = halo.localScale;

            var pointer = new PointerEventData(EventSystem.current);
            var click = card.GetComponent<CardClickHandler>();
            click.OnPointerEnter(pointer);
            yield return null;
            Assert.That(halo.gameObject.activeInHierarchy, Is.True);
            Assert.That(halo.localScale.x, Is.GreaterThan(restScale.x), "hovering widens the halo");

            click.OnPointerExit(pointer);
            yield return null;
            Assert.That(halo.gameObject.activeInHierarchy, Is.True, "leaving the card must not put out its invitation");
            Assert.That(halo.localScale, Is.EqualTo(restScale));

            var flipController = UnityEngine.Object.FindFirstObjectByType<CardFlipController>();
            click.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            yield return WaitUntil(() => !flipController.IsFlipping, "Expected the flip to finish.");
            Assert.That(halo.gameObject.activeInHierarchy, Is.False, "a face-up card is done; no halo");

            click.OnPointerEnter(pointer);
            yield return null;
            Assert.That(halo.gameObject.activeInHierarchy, Is.False, "hovering a face-up card lights nothing");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
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
