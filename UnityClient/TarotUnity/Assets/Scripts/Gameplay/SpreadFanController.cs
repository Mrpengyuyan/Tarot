using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 72: after the shuffle the deck spreads out on the table and the player picks the
    /// cards. The fan owns its props - face-down PF_TarotCard instances with no draw bound - and
    /// their motion: the spread, the hover, the pick queue (one pick waits while a card is in
    /// flight; further clicks are dropped), and the gather back into the deck. A picked card
    /// leaves the fan and becomes the card the player later flips; which card it is is decided
    /// by the backend or the local simulator, never by where the player clicked.
    /// Phase 73: the whole deck (78 cards) in concentric arcs around a pivot on the player's
    /// side, and a hover that picks the card up - it springs up, tips toward the camera and
    /// grows a little while its shadow stays on the cloth and spreads - and parts its
    /// neighbours along the arc. The tilt and growth apply to a pivot the card's visuals hang
    /// from while it is in the fan, never to the root that carries its pointer target, so a tipped
    /// card cannot rise over its neighbours' strips and steal the pointer.
    /// </summary>
    public sealed class SpreadFanController : MonoBehaviour
    {
        [Serializable]
        public sealed class FanRow
        {
            public int count = 39;
            public float radius = 4.5f;
            public float arcDegrees = 96f;
        }

        [SerializeField] private CardView cardPrefab;
        [Tooltip("The arcs' shared pivot, on the player's side of the table. Forward points toward the slots.")]
        [SerializeField] private Transform fanCenter;

        [Header("Arcs")]
        [Tooltip("Nearest the player first. Each row spans its own angle at its own radius around the pivot.")]
        [SerializeField] private FanRow[] rows =
        {
            new FanRow { count = 39, radius = 4.5f, arcDegrees = 96f },
            new FanRow { count = 39, radius = 5.9f, arcDegrees = 78f },
        };
        [Tooltip("Each card lies this much above the one to its left, so a row layers left to right.")]
        [SerializeField] private float layerStep = 0.002f;

        [Header("Spread")]
        [Tooltip("Cards made per frame ahead of the spread (Prepare), out of sight under the cloth.")]
        [SerializeField] private int prepareBatch = 10;
        [Tooltip("Delay between neighbouring cards of a row; the rows spread together.")]
        [SerializeField] private float spreadStagger = 0.028f;
        [SerializeField] private float spreadCardSeconds = 0.3f;
        [SerializeField] private float spreadArcHeight = 0.12f;
        [SerializeField] private float settleSquash = 0.05f;
        [SerializeField] private float settleSeconds = 0.12f;

        [Header("Hover: pick up and part")]
        [SerializeField] private float hoverLift = 0.12f;
        [Tooltip("How far the hovered card comes out toward the player.")]
        [SerializeField] private float hoverSlide = 0.18f;
        [Tooltip("How far the hovered card tips its far edge up, toward the camera.")]
        [SerializeField] private float hoverTiltDegrees = 14f;
        [SerializeField] private float hoverScale = 1.06f;
        [Tooltip("How far the nearest neighbours slide aside along the arc.")]
        [SerializeField] private float partDistance = 0.14f;
        [Tooltip("How many neighbours on each side make room, easing off with distance.")]
        [SerializeField] private float waveRadius = 3f;
        [Tooltip("How much the hovered card's shadow spreads on the cloth as the card rises.")]
        [SerializeField] private float shadowSpread = 0.25f;
        [Tooltip("Spring frequency (Hz) and damping of the hover; under 1 overshoots a little and settles.")]
        [SerializeField] private float springFrequency = 2.8f;
        [SerializeField] private float springDamping = 0.55f;
        [Tooltip("A pointer exit only drops the card after this long, so a card sliding out from under the pointer does not flicker.")]
        [SerializeField] private float hoverReleaseDelay = 0.12f;
        [Tooltip("Hover glow scale for a fan card: small enough that only a thin warm rim shows past its edges.")]
        [SerializeField] private float fanHoverHaloScale = 0.62f;

        [Header("Gather")]
        [SerializeField] private float gatherSeconds = 0.6f;

        [Header("Light")]
        [Tooltip("The fan lies nearer the player than the table's light pool, so it brings its own: faded in with the spread, out with the gather.")]
        [SerializeField] private Light fanLight;
        [SerializeField] private float fanLightIntensity = 260f;
        [SerializeField] private float fanLightFadeSeconds = 0.4f;

        private sealed class FanCard
        {
            public int index;
            public Transform pivot;
            public readonly List<Transform> visuals = new();
            public bool settled;
            public float hover;
            public float hoverVelocity;
            public float part;
            public float partVelocity;
            public BoxCollider box;
            public Vector3 boxCenter;
            public Transform shadow;
            public Vector3 shadowLocalPosition;
            public Quaternion shadowLocalRotation;
            public Vector3 shadowLocalScale;
            public Vector3 shadowRootOffset;
            public Quaternion shadowRootRotation;
        }

        private readonly List<CardView> fanCards = new();
        private readonly Dictionary<CardView, FanCard> state = new();
        private readonly List<CardView> prepared = new();
        private readonly List<List<Renderer>> deckLayers = new();
        private bool preparing;
        private Coroutine lightFade;
        private Vector3 baseScale = Vector3.one;
        private bool spreadDone;
        private CardView hovered;
        private float hoverReleaseAt = -1f;
        private CardView pending;
        private bool busy;
        private int remainingPicks;

        public event Action<CardView> CardPicked;

        public int CardCount
        {
            get
            {
                var total = 0;
                foreach (var row in rows)
                {
                    total += Mathf.Max(0, row.count);
                }

                return total;
            }
        }

        public int RowCount => rows.Length;

        /// <summary>The widest row's angle.</summary>
        public float ArcDegrees
        {
            get
            {
                var widest = 0f;
                foreach (var row in rows)
                {
                    widest = Mathf.Max(widest, row.arcDegrees);
                }

                return widest;
            }
        }

        public IReadOnlyList<CardView> FanCards => fanCards;
        public bool AcceptingPicks => remainingPicks > 0;

        public float RowArcDegrees(int row) => rows[row].arcDegrees;

        public int PreparedCount => prepared.Count;

        /// <summary>
        /// Phase 73 review: makes the fan's cards a few per frame (the whole deck is about 2,100
        /// objects), out of sight under the cloth, so the spread does not hitch. Runs during the
        /// shuffle; <see cref="Spread"/> waits for it and uses what it made.
        /// </summary>
        public IEnumerator Prepare(Transform origin)
        {
            DestroyPrepared();
            if (cardPrefab == null)
            {
                yield break;
            }

            preparing = true;
            var hidden = (origin != null ? origin.position : transform.position) + Vector3.down * 2f;
            for (var i = 0; i < CardCount; i++)
            {
                // Phase 74: face down and switched off until the spread - a fresh prefab shows its
                // face and its back at once, and nothing of it should show through the shuffle.
                var card = Instantiate(cardPrefab, hidden, Quaternion.identity, transform);
                card.SetFaceUp(false);
                card.gameObject.SetActive(false);
                prepared.Add(card);
                if ((i + 1) % Mathf.Max(1, prepareBatch) == 0)
                {
                    yield return null;
                }
            }

            preparing = false;
        }

        public int RowOf(int index)
        {
            Locate(index, out var row, out _);
            return row;
        }

        /// <summary>Phase 72 review: awake, wired, and holding at least as many cards as the spread needs.</summary>
        public bool CanSpread(int cardsNeeded)
        {
            return isActiveAndEnabled && cardPrefab != null && CardCount >= cardsNeeded;
        }

        /// <summary>Phase 72 review: drop the fan at once (after a fault mid-pick) - no gather, the light off.</summary>
        public void Abandon()
        {
            spreadDone = false;
            busy = false;
            Clear();
            DestroyPrepared();
            SetLight(0f);
            ShowDeck(1f);
        }

        public void GetFanPose(int index, out Vector3 position, out Quaternion rotation)
        {
            PoseOnArc(index, 0f, out position, out rotation);
        }

        public IEnumerator Spread(Transform origin)
        {
            Clear();
            spreadDone = false;
            if (cardPrefab == null || origin == null)
            {
                yield break;
            }

            while (preparing)
            {
                yield return null;
            }

            for (var i = 0; i < CardCount; i++)
            {
                var card = i < prepared.Count && prepared[i] != null ? prepared[i] : Instantiate(cardPrefab, transform);
                card.name = $"FanCard_{i:00}";
                card.transform.SetPositionAndRotation(origin.position, origin.rotation);
                card.gameObject.SetActive(true);
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
                state[card] = Track(card, i);
            }

            prepared.Clear();

            baseScale = fanCards.Count > 0 ? fanCards[0].transform.localScale : Vector3.one;
            FadeLight(fanLightIntensity);
            CaptureDeck(origin);
            var start = origin.position;
            var startRotation = origin.rotation;
            var total = spreadCardSeconds + spreadStagger * (LongestRow() - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                foreach (var card in fanCards)
                {
                    var i = state[card].index;
                    Locate(i, out _, out var inRow);
                    var k = Mathf.Clamp01((elapsed - inRow * spreadStagger) / spreadCardSeconds);
                    var e = 1f - (1f - k) * (1f - k);
                    GetFanPose(i, out var p, out var r);
                    card.transform.SetPositionAndRotation(
                        Vector3.Lerp(start, p, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * spreadArcHeight),
                        Quaternion.Slerp(startRotation, r, e));
                }

                ShowDeck(1f - elapsed / total);
                yield return null;
            }

            ShowDeck(0f);
            foreach (var card in fanCards)
            {
                GetFanPose(state[card].index, out var p, out var r);
                card.transform.SetPositionAndRotation(p, r);
            }

            // The whole fan settles with one small squash.
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
            if (card == null || !state.ContainsKey(card))
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
            if (card == null || !state.ContainsKey(card) || remainingPicks <= 0)
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

        /// <summary>
        /// Phase 75: the fan is the whole deck, so the deck on the table empties as the fan
        /// spreads - its layers go from the top down as the cards leave - and fills again as
        /// they gather. Remembers which of the deck's renderers were on, and only touches those.
        /// </summary>
        private void CaptureDeck(Transform origin)
        {
            deckLayers.Clear();
            if (origin == null)
            {
                return;
            }

            var layers = new List<Transform>();
            foreach (Transform child in origin)
            {
                layers.Add(child);
            }

            layers.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
            foreach (var layer in layers)
            {
                var shown = new List<Renderer>();
                foreach (var renderer in layer.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.enabled)
                    {
                        shown.Add(renderer);
                    }
                }

                deckLayers.Add(shown);
            }
        }

        /// <summary>Shows the bottom <paramref name="fraction"/> of the deck's layers.</summary>
        private void ShowDeck(float fraction)
        {
            var visible = Mathf.RoundToInt(deckLayers.Count * Mathf.Clamp01(fraction));
            for (var i = 0; i < deckLayers.Count; i++)
            {
                foreach (var renderer in deckLayers[i])
                {
                    if (renderer != null)
                    {
                        renderer.enabled = i < visible;
                    }
                }
            }
        }

        /// <summary>Phase 75: how much of the deck on the table is showing, 0 to 1 (1 when there is none to track).</summary>
        public float DeckShowing
        {
            get
            {
                if (deckLayers.Count == 0)
                {
                    return 1f;
                }

                var shown = 0;
                foreach (var layer in deckLayers)
                {
                    if (layer.Count > 0 && layer[0] != null && layer[0].enabled)
                    {
                        shown++;
                    }
                }

                return (float)shown / deckLayers.Count;
            }
        }

        public IEnumerator Gather(Transform origin)
        {
            spreadDone = false;
            hovered = null;
            FadeLight(0f);
            foreach (var card in fanCards)
            {
                LetGoOfHover(card, state[card]);
            }

            var cards = new List<CardView>(fanCards);
            if (origin == null || cards.Count == 0)
            {
                Clear();
                ShowDeck(1f);
                yield break;
            }

            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            // Each row gathers from both ends toward its middle.
            var mid = (LongestRow() - 1) / 2f;
            var cardSeconds = gatherSeconds * 0.6f;
            var stagger = mid > 0f ? gatherSeconds * 0.4f / mid : 0f;
            for (var elapsed = 0f; elapsed < gatherSeconds; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    Locate(state[cards[i]].index, out var row, out var inRow);
                    var rowMid = (rows[row].count - 1) / 2f;
                    var order = rowMid - Mathf.Abs(inRow - rowMid);
                    var k = Mathf.Clamp01((elapsed - order * stagger) / cardSeconds);
                    cards[i].transform.position = Vector3.Lerp(starts[i], origin.position, k * k);
                }

                ShowDeck(elapsed / gatherSeconds);
                yield return null;
            }

            Clear();
            ShowDeck(1f);
            SetLight(0f);
        }

        private void Locate(int index, out int row, out int inRow)
        {
            var first = 0;
            for (row = 0; row < rows.Length; row++)
            {
                if (index < first + rows[row].count)
                {
                    inRow = index - first;
                    return;
                }

                first += rows[row].count;
            }

            row = rows.Length - 1;
            inRow = Mathf.Max(0, rows[row].count - 1);
        }

        private int LongestRow()
        {
            var longest = 1;
            foreach (var row in rows)
            {
                longest = Mathf.Max(longest, row.count);
            }

            return longest;
        }

        /// <summary>Where card <paramref name="index"/> lies on its arc, slid <paramref name="part"/> metres along it.</summary>
        private void PoseOnArc(int index, float part, out Vector3 position, out Quaternion rotation)
        {
            Locate(index, out var r, out var inRow);
            var row = rows[r];
            var center = fanCenter != null ? fanCenter : transform;
            var t = row.count > 1 ? (float)inRow / (row.count - 1) - 0.5f : 0f;
            var angle = t * row.arcDegrees + part / Mathf.Max(0.01f, row.radius) * Mathf.Rad2Deg;
            var turn = Quaternion.AngleAxis(angle, center.up);
            position = center.position + turn * center.forward * row.radius + center.up * (inRow * layerStep);
            rotation = turn * center.rotation;
        }

        private FanCard Track(CardView card, int index)
        {
            var tracked = new FanCard { index = index, box = card.GetComponent<BoxCollider>() };
            if (tracked.box != null)
            {
                tracked.boxCenter = tracked.box.center;
            }

            // The visuals hang from a pivot while the card is in the fan; the root keeps the collider.
            tracked.pivot = new GameObject("FanHoverPivot").transform;
            foreach (Transform child in card.transform)
            {
                tracked.visuals.Add(child);
            }

            tracked.pivot.SetParent(card.transform, false);
            foreach (var child in tracked.visuals)
            {
                child.SetParent(tracked.pivot, false);
            }

            foreach (var t in card.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Phase15_CardDropShadow")
                {
                    tracked.shadow = t;
                    tracked.shadowLocalPosition = t.localPosition;
                    tracked.shadowLocalRotation = t.localRotation;
                    tracked.shadowLocalScale = t.localScale;
                    tracked.shadowRootOffset = card.transform.InverseTransformPoint(t.position);
                    tracked.shadowRootRotation = Quaternion.Inverse(card.transform.rotation) * t.rotation;
                    break;
                }
            }

            return tracked;
        }

        private void FadeLight(float target)
        {
            if (fanLight == null)
            {
                return;
            }

            // An inactive fan cannot run the fade; set the light outright instead of throwing.
            if (!isActiveAndEnabled)
            {
                SetLight(target);
                return;
            }

            if (lightFade != null)
            {
                StopCoroutine(lightFade);
            }

            lightFade = StartCoroutine(FadeLightRoutine(target));
        }

        private IEnumerator FadeLightRoutine(float target)
        {
            var from = fanLight.enabled ? fanLight.intensity : 0f;
            fanLight.enabled = true;
            for (var elapsed = 0f; elapsed < fanLightFadeSeconds; elapsed += Time.deltaTime)
            {
                fanLight.intensity = Mathf.Lerp(from, target, elapsed / fanLightFadeSeconds);
                yield return null;
            }

            SetLight(target);
            lightFade = null;
        }

        private void SetLight(float intensity)
        {
            if (fanLight == null)
            {
                return;
            }

            if (lightFade != null && intensity <= 0f)
            {
                StopCoroutine(lightFade);
                lightFade = null;
            }

            fanLight.intensity = intensity;
            fanLight.enabled = intensity > 0.001f;
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
            if (state.TryGetValue(card, out var tracked))
            {
                if (tracked.box != null)
                {
                    tracked.box.center = tracked.boxCenter;
                }

                RestoreShadow(tracked);

                // The flight starts from the pose the hover left it in: the pivot's tilt and growth
                // move onto the root (DealPickedCard eases them out), and the visuals go back home.
                if (tracked.pivot != null)
                {
                    card.transform.rotation = card.transform.rotation * tracked.pivot.localRotation;
                    card.transform.localScale = Vector3.Scale(card.transform.localScale, tracked.pivot.localScale);
                    foreach (var child in tracked.visuals)
                    {
                        child.SetParent(card.transform, false);
                    }

                    Destroy(tracked.pivot.gameObject);
                }

                state.Remove(card);
            }

            fanCards.Remove(card);
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
            state.Clear();
            hovered = null;
            pending = null;
            remainingPicks = 0;
        }

        private void DestroyPrepared()
        {
            foreach (var card in prepared)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            prepared.Clear();
            preparing = false;
        }

        private static void RestoreShadow(FanCard tracked)
        {
            if (tracked.shadow != null)
            {
                tracked.shadow.localPosition = tracked.shadowLocalPosition;
                tracked.shadow.localRotation = tracked.shadowLocalRotation;
                tracked.shadow.localScale = tracked.shadowLocalScale;
            }
        }

        /// <summary>Phase 73 review: the gather starts from the resting pose, not a frozen hover.</summary>
        private void LetGoOfHover(CardView card, FanCard tracked)
        {
            tracked.hover = tracked.hoverVelocity = tracked.part = tracked.partVelocity = 0f;
            if (tracked.pivot != null)
            {
                tracked.pivot.localRotation = Quaternion.identity;
                tracked.pivot.localScale = Vector3.one;
            }

            RestoreShadow(tracked);
            GetFanPose(tracked.index, out var p, out var r);
            card.transform.SetPositionAndRotation(p, r);
            card.transform.localScale = baseScale;
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

            var hoveredIndex = -1;
            var hoveredRow = -1;
            var hoveredInRow = 0;
            if (hovered != null && state.TryGetValue(hovered, out var h))
            {
                hoveredIndex = h.index;
                Locate(h.index, out hoveredRow, out hoveredInRow);
            }

            var dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            var omega = 2f * Mathf.PI * Mathf.Max(0.1f, springFrequency);
            foreach (var card in fanCards)
            {
                var tracked = state[card];
                Locate(tracked.index, out var row, out var inRow);

                var hoverTarget = tracked.index == hoveredIndex ? 1f : 0f;
                var partTarget = 0f;
                if (row == hoveredRow && tracked.index != hoveredIndex)
                {
                    var k = inRow - hoveredInRow;
                    var d = Mathf.Abs(k);
                    if (d <= waveRadius)
                    {
                        var ease = 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (d - 1f) / waveRadius);
                        partTarget = Mathf.Sign(k) * partDistance * ease;
                    }
                }

                // A card whose springs are at rest is left alone (78 cards, most of them idle).
                var atRest = hoverTarget == 0f && partTarget == 0f;
                if (atRest && tracked.settled)
                {
                    continue;
                }

                Spring(ref tracked.hover, ref tracked.hoverVelocity, hoverTarget, omega, dt);
                Spring(ref tracked.part, ref tracked.partVelocity, partTarget, omega, dt);
                tracked.settled = false;
                if (atRest && Mathf.Abs(tracked.hover) < 1e-4f && Mathf.Abs(tracked.hoverVelocity) < 1e-3f
                    && Mathf.Abs(tracked.part) < 1e-4f && Mathf.Abs(tracked.partVelocity) < 1e-3f)
                {
                    tracked.hover = tracked.hoverVelocity = tracked.part = tracked.partVelocity = 0f;
                    tracked.settled = true;
                }

                PoseOnArc(tracked.index, tracked.part, out var p, out var r);
                var towardPlayer = -(r * Vector3.forward);
                var slide = towardPlayer * (hoverSlide * tracked.hover);
                var grow = 1f + (hoverScale - 1f) * tracked.hover;
                card.transform.SetPositionAndRotation(p + Vector3.up * (hoverLift * tracked.hover) + slide, r);
                if (tracked.pivot != null)
                {
                    tracked.pivot.localRotation = Quaternion.AngleAxis(-hoverTiltDegrees * tracked.hover, Vector3.right);
                    tracked.pivot.localScale = Vector3.one * grow;
                }

                // The shadow stays on the cloth under the card and spreads as the card rises.
                if (tracked.shadow != null)
                {
                    var shadowRest = p + r * tracked.shadowRootOffset;
                    tracked.shadow.SetPositionAndRotation(shadowRest + slide, r * tracked.shadowRootRotation);
                    tracked.shadow.localScale = tracked.shadowLocalScale * ((1f + shadowSpread * tracked.hover) / grow);
                }

                // The card moves but its pointer target stays on its resting rectangle; otherwise
                // a card moving out from under the pointer would fall back under it and loop.
                if (tracked.box != null)
                {
                    GetFanPose(tracked.index, out var restPosition, out var restRotation);
                    tracked.box.center = card.transform.InverseTransformPoint(restPosition + restRotation * tracked.boxCenter);
                }
            }
        }

        private void Spring(ref float x, ref float v, float target, float omega, float dt)
        {
            v += (omega * omega * (target - x) - 2f * springDamping * omega * v) * dt;
            x += v * dt;
        }
    }
}
