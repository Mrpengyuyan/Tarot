using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class AudioManagerLifecycleTests
    {
        private readonly List<GameObject> createdObjects = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var audio in Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None))
            {
                Object.Destroy(audio.gameObject);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var owner in createdObjects)
            {
                if (owner != null)
                {
                    Object.Destroy(owner);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FeedbackRemainsUsableAfterTheAudioManagerIsDestroyed()
        {
            var audio = CreateOwner().AddComponent<AudioManager>();
            var feedback = CreateOwner().AddComponent<RitualFeedbackController>();
            Object.Destroy(audio.gameObject);
            yield return null;

            Assert.DoesNotThrow(() => feedback.PlayCue(PresentationCueId.CardFlipped));
            Assert.That(feedback.LastCue, Is.EqualTo(PresentationCueId.CardFlipped));
            Assert.That(feedback.CueCount, Is.EqualTo(1));
            Assert.That(AudioManager.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator ReplacementManagerReceivesFeedbackAfterTheOriginalIsDestroyed()
        {
            var original = CreateOwner().AddComponent<AudioManager>();
            Object.Destroy(original.gameObject);
            yield return null;

            var replacement = CreateOwner().AddComponent<AudioManager>();
            var feedback = CreateOwner().AddComponent<RitualFeedbackController>();
            feedback.PlayCue(PresentationCueId.SpreadSelected);

            Assert.That(AudioManager.Instance, Is.SameAs(replacement));
            Assert.That(replacement.LastCue, Is.EqualTo(PresentationCueId.SpreadSelected));
            Assert.That(replacement.PlayedCueCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingADuplicateDoesNotClearTheActiveManager()
        {
            var active = CreateOwner().AddComponent<AudioManager>();
            var duplicate = CreateOwner().AddComponent<AudioManager>();
            yield return null;

            Assert.That(duplicate == null, Is.True);
            Assert.That(AudioManager.Instance, Is.SameAs(active));
            var feedback = CreateOwner().AddComponent<RitualFeedbackController>();
            feedback.PlayCue(PresentationCueId.ResultReady);
            Assert.That(active.LastCue, Is.EqualTo(PresentationCueId.ResultReady));
            Assert.That(active.PlayedCueCount, Is.EqualTo(1));
        }

        private GameObject CreateOwner()
        {
            var owner = new GameObject("AudioManager lifecycle test");
            createdObjects.Add(owner);
            return owner;
        }
    }
}
