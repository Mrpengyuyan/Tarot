using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Phase 55: the shuffle used to be sound and dust over a perfectly still deck.
    /// This plays the stack itself in the motion language Phases 52/54 established
    /// (anticipation, action, contact, settle): the deck presses down as if a hand
    /// squares it, a riffle ripple runs bottom-to-top through the stacked cards
    /// (each pops up with a small twist and drops back with weight), the deck
    /// squares up with a squash and a camera kick on the contact frame, and
    /// everything settles back to the exact authored stagger - the PlayMode test
    /// holds the stack to its rest pose to sub-millimetre precision.
    /// </summary>
    public sealed class DeckShuffleChoreographer : MonoBehaviour
    {
        [Header("Anticipation")]
        [Tooltip("How far the whole stack presses down before the riffle, like a hand squaring the deck.")]
        [SerializeField] private float anticipationDip = 0.015f;
        [Tooltip("Seconds of the press-down.")]
        [SerializeField] private float anticipationSeconds = 0.12f;

        [Header("Riffle")]
        [Tooltip("How high each card pops during the ripple, in the stack's local units.")]
        [SerializeField] private float riffleLift = 0.055f;
        [Tooltip("Seconds one card spends rising and dropping.")]
        [SerializeField] private float riffleCardSeconds = 0.16f;
        [Tooltip("Delay between neighbouring cards - the ripple runs bottom-to-top.")]
        [SerializeField] private float riffleStagger = 0.025f;
        [Tooltip("Yaw twist at the top of each card's pop.")]
        [SerializeField] private float riffleYawDegrees = 4f;

        [Header("Contact and settle")]
        [Tooltip("Squash on the frame the deck squares up (0.06 = -6% height).")]
        [SerializeField] private float contactSquash = 0.06f;
        [Tooltip("Seconds the squash takes to spring back to an exact rest.")]
        [SerializeField] private float settleSeconds = 0.15f;
        [Tooltip("Camera shake on the contact frame. Quieter than the flip's reveal - the shuffle is a prelude.")]
        [SerializeField] private float contactCameraKick = 0.03f;

        private readonly List<Transform> cards = new();
        private readonly List<Vector3> restPositions = new();
        private readonly List<Quaternion> restRotations = new();
        private Vector3 restRootPosition;
        private Vector3 restRootScale;
        private bool restCaptured;
        private Coroutine active;
        private CameraChoreographyController cameraChoreography;

        // Lazy, self-healing like CardFlipController's - scene-load order never
        // matters, and the shuffle plays fine (minus the kick) with no camera.
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

        public bool IsPlaying => active != null;

        public void Play()
        {
            // The childCount guard keeps ShuffleRoutine from completing synchronously
            // (its empty-stack yield break would run before `active` is assigned,
            // leaving IsPlaying stuck true forever).
            if (active == null && isActiveAndEnabled && transform.childCount > 0)
            {
                active = StartCoroutine(ShuffleRoutine());
            }
        }

        private void CaptureRestPose()
        {
            if (restCaptured)
            {
                return;
            }

            cards.Clear();
            restPositions.Clear();
            restRotations.Clear();
            foreach (Transform child in transform)
            {
                cards.Add(child);
            }

            // The ripple should climb the stack, so order the cards by height.
            cards.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
            foreach (var card in cards)
            {
                restPositions.Add(card.localPosition);
                restRotations.Add(card.localRotation);
            }

            restRootPosition = transform.localPosition;
            restRootScale = transform.localScale;
            restCaptured = true;
        }

        private IEnumerator ShuffleRoutine()
        {
            CaptureRestPose();
            if (cards.Count == 0)
            {
                active = null;
                yield break;
            }

            // Beat 1 - anticipation: the stack presses down, easing out into the pose.
            for (var elapsed = 0f; elapsed < anticipationSeconds; elapsed += Time.deltaTime)
            {
                var k = EaseOut(elapsed / anticipationSeconds);
                transform.localPosition = restRootPosition + Vector3.down * (anticipationDip * k);
                yield return null;
            }

            // Beat 2 - the riffle: a ripple of pops runs bottom-to-top while the
            // stack rises back out of its dip over the ripple's first half.
            var rippleSeconds = riffleCardSeconds + riffleStagger * (cards.Count - 1);
            for (var elapsed = 0f; elapsed < rippleSeconds; elapsed += Time.deltaTime)
            {
                var recover = EaseOut(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, rippleSeconds * 0.5f)));
                transform.localPosition = restRootPosition + Vector3.down * (anticipationDip * (1f - recover));

                for (var i = 0; i < cards.Count; i++)
                {
                    var phase = Mathf.Clamp01((elapsed - i * riffleStagger) / Mathf.Max(0.01f, riffleCardSeconds));
                    var envelope = PopEnvelope(phase);
                    cards[i].localPosition = restPositions[i] + Vector3.up * (riffleLift * envelope);
                    cards[i].localRotation = restRotations[i] * Quaternion.AngleAxis(riffleYawDegrees * envelope, Vector3.up);
                }

                yield return null;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].localPosition = restPositions[i];
                cards[i].localRotation = restRotations[i];
            }

            transform.localPosition = restRootPosition;

            // Beat 3 - contact: the deck squares up. The kick lands on this frame.
            CameraChoreography?.Kick(contactCameraKick);
            var squashed = new Vector3(
                restRootScale.x * (1f + contactSquash * 0.4f),
                restRootScale.y * (1f - contactSquash),
                restRootScale.z * (1f + contactSquash * 0.4f));

            // Beat 4 - settle: the squash springs back ease-out to an exact rest.
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                var k = EaseOut(elapsed / settleSeconds);
                transform.localScale = Vector3.Lerp(squashed, restRootScale, k);
                yield return null;
            }

            transform.localScale = restRootScale;
            transform.localPosition = restRootPosition;
            active = null;
        }

        /// <summary>
        /// One card's pop: rise ease-out over the first 45%, then drop ease-in -
        /// the card falls back with weight instead of floating down.
        /// </summary>
        private static float PopEnvelope(float phase)
        {
            if (phase <= 0f || phase >= 1f)
            {
                return 0f;
            }

            return phase < 0.45f
                ? EaseOut(phase / 0.45f)
                : 1f - EaseIn((phase - 0.45f) / 0.55f);
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
