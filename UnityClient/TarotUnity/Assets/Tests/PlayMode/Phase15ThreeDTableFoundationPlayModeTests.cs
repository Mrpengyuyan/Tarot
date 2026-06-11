using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase15ThreeDTableFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator OptionalPhase15PresentationHelpersAreSafeAtRuntime()
        {
            var owner = new GameObject("Phase15 Runtime Safety Card");
            try
            {
                var card = owner.AddComponent<CardView>();
                Assert.DoesNotThrow(() => card.SetFaceUp(true));
                Assert.DoesNotThrow(() => card.SetFaceUp(false));

                var face = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var back = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
                face.transform.SetParent(owner.transform, false);
                back.transform.SetParent(owner.transform, false);
                shadow.transform.SetParent(owner.transform, false);

                var controller = owner.AddComponent<ThreeDCardPresentationController>();
                SetPrivateField(controller, "cardFaceRenderer", face);
                SetPrivateField(controller, "cardBackRenderer", back);
                SetPrivateField(controller, "cardDropShadowRenderer", shadow);
                SetPrivateField(card, "threeDPresentationController", controller);

                var shellRoot = new GameObject("Phase15 Runtime Shell").transform;
                shellRoot.SetParent(owner.transform, false);
                SetPrivateField(controller, "cardMeshRoot", shellRoot);

                card.SetFaceUp(true);
                yield return null;
                Assert.That(face.enabled, Is.True);
                Assert.That(back.enabled, Is.False);
                Assert.That(shadow.enabled, Is.True);

                card.SetFaceUp(false);
                yield return null;
                Assert.That(face.enabled, Is.False);
                Assert.That(back.enabled, Is.True);
                Assert.That(shadow.enabled, Is.True);

                controller.SetShellVisible(false);
                Assert.That(shellRoot.gameObject.activeSelf, Is.False);
                controller.SetShellVisible(true);
                Assert.That(shellRoot.gameObject.activeSelf, Is.True);

                var stage = owner.AddComponent<Phase15TableStageController>();
                Assert.DoesNotThrow(() => stage.SetStageVisible(false));
                Assert.DoesNotThrow(() => stage.SetStageVisible(true));

                var tableRoot = new GameObject("Phase15 Runtime Table Root");
                tableRoot.transform.SetParent(owner.transform, false);
                var warm = new GameObject("Phase15 Runtime Warm Light").AddComponent<Light>();
                warm.transform.SetParent(owner.transform, false);
                var cool = new GameObject("Phase15 Runtime Cool Light").AddComponent<Light>();
                cool.transform.SetParent(owner.transform, false);

                SetPrivateField(stage, "tableRoot", tableRoot);
                SetPrivateField(stage, "warmKeyLight", warm);
                SetPrivateField(stage, "coolRimLight", cool);

                stage.SetStageVisible(false);
                Assert.That(tableRoot.activeSelf, Is.False);
                Assert.That(warm.enabled, Is.False);
                Assert.That(cool.enabled, Is.False);

                stage.SetStageVisible(true);
                Assert.That(tableRoot.activeSelf, Is.True);
                Assert.That(warm.enabled, Is.True);
                Assert.That(cool.enabled, Is.True);
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
    }
}
