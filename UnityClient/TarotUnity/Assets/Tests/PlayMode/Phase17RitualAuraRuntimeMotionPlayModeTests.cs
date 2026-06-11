using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase17RitualAuraRuntimeMotionPlayModeTests
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
        public IEnumerator RitualAuraMotionControllerIsSafeWithMissingReferences()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion Safety"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            Assert.DoesNotThrow(() => controller.SetAnimating(true));
            Assert.DoesNotThrow(() => controller.Tick(0.1f, 0.25f));
            Assert.DoesNotThrow(() => controller.SetAnimating(false));
            Assert.DoesNotThrow(() => controller.Tick(0.1f, 0.5f));
            Assert.DoesNotThrow(controller.ResetMotion);
            Assert.That(controller.CurrentPulse, Is.EqualTo(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraMotionControllerAnimatesAssignedTransforms()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            var glow = CreateChild("Phase17 Glow", owner.transform);
            glow.localScale = new Vector3(2f, 1f, 1f);
            var rune = CreateChild("Phase17 Rune", owner.transform);
            var particle = CreateChild("Phase17 Particle", owner.transform);
            particle.localPosition = new Vector3(0f, 0.5f, 0f);

            AssignMotionFields(controller, new[] { glow }, new[] { rune }, new[] { particle });

            var baseGlowScale = glow.localScale;
            var baseRuneRotation = rune.localRotation;
            var baseParticlePosition = particle.localPosition;

            controller.SetAnimating(true);
            controller.Tick(0.5f, 1f);

            Assert.That(rune.localRotation, Is.Not.EqualTo(baseRuneRotation));
            Assert.That(glow.localScale, Is.Not.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.Not.EqualTo(baseParticlePosition));
            Assert.That(controller.CurrentPulse, Is.GreaterThan(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraMotionControllerPauseAndResetAreDeterministic()
        {
            var owner = Track(new GameObject("Phase17 Runtime Motion Reset"));
            var controller = owner.AddComponent<RitualAuraMotionController>();

            var glow = CreateChild("Phase17 Reset Glow", owner.transform);
            glow.localScale = new Vector3(1.5f, 0.75f, 1f);
            var rune = CreateChild("Phase17 Reset Rune", owner.transform);
            var particle = CreateChild("Phase17 Reset Particle", owner.transform);
            particle.localPosition = new Vector3(0.1f, 0.2f, 0.3f);

            AssignMotionFields(controller, new[] { glow }, new[] { rune }, new[] { particle });

            var baseGlowScale = glow.localScale;
            var baseRuneRotation = rune.localRotation;
            var baseParticlePosition = particle.localPosition;

            controller.SetAnimating(false);
            controller.Tick(1f, 1f);
            Assert.That(glow.localScale, Is.EqualTo(baseGlowScale));
            Assert.That(rune.localRotation, Is.EqualTo(baseRuneRotation));
            Assert.That(particle.localPosition, Is.EqualTo(baseParticlePosition));

            controller.SetAnimating(true);
            controller.Tick(1f, 1f);
            Assert.That(glow.localScale, Is.Not.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.Not.EqualTo(baseParticlePosition));

            controller.ResetMotion();
            Assert.That(glow.localScale, Is.EqualTo(baseGlowScale));
            Assert.That(particle.localPosition, Is.EqualTo(baseParticlePosition));
            Assert.That(controller.CurrentPulse, Is.EqualTo(1f));

            yield return null;
        }

        private static void AssignMotionFields(
            RitualAuraMotionController controller,
            Transform[] glowPulsers,
            Transform[] runeRings,
            Transform[] particleAnchors)
        {
            SetPrivateField(controller, "glowPulsers", glowPulsers);
            SetPrivateField(controller, "runeRings", runeRings);
            SetPrivateField(controller, "particleAnchors", particleAnchors);
            SetPrivateField(controller, "pulseSpeed", 0.25f);
            SetPrivateField(controller, "pulseAmplitude", 0.2f);
            SetPrivateField(controller, "particleFloatSpeed", 0.25f);
            SetPrivateField(controller, "particleFloatAmplitude", 0.1f);
            SetPrivateField(controller, "runeRotationSpeedDegrees", 10f);
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

        private static Transform CreateChild(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }
    }
}
