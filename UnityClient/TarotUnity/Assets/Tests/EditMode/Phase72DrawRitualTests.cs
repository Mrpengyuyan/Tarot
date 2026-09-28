using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
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

                Assert.That(fan.CardCount, Is.EqualTo(22));
                Assert.That(fan.ArcDegrees, Is.InRange(64f, 76f));

                fan.GetFanPose(0, out var first, out var firstRot);
                fan.GetFanPose(fan.CardCount - 1, out var last, out var lastRot);
                fan.GetFanPose(fan.CardCount / 2, out var mid, out _);
                Assert.That(first.x, Is.LessThan(0f));
                Assert.That(last.x, Is.GreaterThan(0f));
                Assert.That(mid.z, Is.GreaterThan(first.z), "the arc bows toward the slots");
                Assert.That(last.y, Is.GreaterThan(first.y), "later cards lie on top");
                Assert.That(Quaternion.Angle(firstRot, lastRot), Is.EqualTo(fan.ArcDegrees).Within(0.5f));

                fan.GetFanPose(1, out var second, out _);
                var gap = Vector3.Distance(new Vector3(first.x, 0, first.z), new Vector3(second.x, 0, second.z));
                Assert.That(gap, Is.InRange(0.15f, 0.3f), "neighbours overlap by about two thirds of a 0.74 card");
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
    }
}
