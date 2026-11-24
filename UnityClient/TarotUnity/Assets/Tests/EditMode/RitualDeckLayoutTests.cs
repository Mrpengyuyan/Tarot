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

        [TestCase(1280f, 720f)]
        [TestCase(1024f, 768f)]
        [TestCase(2560f, 1440f)]
        [TestCase(2048f, 1536f)]
        public void ParkedDeckScalesProportionallyWithDensityAndStaysAtBottomLeft(float width, float height)
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

            Assert.That(min.x, Is.InRange(width * 0.02f, width * 0.08f));
            Assert.That(min.y, Is.InRange(height * 0.55f, height * 0.72f));
            Assert.That(max.x - min.x, Is.GreaterThan(width * 0.10f), "Enlarged resting deck should have clear presence.");
            Assert.That(max.y - min.y, Is.GreaterThan(width * 0.10f));
            Assert.That(Field<float>(pose, "Opacity"), Is.EqualTo(0.48f));
        }

        [Test]
        public void PickupRisesDirectlyFromBottomLeftToExactScreenCenter()
        {
            var start = Call("Pickup", 1280f, 720f, 0f);
            var mid = Call("Pickup", 1280f, 720f, 0.45f);
            var finish = Call("Pickup", 1280f, 720f, 1f);

            var startOrigin = Field<Vector2>(start, "Origin");
            var midOrigin = Field<Vector2>(mid, "Origin");
            var finishOrigin = Field<Vector2>(finish, "Origin");
            var finishSize = Field<Vector2>(finish, "Size");

            Assert.That(midOrigin.x, Is.GreaterThan(startOrigin.x));
            Assert.That(midOrigin.x, Is.LessThan(finishOrigin.x));
            Assert.That(midOrigin.y, Is.LessThan(startOrigin.y));
            Assert.That(midOrigin.y, Is.GreaterThan(finishOrigin.y * 0.85f), "Pickup must not detour into the upper-left corner.");

            Assert.That(finishOrigin.x + finishSize.x * 0.5f, Is.EqualTo(640f).Within(0.02f));
            Assert.That(finishOrigin.y + finishSize.y * 0.5f, Is.EqualTo(360f).Within(0.02f));
            Assert.That(Field<float>(finish, "Pitch"), Is.EqualTo(20f));
            Assert.That(Field<float>(finish, "Roll"), Is.EqualTo(0f));
            Assert.That(Field<float>(finish, "Scale"), Is.EqualTo(1.62f));
        }

        [Test]
        public void LayerReleaseIsOrderedAndBothPacketsHaveReturnedAtTheEnd()
        {
            Assert.That(Model, Is.Not.Null);
            Assert.That((int)Model.GetField("LayerCount").GetRawConstantValue(), Is.EqualTo(12));
            Assert.That((float)Model.GetField("PickupSeconds").GetRawConstantValue(), Is.EqualTo(0.9f));
            Assert.That((float)Model.GetField("ShuffleSeconds").GetRawConstantValue(), Is.EqualTo(5.4f));
            Assert.That((float)Model.GetField("SettleSeconds").GetRawConstantValue(), Is.EqualTo(0.4f));

            // During the riffle bridge (t = 0.36), layer 0 has already woven while layer 11 is still in its right packet.
            var first = Call("ShuffleLayer", 0, 0.36f, 1280f, 720f);
            var last = Call("ShuffleLayer", 11, 0.36f, 1280f, 720f);
            Assert.That(Mathf.Abs(Field<Vector3>(first, "Offset").x), Is.LessThan(Mathf.Abs(Field<Vector3>(last, "Offset").x)));

            // During the orbital cross-cut (t = 0.65), opposing packets visibly separate in 3D space.
            var orbitA = Call("ShuffleLayer", 0, 0.65f, 1280f, 720f);
            var orbitB = Call("ShuffleLayer", 1, 0.65f, 1280f, 720f);
            Assert.That(Vector3.Distance(Field<Vector3>(orbitA, "Offset"), Field<Vector3>(orbitB, "Offset")), Is.GreaterThan(20f));

            for (var i = 0; i < 12; i++)
            {
                var rest = Call("ShuffleLayer", i, 0f, 1280f, 720f);
                var end = Call("ShuffleLayer", i, 1f, 1280f, 720f);
                Assert.That(Field<Vector3>(end, "Offset"), Is.EqualTo(Field<Vector3>(rest, "Offset")));
                Assert.That(Field<float>(end, "Roll"), Is.Zero);
                Assert.That(Field<float>(end, "Pitch"), Is.Zero);
                Assert.That(Field<float>(end, "Yaw"), Is.Zero);
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
