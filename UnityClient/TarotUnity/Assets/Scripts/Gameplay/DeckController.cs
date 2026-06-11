using System.Collections;
using System.Collections.Generic;
using System;
using TarotUnity.Data;
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

        private readonly List<CardView> activeCards = new();
        private CardArtworkCatalog defaultArtworkCatalog;

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

        private Sprite ResolveArtwork(CardDrawData drawData)
        {
            var catalog = artworkCatalog != null
                ? artworkCatalog
                : defaultArtworkCatalog ??= Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");

            return catalog != null ? catalog.FindSprite(drawData) : null;
        }
    }
}
