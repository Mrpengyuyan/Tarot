using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 37 guards the Midnight Parlor asset foundation: CC0 PBR surfaces and
    /// the composed gold UI kit are present, imported with the right settings, and
    /// baked into the URP materials the redesign phases consume.
    /// </summary>
    public sealed class Phase37AssetFoundationTests
    {
        // Mirrors Phase37AssetFoundationBootstrapper (editor assembly is not
        // referenceable from the test asmdef, so the spec values live here too).
        private const string Textures = "Assets/Art/MidnightParlor/Textures";
        private const string Sprites = "Assets/Art/MidnightParlor/Sprites";
        private const string Materials = "Assets/Art/MidnightParlor/Materials";

        private static Vector4 ExpectedBorder(string name) => name switch
        {
            "TarotPanel" => new Vector4(96f, 96f, 96f, 96f),
            "TarotPanelSubtle" => new Vector4(56f, 56f, 56f, 56f),
            "TarotButton" => new Vector4(72f, 56f, 72f, 56f),
            "TarotParchment" => new Vector4(120f, 120f, 120f, 120f),
            _ => Vector4.zero,
        };

        [Test]
        public void SurfaceTexturesExist()
        {
            foreach (var file in new[]
            {
                "Fabric034_1K-JPG_Color.jpg", "Fabric034_1K-JPG_NormalGL.jpg", "Fabric034_1K-JPG_Roughness.jpg",
                "Wood051_1K-JPG_Color.jpg", "Wood051_1K-JPG_NormalGL.jpg", "Wood051_1K-JPG_Roughness.jpg",
            })
            {
                Assert.That(File.Exists($"{Textures}/{file}"), Is.True, $"missing {file}");
            }
        }

        [Test]
        public void UiKitSpritesExist()
        {
            foreach (var file in new[]
            {
                "TarotCardBack.png", "TarotPanel.png", "TarotPanelSubtle.png", "TarotButton.png",
                "TarotMedallion.png", "TarotParchment.png", "TarotDivider.png", "TarotSocket.png", "TarotGlow.png",
            })
            {
                Assert.That(File.Exists($"{Sprites}/{file}"), Is.True, $"missing {file}");
            }
        }

        [Test]
        public void NormalMapsImportAsNormalMaps()
        {
            foreach (var file in new[] { "Fabric034_1K-JPG_NormalGL.jpg", "Wood051_1K-JPG_NormalGL.jpg" })
            {
                var importer = AssetImporter.GetAtPath($"{Textures}/{file}") as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"no importer for {file}");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.NormalMap), file);
            }
        }

        [Test]
        public void NineSliceSpritesCarryBorders()
        {
            foreach (var name in new[] { "TarotPanel", "TarotPanelSubtle", "TarotButton", "TarotParchment" })
            {
                var importer = AssetImporter.GetAtPath($"{Sprites}/{name}.png") as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"no importer for {name}");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spriteBorder,
                    Is.EqualTo(ExpectedBorder(name)), name);
                Assert.That(importer.spriteBorder.sqrMagnitude, Is.GreaterThan(0f), name);
            }
        }

        [Test]
        public void MaterialsReferenceTheirTextures()
        {
            var cloth = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_TableCloth.mat");
            Assert.That(cloth, Is.Not.Null, "MP_TableCloth missing");
            Assert.That(cloth.GetTexture("_BaseMap")?.name, Is.EqualTo("Fabric034_1K-JPG_Color"));
            Assert.That(cloth.GetTexture("_BumpMap")?.name, Is.EqualTo("Fabric034_1K-JPG_NormalGL"));

            var wood = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_TableWood.mat");
            Assert.That(wood, Is.Not.Null, "MP_TableWood missing");
            Assert.That(wood.GetTexture("_BaseMap")?.name, Is.EqualTo("Wood051_1K-JPG_Color"));

            var back = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_CardBack.mat");
            Assert.That(back, Is.Not.Null, "MP_CardBack missing");
            Assert.That(back.GetTexture("_BaseMap")?.name, Is.EqualTo("TarotCardBack"));

            var socket = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_CardSocket.mat");
            Assert.That(socket, Is.Not.Null, "MP_CardSocket missing");
            Assert.That(socket.renderQueue, Is.GreaterThanOrEqualTo(3000), "socket should render transparent");
        }

        [Test]
        public void LicenseRecordAndBlueprintExist()
        {
            Assert.That(File.Exists("Docs/THIRD_PARTY_ASSETS.md"), Is.True);
            var licenses = File.ReadAllText("Docs/THIRD_PARTY_ASSETS.md");
            Assert.That(licenses, Does.Contain("ambientCG"));
            Assert.That(licenses, Does.Contain("CC0"));

            Assert.That(File.Exists("Docs/PHASE37_VISUAL_REDESIGN_BLUEPRINT.md"), Is.True);
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_cardback.py"), Is.True);
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_uikit.py"), Is.True);
        }
    }
}
