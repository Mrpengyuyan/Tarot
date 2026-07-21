using System.IO;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 46 guards the candles as wax rather than tubing: the translucency
    /// map that makes the flame light the wax it sits in, and the flicker that
    /// keeps the flames from holding one value.
    /// </summary>
    public sealed class Phase46CandleCraftTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Sprites = "Assets/Art/MidnightParlor/Sprites";
        private const string Materials = "Assets/Art/MidnightParlor/Materials";

        [Test]
        public void WaxTexturesExist()
        {
            Assert.That(File.Exists($"{Sprites}/WaxColor.png"), Is.True);
            Assert.That(File.Exists($"{Sprites}/WaxEmission.png"), Is.True);
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_wax.py"), Is.True);

            // The wax grain wraps the cylinder's side, so it has to tile.
            var importer = AssetImporter.GetAtPath($"{Sprites}/WaxColor.png") as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
        }

        [Test]
        public void WaxIsLitThroughItsTranslucencyMap()
        {
            var wax = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_CandleWax.mat");
            Assert.That(wax, Is.Not.Null);
            Assert.That(wax.GetTexture("_BaseMap")?.name, Is.EqualTo("WaxColor"));
            Assert.That(wax.GetTexture("_EmissionMap")?.name, Is.EqualTo("WaxEmission"),
                "a flat emission over the whole stick is what made the candles read as tubing");
            Assert.That(wax.IsKeywordEnabled("_EMISSION"), Is.True);
        }

        [Test]
        public void EveryCandleFlickers()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var name in new[]
            {
                "Phase8_LeftCandle", "Phase8_RightCandle", "MP_BackCandle_L", "MP_BackCandle_R",
            })
            {
                var candle = GameObject.Find(name);
                Assert.That(candle, Is.Not.Null, name);

                var flicker = candle.GetComponent<CandleFlickerController>();
                Assert.That(flicker, Is.Not.Null, $"{name} must flicker; a still flame reads as a render");

                var so = new SerializedObject(flicker);
                Assert.That(so.FindProperty("flameLight").objectReferenceValue, Is.Not.Null, name);
                Assert.That(so.FindProperty("flameBillboard").objectReferenceValue, Is.Not.Null, name);
                Assert.That(so.FindProperty("baseIntensity").floatValue, Is.GreaterThan(0f),
                    $"{name} needs a base intensity to ride around");
            }
        }

        [Test]
        public void FlickerIsSeededPerCandle()
        {
            // Two candles flickering in unison is its own kind of wrong, so the
            // seed must be per-instance rather than a shared clock.
            var type = typeof(CandleFlickerController);
            Assert.That(type.GetField("seed", BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null, "each candle needs its own position in the noise field");
        }

        [Test]
        public void Phase46DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("translucen"));
        }
    }
}
