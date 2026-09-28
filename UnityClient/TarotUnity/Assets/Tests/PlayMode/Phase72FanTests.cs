using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Phase 72: the fan the player picks from.</summary>
    public sealed class Phase72FanTests
    {
        private GameObject root;
        private SpreadFanController fan;
        private Transform origin;
        private Light fanLight;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Phase72_FanRig");
            root.SetActive(false);
            fan = root.AddComponent<SpreadFanController>();
            var center = new GameObject("FanCenter").transform;
            center.SetParent(root.transform, false);
            origin = new GameObject("Origin").transform;
            origin.SetParent(root.transform, false);
            origin.localPosition = new Vector3(-2f, 0f, 0f);
            fanLight = new GameObject("FanLight").AddComponent<Light>();
            fanLight.transform.SetParent(root.transform, false);
            fanLight.enabled = false;
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var so = new UnityEditor.SerializedObject(fan);
            so.FindProperty("fanCenter").objectReferenceValue = center;
            so.FindProperty("cardPrefab").objectReferenceValue = prefab;
            so.FindProperty("fanLight").objectReferenceValue = fanLight;
            so.ApplyModifiedPropertiesWithoutUndo();
#else
            Assert.Ignore("needs the editor to load the card prefab");
#endif
            root.SetActive(true);
            yield return fan.Spread(origin);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpreadLaysTheWholeDeckFaceDownOnTheArcs()
        {
            Assert.That(fan.FanCards.Count, Is.EqualTo(78), "Phase 73: the whole deck");
            for (var i = 0; i < fan.FanCards.Count; i++)
            {
                fan.GetFanPose(i, out var pose, out _);
                Assert.That(Vector3.Distance(fan.FanCards[i].transform.position, pose), Is.LessThan(0.002f), $"card {i} at rest");
                Assert.That(fan.FanCards[i].IsFaceUp, Is.False);
            }

            yield return null;
        }

        // Phase 73: the hover "picks up and parts" - the card under the pointer springs up, tips
        // toward the camera and grows a little; its neighbours slide aside along the arc instead of
        // bobbing; its shadow stays on the cloth and spreads.
        [UnityTest]
        public IEnumerator HoverPicksTheCardUpAndTipsItTowardTheCamera()
        {
            var card = fan.FanCards[19];
            fan.GetFanPose(19, out var rest, out var restRotation);
            var restScale = card.transform.localScale;
            var peak = 0f;
            fan.SetHovered(card, true);
            for (var t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                peak = Mathf.Max(peak, card.transform.position.y - rest.y);
                yield return null;
            }

            var lift = card.transform.position.y - rest.y;
            Assert.That(lift, Is.GreaterThan(0.08f), "picked up");
            Assert.That(peak, Is.GreaterThan(lift * 1.02f), "a spring: it overshoots a little, then settles");
            var tilt = Quaternion.Angle(card.transform.rotation, restRotation);
            Assert.That(tilt, Is.InRange(8f, 20f), "tipped toward the camera");
            var farEnd = card.transform.TransformPoint(Vector3.forward * 0.5f);
            var nearEnd = card.transform.TransformPoint(Vector3.back * 0.5f);
            Assert.That(farEnd.y, Is.GreaterThan(nearEnd.y), "the far edge rises, so the face turns to the player");
            Assert.That(card.transform.localScale.x, Is.GreaterThan(restScale.x * 1.03f));
        }

        [UnityTest]
        public IEnumerator NeighboursPartAlongTheArcInsteadOfBobbing()
        {
            fan.GetFanPose(19, out var hoveredRest, out _);
            var restPositions = Enumerable.Range(0, fan.FanCards.Count).Select(i =>
            {
                fan.GetFanPose(i, out var p, out _);
                return p;
            }).ToArray();
            fan.SetHovered(fan.FanCards[19], true);
            yield return new WaitForSeconds(0.6f);

            foreach (var neighbour in new[] { 18, 20 })
            {
                var now = fan.FanCards[neighbour].transform.position;
                var before = Vector3.Distance(restPositions[neighbour], hoveredRest);
                var after = Vector3.Distance(new Vector3(now.x, hoveredRest.y, now.z), hoveredRest);
                Assert.That(after, Is.GreaterThan(before + 0.05f), $"card {neighbour} makes room");
                Assert.That(now.y - restPositions[neighbour].y, Is.LessThan(0.01f), $"card {neighbour} stays on the cloth");
            }

            Assert.That(Vector3.Distance(fan.FanCards[26].transform.position, restPositions[26]), Is.LessThan(0.002f), "the parting is local");
            Assert.That(Vector3.Distance(fan.FanCards[58].transform.position, restPositions[58]), Is.LessThan(0.002f), "the other arc is untouched");
        }

        [UnityTest]
        public IEnumerator TheShadowStaysOnTheClothAndSpreads()
        {
            var card = fan.FanCards[19];
            var shadow = card.GetComponentsInChildren<Transform>(true).First(t => t.name == "Phase15_CardDropShadow");
            var restY = shadow.position.y;
            var restSize = shadow.lossyScale.x;
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.6f);
            Assert.That(shadow.position.y, Is.EqualTo(restY).Within(0.005f), "the shadow does not lift with the card");
            Assert.That(shadow.lossyScale.x, Is.GreaterThan(restSize * 1.1f), "it spreads as the card rises");

            fan.SetHovered(card, false);
            yield return new WaitForSeconds(0.8f);
            Assert.That(shadow.lossyScale.x, Is.EqualTo(restSize).Within(restSize * 0.02f), "and settles back");
        }

        [UnityTest]
        public IEnumerator BriefExitAndReEnterDoesNotDropTheCard()
        {
            var card = fan.FanCards[8];
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.4f);
            fan.GetFanPose(8, out var rest, out _);
            var lifted = card.transform.position.y - rest.y;

            fan.SetHovered(card, false);
            yield return null;
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.2f);
            Assert.That(card.transform.position.y - rest.y, Is.GreaterThan(lifted * 0.9f), "no flicker back down");
        }

        // Review fix: a hovered card slides toward the player, but its pointer target must stay
        // on the resting rectangle - otherwise the pointer falls off it (onto a neighbour or the
        // cloth), the card drops back under the pointer, and the two swap forever.
        [UnityTest]
        public IEnumerator AHoveredCardKeepsItsPointerTargetWhereItRests()
        {
            var card = fan.FanCards[10];
            var box = card.GetComponent<BoxCollider>();
            fan.GetFanPose(10, out var rest, out _);
            Assert.That(Vector3.Distance(box.bounds.center, rest), Is.LessThan(0.01f), "control: at rest the target is the card");

            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(card.transform.position, rest), Is.GreaterThan(0.2f), "control: the card slid out");
            Assert.That(Vector3.Distance(box.bounds.center, rest), Is.LessThan(0.01f), "the pointer target stays put");
        }

        [UnityTest]
        public IEnumerator APickedCardGetsItsOwnPointerTargetBack()
        {
            var card = fan.FanCards[10];
            var box = card.GetComponent<BoxCollider>();
            var authored = box.center;
            fan.SetHovered(card, true);
            yield return new WaitForSeconds(0.4f);
            fan.StartCoroutine(fan.PickCards(1, (c, i) => Deliver(new List<(CardView, int)>(), c, i)));
            fan.RequestPick(card);
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(box.center, authored), Is.LessThan(1e-4f), "a dealt card is clicked where it lies");
        }

        [UnityTest]
        public IEnumerator PicksDeliverInOrderAndLeaveAGap()
        {
            var delivered = new List<(CardView, int)>();
            var picked = new[] { fan.FanCards[3], fan.FanCards[12], fan.FanCards[20] };
            fan.StartCoroutine(fan.PickCards(3, (c, i) => Deliver(delivered, c, i)));
            yield return null;
            Assert.That(fan.AcceptingPicks, Is.True);

            foreach (var card in picked)
            {
                fan.RequestPick(card);
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.3f);
            Assert.That(delivered.Select(d => d.Item1), Is.EqualTo(picked));
            Assert.That(delivered.Select(d => d.Item2), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(fan.FanCards.Count, Is.EqualTo(fan.CardCount - 3));
            Assert.That(fan.AcceptingPicks, Is.False);
            Assert.That(picked.All(c => c.HoverHaloScale > 1.01f), "picked cards get their normal hover glow back");
        }

        [UnityTest]
        public IEnumerator ExtraClicksWhileBusyAreDroppedAfterOneQueuedPick()
        {
            var delivered = new List<(CardView, int)>();
            fan.StartCoroutine(fan.PickCards(3, (c, i) => Deliver(delivered, c, i, 0.5f)));
            yield return null;

            var first = fan.FanCards[0];
            fan.RequestPick(first);
            while (fan.FanCards.Contains(first))   // wait until it is actually in flight
            {
                yield return null;
            }

            fan.RequestPick(fan.FanCards[4]);   // queued
            fan.RequestPick(fan.FanCards[5]);   // dropped
            fan.RequestPick(fan.FanCards[6]);   // dropped
            yield return new WaitForSeconds(1.3f);

            Assert.That(delivered.Count, Is.EqualTo(2), "one in flight, one queued, the rest dropped");
            Assert.That(fan.AcceptingPicks, Is.True);
        }

        [UnityTest]
        public IEnumerator GatherClearsTheRemainingCards()
        {
            var cards = fan.FanCards.ToList();
            yield return fan.Gather(origin);
            yield return null;
            Assert.That(fan.FanCards.Count, Is.EqualTo(0));
            Assert.That(cards.All(c => c == null), "gathered cards are destroyed");
        }

        [UnityTest]
        public IEnumerator ALightPoolBloomsOverTheFanAndFadesWithTheGather()
        {
            Assert.That(fanLight.enabled, Is.True, "the fan lies outside the table pool, so it brings its own light");
            Assert.That(fanLight.intensity, Is.GreaterThan(1f));
            yield return fan.Gather(origin);
            Assert.That(fanLight.enabled, Is.False, "the table goes back to its usual light");
        }

        private static IEnumerator Deliver(List<(CardView, int)> log, CardView card, int index, float seconds = 0.1f)
        {
            log.Add((card, index));
            yield return new WaitForSeconds(seconds);
        }
    }
}
