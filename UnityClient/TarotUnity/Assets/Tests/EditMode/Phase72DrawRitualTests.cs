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
    }
}

