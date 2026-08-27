using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 59 replaces the candle flame billboard. The old CandleFlame.png was a
    /// 256px sprite imported Compressed, and a flame is a pure smooth gradient -
    /// the one content type that block compression turns into horizontal banding.
    /// It was also a mystery asset with no generator. These guard the fix: a
    /// high-resolution, uncompressed texture with a reproducible generator behind it.
    /// </summary>
    public sealed class Phase59FlameQualityTests
    {
        private const string TexturePath = "Assets/Art/MidnightParlor/Sprites/CandleFlame.png";
        private const string GeneratorPath = "Tools/UiKitGenerator/gen_flame.py";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

        [Test]
        public void TheFlameTextureIsHighResolution()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            Assert.That(tex, Is.Not.Null, $"flame texture missing at {TexturePath}");
            Assert.That(Mathf.Min(tex.width, tex.height), Is.GreaterThanOrEqualTo(512),
                "the 256px flame magnified to a soft-edged blob; 512+ holds the taper");
        }

        [Test]
        public void TheFlameTextureIsImportedUncompressed()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);

            // A flame is a pure gradient - block compression bands it into venetian
            // blinds. The default platform settings must not compress it.
            var defaults = importer.GetDefaultPlatformTextureSettings();
            Assert.That(defaults.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed),
                "the flame is a smooth gradient; compression banded it - keep it uncompressed");
        }

        [Test]
        public void TheFlameHasAReproducibleGenerator()
        {
            Assert.That(File.Exists(GeneratorPath), Is.True,
                $"the flame must be reproducible from {GeneratorPath}, like the wax maps");
        }

        [Test]
        public void Phase59DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing project chronicle at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("flame"));
            Assert.That(text, Does.Contain("compression"));
        }
    }
}
