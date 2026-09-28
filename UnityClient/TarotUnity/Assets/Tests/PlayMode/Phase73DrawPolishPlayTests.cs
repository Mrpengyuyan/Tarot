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

        // Phase 73 review: a hovered card grows 1.06x; picking it used to snap it back in one frame.
        [UnityTest]
        public IEnumerator APickedCardEasesBackToItsSize()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            while (!fan.AcceptingPicks)
            {
                yield return null;
            }

            var card = fan.FanCards[30];
            var body = card.GetComponentsInChildren<Transform>(true).First(t => t.name == "Phase15_CardBody");
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.6f);
            var last = body.lossyScale.x;
            Assert.That(last, Is.GreaterThan(0.74f * 1.03f), "control: hovered, it is larger");
            fan.RequestPick(card);
            // Only the pull beat: the landing's own squash (Phase 54) is a deliberate one-frame impact.
            var biggestStep = 0f;
            for (var t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                yield return null;
                biggestStep = Mathf.Max(biggestStep, Mathf.Abs(body.lossyScale.x - last));
                last = body.lossyScale.x;
            }

            while (!deck.ActiveCards.Contains(card))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            Assert.That(biggestStep, Is.LessThan(0.02f), "no one-frame snap in size");
            Assert.That(body.lossyScale.x, Is.EqualTo(0.74f).Within(0.005f), "it lands at its normal size");
        }

        private static int AliveParticles()
        {
            return Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                .Where(ps => ps.GetComponent<ParticleSystemRenderer>().enabled)
                .Sum(ps => ps.particleCount);
        }
    }
}
