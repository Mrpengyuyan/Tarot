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
