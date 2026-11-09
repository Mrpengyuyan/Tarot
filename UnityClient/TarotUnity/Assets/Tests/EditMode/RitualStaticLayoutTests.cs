using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualStaticLayoutTests
    {
        [TestCase("cover", 1280, 720, "StartReadingButton", 544.164f, 514.406f, 191.672f, 50.219f)]
        [TestCase("cover", 1024, 768, "StartReadingButton", 435.359f, 546.750f, 153.281f, 44.875f)]
        [TestCase("cover", 1280, 720, "QuitButton", 605.633f, 586.164f, 68.734f, 39.5f)]
        [TestCase("cover", 1024, 768, "QuitButton", 479.359f, 614.602f, 65.281f, 37.5f)]
        [TestCase("question", 1280, 720, "RitualQuestionLabel", 231.039f, 288.195f, 817.922f, 53f)]
        [TestCase("question", 1024, 768, "RitualQuestionLabel", 184.953f, 322.719f, 654.094f, 42.5f)]
        [TestCase("question", 1280, 720, "QuestionInput", 231.039f, 369.914f, 817.922f, 78.703f)]
        [TestCase("question", 1024, 768, "QuestionInput", 184.953f, 395.852f, 654.094f, 73.391f)]
        [TestCase("question", 1280, 720, "DrawButton", 537.758f, 491.695f, 204.477f, 54.219f)]
        [TestCase("question", 1024, 768, "DrawButton", 430.242f, 515.195f, 163.516f, 47.875f)]
        [TestCase("spread", 1280, 720, "ThreeCardButton/Title", 507.938f, 415.266f, 264.125f, 34f)]
        [TestCase("spread", 1024, 768, "ThreeCardButton/Title", 406.391f, 481.266f, 211.219f, 27.5f)]
        public void StaticElementsMatchSettledReferenceRectangles(string stage, int width, int height,
            string key, float x, float y, float w, float h)
        {
            var actual = Read<Rect>(Compute(stage, width, height)[key], "Rect");
            Assert.That(actual.x, Is.EqualTo(x).Within(0.4f));
            Assert.That(actual.y, Is.EqualTo(y).Within(0.4f));
            Assert.That(actual.width, Is.EqualTo(w).Within(0.4f));
            Assert.That(actual.height, Is.EqualTo(h).Within(0.4f));
        }

        [TestCase("cover", 1280, 720)]
        [TestCase("cover", 1024, 768)]
        [TestCase("spread", 1280, 720)]
        [TestCase("spread", 1024, 768)]
        [TestCase("question", 1280, 720)]
        [TestCase("question", 1024, 768)]
        public void RenderingDensityScalesEveryRectangleAndFontButNotTracking(string stage, int width, int height)
        {
            var a = Compute(stage, width, height);
            var b = Compute(stage, width * 2, height * 2);
            foreach (DictionaryEntry item in a)
            {
                var x = Read<Rect>(item.Value, "Rect");
                var y = Read<Rect>(b[item.Key], "Rect");
                Assert.That(Vector2.Distance(x.position * 2f, y.position), Is.LessThan(0.1f), item.Key.ToString());
                Assert.That(Vector2.Distance(x.size * 2f, y.size), Is.LessThan(0.1f), item.Key.ToString());
                Assert.That(Read<float>(b[item.Key], "FontSize"), Is.EqualTo(Read<float>(item.Value, "FontSize") * 2f).Within(0.01f));
                Assert.That(Read<float>(b[item.Key], "Tracking"), Is.EqualTo(Read<float>(item.Value, "Tracking")));
            }
        }

        [Test]
        public void CoverUsesTheApprovedTiltAndCenteredOrnaments()
        {
            foreach (var size in new[] { new Vector2(1280, 720), new Vector2(1024, 768) })
            {
                var frame = Compute("cover", size.x, size.y);
                Assert.That(Read<float>(frame["TitleText"], "Rotation"), Is.EqualTo(2f),
                    "CSS -2 degrees maps to +2 around Unity's upward Y axis.");
                foreach (var key in new[] { "RitualCoverSeal", "RitualTitleRule", "RitualCoverCaption", "StartReadingButton", "QuitButton" })
                    Assert.That(Read<Rect>(frame[key], "Rect").center.x, Is.EqualTo(size.x / 2f).Within(0.1f));
            }
        }

        private static IDictionary Compute(string stage, float width, float height)
        {
            var type = typeof(RitualReadingView).Assembly.GetType("TarotUnity.UI.RitualStaticLayout");
            Assert.That(type, Is.Not.Null, "Static stages need measured reference geometry, not approximate placements.");
            return (IDictionary)type.GetMethod("Compute", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { stage, width, height });
        }

        private static T Read<T>(object element, string key) => (T)element.GetType().GetField(key).GetValue(element);
    }
}
