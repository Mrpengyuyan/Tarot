using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 42 guards the two defects the played build exposed: the player must
    /// present at native resolution (a stretched 720p window was softening every
    /// glyph), and the menu must stay staged as a lit room rather than a flat-lit
    /// tabletop under a stock daylight skybox.
    /// </summary>
    public sealed class Phase42MenuElevationTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Materials = "Assets/Art/MidnightParlor/Materials";

        [Test]
        public void PlayerPresentsAtNativeResolution()
        {
            Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow),
                "a fixed-size fullscreen mode upscales and softens all text");
            Assert.That(PlayerSettings.resizableWindow, Is.True);
            Assert.That(PlayerSettings.macRetinaSupport, Is.True);
            Assert.That(PlayerSettings.defaultScreenWidth, Is.GreaterThanOrEqualTo(1920));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.GreaterThanOrEqualTo(1080));
        }

        [Test]
        public void MenuIsLitByItsCandlesNotBySky()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat),
                "skybox ambient lit this midnight room with a daylight sky");
            Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.2f),
                "ambient is a shadow floor, not a light source");
            Assert.That(RenderSettings.reflectionIntensity, Is.LessThan(0.2f));

            var key = GameObject.Find("Menu Key Light")?.GetComponent<Light>();
            Assert.That(key, Is.Not.Null);
            Assert.That(key.intensity, Is.LessThan(0.2f),
                "the directional reaches the far rim and bars it across frame");

            foreach (var name in new[] { "Phase8_LeftCandle", "Phase8_RightCandle" })
            {
                var candle = GameObject.Find(name);
                Assert.That(candle, Is.Not.Null, name);
                var light = candle.GetComponent<Light>();
                Assert.That(light, Is.Not.Null, name);
                Assert.That(light.intensity, Is.GreaterThan(2f), $"{name} must own the room");
                // The Light rides on the root, so the root sits at flame height and
                // the wax hangs below it - never re-zero this transform.
                Assert.That(candle.transform.position.y, Is.GreaterThan(0.3f),
                    $"{name} light must sit at the flame, not the table");
                Assert.That(Mathf.Abs(candle.transform.position.x), Is.GreaterThan(1.5f),
                    $"{name} must frame the composition, not stand in the centre");
            }
        }

        [Test]
        public void MenuCameraIsSeatedAndCinematic()
        {
            EditorSceneManager.OpenScene(ScenePath);

            // The menu camera is Untagged, so Camera.main is null here - same
            // fallback the bootstrapper and the Phase 21 test use.
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.fieldOfView, Is.InRange(28f, 45f),
                "60deg is Unity's wide-angle default and distorts the tabletop");
            Assert.That(camera.transform.eulerAngles.x, Is.GreaterThan(20f),
                "a shallow pitch compresses the velvet into a thin band");
        }

        [Test]
        public void StageCarriesTheCandleFillAndWashesAreRetired()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var fill = GameObject.Find("MP_MenuStage")?.transform.Find("MP_TableFill");
            Assert.That(fill, Is.Not.Null, "the unseen warm fill holds the gold and wax colour");
            Assert.That(fill.GetComponent<Light>(), Is.Not.Null);

            var canvas = GameObject.Find("MainMenuCanvas").transform;
            foreach (var name in new[]
            {
                "Phase7_MenuVignette", "Phase7_MenuBackdrop", "MenuBackdrop", "Phase8_MenuCrest",
            })
            {
                var wash = canvas.Find(name);
                Assert.That(wash, Is.Not.Null, $"{name} stays in the scene, deactivated");
                Assert.That(wash.gameObject.activeSelf, Is.False, name);
            }
        }

        [Test]
        public void VelvetAndWalnutSitDarkEnoughForGoldToRead()
        {
            var cloth = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_TableCloth.mat");
            Assert.That(cloth.GetColor("_BaseColor").r, Is.LessThan(0.35f),
                "a mid-red tint blows out to pool-table scarlet under the candle key");

            var wood = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_TableWood.mat");
            Assert.That(wood.GetColor("_BaseColor").maxColorComponent, Is.LessThan(0.5f),
                "near-white walnut picked up every bounce and barred the frame");

            var wax = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MP_CandleWax.mat");
            Assert.That(wax, Is.Not.Null);
            Assert.That(wax.GetColor("_EmissionColor").maxColorComponent, Is.GreaterThan(0.1f),
                "wax is translucent and lit from within; emission is what sells it");
        }

        [Test]
        public void Phase42DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            var text = File.ReadAllText(doc);
            Assert.That(text, Does.Contain("skybox"));
            Assert.That(text, Does.Contain("FullScreenWindow"));
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_backdrop.py"), Is.True);
        }
    }
}
