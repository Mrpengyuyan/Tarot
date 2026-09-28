using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 73: the user's second pass on the draw - a face-up card that is just the card and its
    /// art (no glaring legacy layers), no more drifting yellow sparks, a deck that clears every
    /// slot, a two-second shuffle, and the whole deck (78 cards) in two arcs under a lit table.
    /// </summary>
    public sealed class Phase73DrawPolishTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        // Layers from Phases 7-15 stacked on the face: an ivory slab, gold bars, glow panels, a band.
        private static readonly string[] LegacyFaceLayers =
        {
            "Front", "FrontFrame", "InnerGlow", "Phase7_TitleBand", "Top", "Bottom", "Left", "Right",
            "Phase12_FaceArtworkLabel", "Phase14_RevealGlow", "Phase15_CardFacePlane",
        };

        [Test]
        public void TheFaceIsJustTheCardAndItsArt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var name in LegacyFaceLayers)
            {
                var r = renderers.FirstOrDefault(x => x.name == name);
                Assert.That(r, Is.Not.Null, $"control: {name} is still in the prefab");
                Assert.That(r.enabled, Is.False, $"{name} no longer draws");
            }

            foreach (var name in new[] { "Phase15_CardBody", "Phase12_FaceArtworkFrame", "Phase12_FaceArtworkPlaceholder" })
            {
                Assert.That(renderers.First(x => x.name == name).enabled, Is.True, $"{name} still draws");
            }

            Assert.That(new SerializedObject(prefab.GetComponent<ThreeDCardPresentationController>())
                .FindProperty("cardFaceRenderer").objectReferenceValue, Is.Null, "nothing turns the face plane back on");
            Assert.That(new SerializedObject(prefab.GetComponent<DimensionalCardRevealController>())
                .FindProperty("revealGlowRenderer").objectReferenceValue, Is.Null, "nothing turns the reveal glow back on");
        }

        [Test]
        public void TheOldRitualParticlesAreSilent()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(systems.Length, Is.GreaterThanOrEqualTo(7), "control: the Phase 8/18 systems are still there");
            foreach (var ps in systems)
            {
                Assert.That(ps.emission.enabled, Is.False, $"{ps.name} emits nothing");
                Assert.That(ps.main.playOnAwake, Is.False, ps.name);
                Assert.That(ps.GetComponent<ParticleSystemRenderer>().enabled, Is.False, $"{ps.name} draws nothing");
            }
        }

        [Test]
        public void TheDeckClearsEverySlot()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var deck = DeckBounds();
            var sockets = GameObject.Find("MP_TableStage").GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.StartsWith("MP_Socket_")).ToArray();
            Assert.That(sockets.Length, Is.EqualTo(14), "control: 1 + 3 + 10 sockets");
            foreach (var socket in sockets)
            {
                var b = socket.bounds;
                var gapX = Mathf.Max(b.min.x - deck.max.x, deck.min.x - b.max.x);
                var gapZ = Mathf.Max(b.min.z - deck.max.z, deck.min.z - b.max.z);
                Assert.That(Mathf.Max(gapX, gapZ), Is.GreaterThan(0.15f), $"the deck clears {socket.name}");
            }
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        public void TheDeckIsInViewWhereItMatters(float aspect)
        {
            EditorSceneManager.OpenScene(ScenePath);
            var deck = DeckBounds();
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            var so = new SerializedObject(choreography);
            var poses = new List<(string, Transform, float)>();
            // The room's opening view, the shuffle, and the draw (the fan spreads out of the deck).
            // The one- and three-card flip poses are close-ups of the slots and may crop it.
            foreach (var (pose, fov) in new[] { ("defaultPose", "defaultFov"), ("deckPose", "deckFov") })
            {
                poses.Add((pose, (Transform)so.FindProperty(pose).objectReferenceValue, so.FindProperty(fov).floatValue));
            }

            foreach (var list in new[] { "drawPoses" })
            {
                var array = so.FindProperty(list);
                for (var i = 0; i < array.arraySize; i++)
                {
                    var entry = array.GetArrayElementAtIndex(i);
                    poses.Add(($"{list}[{i}]", (Transform)entry.FindPropertyRelative("pose").objectReferenceValue, entry.FindPropertyRelative("fov").floatValue));
                }
            }

            var go = new GameObject("Phase73_DeckProbe");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.aspect = aspect;
                foreach (var (name, pose, fov) in poses)
                {
                    cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
                    cam.fieldOfView = fov;
                    foreach (var corner in Corners(deck))
                    {
                        var v = cam.WorldToViewportPoint(corner);
                        Assert.That(v.z > 0f && v.x > 0.01f && v.x < 0.99f && v.y > 0.01f && v.y < 0.99f, Is.True,
                            $"the deck is in view from {name} at aspect {aspect:0.00} (corner at {v})");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TheShuffleTakesAboutTwoSeconds()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var shuffle = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            Assert.That(shuffle.PlannedSeconds, Is.InRange(1.9f, 2.1f));
        }

        [Test]
        public void TheFanHoldsTheWholeDeckInTwoArcs()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            Assert.That(fan.CardCount, Is.EqualTo(78), "a full tarot deck");
            Assert.That(fan.RowCount, Is.EqualTo(2));

            var rows = new[] { new List<Vector3>(), new List<Vector3>() };
            for (var i = 0; i < fan.CardCount; i++)
            {
                fan.GetFanPose(i, out var p, out _);
                rows[fan.RowOf(i)].Add(p);
            }

            Assert.That(rows[0].Count, Is.EqualTo(39));
            Assert.That(rows[1].Count, Is.EqualTo(39));
            foreach (var row in rows)
            {
                for (var i = 1; i < row.Count; i++)
                {
                    var gap = Vector2.Distance(new Vector2(row[i].x, row[i].z), new Vector2(row[i - 1].x, row[i - 1].z));
                    Assert.That(gap, Is.InRange(0.17f, 0.3f), "each card shows a strip wide enough to pick");
                }
            }

            var closest = rows[0].SelectMany(a => rows[1].Select(b => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)))).Min();
            Assert.That(closest, Is.GreaterThan(1.3f), "the two arcs do not overlap (a face-down card is 1.18 long)");
        }

        [Test]
        public void TheFanClearsEverySlotAndStaysOnTheCloth()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var sockets = GameObject.Find("MP_TableStage").GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.StartsWith("MP_Socket_")).Select(r => r.bounds).ToArray();
            var cloth = GameObject.Find("MP_TableCloth").GetComponent<Renderer>().bounds;
            for (var i = 0; i < fan.CardCount; i++)
            {
                fan.GetFanPose(i, out var p, out var r);
                foreach (var sx in new[] { -0.39f, 0.39f })
                {
                    foreach (var sz in new[] { -0.59f, 0.59f })
                    {
                        var corner = p + r * new Vector3(sx, 0f, sz);
                        Assert.That(corner.x > cloth.min.x && corner.x < cloth.max.x && corner.z > cloth.min.z && corner.z < cloth.max.z,
                            Is.True, $"card {i} lies on the cloth");
                        foreach (var socket in sockets)
                        {
                            var inside = corner.x > socket.min.x - 0.05f && corner.x < socket.max.x + 0.05f
                                && corner.z > socket.min.z - 0.05f && corner.z < socket.max.z + 0.05f;
                            Assert.That(inside, Is.False, $"card {i} clears the slot at {socket.center}");
                        }
                    }
                }
            }
        }

        [Test]
        public void TheFanLightCoversBothArcs()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var light = (Light)new SerializedObject(fan).FindProperty("fanLight").objectReferenceValue;
            for (var i = 0; i < fan.CardCount; i++)
            {
                fan.GetFanPose(i, out var p, out _);
                var angle = Vector3.Angle(light.transform.forward, p - light.transform.position);
                Assert.That(angle, Is.LessThan(light.spotAngle * 0.5f * 0.85f), $"card {i} is inside the light pool");
                Assert.That(Vector3.Distance(light.transform.position, p), Is.LessThan(light.range * 0.8f));
            }
        }

        // The user: the fan's surroundings were black - make them table. Every ray the draw camera
        // casts lands on cloth, inside the side rims.
        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        public void TheTableFillsTheDrawView(float aspect)
        {
            EditorSceneManager.OpenScene(ScenePath);
            var cloths = new[] { "MP_TableCloth", "MP_TableClothNear" }.Select(n => GameObject.Find(n)).ToArray();
            Assert.That(cloths.All(c => c != null), Is.True, "the cloth and its near-side extension");
            var left = GameObject.Find("MP_TableRimLeft").GetComponent<Renderer>().bounds;
            var right = GameObject.Find("MP_TableRimRight").GetComponent<Renderer>().bounds;
            var near = cloths.Min(c => c.GetComponent<Renderer>().bounds.min.z);
            var far = cloths.Max(c => c.GetComponent<Renderer>().bounds.max.z);
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            var go = new GameObject("Phase73_TableProbe");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.aspect = aspect;
                foreach (var count in new[] { 1, 3, 10 })
                {
                    choreography.TryGetDrawPose(count, out var pose, out var fov);
                    cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
                    cam.fieldOfView = fov;
                    for (var vx = 0.02f; vx <= 0.99f; vx += 0.12f)
                    {
                        for (var vy = 0.02f; vy <= 0.99f; vy += 0.12f)
                        {
                            var ray = cam.ViewportPointToRay(new Vector3(vx, vy, 0f));
                            Assert.That(ray.direction.y, Is.LessThan(0f), "the view looks down at the table");
                            var hit = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                            var onTable = hit.x > left.min.x && hit.x < right.max.x && hit.z > near && hit.z < far;
                            Assert.That(onTable, Is.True, $"the draw view ({count} cards, aspect {aspect:0.00}) at ({vx:0.00}, {vy:0.00}) lands on the table, not at {hit}");
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Bounds DeckBounds()
        {
            var renderers = GameObject.Find("MP_DeckStack").GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers)
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            foreach (var x in new[] { b.min.x, b.max.x })
            {
                foreach (var y in new[] { b.min.y, b.max.y })
                {
                    foreach (var z in new[] { b.min.z, b.max.z })
                    {
                        yield return new Vector3(x, y, z);
                    }
                }
            }
        }
    }
}
