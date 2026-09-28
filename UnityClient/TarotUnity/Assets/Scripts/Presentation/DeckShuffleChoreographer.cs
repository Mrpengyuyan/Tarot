using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Phase 55 gave the shuffle its motion language (anticipation, action, contact,
    /// settle); Phase 72 makes it a real shuffle in three beats. The stack presses down,
    /// is cut into two piles held apart, and riffled - the cards fall one by one from
    /// alternating piles back onto the stack - twice, the second time quicker; then it
    /// squares up with a squash and a camera kick on the contact frame. Even cards (from
    /// the bottom) form the left pile and odd cards the right, so the riffle that drops
    /// them bottom-to-top lands every card exactly on its own authored rest - the
    /// PlayMode test holds the stack to its rest pose to sub-millimetre precision.
    /// </summary>
    public sealed class DeckShuffleChoreographer : MonoBehaviour
    {
        [Header("Anticipation")]
        [Tooltip("How far the whole stack presses down before the riffle, like a hand squaring the deck.")]
        [SerializeField] private float anticipationDip = 0.015f;
        [Tooltip("Seconds of the press-down.")]
        [SerializeField] private float anticipationSeconds = 0.12f;

        [Header("Phase72 Cut")]
        [Tooltip("How far each pile moves sideways from the stack centre, in the stack's local units.")]
        [SerializeField] private float cutSpread = 0.42f;
        [Tooltip("Seconds the first cut takes to part the deck.")]
        [SerializeField] private float cutSeconds = 0.33f;
        [Tooltip("Seconds the second cut takes - quicker, the hands know the move now.")]
        [SerializeField] private float recutSeconds = 0.2f;
        [Tooltip("Opposite yaw each pile takes as it is held apart.")]
        [SerializeField] private float cutYawDegrees = 6f;

        [Header("Riffle")]
        [Tooltip("How high each pile rides while held apart.")]
        [SerializeField] private float riffleLift = 0.045f;
        [Tooltip("Seconds one card takes to fall from its pile onto the stack.")]
        [SerializeField] private float riffleCardSeconds = 0.14f;
        [Tooltip("Delay between cards falling - left and right piles alternate.")]
        [SerializeField] private float riffleStagger = 0.035f;
        [Tooltip("Yaw shiver as each card falls.")]
        [SerializeField] private float riffleYawDegrees = 4f;
        [Tooltip("How far the piles' inner edges lift before the cards fall (roll about the long axis).")]
        [SerializeField] private float riffleBendDegrees = 10f;
        [Tooltip("The second riffle runs this much faster than the first.")]
        [SerializeField] private float secondRiffleSpeedup = 1.25f;

        [Header("Contact and settle")]
        [Tooltip("Squash on the frame the deck squares up (0.06 = -6% height).")]
        [SerializeField] private float contactSquash = 0.06f;
        [Tooltip("Seconds the squash takes to spring back to an exact rest.")]
        [SerializeField] private float settleSeconds = 0.15f;
        [Tooltip("A held beat after the square-up so the deck reads as ready before the fan.")]
        [SerializeField] private float squareHoldSeconds = 0.18f;
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

        /// <summary>The whole shuffle's length for the current knobs and stack size.</summary>
        public float PlannedSeconds
        {
            get
            {
                var n = Mathf.Max(1, transform.childCount);
                var riffle = riffleCardSeconds + riffleStagger * (n - 1);
                return anticipationSeconds + cutSeconds + riffle
                    + recutSeconds + riffle / Mathf.Max(0.01f, secondRiffleSpeedup)
                    + settleSeconds + squareHoldSeconds;
            }
        }

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

            // Bottom to top: the riffle drops cards in this order, so each lands on its own rest.
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

        /// <summary>
        /// Where card i waits while the deck is cut: even cards in the left pile, odd in the
        /// right, each pile stacked compactly, riding a little high, turned and bent inward.
        /// </summary>
        private void PilePose(int i, out Vector3 position, out Quaternion rotation)
        {
            var side = i % 2 == 0 ? -1f : 1f;
            var baseY = restPositions[0].y;
            var step = cards.Count > 1 ? (restPositions[cards.Count - 1].y - baseY) / (cards.Count - 1) : 0f;
            position = new Vector3(side * cutSpread, baseY + riffleLift + (i / 2) * step, restPositions[i].z);
            rotation = Quaternion.AngleAxis(-side * cutYawDegrees, Vector3.up)
                * Quaternion.AngleAxis(side * riffleBendDegrees, Vector3.forward)
                * restRotations[i];
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
                transform.localPosition = restRootPosition + Vector3.down * (anticipationDip * EaseOut(elapsed / anticipationSeconds));
                yield return null;
            }

            transform.localPosition = restRootPosition;

            // Beat 2 - cut and riffle, twice; the second pass is quicker.
            yield return Cut(cutSeconds);
            yield return Riffle(1f);
            yield return Cut(recutSeconds);
            yield return Riffle(Mathf.Max(0.01f, secondRiffleSpeedup));

            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].localPosition = restPositions[i];
                cards[i].localRotation = restRotations[i];
            }

            // Beat 3 - square: the deck squares up. The kick lands on this frame.
            CameraChoreography?.Kick(contactCameraKick);
            var squashed = new Vector3(
                restRootScale.x * (1f + contactSquash * 0.4f),
                restRootScale.y * (1f - contactSquash),
                restRootScale.z * (1f + contactSquash * 0.4f));
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(squashed, restRootScale, EaseOut(elapsed / settleSeconds));
                yield return null;
            }

            transform.localScale = restRootScale;
            transform.localPosition = restRootPosition;
            for (var elapsed = 0f; elapsed < squareHoldSeconds; elapsed += Time.deltaTime)
            {
                yield return null;
            }

            active = null;
        }

        /// <summary>The cut: every card eases from where it is into its pile.</summary>
        private IEnumerator Cut(float seconds)
        {
            var from = new Vector3[cards.Count];
            var fromRotation = new Quaternion[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                from[i] = cards[i].localPosition;
                fromRotation[i] = cards[i].localRotation;
            }

            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                var k = EaseInOut(elapsed / seconds);
                for (var i = 0; i < cards.Count; i++)
                {
                    PilePose(i, out var p, out var r);
                    cards[i].localPosition = Vector3.Lerp(from[i], p, k);
                    cards[i].localRotation = Quaternion.Slerp(fromRotation[i], r, k);
                }

                yield return null;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                PilePose(i, out var p, out var r);
                cards[i].localPosition = p;
                cards[i].localRotation = r;
            }
        }

        /// <summary>
        /// The riffle: bottom to top, each card drops from its pile onto its rest -
        /// alternating left and right - with a small yaw shiver.
        /// </summary>
        private IEnumerator Riffle(float speed)
        {
            var cardSeconds = Mathf.Max(0.01f, riffleCardSeconds / speed);
            var stagger = riffleStagger / speed;
            var total = cardSeconds + stagger * (cards.Count - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    var phase = Mathf.Clamp01((elapsed - i * stagger) / cardSeconds);
                    PilePose(i, out var p, out var r);
                    var fall = EaseIn(phase);
                    cards[i].localPosition = Vector3.Lerp(p, restPositions[i], fall);
                    var shiver = Mathf.Sin(phase * Mathf.PI) * riffleYawDegrees;
                    cards[i].localRotation = Quaternion.Slerp(r, restRotations[i], fall) * Quaternion.AngleAxis(shiver, Vector3.up);
                }

                yield return null;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].localPosition = restPositions[i];
                cards[i].localRotation = restRotations[i];
            }
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
    }
}
