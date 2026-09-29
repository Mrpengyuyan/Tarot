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

        // Phase 74: the flip used to spin the card flat on the table (a yaw to 90 degrees and
        // back) and swap its face mid-spin - it never turned over. Now it is lifted off its slot
        // and tipped toward the player, turned over its long edge (the face swaps edge-on), held
        // while a light sweeps across the face, and set down with a small squash.
        // The beats reuse the older knobs: anticipationPause is the lift, flipDuration the turn,
        // faceRevealPause the hold, settleSeconds the set-down.
        [Header("Phase74 Lift, Turn and Sheen")]
        [Tooltip("How high the card is lifted before it turns - clear of the cloth when it stands on its edge.")]
        [SerializeField] private float raiseHeight = 0.42f;
        [Tooltip("How far the card's far end tips up toward the player while it is lifted.")]
        [SerializeField] private float raiseTiltDegrees = 10f;
        [Tooltip("Push the camera in toward the card as it flips (Phase 21). Off: the lifted card now comes toward the camera itself, and both together carried it out of the one-card close-up.")]
        [SerializeField] private bool pushInOnFlip;
        [Tooltip("How far the card drifts sideways while it turns, as if rolling over its edge.")]
        [SerializeField] private float turnSway = 0.08f;
        [Tooltip("Squash as the card touches down (0.05 = -5% height), then recovers.")]
        [SerializeField] private float setDownSquash = 0.05f;
        [Tooltip("Seconds the set-down squash takes to recover.")]
        [SerializeField] private float setDownSquashSeconds = 0.1f;
        [Tooltip("The light that sweeps across the face after the turn (a quad over the face, off until then).")]
        [SerializeField] private Renderer revealSheen;
        [Tooltip("Seconds the sweep takes to cross the face.")]
        [SerializeField] private float sheenSeconds = 0.55f;
        [Tooltip("The sweep's peak strength (its material adds light).")]
        [SerializeField] private float sheenIntensity = 0.85f;
        [Tooltip("Where the sweep starts and ends, as a texture offset - far enough that the band is off the face.")]
        [SerializeField] private float sheenTravel = 0.9f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private bool isFlipping;
        private MaterialPropertyBlock sheenBlock;
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
            var restPosition = t.localPosition;
            var restRotation = t.localRotation;
            var restScale = t.localScale;
            var side = restRotation * Vector3.right;

            void Pose(float lift, float sway, float tilt, float roll)
            {
                t.localPosition = restPosition + Vector3.up * lift + side * sway;
                t.localRotation = restRotation
                    * Quaternion.AngleAxis(-tilt, Vector3.right)
                    * Quaternion.AngleAxis(roll, Vector3.forward);
            }

            if (cameraPunchEnabled && pushInOnFlip)
            {
                CameraChoreography?.PunchToward(card.transform);
            }

            // 1) Lift - a small press straight down (the Phase 52 wind-up dip; the card stays flat
            // so no edge digs into the cloth), then the card rises off its slot, tipping its far
            // end up toward the player and cocking back against the turn as it goes.
            var raise = Mathf.Max(0.01f, anticipationPause);
            var press = raise * 0.3f;
            for (var elapsed = 0f; elapsed < press; elapsed += Time.deltaTime)
            {
                Pose(-windBackDip * EaseOut(elapsed / press), 0f, 0f, 0f);
                yield return null;
            }

            Pose(-windBackDip, 0f, 0f, 0f);
            yield return null;

            var rise = raise - press;
            for (var elapsed = 0f; elapsed < rise; elapsed += Time.deltaTime)
            {
                var k = SmoothStep(elapsed / rise);
                Pose(Mathf.Lerp(-windBackDip, raiseHeight, k), 0f, raiseTiltDegrees * k, -windBackAngle * k);
                yield return null;
            }

            // 2) Turn over the long edge. It accelerates up to edge-on, where the face swaps -
            // at full speed, with the cue and the camera kick - then swings down past flat into
            // a small overshoot while the reveal's scale pop fades. The light starts its sweep
            // as the face comes round.
            var turn = Mathf.Max(0.02f, flipDuration);
            var half = turn * 0.5f;
            for (var elapsed = 0f; elapsed < half; elapsed += Time.deltaTime)
            {
                var k = elapsed / half;
                var whole = elapsed / turn;
                Pose(raiseHeight + liftDuringFlip * Mathf.Sin(Mathf.PI * whole), -turnSway * Mathf.Sin(Mathf.PI * whole),
                    raiseTiltDegrees, Mathf.Lerp(-windBackAngle, 90f, EaseIn(k)));
                yield return null;
            }

            card.SetFaceUp(faceUp);
            RitualFeedback?.PlayCue(flipCue, card.transform);
            if (cameraPunchEnabled)
            {
                CameraChoreography?.Kick(revealCameraShake);
            }

            var sweeping = false;
            for (var elapsed = 0f; elapsed < half; elapsed += Time.deltaTime)
            {
                var k = elapsed / half;
                var whole = 0.5f + k * 0.5f;
                Pose(raiseHeight + liftDuringFlip * Mathf.Sin(Mathf.PI * whole), -turnSway * Mathf.Sin(Mathf.PI * whole),
                    raiseTiltDegrees, Mathf.Lerp(-90f, settleOvershootAngle, EaseOut(k)));
                t.localScale = restScale * (1f + revealScalePunch * (1f - k));
                if (!sweeping && k >= 0.5f && faceUp)
                {
                    sweeping = true;
                    StartCoroutine(SweepSheen(card));
                }

                yield return null;
            }

            t.localScale = restScale;
            if (!sweeping && faceUp)
            {
                StartCoroutine(SweepSheen(card));
            }

            // 3) Hold - the face is shown to the player, lifted and tipped toward them, while
            // the overshoot settles and the light crosses it.
            var hold = Mathf.Max(0.01f, faceRevealPause);
            for (var elapsed = 0f; elapsed < hold; elapsed += Time.deltaTime)
            {
                Pose(raiseHeight, 0f, raiseTiltDegrees, Mathf.Lerp(settleOvershootAngle, 0f, EaseOut(elapsed / hold)));
                yield return null;
            }

            // 4) Set down - back onto the slot, accelerating to the touch, then a small squash
            // that recovers to the exact rest the card was dealt to.
            var down = Mathf.Max(0.01f, settleSeconds);
            for (var elapsed = 0f; elapsed < down; elapsed += Time.deltaTime)
            {
                var k = elapsed / down;
                Pose(raiseHeight * (1f - EaseIn(k)), 0f, raiseTiltDegrees * (1f - SmoothStep(k)), 0f);
                yield return null;
            }

            t.localPosition = restPosition;
            t.localRotation = restRotation;
            if (cameraPunchEnabled)
            {
                CameraChoreography?.Kick(revealCameraShake * 0.4f);
            }

            var squashed = new Vector3(
                restScale.x * (1f + setDownSquash * 0.6f),
                restScale.y * (1f - setDownSquash),
                restScale.z);
            for (var elapsed = 0f; elapsed < setDownSquashSeconds; elapsed += Time.deltaTime)
            {
                t.localScale = Vector3.Lerp(squashed, restScale, EaseOut(elapsed / setDownSquashSeconds));
                yield return null;
            }

            t.localPosition = restPosition;
            t.localRotation = restRotation;
            t.localScale = restScale;
            card.SetHighlighted(false);
            isFlipping = false;
        }

        /// <summary>
        /// Phase 74: a soft band of light crosses the face once, fading in and out. The sheen is a
        /// child of the artwork; it is sized to the sprite now on it, so it lies exactly over the
        /// picture and never over the card body around it.
        /// </summary>
        private IEnumerator SweepSheen(CardView card)
        {
            if (revealSheen == null)
            {
                yield break;
            }

            var art = card != null ? card.FaceArtwork : null;
            if (art == null || art.sprite == null)
            {
                yield break;   // no picture on the face: nothing to catch the light
            }

            if (revealSheen.transform.parent == art.transform)
            {
                var bounds = art.sprite.bounds;
                var sheenTransform = revealSheen.transform;
                sheenTransform.localPosition = new Vector3(bounds.center.x, bounds.center.y, sheenTransform.localPosition.z);
                sheenTransform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
            }

            sheenBlock ??= new MaterialPropertyBlock();
            revealSheen.enabled = true;
            var seconds = Mathf.Max(0.01f, sheenSeconds);
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / seconds;
                revealSheen.GetPropertyBlock(sheenBlock);
                sheenBlock.SetVector(BaseMapSt, new Vector4(1f, 1f, Mathf.Lerp(sheenTravel, -sheenTravel, SmoothStep(k)), 0f));
                sheenBlock.SetColor(BaseColor, new Color(1f, 1f, 1f, sheenIntensity * Mathf.Sin(Mathf.PI * k)));
                revealSheen.SetPropertyBlock(sheenBlock);
                yield return null;
            }

            revealSheen.enabled = false;
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
