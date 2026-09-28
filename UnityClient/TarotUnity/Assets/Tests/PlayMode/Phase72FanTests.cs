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
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var so = new UnityEditor.SerializedObject(fan);
            so.FindProperty("fanCenter").objectReferenceValue = center;
            so.FindProperty("cardPrefab").objectReferenceValue = prefab;
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
        public IEnumerator SpreadLaysTwentyTwoFaceDownCardsOnTheArc()
        {
            Assert.That(fan.FanCards.Count, Is.EqualTo(22));
            for (var i = 0; i < fan.FanCards.Count; i++)
            {
                fan.GetFanPose(i, out var pose, out _);
                Assert.That(Vector3.Distance(fan.FanCards[i].transform.position, pose), Is.LessThan(0.002f), $"card {i} at rest");
                Assert.That(fan.FanCards[i].IsFaceUp, Is.False);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverLiftsTheCardAndItsNeighboursLess()
        {
            var cards = fan.FanCards;
            fan.SetHovered(cards[10], true);
            yield return new WaitForSeconds(0.4f);
            fan.GetFanPose(10, out var p10, out _);
            fan.GetFanPose(11, out var p11, out _);
            fan.GetFanPose(15, out var p15, out _);
            var lift10 = cards[10].transform.position.y - p10.y;
            var lift11 = cards[11].transform.position.y - p11.y;
            var lift15 = cards[15].transform.position.y - p15.y;
            Assert.That(lift10, Is.GreaterThan(0.02f));
            Assert.That(lift11, Is.InRange(0.002f, lift10 - 0.001f), "a neighbour rises less");
            Assert.That(lift15, Is.LessThan(0.001f), "the wave is local");
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
            Assert.That(fan.FanCards.Count, Is.EqualTo(19));
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

        private static IEnumerator Deliver(List<(CardView, int)> log, CardView card, int index, float seconds = 0.1f)
        {
            log.Add((card, index));
            yield return new WaitForSeconds(seconds);
        }
    }
}
