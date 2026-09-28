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
    /// <summary>Phase 73: a flipped card stays clean, and a whole round leaves no sparks behind.</summary>
    public sealed class Phase73DrawPolishPlayTests
    {
        private static readonly string[] LegacyFaceLayers =
        {
            "Front", "FrontFrame", "InnerGlow", "Phase7_TitleBand", "Top", "Bottom", "Left", "Right",
            "Phase12_FaceArtworkLabel", "Phase14_RevealGlow", "Phase15_CardFacePlane",
        };

        [UnityTest]
        public IEnumerator AFlippedCardShowsOnlyItsArtAndNoSparksAppear()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var maxParticles = 0;
            var picking = DrawRitualTestDriver.PickAll();
            while (picking.MoveNext())
            {
                maxParticles = Mathf.Max(maxParticles, AliveParticles());
                yield return picking.Current;
            }

            while (flow.State != ReadingFlowState.WaitingForFlip)
            {
                maxParticles = Mathf.Max(maxParticles, AliveParticles());
                yield return null;
            }

            var card = deck.ActiveCards.First();
            card.GetComponent<CardClickHandler>().OnPointerClick(
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            var flipper = card.GetComponent<CardFlipController>();
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((!card.IsFaceUp || flipper.IsFlipping) && Time.realtimeSinceStartup < deadline)
            {
                maxParticles = Mathf.Max(maxParticles, AliveParticles());
                yield return null;
            }

            for (var i = 0; i < 30; i++)
            {
                maxParticles = Mathf.Max(maxParticles, AliveParticles());
                yield return null;
            }

            Assert.That(card.IsFaceUp, Is.True, "control: the card flipped");
            var renderers = card.GetComponentsInChildren<Renderer>(true);
            foreach (var name in LegacyFaceLayers)
            {
                Assert.That(renderers.First(r => r.name == name).enabled && renderers.First(r => r.name == name).gameObject.activeInHierarchy,
                    Is.False, $"{name} stays dark on a face-up card");
            }

            Assert.That(renderers.First(r => r.name == "Phase12_FaceArtworkPlaceholder").enabled, Is.True, "the art shows");
            Assert.That(maxParticles, Is.EqualTo(0), "no sparks through shuffle, pick, deal and flip");
        }

        private static int AliveParticles()
        {
            return Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                .Where(ps => ps.GetComponent<ParticleSystemRenderer>().enabled)
                .Sum(ps => ps.particleCount);
        }
    }
}
