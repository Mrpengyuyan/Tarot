using System.Collections;
using System.Collections.Generic;
using System;
using TarotUnity.Data;
using TarotUnity.Presentation;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed class DeckController : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform cardParent;
        [SerializeField] private float dealDuration = 0.35f;
        [SerializeField] private float dealInterval = 0.12f;
        [SerializeField] private float dealArcHeight = 0.45f;
        [SerializeField] private float dealTiltDegrees = 10f;
        [SerializeField] private float postDealSettleSeconds = 0.10f;
        [SerializeField] private AnimationCurve dealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private CardArtworkCatalog artworkCatalog;

        // Phase 54 landing weight. The card used to fly its arc and then snap dead
        // onto the slot - the same weightlessness the flip had before Phase 52. Now
        // it lands: a brief squash-and-recover on contact, and a camera kick on the
        // exact impact frame so the touchdown reads, matching the flip's language.
        [Header("Phase54 Landing")]
        [Tooltip("How much the card squashes on contact (0.1 = -10% height, +6% width), then recovers.")]
        [SerializeField] private float landingSquash = 0.1f;
        [Tooltip("Seconds the squash takes to spring back to an exact rest.")]
        [SerializeField] private float landingSeconds = 0.14f;
        [Tooltip("Camera shake on the landing frame. Subtle - deals arrive in quick succession.")]
        [SerializeField] private float landingCameraKick = 0.03f;

        // Phase 72: a card the player picked from the fan is pulled out, hovers glowing,
        // flies to its slot trailing light, and lands with the Phase 54 weight.
        [Header("Phase72 Pick")]
        [SerializeField] private float pickPullSeconds = 0.15f;
        [SerializeField] private float pickPullDistance = 0.25f;
        [SerializeField] private float pickRise = 0.55f;
        [SerializeField] private float pickHoverSeconds = 0.25f;
        [SerializeField] private float pickHoverBob = 0.02f;
        [SerializeField] private float pickGlowBoost = 1.6f;
        [SerializeField] private float returnSeconds = 0.45f;

        private readonly List<CardView> activeCards = new();
        private CardArtworkCatalog defaultArtworkCatalog;
        private CameraChoreographyController cameraChoreography;

        // Lazy, self-healing like CardFlipController's - scene-load order and reloads
        // never matter, and the deck works fine (minus the kick) if no camera exists.
        private CameraChoreographyController CameraChoreography
        {
            get
            {
                if (cameraChoreography == null)
                {
                    cameraChoreography = FindFirstObjectByType<CameraChoreographyController>();
                }

                return cameraChoreography;
            }
        }

        public event Action<CardView> CardDealStarted;
        public event Action<CardView> CardDealt;

        /// <summary>Phase 72: raised when a picked card starts its hover beat, so its slot can answer.</summary>
        public event Action<CardView> CardHovering;

        public IReadOnlyList<CardView> ActiveCards => activeCards;

        public void Clear()
        {
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            activeCards.Clear();
        }

        public IEnumerator DealCards(IList<CardDrawData> draws, IList<Transform> slots)
        {
            Clear();

            if (cardPrefab == null || draws == null || slots == null)
            {
                yield break;
            }

            var count = Mathf.Min(draws.Count, slots.Count);
            for (var i = 0; i < count; i++)
            {
                var card = Instantiate(cardPrefab, cardParent != null ? cardParent : transform);
                card.transform.SetPositionAndRotation(transform.position, transform.rotation);
                card.Bind(draws[i]);
                card.SetFaceArtwork(ResolveArtwork(draws[i]));
                activeCards.Add(card);

                CardDealStarted?.Invoke(card);
                yield return MoveCardToSlot(card.transform, slots[i]);
                yield return LandingSettle(card.transform);
                card.SetHighlighted(true);
                if (postDealSettleSeconds > 0f)
                {
                    yield return new WaitForSeconds(postDealSettleSeconds);
                }

                CardDealt?.Invoke(card);
                yield return new WaitForSeconds(dealInterval);
            }
        }

        /// <summary>
        /// Phase 72: deals a card the player picked from the fan. Four beats - pull (out of
        /// the fan toward the player and up, accelerating), hover (turns to the slot's heading
        /// and bobs while its glow swells), flight (the deal arc, trailing light), and the
        /// Phase 54 landing. The card joins ActiveCards on landing, still face down and unbound.
        /// </summary>
        public IEnumerator DealPickedCard(CardView card, Transform slot)
        {
            if (card == null || slot == null)
            {
                yield break;
            }

            var t = card.transform;
            t.SetParent(cardParent != null ? cardParent : transform, true);
            card.SetHighlighted(true);
            CardDealStarted?.Invoke(card);

            var start = t.position;
            var toward = -(t.rotation * Vector3.forward);
            toward.y = 0f;
            var pulled = start + toward.normalized * pickPullDistance + Vector3.up * pickRise;

            // Phase 73: a card picked from the fan may still be grown by its hover; it eases back
            // to its own size as it comes out instead of snapping on the click.
            var fromScale = t.localScale;
            var restScale = cardPrefab != null ? cardPrefab.transform.localScale : Vector3.one;
            for (var elapsed = 0f; elapsed < pickPullSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / pickPullSeconds;
                t.position = Vector3.Lerp(start, pulled, k * k);
                t.localScale = Vector3.Lerp(fromScale, restScale, k);
                yield return null;
            }

            t.position = pulled;
            t.localScale = restScale;

            CardHovering?.Invoke(card);
            var fromRotation = t.rotation;
            for (var elapsed = 0f; elapsed < pickHoverSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / pickHoverSeconds;
                t.rotation = Quaternion.Slerp(fromRotation, slot.rotation, k * k * (3f - 2f * k));
                t.position = pulled + Vector3.up * (Mathf.Sin(k * Mathf.PI * 2f) * pickHoverBob);
                card.SetHaloBoost(1f + (pickGlowBoost - 1f) * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            card.SetHaloBoost(1f);
            t.SetPositionAndRotation(pulled, slot.rotation);

            var trail = card.GetComponentInChildren<TrailRenderer>(true);
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }

            yield return MoveCardToSlot(t, slot);
            if (trail != null)
            {
                trail.emitting = false;
            }

            yield return LandingSettle(t);
            activeCards.Add(card);
            var tilt = card.GetComponent<CardHoverTiltController>();
            if (tilt != null)
            {
                tilt.Resume();
            }

            CardDealt?.Invoke(card);
        }

        /// <summary>Phase 72: gives the landed cards their draws, in slot order, once the reading exists.</summary>
        public void BindDealtCards(IList<CardDrawData> draws)
        {
            if (draws == null)
            {
                return;
            }

            if (draws.Count != activeCards.Count)
            {
                Debug.LogWarning($"DeckController: {draws.Count} draws for {activeCards.Count} dealt cards.");
            }

            for (var i = 0; i < Mathf.Min(draws.Count, activeCards.Count); i++)
            {
                var card = activeCards[i];
                card.Bind(draws[i]);
                card.SetFaceArtwork(ResolveArtwork(draws[i]));
                card.SetHighlighted(true);
            }
        }

        /// <summary>Phase 72: a failed BackendOnly start sends the landed cards back to the deck.</summary>
        public IEnumerator ReturnDealtCards()
        {
            var cards = new List<CardView>(activeCards);
            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            for (var elapsed = 0f; elapsed < returnSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / returnSeconds;
                var e = k * k * (3f - 2f * k);
                for (var i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null)
                    {
                        cards[i].transform.position = Vector3.Lerp(starts[i], transform.position, e)
                            + Vector3.up * (Mathf.Sin(k * Mathf.PI) * dealArcHeight * 0.5f);
                    }
                }

                yield return null;
            }

            Clear();
        }

        private IEnumerator MoveCardToSlot(Transform cardTransform, Transform slot)
        {
            if (cardTransform == null || slot == null)
            {
                yield break;
            }

            var startPosition = cardTransform.position;
            var startRotation = cardTransform.rotation;
            var elapsed = 0f;

            while (elapsed < dealDuration)
            {
                var t = dealCurve.Evaluate(elapsed / Mathf.Max(0.01f, dealDuration));
                var arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * dealArcHeight);
                cardTransform.position = Vector3.Lerp(startPosition, slot.position, t) + arc;
                var tilt = Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI) * dealTiltDegrees, -Mathf.Sin(t * Mathf.PI) * dealTiltDegrees * 0.45f);
                cardTransform.rotation = Quaternion.Slerp(startRotation, slot.rotation, t) * tilt;
                elapsed += Time.deltaTime;
                yield return null;
            }

            cardTransform.SetPositionAndRotation(slot.position, slot.rotation);
        }

        /// <summary>
        /// The touchdown. The card arrives compressed and springs back to its exact
        /// rest scale (ease-out), and the camera takes a small kick on the impact
        /// frame - the deal's counterpart to the flip's reveal kick. Ends at the
        /// base scale precisely so the later flip starts from a clean rest.
        /// </summary>
        private IEnumerator LandingSettle(Transform cardTransform)
        {
            if (cardTransform == null || landingSeconds <= 0f)
            {
                yield break;
            }

            CameraChoreography?.Kick(landingCameraKick);

            var baseScale = cardTransform.localScale;
            var squashed = new Vector3(
                baseScale.x * (1f + landingSquash * 0.6f),
                baseScale.y * (1f - landingSquash),
                baseScale.z);

            for (var elapsed = 0f; elapsed < landingSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / landingSeconds;
                var ease = 1f - (1f - k) * (1f - k);
                cardTransform.localScale = Vector3.Lerp(squashed, baseScale, ease);
                yield return null;
            }

            cardTransform.localScale = baseScale;
        }

        private Sprite ResolveArtwork(CardDrawData drawData)
        {
            var catalog = artworkCatalog != null
                ? artworkCatalog
                : defaultArtworkCatalog ??= Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");

            return catalog != null ? catalog.FindSprite(drawData) : null;
        }
    }
}
