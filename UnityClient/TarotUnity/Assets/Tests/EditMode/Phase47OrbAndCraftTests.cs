using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 47 guards the three notes it answered - the orb reads as glass, the
    /// deck can be counted, the button has a frame - and the two additions that
    /// were pulled back out for cluttering the table.
    /// </summary>
    public sealed class Phase47OrbAndCraftTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Materials = "Assets/Art/MidnightParlor/Materials";
        private const string Sprites = "Assets/Art/MidnightParlor/Sprites";

        [Test]
        public void OrbIsGlassNotAMarble()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_OrbGlass.mat");
            Assert.That(mat, Is.Not.Null);
            Assert.That(mat.shader.name, Is.EqualTo("TarotUnity/ScryingOrb"),
                "a Lit sphere has no Fresnel rim, which is the whole tell of glass");
            Assert.That(mat.GetTexture("_InteriorTex")?.name, Is.EqualTo("OrbInterior"),
                "a crystal ball has something inside it");
            Assert.That(mat.GetFloat("_RimIntensity"), Is.GreaterThan(0f));
            Assert.That(mat.GetFloat("_InteriorDepth"), Is.GreaterThan(0f),
                "without a parallax offset the nebula is a decal on the surface");
        }

        [Test]
        public void OrbShaderAndInteriorExist()
        {
            Assert.That(File.Exists("Assets/Shaders/TarotScryingOrb.shader"), Is.True);
            Assert.That(File.Exists($"{Sprites}/OrbInterior.png"), Is.True);
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_orb.py"), Is.True);
            Assert.That(Shader.Find("TarotUnity/ScryingOrb"), Is.Not.Null, "orb shader failed to compile");
        }

        [Test]
        public void OrbHaloIsRetired()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var halo = GameObject.Find("MP_ScryingOrb")?.transform.Find("Halo");
            Assert.That(halo, Is.Not.Null, "kept in scene, deactivated");
            Assert.That(halo.gameObject.activeSelf, Is.False,
                "the halo propped up a marble that could not hold an edge; the Fresnel rim does it now");
        }

        [Test]
        public void DeckCanBeCounted()
        {
            var body = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_DeckBody.mat");
            Assert.That(body, Is.Not.Null);
            Assert.That(body.GetColor("_BaseColor").maxColorComponent, Is.GreaterThan(0.4f),
                "a near-black tint crushed the stacked card edges into one lump");
        }

        [Test]
        public void TheTableIsNotOverAdded()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.That(GameObject.Find("MP_MenuStage").transform.Find("MP_CardFan"), Is.Null,
                "the fan stood where the front-left candle stands, and repeated the deck");

            foreach (var candleName in new[]
            {
                "Phase8_LeftCandle", "Phase8_RightCandle", "MP_BackCandle_L", "MP_BackCandle_R",
            })
            {
                var candle = GameObject.Find(candleName);
                Assert.That(candle.transform.Find("Spill"), Is.Null,
                    $"{candleName} already carries a WaxPool; a spill made it two discs");
            }
        }

        [Test]
        public void Phase47DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("Fresnel"));
        }
    }
}
