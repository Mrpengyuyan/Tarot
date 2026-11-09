using System;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualPickLayoutTests
    {
        [TestCase(1280,720,0,58.0362f,348.7341f,189.662f,206.626f)]
        [TestCase(1280,720,19,588.2422f,281.0156f,103.5156f,180.1953f)]
        [TestCase(1280,720,77,1032.294f,434.8903f,189.662f,206.626f)]
        [TestCase(1024,768,0,46.6102f,365.4f,151.6722f,165.2382f)]
        [TestCase(1024,768,58,470.6094f,391.6563f,82.7813f,144.1016f)]
        public void FanMatchesMeasuredV6Bounds(int width,int height,int index,float x,float y,float w,float h)
        {
            var cards = Read<Array>(Compute(3,width,height),"Fan");
            Assert.That(cards.Length,Is.EqualTo(78));
            AssertRect(Read<Rect>(cards.GetValue(index),"Bounds"),new Rect(x,y,w,h));
        }

        [TestCase(3,1280,720,0,477.0703f,51.2578f,83.0625f,144.4063f)]
        [TestCase(3,1024,768,2,575.8672f,54.6172f,66.4297f,115.4844f)]
        [TestCase(10,1024,768,0,359.2109f,39.2969f,43.9453f,76.6484f)]
        [TestCase(10,1024,768,5,359.2109f,141.2188f,43.9453f,76.6484f)]
        public void RailMatchesMeasuredV6Slots(int count,int width,int height,int index,float x,float y,float w,float h)
        {
            var frame = Compute(count,width,height);
            var slots = Read<Rect[]>(frame,"Slots");
            Assert.That(slots.Length,Is.EqualTo(count));
            AssertRect(slots[index],new Rect(x,y,w,h));
        }

        [TestCase(1,1280,720)] [TestCase(3,1280,720)] [TestCase(10,1280,720)]
        [TestCase(1,1024,768)] [TestCase(3,1024,768)] [TestCase(10,1024,768)]
        public void DensityScalesAllCardGeometryWithoutChangingSelectionCount(int count,int width,int height)
        {
            var a=Compute(count,width,height);var b=Compute(count,width*2,height*2);
            foreach(var key in new[]{"Fan","Selected"})
            {
                var x=Read<Array>(a,key);var y=Read<Array>(b,key);
                Assert.That(y.Length,Is.EqualTo(x.Length));
                for(var i=0;i<x.Length;i++)
                {
                    var rect=Read<Rect>(x.GetValue(i),"Bounds");
                    AssertRect(Read<Rect>(y.GetValue(i),"Bounds"),new Rect(rect.position*2f,rect.size*2f),0.1f);
                }
            }
        }

        [Test]
        public void OrderLabelsSitJustBelowTheRailRatherThanAnotherLineTooLow()
        {
            var centers = Read<Vector2[]>(Compute(3,1280,720),"Labels");
            Assert.That(centers[0].y,Is.EqualTo(214f).Within(1f));
        }

        [Test]
        public void FlightUsesTheApprovedGlobalBezierCurve()
        {
            var type=RequireType();
            var ease=type.GetMethod("FlightEase",BindingFlags.Public|BindingFlags.Static);
            Assert.That(ease,Is.Not.Null);
            Assert.That((float)ease.Invoke(null,new object[]{0.25f}),Is.EqualTo(0.7228527f).Within(0.0001f));
            Assert.That((float)ease.Invoke(null,new object[]{0.5f}),Is.EqualTo(0.9315868f).Within(0.0001f));
            Assert.That((float)ease.Invoke(null,new object[]{0.75f}),Is.EqualTo(0.9881165f).Within(0.0001f));
        }

        private static Type RequireType()
        {
            var t=typeof(RitualReadingView).Assembly.GetType("TarotUnity.UI.RitualPickLayout");
            Assert.That(t,Is.Not.Null,"Calibrate the real pick phase from measured reference geometry.");
            return t;
        }
        private static object Compute(int count,float width,float height) => RequireType().GetMethod("Compute").Invoke(null,new object[]{count,width,height});
        private static T Read<T>(object value,string key)
        {
            var type=value.GetType();return (T)(type.GetField(key)?.GetValue(value)??type.GetProperty(key)?.GetValue(value));
        }
        private static void AssertRect(Rect a,Rect b,float tolerance=0.4f)
        {
            Assert.That(a.x,Is.EqualTo(b.x).Within(tolerance));Assert.That(a.y,Is.EqualTo(b.y).Within(tolerance));
            Assert.That(a.width,Is.EqualTo(b.width).Within(tolerance));Assert.That(a.height,Is.EqualTo(b.height).Within(tolerance));
        }
    }
}
