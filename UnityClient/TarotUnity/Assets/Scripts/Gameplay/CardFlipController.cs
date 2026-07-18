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
        [SerializeField] private float liftDuringFlip = 0.16f;
        [SerializeField] private PresentationCueId flipCue = PresentationCueId.CardFlipped;
        [SerializeField] private bool cameraPunchEnabled = true;

        // Phase 52 weight & snap. The flip used to spin linearly to 90 degrees and
        // back with a static pause standing in for anticipation - mechanical, and
        // draggy right at the reveal because it eased to a crawl at the edge-on
        // seam. These give it the game-feel arc: a wind-up, a whip through the
        // reveal at speed, a scale pop on the face, and an overshoot that settles.
        [Header("Phase52 Weight & Snap")]
        [Tooltip("Degrees the card winds back (opposite the flip) during the anticipation beat.")]
        [SerializeField] private float windBackAngle = 11f;
        [Tooltip("How far the card dips as it winds up, so the flip launches from a cocked pose.")]
        [SerializeField] private float windBackDip = 0.02f;
        [Tooltip("Degrees the landing overshoots past flat before it settles.")]
        [SerializeField] private float settleOvershootAngle = 6f;
        [Tooltip("Seconds the overshoot takes to damp back to an exact rest.")]
        [SerializeField] private float settleSeconds = 0.12f;
        [Tooltip("Scale pop at the instant of reveal (0.06 = +6%), fading as the face settles.")]
        [SerializeField] private float revealScalePunch = 0.06f;
        [Tooltip("Camera shake fired exactly on the reveal so the punch lands with the face, not before it.")]
        [SerializeField] private float revealCameraShake = 0.05f;

        private bool isFlipping;
        private CameraChoreographyController cameraChoreography;
        private RitualFeedbackController ritualFeedback;

        public bool IsFlipping => isFlipping;

        // Cached lazily instead of in Awake so scene-load order never matters; Unity's
        // destroyed-object == null lets the cache self-heal across scene reloads.
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

        private RitualFeedbackController RitualFeedback
        {
            get
            {
                if (ritualFeedback == null)
                {
                    ritualFeedback = FindFirstObjectByType<RitualFeedbackController>();
                }

                return ritualFeedback;
            }
        }

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
            card.GetComponent<CardHoverTiltController>()?.Suspend();

            var t = card.transform;
            var startPosition = t.localPosition;
            var startScale = t.localScale;

            var restRotation = Quaternion.identity;
            var woundRotation = Quaternion.Euler(0f, -windBackAngle, 0f);
            var edgeRotation = Quaternion.Euler(0f, 90f, 0f);
            var overshootRotation = Quaternion.Euler(0f, -settleOvershootAngle, 0f);

            if (cameraPunchEnabled)
            {
                CameraChoreography?.PunchToward(card.transform);
            }

            // 1) Anticipation - wind back and dip, easing out so the card settles
            // cocked and ready instead of just sitting still for the beat.
            for (var elapsed = 0f; elapsed < anticipationPause; elapsed += Time.deltaTime)
            {
                var k = EaseOut(elapsed / Mathf.Max(0.01f, anticipationPause));
                t.localRotation = Quaternion.Slerp(restRotation, woundRotation, k);
                t.localPosition = startPosition + Vector3.down * (windBackDip * k);
                yield return null;
            }

            var halfDuration = Mathf.Max(0.01f, flipDuration * 0.5f);

            // 2) Whip up to edge-on - accelerate so the reveal happens at full speed,
            // not at the old ease-out crawl. The lift rises to meet the reveal.
            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                var k = elapsed / halfDuration;
                t.localRotation = Quaternion.Slerp(woundRotation, edgeRotation, EaseIn(k));
                var lift = Mathf.Sin(k * Mathf.PI * 0.5f) * liftDuringFlip;
                t.localPosition = startPosition + Vector3.up * (lift - windBackDip * (1f - k));
                yield return null;
            }

            // 3) The reveal at the edge-on seam: swap the face, sound it, and land
            // the camera shake on this instant so the impact reads with the face.
            card.SetFaceUp(faceUp);
            RitualFeedback?.PlayCue(flipCue, card.transform);
            if (cameraPunchEnabled)
            {
                CameraChoreography?.Kick(revealCameraShake);
            }

            if (faceRevealPause > 0f)
            {
                yield return new WaitForSeconds(faceRevealPause);
            }

            // 4) Swing into view with a scale pop, decelerating past flat into a
            // small overshoot so the card arrives with weight.
            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                var k = EaseOut(elapsed / halfDuration);
                t.localRotation = Quaternion.Slerp(edgeRotation, overshootRotation, k);
                var lift = Mathf.Sin((1f - k) * Mathf.PI * 0.5f) * liftDuringFlip;
                t.localPosition = startPosition + Vector3.up * lift;
                t.localScale = startScale * (1f + revealScalePunch * (1f - k));
                yield return null;
            }

            // 5) Damped settle - the overshoot rotation and any residual scale ease
            // back to an exact rest so the card never drifts from where it started.
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                var k = EaseOut(elapsed / Mathf.Max(0.01f, settleSeconds));
                t.localRotation = Quaternion.Slerp(overshootRotation, restRotation, k);
                t.localPosition = startPosition;
                t.localScale = startScale;
                yield return null;
            }

            t.localRotation = restRotation;
            t.localPosition = startPosition;
            t.localScale = startScale;
            card.SetHighlighted(false);
            isFlipping = false;
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
