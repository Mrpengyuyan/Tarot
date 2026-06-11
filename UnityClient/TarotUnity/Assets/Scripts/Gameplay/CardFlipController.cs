using System.Collections;
using TarotUnity.Core;
using TarotUnity.Presentation;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed class CardFlipController : MonoBehaviour
    {
        [SerializeField] private float flipDuration = 0.45f;
        [SerializeField] private float anticipationPause = 0.1f;
        [SerializeField] private float faceRevealPause = 0.06f;
        [SerializeField] private float liftDuringFlip = 0.12f;
        [SerializeField] private PresentationCueId flipCue = PresentationCueId.CardFlipped;
        [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private bool isFlipping;

        public void Flip(CardView card)
        {
            if (card == null || isFlipping)
            {
                return;
            }

            StartCoroutine(FlipRoutine(card, !card.IsFaceUp));
        }

        public IEnumerator FlipRoutine(CardView card, bool faceUp)
        {
            if (card == null)
            {
                yield break;
            }

            isFlipping = true;
            card.SetHighlighted(true);

            if (anticipationPause > 0f)
            {
                yield return new WaitForSeconds(anticipationPause);
            }

            var startRotation = card.transform.localRotation;
            var startPosition = card.transform.localPosition;
            var midwayRotation = Quaternion.Euler(0f, 90f, 0f);
            var endRotation = Quaternion.Euler(0f, 0f, 0f);
            var halfDuration = Mathf.Max(0.01f, flipDuration * 0.5f);

            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                var t = flipCurve.Evaluate(elapsed / halfDuration);
                card.transform.localRotation = Quaternion.Slerp(startRotation, midwayRotation, t);
                card.transform.localPosition = startPosition + Vector3.up * (Mathf.Sin(t * Mathf.PI * 0.5f) * liftDuringFlip);
                yield return null;
            }

            card.SetFaceUp(faceUp);
            FindFirstObjectByType<RitualFeedbackController>()?.PlayCue(flipCue, card.transform);

            if (faceRevealPause > 0f)
            {
                yield return new WaitForSeconds(faceRevealPause);
            }

            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                var t = flipCurve.Evaluate(elapsed / halfDuration);
                card.transform.localRotation = Quaternion.Slerp(midwayRotation, endRotation, t);
                card.transform.localPosition = startPosition + Vector3.up * (Mathf.Sin((1f - t) * Mathf.PI * 0.5f) * liftDuringFlip);
                yield return null;
            }

            card.transform.localRotation = endRotation;
            card.transform.localPosition = startPosition;
            card.SetHighlighted(false);
            isFlipping = false;
        }
    }
}
