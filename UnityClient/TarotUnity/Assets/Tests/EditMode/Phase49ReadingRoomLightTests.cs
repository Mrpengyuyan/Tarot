using System.IO;
using NUnit.Framework;
using TarotUnity.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 49 guards the ReadingRoom's lighting contract - the same one the menu
    /// has carried since Phase 42, now that both screens share it. The room must be
    /// lit by things that are in it, not by a daylight sky.
    /// </summary>
    public sealed class Phase49ReadingRoomLightTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";

        private static readonly string[] RoomCandles =
        {
            "MP_RoomCandle_L", "MP_RoomCandle_R", "MP_RoomCandle_BackL", "MP_RoomCandle_BackR",
        };

        [Test]
        public void RoomIsLitByItsCandlesNotBySky()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat),
                "skybox ambient lit this midnight room with a daylight sky");
            Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.2f),
                "ambient is a shadow floor, not a light source");
            Assert.That(RenderSettings.reflectionIntensity, Is.LessThan(0.2f));

            var key = GameObject.Find("Table Key Light")?.GetComponent<Light>();
            Assert.That(key, Is.Not.Null);
            Assert.That(key.intensity, Is.LessThan(0.2f),
                "the directional reaches the far rim and bars it across the frame");
        }

        [Test]
        public void TheRoomContainsItsOwnLightSources()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var name in RoomCandles)
            {
                var candle = GameObject.Find(name);
                Assert.That(candle, Is.Not.Null,
                    $"{name} missing - seven point lights lit this table from nowhere");
                Assert.That(candle.GetComponent<Light>(), Is.Not.Null, name);
                Assert.That(candle.transform.Find("Flame"), Is.Not.Null, $"{name} needs a visible flame");
                Assert.That(candle.GetComponent<CandleFlickerController>(), Is.Not.Null,
                    $"{name} must flicker, like the menu's");
            }

            var fill = GameObject.Find("MP_TableStage")?.transform.Find("MP_RoomFill");
            Assert.That(fill?.GetComponent<Light>(), Is.Not.Null, "the unseen warm fill holds the gold");
        }

        [Test]
        public void CandlesStayInsideTheFrameAndClearOfTheCards()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            camera.aspect = 16f / 9f;

            foreach (var name in RoomCandles)
            {
                var candle = GameObject.Find(name);
                var vp = camera.WorldToViewportPoint(candle.transform.position);
                Assert.That(vp.z, Is.GreaterThan(0f), $"{name} is behind the camera");
                Assert.That(vp.x, Is.InRange(0.02f, 0.98f),
                    $"{name} stands on the frame's cut - the first pass put the front pair at exactly x=+-3.34");
            }

            // The card row lives at x -1.45..1.45; candles must frame it, not stand in it.
            foreach (var name in new[] { "MP_RoomCandle_L", "MP_RoomCandle_R" })
            {
                var x = GameObject.Find(name).transform.position.x;
                Assert.That(Mathf.Abs(x), Is.GreaterThan(2.2f), $"{name} crowds the card row");
            }
        }

        [Test]
        public void LegacyLightsNoLongerFightTheCandles()
        {
            EditorSceneManager.OpenScene(ScenePath);

            // Kept (the reveal/flip phases reference them by name) but dropped to a
            // floor: tuned against a daylight skybox, they stack into a flat wash.
            foreach (var name in new[]
            {
                "Ritual Ember Light", "Moon Fill Light", "Phase12_FocusedCardLight",
                "Phase14_RevealLightWarm", "Phase15_WarmKeyLight", "Phase15_CoolRimLight",
            })
            {
                var light = GameObject.Find(name)?.GetComponent<Light>();
                Assert.That(light, Is.Not.Null, $"{name} must survive - later phases reference it");
                Assert.That(light.intensity, Is.LessThan(0.7f), $"{name} out-shouts the candles");
            }
        }

        [Test]
        public void Phase49DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("skybox"));
        }
    }
}
