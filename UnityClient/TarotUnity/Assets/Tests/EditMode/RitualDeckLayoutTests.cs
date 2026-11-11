using System;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualDeckLayoutTests
    {
        private static Type Model => typeof(RitualPickLayout).Assembly.GetType("TarotUnity.UI.RitualDeckLayout");

        [TestCase(1280f, 720f, 51.4718f, 534.7723f, 115.8324f, 103.4629f)]
        [TestCase(1024f, 768f, 40.9243f, 563.5523f, 92.5491f, 82.6026f)]
        [TestCase(2560f, 1440f, 102.9436f, 1069.5446f, 231.6648f, 206.9258f)]
        [TestCase(2048f, 1536f, 81.8486f, 1127.1046f, 185.0982f, 165.2052f)]
        public void ParkedDeckMatchesTheObservedCssRectangle(float width, float height, float x, float y, float w, float h)
        {
            var pose = Call("Parked", width, height, false);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < 12; i++)
            for (var corner = 0; corner < 4; corner++)
            {
                var point = (Vector3)Call("ProjectCorner", pose, Call("ShuffleLayer", i, 0f, width, height), corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            Assert.That(min.x, Is.EqualTo(x).Within(0.15f));
            Assert.That(min.y, Is.EqualTo(y).Within(0.15f));
            Assert.That(max.x - min.x, Is.EqualTo(w).Within(0.15f));
            Assert.That(max.y - min.y, Is.EqualTo(h).Within(0.15f));
            Assert.That(Field<float>(pose, "Opacity"), Is.EqualTo(0.48f));
        }

        [Test]
        public void PickupPassesThroughTheSameLiftedWaypointAndFinishesCentered()
        {
            var start = Call("Pickup", 1280f, 720f, 0f);
            var mid = Call("Pickup", 1280f, 720f, 0.45f);
            var finish = Call("Pickup", 1280f, 720f, 1f);
            Assert.That(Field<Vector2>(start, "Origin").x, Is.EqualTo(64.9f).Within(0.02f));
            Assert.That(Field<Vector2>(mid, "Origin").x, Is.EqualTo(307.72f).Within(0.02f));
            Assert.That(Field<float>(mid, "Pitch"), Is.EqualTo(12f));
            Assert.That(Field<float>(mid, "Roll"), Is.EqualTo(-12f));
            Assert.That(Field<float>(mid, "Scale"), Is.EqualTo(1.8f));
            Assert.That(Field<Vector2>(finish, "Origin").x + Field<Vector2>(finish, "Size").x * 0.5f,
                Is.EqualTo(640f).Within(0.02f));
            Assert.That(Field<float>(finish, "Pitch"), Is.EqualTo(23f));
            Assert.That(Field<float>(finish, "Roll"), Is.EqualTo(-3f));
            Assert.That(Field<float>(finish, "Scale"), Is.EqualTo(1.75f));
        }

        [Test]
        public void LayerReleaseIsOrderedAndBothPacketsHaveReturnedAtTheEnd()
        {
            Assert.That(Model, Is.Not.Null);
            Assert.That((int)Model.GetField("LayerCount").GetRawConstantValue(), Is.EqualTo(12));
            Assert.That((float)Model.GetField("PickupSeconds").GetRawConstantValue(), Is.EqualTo(0.9f));
            Assert.That((float)Model.GetField("ShuffleSeconds").GetRawConstantValue(), Is.EqualTo(5.4f));
            Assert.That((float)Model.GetField("SettleSeconds").GetRawConstantValue(), Is.EqualTo(0.4f));
            var first = Call("ShuffleLayer", 0, 0.5f, 1280f, 720f);
            var last = Call("ShuffleLayer", 11, 0.5f, 1280f, 720f);
            Assert.That(Field<Vector3>(first, "Offset").x, Is.EqualTo(0f).Within(0.001f), "The first release has already landed.");
            Assert.That(Field<Vector3>(last, "Offset").x, Is.EqualTo(1278f * 0.088f).Within(0.001f), "The final release still waits in its packet.");
            for (var i = 0; i < 12; i++)
            {
                var rest = Call("ShuffleLayer", i, 0f, 1280f, 720f);
                var end = Call("ShuffleLayer", i, 1f, 1280f, 720f);
                Assert.That(Field<Vector3>(end, "Offset"), Is.EqualTo(Field<Vector3>(rest, "Offset")));
                Assert.That(Field<float>(end, "Roll"), Is.Zero);
                Assert.That(Field<Vector3>(end, "Offset").z, Is.EqualTo(i * 1.2f).Within(0.001f));
            }
        }

        [TestCase(1280f, 720f)]
        [TestCase(1024f, 768f)]
        public void RaisedSplitWeaveAndRecutStayInsideTheFrame(float width, float height)
        {
            var raised = Call("Raised", width, height);
            for (var t = 0; t <= 100; t++)
            for (var i = 0; i < 12; i++)
            for (var corner = 0; corner < 4; corner++)
            {
                var point = (Vector3)Call("ProjectCorner", raised, Call("ShuffleLayer", i, t / 100f, width, height), corner);
                Assert.That(point.x, Is.InRange(width * 0.12f, width * 0.88f));
                Assert.That(point.y, Is.InRange(1f, height - 1f));
            }
        }

        private static object Call(string method, params object[] args)
        {
            Assert.That(Model, Is.Not.Null, "The approved deck pose model has not been implemented.");
            return Model.GetMethod(method).Invoke(null, args);
        }
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name).GetValue(target);
    }
}
