using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 52 drives a real flip on the card prefab and watches the transform each
    /// frame. It proves the two things the redesign has to guarantee: the secondary
    /// motion is actually there (a wind-up dip below rest, a lift above it, and a
    /// scale pop), and - the correctness bar - the overshoot settles to an *exact*
    /// rest, so a flipped card never drifts from where it was dealt.
    /// </summary>
    public sealed class Phase52FlipMotionTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        [UnityTest]
        public IEnumerator FlipWindsUpPopsAndSettlesToExactRest()
        {
#if !UNITY_EDITOR
            yield break;
#else
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            Assert.That(prefab, Is.Not.Null, "card prefab missing");

            var go = Object.Instantiate(prefab);
            var card = go.GetComponent<CardView>();
            var flip = go.GetComponent<CardFlipController>();
            Assert.That(card, Is.Not.Null);
            Assert.That(flip, Is.Not.Null);

            var t = go.transform;
            var startPosition = t.localPosition;
            var startScale = t.localScale;

            var minY = startPosition.y;
            var maxY = startPosition.y;
            var maxScale = startScale.x;

            var routine = flip.FlipRoutine(card, true);
            while (routine.MoveNext())
            {
                minY = Mathf.Min(minY, t.localPosition.y);
                maxY = Mathf.Max(maxY, t.localPosition.y);
                maxScale = Mathf.Max(maxScale, t.localScale.x);
                yield return routine.Current;
            }

            Assert.That(minY, Is.LessThan(startPosition.y - 0.004f),
                "the card should dip below rest as it winds up (anticipation)");
            Assert.That(maxY, Is.GreaterThan(startPosition.y + 0.05f),
                "the card should lift clear of rest through the flip");
            Assert.That(maxScale, Is.GreaterThan(startScale.x + 0.01f),
                "the reveal should pop the scale");

            Assert.That(Vector3.Distance(t.localPosition, startPosition), Is.LessThan(0.0005f),
                "the settle must return the card to the exact position it was dealt");
            Assert.That(Vector3.Distance(t.localScale, startScale), Is.LessThan(0.0005f),
                "the scale pop must fully recover");
            Assert.That(Quaternion.Angle(t.localRotation, Quaternion.identity), Is.LessThan(0.05f),
                "the overshoot must settle flat, not leave the card cocked");
            Assert.That(flip.IsFlipping, Is.False, "the flip must release its state when done");
            Assert.That(card.IsFaceUp, Is.True, "the card is face up after the flip");

            Object.Destroy(go);
#endif
        }
    }
}
