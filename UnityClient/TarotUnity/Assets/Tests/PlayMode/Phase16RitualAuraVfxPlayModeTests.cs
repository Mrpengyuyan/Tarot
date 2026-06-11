using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase16RitualAuraVfxPlayModeTests
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
        public IEnumerator RitualAuraControllerIsSafeWithMissingReferences()
        {
            var owner = Track(new GameObject("Phase16 Runtime Aura Safety"));
            var controller = owner.AddComponent<RitualAuraController>();

            Assert.DoesNotThrow(() => controller.SetAuraVisible(true));
            Assert.DoesNotThrow(() => controller.SetRuneVisible(false));
            Assert.DoesNotThrow(() => controller.SetParticlesVisible(false));
            Assert.DoesNotThrow(() => controller.SetIntensity(-1f));
            Assert.That(controller.CurrentIntensity, Is.EqualTo(0f));
            Assert.DoesNotThrow(() => controller.SetIntensity(2f));
            Assert.That(controller.CurrentIntensity, Is.EqualTo(1f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RitualAuraControllerTogglesAssignedVisuals()
        {
            var owner = Track(new GameObject("Phase16 Runtime Aura Toggle"));
            var controller = owner.AddComponent<RitualAuraController>();
            var auraRoot = new GameObject("Phase16 Runtime Aura Root");
            auraRoot.transform.SetParent(owner.transform, false);

            var glow = CreateTestRenderer("Phase16 Runtime Glow Renderer", owner.transform);
            var rune = CreateTestRenderer("Phase16 Runtime Rune Renderer", owner.transform);
            var particle = CreateTestRenderer("Phase16 Runtime Particle Renderer", owner.transform);

            var auraLight = new GameObject("Phase16 Runtime Aura Light").AddComponent<Light>();
            auraLight.transform.SetParent(owner.transform, false);

            SetPrivateField(controller, "auraRoot", auraRoot);
            SetPrivateField(controller, "glowRenderers", new Renderer[] { glow });
            SetPrivateField(controller, "runeRenderers", new Renderer[] { rune });
            SetPrivateField(controller, "particleRenderers", new Renderer[] { particle });
            SetPrivateField(controller, "auraLights", new[] { auraLight });

            controller.SetAuraVisible(false);
            yield return null;
            Assert.That(auraRoot.activeSelf, Is.False);
            Assert.That(glow.enabled, Is.False);
            Assert.That(rune.enabled, Is.False);
            Assert.That(particle.enabled, Is.False);
            Assert.That(auraLight.enabled, Is.False);

            controller.SetAuraVisible(true);
            yield return null;
            Assert.That(auraRoot.activeSelf, Is.True);
            Assert.That(glow.enabled, Is.True);
            Assert.That(rune.enabled, Is.True);
            Assert.That(particle.enabled, Is.True);
            Assert.That(auraLight.enabled, Is.True);

            controller.SetRuneVisible(false);
            Assert.That(rune.enabled, Is.False);
            Assert.That(glow.enabled, Is.True);
            Assert.That(particle.enabled, Is.True);

            controller.SetParticlesVisible(false);
            Assert.That(particle.enabled, Is.False);

            controller.SetIntensity(0.42f);
            Assert.That(controller.CurrentIntensity, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(auraLight.intensity, Is.EqualTo(0.42f).Within(0.001f));
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

        private static MeshRenderer CreateTestRenderer(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>();
            return gameObject.AddComponent<MeshRenderer>();
        }
    }
}
