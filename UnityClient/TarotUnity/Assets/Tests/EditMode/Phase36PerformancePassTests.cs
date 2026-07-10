using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 36 guards the flip-path caching: the flip controller keeps cached
    /// references to the camera choreography and ritual feedback controllers instead
    /// of re-scanning the scene on every flip.
    /// </summary>
    public sealed class Phase36PerformancePassTests
    {
        private const string DocPath = "Docs/PHASE36_PERFORMANCE_PASS.md";

        [Test]
        public void CardFlipControllerCachesSceneReferences()
        {
            var type = typeof(CardFlipController);
            Assert.That(
                type.GetField("cameraChoreography", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null,
                "CardFlipController should cache the camera choreography reference");
            Assert.That(
                type.GetField("ritualFeedback", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null,
                "CardFlipController should cache the ritual feedback reference");
        }

        [Test]
        public void Phase36DocumentationExists()
        {
            Assert.That(System.IO.File.Exists(DocPath), Is.True, $"missing {DocPath}");
            var text = System.IO.File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("allocation-free"));
            Assert.That(text, Does.Contain("MTL_HUD_ENABLED"));
        }
    }
}
