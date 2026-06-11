using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class DimensionalCardRevealController : MonoBehaviour
    {
        [SerializeField] private Transform cardRoot;
        [SerializeField] private Renderer revealGlowRenderer;
        [SerializeField] private float revealLift = 0.08f;
        [SerializeField] private float revealScale = 1.035f;
        [SerializeField] private float settleSeconds = 0.18f;
        [SerializeField] private AnimationCurve settleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 restingLocalPosition;
        private Vector3 restingLocalScale;
        private Coroutine activeRoutine;

        private Transform Target => cardRoot != null ? cardRoot : transform;

        private void Awake()
        {
            CaptureRestingPose();
            SetGlowVisible(false);
        }

        public void CaptureRestingPose()
        {
            restingLocalPosition = Target.localPosition;
            restingLocalScale = Target.localScale;
        }

        public void PlayReveal()
        {
            if (!isActiveAndEnabled)
            {
                SetGlowVisible(true);
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                Target.localPosition = restingLocalPosition;
                Target.localScale = restingLocalScale;
                activeRoutine = null;
            }
            else
            {
                CaptureRestingPose();
            }

            activeRoutine = StartCoroutine(RevealRoutine());
        }

        public void SetGlowVisible(bool visible)
        {
            if (revealGlowRenderer != null)
            {
                revealGlowRenderer.enabled = visible;
            }
        }

        private IEnumerator RevealRoutine()
        {
            SetGlowVisible(true);

            var target = Target;
            var liftedPosition = restingLocalPosition + Vector3.up * revealLift;
            var liftedScale = restingLocalScale * revealScale;

            target.localPosition = liftedPosition;
            target.localScale = liftedScale;

            var duration = Mathf.Max(0.01f, settleSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var normalized = elapsed / duration;
                var t = settleCurve != null ? settleCurve.Evaluate(normalized) : normalized;
                target.localPosition = Vector3.Lerp(liftedPosition, restingLocalPosition, t);
                target.localScale = Vector3.Lerp(liftedScale, restingLocalScale, t);
                yield return null;
            }

            target.localPosition = restingLocalPosition;
            target.localScale = restingLocalScale;
            activeRoutine = null;
        }
    }
}
