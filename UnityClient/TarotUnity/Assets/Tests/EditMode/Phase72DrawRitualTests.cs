using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 72: the draw ritual - three-beat shuffle, a fan the player picks from, picked cards fly to their slots.</summary>
    public sealed class Phase72DrawRitualTests
    {
        [Test]
        public void ShuffleHasCutAndRiffleKnobsInATastefulEnvelope()
        {
            var probe = new GameObject("Phase72_StackProbe");
            try
            {
                var so = new SerializedObject(probe.AddComponent<DeckShuffleChoreographer>());
                Assert.That(so.FindProperty("cutSpread")?.floatValue, Is.InRange(0.2f, 0.6f), "piles clear each other");
                Assert.That(so.FindProperty("cutSeconds")?.floatValue, Is.InRange(0.2f, 0.45f));
                Assert.That(so.FindProperty("recutSeconds")?.floatValue, Is.InRange(0.1f, 0.3f));
                Assert.That(so.FindProperty("cutYawDegrees")?.floatValue, Is.InRange(2f, 12f));
                Assert.That(so.FindProperty("riffleBendDegrees")?.floatValue, Is.InRange(4f, 20f), "inner edges lift");
                Assert.That(so.FindProperty("secondRiffleSpeedup")?.floatValue, Is.InRange(1.05f, 1.5f));
                Assert.That(so.FindProperty("squareHoldSeconds")?.floatValue, Is.InRange(0.05f, 0.3f));
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private static CardView SpawnCard()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
            var card = Object.Instantiate(prefab);
            card.SetFaceUp(false);
            return card;
        }

        private static Transform Halo(CardView card)
        {
            return card.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Highlight");
        }

        private static void CallAwake(Component component)
        {
            component.GetType().GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(component, null);
        }

        [Test]
        public void HoverHaloScaleIsAdjustableForTheFan()
        {
            var card = SpawnCard();
            try
            {
                var halo = Halo(card);
                card.SetHighlighted(true);
                var rest = halo.localScale;
                card.HoverHaloScale = 1f;
                card.SetHovered(true);
                Assert.That(halo.localScale.x, Is.EqualTo(rest.x).Within(1e-4f), "a fan card's hover keeps the rest halo");
                card.ClearHoverHaloScale();
                Assert.That(halo.localScale.x, Is.GreaterThan(rest.x * 1.05f), "back to the landed card's wider hover (prefab: 1.12)");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void HaloBoostShowsAndWidensTheGlow()
        {
            var card = SpawnCard();
            try
            {
                var halo = Halo(card);
                card.SetHighlighted(true);
                var rest = halo.localScale;
                card.SetHighlighted(false);
                Assert.That(halo.gameObject.activeSelf, Is.False, "control: no glow at rest");

                card.SetHaloBoost(1.6f);
                Assert.That(halo.gameObject.activeSelf, Is.True, "a boost lights the glow on its own");
                Assert.That(halo.localScale.x, Is.EqualTo(rest.x * 1.6f).Within(1e-3f));

                card.SetHaloBoost(1f);
                Assert.That(halo.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ClickHandlerReportsHoverChanges()
        {
            var card = SpawnCard();
            try
            {
                var handler = card.GetComponent<CardClickHandler>();
                CallAwake(handler);
                var seen = new System.Collections.Generic.List<bool>();
                handler.HoverChanged += (_, on) => seen.Add(on);
                handler.OnPointerEnter(null);
                handler.OnPointerExit(null);
                Assert.That(seen, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ResumeLetsTheTiltRecaptureItsRest()
        {
            var card = SpawnCard();
            try
            {
                var tilt = card.GetComponent<CardHoverTiltController>();
                CallAwake(tilt);
                tilt.Suspend();
                tilt.HoverEnter();
                Assert.That(tilt.IsHovering, Is.False, "control: suspended");

                card.transform.position = new Vector3(1f, 0f, 2f);
                tilt.Resume();
                tilt.HoverEnter();
                Assert.That(tilt.IsSuspended, Is.False);
                Assert.That(tilt.IsHovering, Is.True);
                tilt.ReleaseImmediate();
                Assert.That(card.transform.position, Is.EqualTo(new Vector3(1f, 0f, 2f)), "the new rest, not the old one");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }
        [Test]
        public void FanPosesSpanTheArcAndLayerLeftToRight()
        {
            var root = new GameObject("Phase72_FanProbe");
            try
            {
                var fan = root.AddComponent<SpreadFanController>();
                var center = new GameObject("FanCenter").transform;
                center.SetParent(root.transform, false);
                var so = new SerializedObject(fan);
                so.FindProperty("fanCenter").objectReferenceValue = center;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Phase 73: the whole deck in two arcs; each arc spans its angle and layers left to right.
                Assert.That(fan.CardCount, Is.EqualTo(78));
                var first = 0;
                for (var row = 0; row < fan.RowCount; row++)
                {
                    var count = Enumerable.Range(0, fan.CardCount).Count(i => fan.RowOf(i) == row);
                    var last = first + count - 1;
                    fan.GetFanPose(first, out var a, out var aRot);
                    fan.GetFanPose(last, out var b, out var bRot);
                    fan.GetFanPose(first + count / 2, out var mid, out _);
                    fan.GetFanPose(first + 1, out var second, out _);
                    Assert.That(a.x, Is.LessThan(0f));
                    Assert.That(b.x, Is.GreaterThan(0f));
                    Assert.That(mid.z, Is.GreaterThan(a.z), "the arc bows toward the slots");
                    Assert.That(b.y, Is.GreaterThan(a.y), "later cards lie on top");
                    Assert.That(Quaternion.Angle(aRot, bRot), Is.EqualTo(fan.RowArcDegrees(row)).Within(0.5f));
                    Assert.That(fan.RowArcDegrees(row), Is.InRange(60f, 110f));
                    var gap = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(second.x, 0, second.z));
                    Assert.That(gap, Is.InRange(0.17f, 0.3f), "each card shows a strip wide enough to pick");
                    first = last + 1;
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void DrawPoseFallsBackToTheFirstEntry()
        {
            var go = new GameObject("Phase72_CamProbe");
            try
            {
                var cam = go.AddComponent<CameraChoreographyController>();
                var near = new GameObject("Near").transform;
                var far = new GameObject("Far").transform;
                near.SetParent(go.transform);
                far.SetParent(go.transform);
                var so = new SerializedObject(cam);
                var poses = so.FindProperty("drawPoses");
                poses.arraySize = 2;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("cardCount").intValue = 3;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("pose").objectReferenceValue = near;
                poses.GetArrayElementAtIndex(0).FindPropertyRelative("fov").floatValue = 45f;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("cardCount").intValue = 10;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("pose").objectReferenceValue = far;
                poses.GetArrayElementAtIndex(1).FindPropertyRelative("fov").floatValue = 50f;
                so.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(cam.TryGetDrawPose(10, out var p10, out var f10), Is.True);
                Assert.That(p10, Is.SameAs(far));
                Assert.That(f10, Is.EqualTo(50f));
                Assert.That(cam.TryGetDrawPose(5, out var p5, out _), Is.True);
                Assert.That(p5, Is.SameAs(near), "unknown counts use the first entry");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FocusSocketLightsOnlyThatSocket()
        {
            var go = new GameObject("Phase72_StepProbe");
            try
            {
                var flowGo = new GameObject("Flow");
                flowGo.transform.SetParent(go.transform);
                var flow = flowGo.AddComponent<ReadingFlowController>();
                flow.SelectSpread(1, 3);
                var glows = Enumerable.Range(0, 3).Select(i => new GameObject($"Glow{i}")).ToArray();
                foreach (var g in glows)
                {
                    g.transform.SetParent(go.transform);
                }

                var indicator = go.AddComponent<RitualStepIndicator>();
                var so = new SerializedObject(indicator);
                so.FindProperty("flowController").objectReferenceValue = flow;
                var sets = so.FindProperty("socketGlowSets");
                sets.arraySize = 1;
                sets.GetArrayElementAtIndex(0).FindPropertyRelative("cardCount").intValue = 3;
                var arr = sets.GetArrayElementAtIndex(0).FindPropertyRelative("glows");
                arr.arraySize = 3;
                for (var i = 0; i < 3; i++)
                {
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = glows[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                indicator.ApplyFlowState(ReadingFlowState.Drawing);
                Assert.That(glows.All(g => g.activeSelf), Is.True, "control: the draw lights the spread");

                indicator.FocusSocket(1);
                Assert.That(glows.Select(g => g.activeSelf), Is.EqualTo(new[] { false, true, false }));
                Assert.That(indicator.FocusedSocket, Is.EqualTo(1));

                indicator.FocusSocket(-1);
                Assert.That(glows.All(g => g.activeSelf), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";

        [Test]
        public void TheCardCarriesAQuietFlightTrail()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var trail = prefab.GetComponentInChildren<TrailRenderer>(true);
            Assert.That(trail, Is.Not.Null);
            Assert.That(trail.emitting, Is.False, "only the flight emits");
            Assert.That(trail.sharedMaterial.name, Is.EqualTo("MP_WarmGlow"));
            Assert.That(trail.time, Is.InRange(0.2f, 0.4f));
            Assert.That(trail.widthCurve.Evaluate(1f), Is.LessThan(trail.widthCurve.Evaluate(0f)), "it tapers");
        }

        [Test]
        public void TheRoomHasAFanWiredToTheController()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            Assert.That(fan, Is.Not.Null);
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var so = new SerializedObject(room);
            Assert.That(so.FindProperty("spreadFan").objectReferenceValue, Is.SameAs(fan));
            Assert.That(so.FindProperty("stepIndicator").objectReferenceValue, Is.Not.Null);
            Assert.That(so.FindProperty("pickHiddenUi").arraySize, Is.EqualTo(6));
            var fanSo = new SerializedObject(fan);
            Assert.That(fanSo.FindProperty("cardPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(fanSo.FindProperty("fanCenter").objectReferenceValue, Is.Not.Null);
            var light = (Light)fanSo.FindProperty("fanLight").objectReferenceValue;
            Assert.That(light, Is.Not.Null, "the fan brings its own light pool");
            Assert.That(light.type, Is.EqualTo(LightType.Spot));
            Assert.That(light.enabled, Is.False, "off until the fan spreads");
            Assert.That(Vector3.Dot(light.transform.forward, Vector3.down), Is.GreaterThan(0.8f), "it shines down on the fan");
        }

        [TestCase(1, 16f / 9f)]
        [TestCase(3, 16f / 9f)]
        [TestCase(10, 16f / 9f)]
        [TestCase(1, 4f / 3f)]
        [TestCase(3, 4f / 3f)]
        [TestCase(10, 4f / 3f)]
        public void TheDrawCameraHoldsTheFanAndEverySlot(int cardCount, float aspect)
        {
            EditorSceneManager.OpenScene(ScenePath);
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            Assert.That(choreography.TryGetDrawPose(cardCount, out var pose, out var fov), Is.True);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var slots = Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(cardCount);
            Assert.That(slots.Count, Is.EqualTo(cardCount), "control: the spread's slots");

            var go = new GameObject("Phase72_FrustumProbe");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
                cam.fieldOfView = fov;
                cam.aspect = aspect;

                var half = new Vector3(0.39f, 0f, 0.59f);   // the face-down card: its Back is 0.78 x 1.18
                void Check(Vector3 center, Quaternion rotation, string what)
                {
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        foreach (var sz in new[] { -1f, 1f })
                        {
                            var corner = center + rotation * new Vector3(half.x * sx, 0f, half.z * sz);
                            var v = cam.WorldToViewportPoint(corner);
                            Assert.That(v.z, Is.GreaterThan(0f), $"{what} in front");
                            Assert.That(v.x, Is.InRange(0.02f, 0.98f), $"{what} x at {cardCount} cards, aspect {aspect:0.00}");
                            Assert.That(v.y, Is.InRange(0.02f, 0.98f), $"{what} y at {cardCount} cards, aspect {aspect:0.00}");
                        }
                    }
                }

                // Review follow-up: the moving poses too - a hovered card slid toward the player,
                // and a picked card pulled out and risen for its hover beat (turning to its slot).
                var fanSo = new SerializedObject(fan);
                var hoverLift = fanSo.FindProperty("hoverLift").floatValue;
                var hoverSlide = fanSo.FindProperty("hoverSlide").floatValue;
                var deckSo = new SerializedObject(Object.FindFirstObjectByType<DeckController>());
                var pull = deckSo.FindProperty("pickPullDistance").floatValue;
                var rise = deckSo.FindProperty("pickRise").floatValue + deckSo.FindProperty("pickHoverBob").floatValue;
                for (var i = 0; i < fan.CardCount; i++)
                {
                    fan.GetFanPose(i, out var p, out var r);
                    Check(p, r, $"fan card {i}");
                    var towardPlayer = -(r * Vector3.forward);
                    Check(p + Vector3.up * hoverLift + towardPlayer * hoverSlide, r, $"hovered fan card {i}");
                    var flat = new Vector3(towardPlayer.x, 0f, towardPlayer.z).normalized;
                    var risen = p + flat * pull + Vector3.up * rise;
                    Check(risen, r, $"picked card {i} rising");
                    Check(risen, Quaternion.identity, $"picked card {i} turned to its slot");
                }

                foreach (var slot in slots)
                {
                    Check(slot.position, slot.rotation, slot.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        [Test]
        public void Phase72DocumentationExists()
        {
            const string path = "Docs/PHASE72_DRAW_RITUAL.md";
            Assert.That(System.IO.File.Exists(path), Is.True);
            var text = System.IO.File.ReadAllText(path);
            Assert.That(text, Does.Contain("SpreadFanController"));
            Assert.That(text, Does.Contain("DealPickedCard"));
        }
    }
}
