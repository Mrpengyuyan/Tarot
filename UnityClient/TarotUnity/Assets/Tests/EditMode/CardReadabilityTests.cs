using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class CardReadabilityTests
    {
        [Test]
        public void EveryCardHasMinificationFilteringWithoutLosingItsSprite()
        {
            var catalog = Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
            Assert.That(catalog.Entries.Count, Is.EqualTo(78));
            foreach (var entry in catalog.Entries)
            {
                var sprite = catalog.FindSprite(entry.key);
                Assert.That(sprite, Is.Not.Null, entry.key);
                var path = AssetDatabase.GetAssetPath(sprite);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.mipmapEnabled, Is.True, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Trilinear), path);
                Assert.That(importer.anisoLevel, Is.GreaterThanOrEqualTo(4), path);
                Assert.That(sprite.texture.mipmapCount, Is.GreaterThan(1), path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            }
        }

        [TestCase("Assets/Materials/MAT_HolographicCardFace.mat")]
        [TestCase("Assets/Materials/MAT_HolographicHeroCardUI.mat")]
        public void CardHighlightsCannotWashOutTheIllustration(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material, Is.Not.Null);
            Assert.That(material.GetFloat("_GlareIntensity"), Is.InRange(0f, 0.08f));
            Assert.That(material.GetFloat("_Iridescence"), Is.InRange(0f, 0.02f));
        }

        [TestCase(720f)]
        [TestCase(800f)]
        [TestCase(960f)]
        public void ThreeCardsAreLargeCompactAndLeaveRoomToRead(float height)
        {
            var layout = ResultSpreadLayout.Compute(3, height);
            Assert.That(layout.Rows, Is.EqualTo(1));
            Assert.That(234f * layout.Cells[0].Scale, Is.InRange(200f, 220f));
            Assert.That(layout.RowPitch, Is.InRange(220f, 260f));
            Assert.That(layout.Cells[0].Position.x, Is.EqualTo(-layout.Cells[2].Position.x));
            Assert.That(layout.Cells[1].Position.x, Is.Zero);
            Assert.That(layout.ReadingSize.x, Is.EqualTo(1024f));
            Assert.That(layout.ReadingSize.y, Is.GreaterThanOrEqualTo(230f));
            Assert.That(layout.BandBottom - layout.ReadingTop, Is.GreaterThanOrEqualTo(11.99f));
            Assert.That(layout.ReadingBottom, Is.EqualTo(-height / 2f + 94f).Within(0.01f));
        }
    }
}
