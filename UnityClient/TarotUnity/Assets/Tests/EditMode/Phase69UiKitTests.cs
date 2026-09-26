using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the generated glass, card-stock and sparkle sprites.</summary>
    public sealed class Phase69UiKitTests
    {
        private const string Folder = "Assets/Art/MidnightParlor/Sprites";

        [TestCase("GlassPanel", 32f)]
        [TestCase("CardStock", 32f)]
        [TestCase("Sparkle", 0f)]
        public void SpriteImportsAsSingleWithItsBorder(string name, float border)
        {
            var path = $"{Folder}/{name}.png";
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spriteBorder, Is.EqualTo(new Vector4(border, border, border, border)));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));
        }

        [Test]
        public void GlassIsSmokyInsideAndClearInTheMargin()
        {
            var tex = Load("GlassPanel");
            Assert.That(tex.width, Is.EqualTo(96));
            var centre = tex.GetPixel(48, 48);
            Assert.That(centre.a, Is.EqualTo(0.52f).Within(0.02f));
            Assert.That(centre.r, Is.EqualTo(14f / 255f).Within(0.02f));
            Assert.That(tex.GetPixel(3, 48).a, Is.EqualTo(0f), "the 8 px margin is clear");
            var line = tex.GetPixel(8, 48);
            Assert.That(line.r, Is.GreaterThan(0.4f), "the outer gold hairline sits on the margin edge");
            var inner = tex.GetPixel(13, 48);
            Assert.That(inner.r, Is.GreaterThan(centre.r + 0.05f), "the faint inner line");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void CardStockIsIvoryWithADarkKeylineAndAShadow()
        {
            var tex = Load("CardStock");
            var centre = tex.GetPixel(48, 48);
            Assert.That(centre.a, Is.EqualTo(1f).Within(0.01f));
            Assert.That(centre.r, Is.GreaterThan(0.88f));
            Assert.That(tex.GetPixel(48, 78).r, Is.GreaterThan(tex.GetPixel(48, 18).r), "lighter at the top (rows clear of the keyline 3.5 px inside the edge)");
            Assert.That(tex.GetPixel(11, 48).r, Is.LessThan(0.5f), "keyline 3 px inside the stock edge");
            var shadow = tex.GetPixel(4, 40);
            Assert.That(shadow.a, Is.GreaterThan(0.05f).And.LessThan(0.6f));
            Assert.That(shadow.r, Is.LessThan(0.1f));
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void SparkleIsAFourPointStar()
        {
            var tex = Load("Sparkle");
            Assert.That(tex.width, Is.EqualTo(64));
            Assert.That(tex.GetPixel(32, 32).a, Is.GreaterThan(0.95f));
            Assert.That(tex.GetPixel(32, 50).a, Is.GreaterThan(0.5f), "an arm");
            Assert.That(tex.GetPixel(44, 44).a, Is.LessThan(0.05f), "the diagonal between arms");
            Assert.That(tex.GetPixel(1, 1).a, Is.LessThan(0.02f));
            Object.DestroyImmediate(tex);
        }

        private static Texture2D Load(string name)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes($"{Folder}/{name}.png"));
            return tex;
        }
    }
}
