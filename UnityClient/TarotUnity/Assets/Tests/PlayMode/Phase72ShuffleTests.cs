using System.Collections;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>The real deck is picked up, cut and riffled twice, then squared without a duplicate stack.</summary>
    public sealed class Phase72ShuffleTests
    {
        [UnityTest]
        public IEnumerator PickupAndTwoRifflesGiveTheDeckAReadableRitual()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }
            yield return null;

            var choreographer = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            var stack = choreographer.transform;
            var minX = float.MaxValue;
            var maxX = float.MinValue;

            var started = Time.time;
            var anchor = choreographer.GetComponent<RitualDeckAnchor>();
            yield return anchor.Pickup();
            choreographer.Play();
            while (choreographer.IsPlaying && Time.time - started < 10f)
            {
                foreach (Transform card in stack)
                {
                    minX = Mathf.Min(minX, card.localPosition.x);
                    maxX = Mathf.Max(maxX, card.localPosition.x);
                }

                yield return null;
            }

            var seconds = Time.time - started;
            Assert.That(seconds, Is.InRange(6.2f, 7.3f), "pickup, two pronounced cuts and square-up");
            Assert.That(minX, Is.LessThan(-0.15f), "one pile goes left");
            Assert.That(maxX, Is.GreaterThan(0.15f), "the other goes right");
            Assert.That(seconds, Is.EqualTo(choreographer.PlannedSeconds + anchor.PickupSeconds).Within(0.6f));
        }
    }
}
