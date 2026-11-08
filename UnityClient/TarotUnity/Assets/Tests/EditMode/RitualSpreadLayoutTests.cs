using System;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualSpreadLayoutTests
    {
        [TestCase(1, 1280, 720)]
        [TestCase(3, 1280, 720)]
        [TestCase(10, 1280, 720)]
        [TestCase(1, 1024, 768)]
        [TestCase(3, 1024, 768)]
        [TestCase(10, 1024, 768)]
        public void HigherRenderingResolutionPreservesTheReferenceComposition(int count, int width, int height)
        {
            var normal = Read<Array>(Compute(count, width, height), "Cards");
            var dense = Read<Array>(Compute(count, width * 2, height * 2), "Cards");
            for (var i = 0; i < count; i++)
            {
                var a = Read<Rect>(normal.GetValue(i), "CardRect");
                var b = Read<Rect>(dense.GetValue(i), "CardRect");
                Assert.That(b.x, Is.EqualTo(a.x * 2f).Within(0.1f));
                Assert.That(b.y, Is.EqualTo(a.y * 2f).Within(0.1f));
                Assert.That(b.width, Is.EqualTo(a.width * 2f).Within(0.1f));
                Assert.That(b.height, Is.EqualTo(a.height * 2f).Within(0.1f));
            }
        }

        [Test]
        public void ResizingAroundWideAspectDoesNotJumpTheCardBand()
        {
            var wide = Read<Array>(Compute(3, 1280f, 720f), "Cards");
            var almost = Read<Array>(Compute(3, 1278f, 720f), "Cards");
            Assert.That(Read<Rect>(almost.GetValue(1), "CardRect").center.y,
                Is.EqualTo(Read<Rect>(wide.GetValue(1), "CardRect").center.y).Within(2f));
        }

        [TestCase(1, 1280, 720, 0, 540.2075f, 240.3062f, 199.577f, 316.5042f)]
        [TestCase(3, 1280, 720, 0, 314.1537f, 264.1571f, 185.9346f, 265.0825f)]
        [TestCase(3, 1280, 720, 1, 560.7938f, 268.6375f, 158.4124f, 255.4997f)]
        [TestCase(10, 1280, 720, 0, 300.7657f, 228.086f, 88.5412f, 138.0725f)]
        [TestCase(10, 1280, 720, 7, 598.6281f, 396.5716f, 82.736f, 141.9741f)]
        [TestCase(1, 1024, 768, 0, 432.852f, 322.7928f, 158.2961f, 252.9658f)]
        [TestCase(3, 1024, 768, 1, 449.0746f, 345.7594f, 125.8428f, 204.2457f)]
        [TestCase(10, 1024, 768, 0, 241.343f, 314.8469f, 69.7227f, 110.6549f)]
        [TestCase(10, 1024, 768, 7, 478.9869f, 457.1073f, 66.0104f, 113.2832f)]
        public void ProjectedCardsMatchMeasuredV6RatherThanTheOldTable(int count, int width, int height,
            int index, float x, float y, float w, float h)
        {
            var layout = Compute(count, width, height);
            var cards = Read<Array>(layout, "Cards");
            Assert.That(cards.Length, Is.EqualTo(count));
            var rect = Read<Rect>(cards.GetValue(index), "CardRect");
            Assert.That(rect.x, Is.EqualTo(x).Within(0.35f));
            Assert.That(rect.y, Is.EqualTo(y).Within(0.35f));
            Assert.That(rect.width, Is.EqualTo(w).Within(0.35f));
            Assert.That(rect.height, Is.EqualTo(h).Within(0.35f));
        }

        [TestCase(1280, 720, 548.9531f, 616f, 182.0938f, 47.7188f)]
        [TestCase(1024, 768, 439.1875f, 664.0234f, 145.6172f, 44f)]
        public void ActionAndLabelsUseTheApprovedSharedCenterAndSafeGap(int width, int height,
            float x, float y, float w, float h)
        {
            foreach (var count in new[] { 1, 3, 10 })
            {
                var layout = Compute(count, width, height);
                var action = Read<Rect>(layout, "ActionRect");
                Assert.That(action.x, Is.EqualTo(x).Within(0.35f));
                Assert.That(action.y, Is.EqualTo(y).Within(0.35f));
                Assert.That(action.width, Is.EqualTo(w).Within(0.35f));
                Assert.That(action.height, Is.EqualTo(h).Within(0.35f));
                foreach (var card in Read<Array>(layout, "Cards"))
                {
                    var label = Read<Vector2>(card, "LabelCenter");
                    Assert.That(label.y, Is.GreaterThan(Read<Rect>(card, "CardRect").yMax));
                    Assert.That(label.y + 20f, Is.LessThan(action.yMin));
                }
            }
        }

        private static object Compute(int count, float width, float height)
        {
            var type = typeof(RitualReadingView).Assembly.GetType("TarotUnity.UI.RitualSpreadLayout");
            Assert.That(type, Is.Not.Null, "The approved preview needs an explicit measured screen-space layout.");
            var method = type.GetMethod("Compute", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { count, (float)width, (float)height });
        }

        private static T Read<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }
    }
}
