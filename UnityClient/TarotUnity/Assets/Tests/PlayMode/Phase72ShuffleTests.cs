using System.Collections;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the shuffle is three beats - cut, two riffles, square. Phase 74 stretched it to about 3 s, Phase 75 to about 4 s.</summary>
    public sealed class Phase72ShuffleTests
    {
        [UnityTest]
        public IEnumerator ShuffleCutsTheDeckIntoTwoPilesAndLastsAboutFourSeconds()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            var choreographer = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            var stack = choreographer.transform;
            var minX = float.MaxValue;
            var maxX = float.MinValue;

            var started = Time.time;
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
            Assert.That(seconds, Is.InRange(3.7f, 4.4f), "three beats, not a single shiver");
            Assert.That(minX, Is.LessThan(-0.15f), "one pile goes left");
            Assert.That(maxX, Is.GreaterThan(0.15f), "the other goes right");
            Assert.That(choreographer.PlannedSeconds, Is.InRange(3.7f, 4.4f));
        }
    }
}
