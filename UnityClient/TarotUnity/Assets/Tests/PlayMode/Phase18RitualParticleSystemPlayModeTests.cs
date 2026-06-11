using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase18RitualParticleSystemPlayModeTests
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
        public IEnumerator RitualParticleSystemControllerIsSafeWithMissingReferences()
        {
            var owner = Track(new GameObject("Phase18 Particle Safety"));
            var controller = owner.AddComponent<RitualParticleSystemController>();

            Assert.DoesNotThrow(() => controller.SetIntensity(0.5f));
            Assert.DoesNotThrow(() => controller.SetParticlesVisible(true));
            Assert.DoesNotThrow(controller.PlayAmbient);
            Assert.DoesNotThrow(controller.TriggerRevealBurst);
            Assert.DoesNotThrow(() => controller.SimulateTick(0.25f));
            Assert.DoesNotThrow(() => controller.StopAll(true));
            Assert.That(controller.CurrentIntensity, Is.EqualTo(0.5f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualParticleSystemControllerClampsIntensityAndAppliesEmission()
        {
            var owner = Track(new GameObject("Phase18 Particle Emission"));
            var controller = owner.AddComponent<RitualParticleSystemController>();
            var ambient = CreateParticle("Ambient", owner.transform, 10f, true);
            var focus = CreateParticle("Focus", owner.transform, 6f, true);
            var reveal = CreateParticle("Reveal", owner.transform, 4f, false);

            AssignParticleFields(controller, new[] { ambient }, new[] { focus }, new[] { reveal });
            SetPrivateField(controller, "baseEmissionMultiplier", 1f);
            SetPrivateField(controller, "focusEmissionMultiplier", 0.5f);
            SetPrivateField(controller, "revealEmissionMultiplier", 2f);

            controller.SetIntensity(2f);

            Assert.That(controller.CurrentIntensity, Is.EqualTo(1f));
            Assert.That(GetEmissionRate(ambient), Is.EqualTo(10f).Within(0.001f));
            Assert.That(GetEmissionRate(focus), Is.EqualTo(3f).Within(0.001f));
            Assert.That(GetEmissionRate(reveal), Is.EqualTo(8f).Within(0.001f));

            controller.SetIntensity(-1f);

            Assert.That(controller.CurrentIntensity, Is.EqualTo(0f));
            Assert.That(GetEmissionRate(ambient), Is.EqualTo(0f).Within(0.001f));
            Assert.That(GetEmissionRate(focus), Is.EqualTo(0f).Within(0.001f));
            Assert.That(GetEmissionRate(reveal), Is.EqualTo(0f).Within(0.001f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualParticleSystemControllerPlaysAndStopsAssignedParticles()
        {
            var owner = Track(new GameObject("Phase18 Particle Playback"));
            var controller = owner.AddComponent<RitualParticleSystemController>();
            var ambient = CreateParticle("Ambient Playback", owner.transform, 10f, true);
            var focus = CreateParticle("Focus Playback", owner.transform, 5f, true);

            AssignParticleFields(controller, new[] { ambient }, new[] { focus }, null);

            controller.SetIntensity(1f);
            controller.PlayAmbient();
            yield return null;

            Assert.That(controller.IsPlaying, Is.True);
            Assert.That(ambient.isPlaying, Is.True);
            Assert.That(focus.isPlaying, Is.True);

            controller.StopAll(true);
            yield return null;

            Assert.That(controller.IsPlaying, Is.False);
            Assert.That(ambient.isPlaying, Is.False);
            Assert.That(focus.isPlaying, Is.False);
            Assert.That(ambient.particleCount, Is.EqualTo(0));
            Assert.That(focus.particleCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RitualParticleSystemControllerTriggersRevealBurst()
        {
            var owner = Track(new GameObject("Phase18 Particle Burst"));
            var controller = owner.AddComponent<RitualParticleSystemController>();
            var reveal = CreateParticle("Reveal Burst", owner.transform, 0f, false);

            AssignParticleFields(controller, null, null, new[] { reveal });
            SetPrivateField(controller, "revealEmissionMultiplier", 1.5f);

            controller.SetIntensity(1f);
            controller.TriggerRevealBurst();

            Assert.That(reveal.particleCount, Is.GreaterThan(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualParticleSystemControllerSimulatesAssignedParticles()
        {
            var owner = Track(new GameObject("Phase18 Particle Simulate"));
            var controller = owner.AddComponent<RitualParticleSystemController>();
            var ambient = CreateParticle("Ambient Simulate", owner.transform, 12f, true);

            AssignParticleFields(controller, new[] { ambient }, null, null);

            controller.SetIntensity(1f);
            controller.PlayAmbient();
            controller.SimulateTick(0.5f);

            Assert.That(ambient.time, Is.GreaterThan(0f));

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

        private static float GetEmissionRate(ParticleSystem particles)
        {
            return particles.emission.rateOverTime.constantMax;
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
