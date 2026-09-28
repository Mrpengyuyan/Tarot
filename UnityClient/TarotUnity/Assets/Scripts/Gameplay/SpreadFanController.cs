using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 72: after the shuffle the deck spreads into an arc on the table and the player
    /// picks the cards. The fan owns its props - face-down PF_TarotCard instances with no
    /// draw bound - and their motion: the spread, the hover wave that follows the pointer,
    /// the pick queue (one pick waits while a card is in flight; further clicks are dropped),
    /// and the gather back into the deck. A picked card leaves the fan and becomes the card
    /// the player later flips; which card it is is decided by the backend or the local
    /// simulator, never by where the player clicked.
    /// </summary>
    public sealed class SpreadFanController : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [Tooltip("The middle card's resting pose. Forward points from the player toward the slots.")]
        [SerializeField] private Transform fanCenter;

        [Header("Arc")]
        [SerializeField] private int cardCount = 22;
        [SerializeField] private float radius = 3.4f;
        [SerializeField] private float arcDegrees = 70f;
        [Tooltip("Each card lies this much above the one to its left, so the fan layers left to right.")]
        [SerializeField] private float layerStep = 0.004f;

        [Header("Spread")]
        [SerializeField] private float spreadStagger = 0.035f;
        [SerializeField] private float spreadCardSeconds = 0.28f;
        [SerializeField] private float spreadArcHeight = 0.12f;
        [SerializeField] private float settleSquash = 0.05f;
        [SerializeField] private float settleSeconds = 0.12f;

        [Header("Hover wave")]
        [SerializeField] private float hoverLift = 0.05f;
        [Tooltip("How far the hovered card slides out toward the player.")]
        [SerializeField] private float hoverSlide = 0.3f;
        [Tooltip("How many neighbours on each side rise with the hovered card.")]
        [SerializeField] private float waveRadius = 2.5f;
        [SerializeField] private float hoverResponseSeconds = 0.08f;
        [Tooltip("A pointer exit only drops the card after this long, so a card sliding out from under the pointer does not flicker.")]
        [SerializeField] private float hoverReleaseDelay = 0.12f;
        [Tooltip("Hover glow scale for a fan card - restrained, the fan is crowded.")]
        [SerializeField] private float fanHoverHaloScale = 1f;

        [Header("Gather")]
        [SerializeField] private float gatherSeconds = 0.6f;

        private readonly List<CardView> fanCards = new();
        private readonly Dictionary<CardView, int> slotOf = new();
        private readonly Dictionary<CardView, float> lift = new();
        private readonly Dictionary<CardView, float> slide = new();
        private bool spreadDone;
        private CardView hovered;
        private float hoverReleaseAt = -1f;
        private CardView pending;
        private bool busy;
        private int remainingPicks;

        public event Action<CardView> CardPicked;

        public int CardCount => cardCount;
        public float ArcDegrees => arcDegrees;
        public IReadOnlyList<CardView> FanCards => fanCards;
        public bool AcceptingPicks => remainingPicks > 0;

        public void GetFanPose(int index, out Vector3 position, out Quaternion rotation)
        {
            var center = fanCenter != null ? fanCenter : transform;
            var t = cardCount > 1 ? (float)index / (cardCount - 1) - 0.5f : 0f;
            var turn = Quaternion.AngleAxis(t * arcDegrees, center.up);
            var pivot = center.position - center.forward * radius;
            position = pivot + turn * center.forward * radius + center.up * (index * layerStep);
            rotation = turn * center.rotation;
        }

        public IEnumerator Spread(Transform origin)
        {
            Clear();
            spreadDone = false;
            if (cardPrefab == null || origin == null)
            {
                yield break;
            }

            for (var i = 0; i < cardCount; i++)
            {
                var card = Instantiate(cardPrefab, transform);
                card.name = $"FanCard_{i:00}";
                card.transform.SetPositionAndRotation(origin.position, origin.rotation);
                card.SetFaceUp(false);
                card.HoverHaloScale = fanHoverHaloScale;
                var tilt = card.GetComponent<CardHoverTiltController>();
                if (tilt != null)
                {
                    tilt.Suspend();
                }

                var click = card.GetComponent<CardClickHandler>();
                if (click == null)
                {
                    click = card.gameObject.AddComponent<CardClickHandler>();
                }

                click.Clicked += RequestPick;
                click.HoverChanged += SetHovered;
                fanCards.Add(card);
                slotOf[card] = i;
                lift[card] = 0f;
                slide[card] = 0f;
            }

            var start = origin.position;
            var startRotation = origin.rotation;
            var total = spreadCardSeconds + spreadStagger * (cardCount - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < fanCards.Count; i++)
                {
                    var k = Mathf.Clamp01((elapsed - i * spreadStagger) / spreadCardSeconds);
                    var e = 1f - (1f - k) * (1f - k);
                    GetFanPose(i, out var p, out var r);
                    fanCards[i].transform.SetPositionAndRotation(
                        Vector3.Lerp(start, p, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * spreadArcHeight),
                        Quaternion.Slerp(startRotation, r, e));
                }

                yield return null;
            }

            for (var i = 0; i < fanCards.Count; i++)
            {
                GetFanPose(i, out var p, out var r);
                fanCards[i].transform.SetPositionAndRotation(p, r);
            }

            // The whole row settles with one small squash.
            var baseScale = fanCards.Count > 0 ? fanCards[0].transform.localScale : Vector3.one;
            var squashed = new Vector3(baseScale.x * (1f + settleSquash * 0.6f), baseScale.y * (1f - settleSquash), baseScale.z);
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / settleSeconds;
                foreach (var card in fanCards)
                {
                    card.transform.localScale = Vector3.Lerp(squashed, baseScale, 1f - (1f - k) * (1f - k));
                }

                yield return null;
            }

            foreach (var card in fanCards)
            {
                card.transform.localScale = baseScale;
            }

            spreadDone = true;
        }

        public void SetHovered(CardView card, bool on)
        {
            if (card == null || !slotOf.ContainsKey(card))
            {
                return;
            }

            if (on)
            {
                hovered = card;
                hoverReleaseAt = -1f;
            }
            else if (hovered == card)
            {
                hoverReleaseAt = Time.time + hoverReleaseDelay;
            }
        }

        public void RequestPick(CardView card)
        {
            if (card == null || !slotOf.ContainsKey(card) || remainingPicks <= 0)
            {
                return;
            }

            if (!busy)
            {
                pending = card;
                return;
            }

            // One pick may wait while a card is in flight; further clicks are dropped.
            if (pending == null && remainingPicks > 1)
            {
                pending = card;
            }
        }

        public IEnumerator PickCards(int count, Func<CardView, int, IEnumerator> deliver)
        {
            remainingPicks = Mathf.Min(count, fanCards.Count);
            pending = null;
            for (var index = 0; index < count && remainingPicks > 0; index++)
            {
                while (pending == null)
                {
                    yield return null;
                }

                var card = pending;
                pending = null;
                busy = true;
                Release(card);
                remainingPicks--;
                CardPicked?.Invoke(card);
                if (deliver != null)
                {
                    yield return deliver(card, index);
                }

                busy = false;
            }

            remainingPicks = 0;
            pending = null;
        }

        public IEnumerator Gather(Transform origin)
        {
            spreadDone = false;
            hovered = null;
            var cards = new List<CardView>(fanCards);
            if (origin == null || cards.Count == 0)
            {
                Clear();
                yield break;
            }

            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            // From both ends toward the middle.
            var mid = (cards.Count - 1) / 2f;
            var cardSeconds = gatherSeconds * 0.6f;
            var stagger = mid > 0f ? gatherSeconds * 0.4f / mid : 0f;
            for (var elapsed = 0f; elapsed < gatherSeconds; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    var order = mid - Mathf.Abs(i - mid);
                    var k = Mathf.Clamp01((elapsed - order * stagger) / cardSeconds);
                    cards[i].transform.position = Vector3.Lerp(starts[i], origin.position, k * k);
                }

                yield return null;
            }

            Clear();
        }

        private void Release(CardView card)
        {
            var click = card.GetComponent<CardClickHandler>();
            if (click != null)
            {
                click.Clicked -= RequestPick;
                click.HoverChanged -= SetHovered;
            }

            card.SetHovered(false);
            card.ClearHoverHaloScale();
            fanCards.Remove(card);
            slotOf.Remove(card);
            lift.Remove(card);
            slide.Remove(card);
            if (hovered == card)
            {
                hovered = null;
            }
        }

        private void Clear()
        {
            foreach (var card in fanCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            fanCards.Clear();
            slotOf.Clear();
            lift.Clear();
            slide.Clear();
            hovered = null;
            pending = null;
            remainingPicks = 0;
        }

        private void Update()
        {
            if (!spreadDone)
            {
                return;
            }

            if (hovered != null && hoverReleaseAt > 0f && Time.time >= hoverReleaseAt)
            {
                hovered = null;
                hoverReleaseAt = -1f;
            }

            var h = hovered != null && slotOf.TryGetValue(hovered, out var hs) ? hs : -1;
            var blend = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, hoverResponseSeconds));
            foreach (var card in fanCards)
            {
                var i = slotOf[card];
                var distance = h >= 0 ? Mathf.Abs(i - h) : float.MaxValue;
                var weight = distance <= waveRadius ? 0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / (waveRadius + 1f)) : 0f;
                lift[card] = Mathf.Lerp(lift[card], hoverLift * weight, blend);
                slide[card] = Mathf.Lerp(slide[card], i == h ? hoverSlide : 0f, blend);
                GetFanPose(i, out var p, out var r);
                var towardPlayer = -(r * Vector3.forward);
                card.transform.SetPositionAndRotation(p + Vector3.up * lift[card] + towardPlayer * slide[card], r);
            }
        }
    }
}
