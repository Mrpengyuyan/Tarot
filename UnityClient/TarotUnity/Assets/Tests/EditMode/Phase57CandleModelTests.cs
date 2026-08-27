using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 57 remodels the candles: the WaxPool/Body/Lip primitive stack becomes
    /// one lathed surface with a real profile. These guard the modelling contract -
    /// the resolution, the profile features that make it read as wax, and the fact
    /// that the geometry pass did not move the flame or the light that six phases
    /// spent tuning.
    /// </summary>
    public sealed class Phase57CandleModelTests
    {
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";
        private const string MeshFolder = "Assets/Models/Candles";

        private static readonly (string Scene, string[] Candles)[] Screens =
        {
            ("Assets/Scenes/MainMenu.unity", new[]
            {
                "Phase8_LeftCandle", "Phase8_RightCandle", "MP_BackCandle_L", "MP_BackCandle_R",
            }),
            ("Assets/Scenes/ReadingRoom.unity", new[]
            {
                "MP_RoomCandle_L", "MP_RoomCandle_R", "MP_RoomCandle_BackL", "MP_RoomCandle_BackR",
            }),
        };

        [Test]
        public void EveryCandleIsLathedRatherThanStackedFromPrimitives()
        {
            foreach (var (scenePath, candles) in Screens)
            {
                EditorSceneManager.OpenScene(scenePath);
                foreach (var name in candles)
                {
                    var candle = GameObject.Find(name);
                    Assert.That(candle, Is.Not.Null, $"{name} missing");

                    foreach (var superseded in new[] { "WaxPool", "Body", "Lip" })
                    {
                        Assert.That(candle.transform.Find(superseded), Is.Null,
                            $"{name} still carries the primitive {superseded}; it would double the silhouette");
                    }

                    var wax = candle.transform.Find("Wax");
                    Assert.That(wax, Is.Not.Null, $"{name} needs its lathed Wax mesh");

                    var mesh = wax.GetComponent<MeshFilter>()?.sharedMesh;
                    Assert.That(mesh, Is.Not.Null, $"{name} Wax has no mesh");

                    // Unity's built-in cylinder carries 88 vertices at 20 radial
                    // segments - 18 degrees per facet. Anything near that number
                    // means a primitive crept back in.
                    Assert.That(mesh.vertexCount, Is.GreaterThan(800),
                        $"{name} is back on a low-resolution mesh ({mesh.vertexCount} verts)");
                }
            }
        }

        [Test]
        public void TheProfileHasAPooledBaseAShoulderAndABurnCrater()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshFolder}/MP_RoomCandle_L_Wax.asset");
            Assert.That(mesh, Is.Not.Null, "the front room candle's wax mesh is missing");

            var vertices = mesh.vertices;
            var top = mesh.bounds.max.y;

            float RadiusNear(float y, float band)
            {
                var widest = 0f;
                foreach (var v in vertices)
                {
                    if (Mathf.Abs(v.y - y) <= band)
                    {
                        widest = Mathf.Max(widest, new Vector2(v.x, v.z).magnitude);
                    }
                }

                return widest;
            }

            var baseRadius = RadiusNear(0.004f, 0.004f);
            var midRadius = RadiusNear(top * 0.5f, top * 0.05f);
            var shoulderRadius = RadiusNear(top * 0.93f, top * 0.02f);

            Assert.That(baseRadius, Is.GreaterThan(midRadius * 1.2f),
                "the base must pool outward - that is the spill the old disc faked");
            Assert.That(shoulderRadius, Is.GreaterThan(midRadius),
                "the shoulder must swell below the burn instead of stepping out as a collar");

            // The crater: near the axis the surface sits below the rim, so the top
            // is a dish rather than the flat lid the old Lip presented.
            var craterFloor = float.MaxValue;
            foreach (var v in vertices)
            {
                if (new Vector2(v.x, v.z).magnitude < midRadius * 0.25f && v.y > top * 0.5f)
                {
                    craterFloor = Mathf.Min(craterFloor, v.y);
                }
            }

            Assert.That(craterFloor, Is.LessThan(top - 0.008f),
                "the top must dish down to the wick, not present a flat lid");
        }

        [Test]
        public void RemodellingDidNotMoveTheFlameOrTheLight()
        {
            // The Light lives on the candle root and the flame billboard sits at its
            // origin; the wax hangs below on negative offsets. If the geometry pass
            // shifted either, the room's lighting moves with it.
            foreach (var (scenePath, candles) in Screens)
            {
                EditorSceneManager.OpenScene(scenePath);
                foreach (var name in candles)
                {
                    var candle = GameObject.Find(name);
                    var flame = candle.transform.Find("Flame");
                    Assert.That(flame, Is.Not.Null, $"{name} lost its flame");
                    Assert.That(flame.localPosition.y, Is.EqualTo(0f).Within(0.0001f),
                        $"{name} flame must stay at the root, where the Light is");
                    Assert.That(candle.GetComponent<Light>(), Is.Not.Null,
                        $"{name} must still be a light source");

                    var wax = candle.transform.Find("Wax");
                    var mesh = wax.GetComponent<MeshFilter>().sharedMesh;
                    // The rim should sit just under the flame - the 0.085 gap Phase 42
                    // established between the wax top and the flame's centre.
                    var rimToFlame = -(wax.localPosition.y + mesh.bounds.max.y);
                    Assert.That(rimToFlame, Is.EqualTo(0.085f).Within(0.004f),
                        $"{name} flame no longer sits just above its wick");
                }
            }
        }

        [Test]
        public void Phase57DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing project chronicle at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("crater"));
            Assert.That(text, Does.Contain("lathe"));
        }
    }
}
