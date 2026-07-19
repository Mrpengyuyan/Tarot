using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 55 drives a real shuffle on the ReadingRoom deck stack and watches
    /// every card every frame. It proves the motion is there (cards visibly pop
    /// during the riffle) and - the correctness bar - that the authored stagger is
    /// restored exactly: every card back at its rest pose to sub-millimetre
    /// precision, the stack root at its exact rest scale, so Phase 38's composition
    /// is untouched once the shuffle ends.
    /// </summary>
    public sealed class Phase55ShuffleFeelTests
    {
        [UnityTest]
        public IEnumerator ShuffleRipplesTheStackThenRestoresTheAuthoredStaggerExactly()
        {
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            var choreographer = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            Assert.That(choreographer, Is.Not.Null,
                "the ReadingRoom deck stack should carry the shuffle choreographer");

            var stack = choreographer.transform;
            Assert.That(stack.childCount, Is.GreaterThanOrEqualTo(8),
                "the Midnight Parlor stack should still be a real pile of cards");

            var restPositions = new List<Vector3>();
            var restRotations = new List<Quaternion>();
            foreach (Transform card in stack)
            {
                restPositions.Add(card.localPosition);
                restRotations.Add(card.localRotation);
            }

            var restRootScale = stack.localScale;
            var restRootPosition = stack.localPosition;

            // Play() runs the production path - the choreographer's own coroutine -
            // and we sample alongside it every rendered frame (the Phase 54 lesson:
            // hand-driving an enumerator can only see its outer yield points).
            choreographer.Play();
            Assert.That(choreographer.IsPlaying, Is.True, "Play() should start the shuffle");

            var maxLift = 0f;
            var minRootScaleY = float.MaxValue;
            var safetySeconds = 0f;
            while (choreographer.IsPlaying && safetySeconds < 15f)
            {
                for (var i = 0; i < restPositions.Count; i++)
                {
                    maxLift = Mathf.Max(maxLift, stack.GetChild(i).localPosition.y - restPositions[i].y);
                }

                minRootScaleY = Mathf.Min(minRootScaleY, stack.localScale.y);
                safetySeconds += Time.deltaTime;
                yield return null;
            }

            Assert.That(choreographer.IsPlaying, Is.False, "the shuffle should finish within the safety window");
            Assert.That(maxLift, Is.GreaterThan(0.02f),
                "cards should visibly pop during the riffle");
            Assert.That(minRootScaleY, Is.LessThan(restRootScale.y * 0.98f),
                "the stack should squash as it squares up on contact");

            for (var i = 0; i < restPositions.Count; i++)
            {
                var card = stack.GetChild(i);
                Assert.That(Vector3.Distance(card.localPosition, restPositions[i]), Is.LessThan(0.0005f),
                    $"card {card.name} must return exactly to its authored stagger");
                Assert.That(Quaternion.Angle(card.localRotation, restRotations[i]), Is.LessThan(0.1f),
                    $"card {card.name} must return exactly to its authored twist");
            }

            Assert.That(Vector3.Distance(stack.localScale, restRootScale), Is.LessThan(0.0005f),
                "the stack root must recover its exact rest scale");
            Assert.That(Vector3.Distance(stack.localPosition, restRootPosition), Is.LessThan(0.0005f),
                "the stack root must recover its exact rest position");
        }
    }
}
