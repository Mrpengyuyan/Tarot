using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase14DimensionalRevealPlayModeTests
    {
        [UnityTest]
        public IEnumerator CardViewRunsDimensionalRevealOnlyWhenTransitioningFaceUp()
        {
            var owner = new GameObject("Phase14 Reveal Card");
            var cardRoot = new GameObject("Phase14_DimensionalRoot").transform;
            cardRoot.SetParent(owner.transform, false);

            var glowObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glowObject.name = "Phase14_RevealGlow";
            glowObject.transform.SetParent(cardRoot, false);

            try
            {
                var glowRenderer = glowObject.GetComponent<MeshRenderer>();
                glowRenderer.enabled = false;

                var controller = owner.AddComponent<DimensionalCardRevealController>();
                SetPrivateField(controller, "cardRoot", cardRoot);
                SetPrivateField(controller, "revealGlowRenderer", glowRenderer);
                SetPrivateField(controller, "revealLift", 0.2f);
                SetPrivateField(controller, "revealScale", 1.2f);
                SetPrivateField(controller, "settleSeconds", 0.05f);

                var card = owner.AddComponent<CardView>();
                SetPrivateField(card, "dimensionalRevealController", controller);

                card.SetFaceUp(false);
                yield return null;
                Assert.That(glowRenderer.enabled, Is.False);
                Assert.That(cardRoot.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(cardRoot.localScale, Is.EqualTo(Vector3.one));

                card.SetFaceUp(true);
                Assert.That(glowRenderer.enabled, Is.True);
                Assert.That(cardRoot.localPosition.y, Is.GreaterThan(0f));
                Assert.That(cardRoot.localScale.x, Is.GreaterThan(1f));

                yield return new WaitForSeconds(0.12f);
                AssertVectorClose(cardRoot.localPosition, Vector3.zero, "first reveal should settle position");
                AssertVectorClose(cardRoot.localScale, Vector3.one, "first reveal should settle scale");

                card.SetFaceUp(true);
                yield return null;
                AssertVectorClose(cardRoot.localPosition, Vector3.zero, "repeated face-up call should not relaunch reveal position");
                AssertVectorClose(cardRoot.localScale, Vector3.one, "repeated face-up call should not relaunch reveal scale");

                card.SetFaceUp(false);
                Assert.That(glowRenderer.enabled, Is.False);
            }
            finally
            {
                Object.Destroy(owner);
            }
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void AssertVectorClose(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f), message);
        }
    }
}
