using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 54 gives the dealt card a landing - a squash-and-recover on contact and
    /// a camera kick on the impact frame - so it arrives with weight instead of
    /// snapping dead onto the slot. These keep the landing tuning in a tasteful
    /// envelope; the deal pacing floors (duration, arc, interval) stay with Phase 9.
    /// </summary>
    public sealed class Phase54DealLandingTests
    {
        private const string DocPath = "Docs/PHASE54_DEAL_LANDING.md";

        [Test]
        public void LandingTuningStaysInATastefulEnvelope()
        {
            var probe = new GameObject("Phase54_DeckProbe");
            try
            {
                var deck = probe.AddComponent<DeckController>();
                var so = new SerializedObject(deck);

                Assert.That(so.FindProperty("landingSquash")?.floatValue, Is.InRange(0.04f, 0.2f),
                    "the squash should read as weight absorbed, not as a splat");
                Assert.That(so.FindProperty("landingSeconds")?.floatValue, Is.InRange(0.06f, 0.25f),
                    "the landing springs back quickly");
                Assert.That(so.FindProperty("landingCameraKick")?.floatValue, Is.InRange(0f, 0.08f),
                    "the touchdown kick is subtle - deals arrive in quick succession");
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void Phase54DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase54 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("landing"));
            Assert.That(text, Does.Contain("squash"));
        }
    }
}
