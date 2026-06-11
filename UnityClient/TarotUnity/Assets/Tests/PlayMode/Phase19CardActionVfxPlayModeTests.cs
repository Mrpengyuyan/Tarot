using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Core;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase19CardActionVfxPlayModeTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDownCreatedObjects()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.Destroy(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualActionVfxControllerIsSafeWithMissingParticleController()
        {
            var owner = Track(new GameObject("Phase19 Action VFX Safety"));
            var controller = owner.AddComponent<RitualActionVfxController>();
            var anchor = Track(new GameObject("Phase19 Action VFX Anchor")).transform;

            Assert.DoesNotThrow(() => controller.PlayCue(PresentationCueId.ShuffleStarted, anchor));
            Assert.DoesNotThrow(() => controller.PlayCue(PresentationCueId.CardDealt));
            Assert.DoesNotThrow(() => controller.PlayCue(PresentationCueId.CardFlipped));
            Assert.DoesNotThrow(() => controller.PlayCue(PresentationCueId.ResultReveal));
            Assert.That(controller.LastCue, Is.EqualTo(PresentationCueId.ResultReveal));
            Assert.That(controller.LastAnchor, Is.Null);
            Assert.That(controller.CueCount, Is.EqualTo(4));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualActionVfxControllerMapsShuffleAndDealToAmbientPlayback()
        {
            var owner = Track(new GameObject("Phase19 Action VFX Ambient"));
            var particleController = owner.AddComponent<RitualParticleSystemController>();
            var actionVfx = owner.AddComponent<RitualActionVfxController>();
            var ambient = CreateParticle("Phase19 Ambient Particle", owner.transform, 10f, true);
            var focus = CreateParticle("Phase19 Focus Particle", owner.transform, 6f, true);

            AssignParticleFields(particleController, new[] { ambient }, new[] { focus }, null);
            SetPrivateField(actionVfx, "particleSystemController", particleController);
            SetPrivateField(actionVfx, "shuffleIntensity", 0.76f);
            SetPrivateField(actionVfx, "dealIntensity", 0.66f);

            actionVfx.PlayCue(PresentationCueId.ShuffleStarted);
            yield return null;

            Assert.That(actionVfx.LastCue, Is.EqualTo(PresentationCueId.ShuffleStarted));
            Assert.That(actionVfx.CueCount, Is.EqualTo(1));
            Assert.That(particleController.CurrentIntensity, Is.EqualTo(0.76f).Within(0.001f));
            Assert.That(particleController.IsPlaying, Is.True);
            Assert.That(ambient.isPlaying, Is.True);
            Assert.That(focus.isPlaying, Is.True);

            actionVfx.PlayCue(PresentationCueId.CardDealt);
            yield return null;

            Assert.That(particleController.CurrentIntensity, Is.EqualTo(0.66f).Within(0.001f));
            Assert.That(particleController.IsPlaying, Is.True);
            Assert.That(ambient.isPlaying, Is.True);
            Assert.That(focus.isPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator RitualActionVfxControllerMapsFlipToRevealBurst()
        {
            var owner = Track(new GameObject("Phase19 Action VFX Flip"));
            var particleController = owner.AddComponent<RitualParticleSystemController>();
            var actionVfx = owner.AddComponent<RitualActionVfxController>();
            var reveal = CreateParticle("Phase19 Reveal Burst Particle", owner.transform, 0f, false);

            AssignParticleFields(particleController, null, null, new[] { reveal });
            SetPrivateField(actionVfx, "particleSystemController", particleController);
            SetPrivateField(actionVfx, "flipIntensity", 0.95f);

            actionVfx.PlayCue(PresentationCueId.CardFlipped);

            Assert.That(actionVfx.LastCue, Is.EqualTo(PresentationCueId.CardFlipped));
            Assert.That(particleController.CurrentIntensity, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(reveal.particleCount, Is.GreaterThan(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualActionVfxControllerMapsResultRevealToSafeResultVfx()
        {
            var owner = Track(new GameObject("Phase19 Action VFX Result"));
            var particleController = owner.AddComponent<RitualParticleSystemController>();
            var actionVfx = owner.AddComponent<RitualActionVfxController>();
            var ambient = CreateParticle("Phase19 Result Ambient", owner.transform, 5f, true);
            var reveal = CreateParticle("Phase19 Result Reveal", owner.transform, 0f, false);

            AssignParticleFields(particleController, new[] { ambient }, null, new[] { reveal });
            SetPrivateField(actionVfx, "particleSystemController", particleController);
            SetPrivateField(actionVfx, "resultIntensity", 0.72f);

            actionVfx.PlayCue(PresentationCueId.ResultReveal);
            yield return null;

            Assert.That(actionVfx.LastCue, Is.EqualTo(PresentationCueId.ResultReveal));
            Assert.That(particleController.CurrentIntensity, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(particleController.IsPlaying, Is.True);
            Assert.That(ambient.isPlaying, Is.True);
            Assert.That(reveal.particleCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator RitualFeedbackControllerForwardsCueToActionVfxWhenEnabled()
        {
            var owner = Track(new GameObject("Phase19 Feedback Forward"));
            var feedback = owner.AddComponent<RitualFeedbackController>();
            var actionVfx = owner.AddComponent<RitualActionVfxController>();
            var anchor = Track(new GameObject("Phase19 Feedback Anchor")).transform;

            SetPrivateField(feedback, "actionVfxController", actionVfx);
            SetPrivateField(feedback, "forwardCuesToActionVfx", true);

            feedback.PlayCue(PresentationCueId.CardFlipped, anchor);

            Assert.That(feedback.LastCue, Is.EqualTo(PresentationCueId.CardFlipped));
            Assert.That(feedback.CueCount, Is.EqualTo(1));
            Assert.That(actionVfx.LastCue, Is.EqualTo(PresentationCueId.CardFlipped));
            Assert.That(actionVfx.LastAnchor, Is.SameAs(anchor));
            Assert.That(actionVfx.CueCount, Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualFeedbackControllerDoesNotForwardCueWhenDisabled()
        {
            var owner = Track(new GameObject("Phase19 Feedback Disabled"));
            var feedback = owner.AddComponent<RitualFeedbackController>();
            var actionVfx = owner.AddComponent<RitualActionVfxController>();

            SetPrivateField(feedback, "actionVfxController", actionVfx);
            SetPrivateField(feedback, "forwardCuesToActionVfx", false);

            feedback.PlayCue(PresentationCueId.ResultReady);

            Assert.That(feedback.LastCue, Is.EqualTo(PresentationCueId.ResultReady));
            Assert.That(actionVfx.CueCount, Is.EqualTo(0));
            Assert.That(actionVfx.LastCue, Is.EqualTo(PresentationCueId.None));

            yield return null;
        }

        private ParticleSystem CreateParticle(string name, Transform parent, float emissionRate, bool loop)
        {
            var gameObject = Track(new GameObject(name));
            gameObject.transform.SetParent(parent, false);
            var particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = loop;
            main.duration = 2f;
            main.startLifetime = 1f;
            main.startSpeed = 0.1f;
            main.startSize = 0.05f;
            main.maxParticles = 64;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            return particles;
        }

        private static void AssignParticleFields(
            RitualParticleSystemController controller,
            ParticleSystem[] ambient,
            ParticleSystem[] focus,
            ParticleSystem[] reveal)
        {
            SetPrivateField(controller, "ambientParticles", ambient);
            SetPrivateField(controller, "focusParticles", focus);
            SetPrivateField(controller, "revealParticles", reveal);
            SetPrivateField(controller, "playOnEnable", false);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private GameObject Track(GameObject gameObject)
        {
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
