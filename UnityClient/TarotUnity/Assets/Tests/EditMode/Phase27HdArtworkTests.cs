using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase27HdArtworkTests
    {
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string HdFolder = "Assets/Art/Tarot/RWS1909_HD";
        private const string DocPath = "Docs/PHASE27_HD_ARTWORK.md";
        private const int HdHeightFloor = 1000; // old scans were 666 tall; HD are ~1280.

        private static IEnumerable<string> AllCardKeys()
        {
            for (var n = 0; n <= 21; n++)
            {
                yield return $"major_{n:00}";
            }

            foreach (var suit in new[] { "cups", "pentacles", "swords", "wands" })
            {
                for (var n = 1; n <= 14; n++)
                {
                    yield return $"{suit}_{n:00}";
                }
            }
        }

        [Test]
        public void CatalogResolvesAllSeventyEightCards()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "Catalog asset missing");
            Assert.That(catalog.Entries.Count, Is.EqualTo(78), "Full deck should be 78 cards");

            foreach (var key in AllCardKeys())
            {
                Assert.That(catalog.FindSprite(key), Is.Not.Null, $"No artwork resolved for {key}");
            }
        }

        [Test]
        public void EveryCardSpriteIsHdFromTheHdFolder()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            foreach (var key in AllCardKeys())
            {
                var sprite = catalog.FindSprite(key);
                Assert.That(sprite, Is.Not.Null, key);
                Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(HdHeightFloor),
                    $"{key} is not HD (height {sprite.texture.height})");

                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(path, Does.StartWith(HdFolder),
                    $"{key} should be wired to the HD folder, not the legacy art (got {path})");
            }
        }

        [Test]
        public void HdImportSettingsAreSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HdFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), $"{path} should import as Sprite");
                Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(2048), $"{path} should keep HD resolution");
            }
        }

        [Test]
        public void Phase27DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase27 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("HD"));
            Assert.That(text, Does.Contain("public domain"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}
