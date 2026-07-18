using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 52 gives the flip real weight - a wind-up, a whip through the reveal,
    /// a scale pop, and an overshoot settle - and lands the camera shake on the
    /// reveal instant. These guard the new tuning stays in a tasteful envelope so a
    /// future edit can't quietly turn the pop into a lurch. The scalar pacing floors
    /// (duration, lift, pauses) are still owned by Phase 9 and Phase 23.
    /// </summary>
    public sealed class Phase52FlipFeelTests
    {
        private const string DocPath = "Docs/PHASE52_FLIP_CAMERA_FEEL.md";

        [Test]
        public void WeightAndSnapTuningStaysInATastefulEnvelope()
        {
            var probe = new GameObject("Phase52_FlipProbe");
            try
            {
                var flip = probe.AddComponent<CardFlipController>();
                var so = new SerializedObject(flip);

                Assert.That(so.FindProperty("windBackAngle")?.floatValue, Is.InRange(4f, 20f),
                    "anticipation wind-back should read as a cocked pose, not a spin");
                Assert.That(so.FindProperty("windBackDip")?.floatValue, Is.InRange(0f, 0.06f),
                    "the wind-up dip is a subtle drop, not a plunge");
                Assert.That(so.FindProperty("settleOvershootAngle")?.floatValue, Is.InRange(2f, 12f),
                    "the landing overshoots enough to read, not enough to wobble");
                Assert.That(so.FindProperty("settleSeconds")?.floatValue, Is.InRange(0.05f, 0.25f),
                    "the settle damps quickly");
                Assert.That(so.FindProperty("revealScalePunch")?.floatValue, Is.InRange(0.02f, 0.12f),
                    "the reveal pop is emphasis, not a jump-scare");
                Assert.That(so.FindProperty("revealCameraShake")?.floatValue, Is.InRange(0.02f, 0.12f),
                    "the reveal must actually kick the camera so the impact reads");
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void Phase52DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase52 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("anticipation"));
            Assert.That(text, Does.Contain("overshoot"));
            Assert.That(text, Does.Contain("reveal"));
        }
    }
}
