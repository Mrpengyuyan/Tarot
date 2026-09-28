using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 72: the draw ritual - three-beat shuffle, a fan the player picks from, picked cards fly to their slots.</summary>
    public sealed class Phase72DrawRitualTests
    {
        [Test]
        public void ShuffleHasCutAndRiffleKnobsInATastefulEnvelope()
        {
            var probe = new GameObject("Phase72_StackProbe");
            try
            {
                var so = new SerializedObject(probe.AddComponent<DeckShuffleChoreographer>());
                Assert.That(so.FindProperty("cutSpread")?.floatValue, Is.InRange(0.2f, 0.6f), "piles clear each other");
                Assert.That(so.FindProperty("cutSeconds")?.floatValue, Is.InRange(0.2f, 0.45f));
                Assert.That(so.FindProperty("recutSeconds")?.floatValue, Is.InRange(0.1f, 0.3f));
                Assert.That(so.FindProperty("cutYawDegrees")?.floatValue, Is.InRange(2f, 12f));
                Assert.That(so.FindProperty("riffleBendDegrees")?.floatValue, Is.InRange(4f, 20f), "inner edges lift");
                Assert.That(so.FindProperty("secondRiffleSpeedup")?.floatValue, Is.InRange(1.05f, 1.5f));
                Assert.That(so.FindProperty("squareHoldSeconds")?.floatValue, Is.InRange(0.05f, 0.3f));
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }
    }
}
