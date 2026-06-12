using UnityEngine;
using UnityEngine.EventSystems;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Hearthstone-style physicality for face-down cards: hovering lifts the
    /// card slightly and tilts it toward the pointer, so it reads as a real
    /// object under the player's fingertips. Response stays near 100ms per
    /// interaction-feedback research. Flip suspends the hover permanently for
    /// that card so the two never fight over the transform.
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public sealed class CardHoverTiltController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private float hoverLift = 0.045f;
        [SerializeField] private float maxTiltDegrees = 7f;
        [SerializeField] private float responseSeconds = 0.06f;
        [SerializeField] private float cardHalfWidth = 0.35f;
        [SerializeField] private float cardHalfLength = 0.49f;

        private CardView cardView;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private bool restCaptured;
        private bool hovering;
        private bool suspended;
        private float currentLift;
        private Vector2 currentTilt;
        private Vector2 targetTilt;

        public bool IsHovering => hovering;
        public bool IsSuspended => suspended;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverExit();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.isValid)
            {
                HoverMove(eventData.pointerCurrentRaycast.worldPosition);
            }
        }

        public void HoverEnter()
        {
            if (suspended || (cardView != null && cardView.IsFaceUp))
            {
                return;
            }

            if (!restCaptured)
            {
                CaptureRestPose();
            }

            hovering = true;
        }

        public void HoverMove(Vector3 worldPoint)
        {
            if (!hovering)
            {
                return;
            }

            var local = transform.InverseTransformPoint(worldPoint);
            var x = Mathf.Clamp(local.x / Mathf.Max(0.01f, cardHalfWidth), -1f, 1f);
            var z = Mathf.Clamp(local.z / Mathf.Max(0.01f, cardHalfLength), -1f, 1f);
            targetTilt = new Vector2(z * maxTiltDegrees, -x * maxTiltDegrees);
        }

        public void HoverExit()
        {
            hovering = false;
            targetTilt = Vector2.zero;
        }

        public void Suspend()
        {
            suspended = true;
            ReleaseImmediate();
        }

        public void ReleaseImmediate()
        {
            hovering = false;
            targetTilt = Vector2.zero;
            currentTilt = Vector2.zero;
            currentLift = 0f;

            if (restCaptured)
            {
                transform.localPosition = restLocalPosition;
                transform.localRotation = restLocalRotation;
            }
        }

        private void Update()
        {
            if (!restCaptured)
            {
                return;
            }

            var targetLift = hovering ? hoverLift : 0f;
            var blend = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, responseSeconds));
            currentLift = Mathf.Lerp(currentLift, targetLift, blend);
            currentTilt = Vector2.Lerp(currentTilt, targetTilt, blend);

            if (!hovering && currentLift < 0.0005f && currentTilt.sqrMagnitude < 0.001f)
            {
                transform.localPosition = restLocalPosition;
                transform.localRotation = restLocalRotation;
                return;
            }

            transform.localPosition = restLocalPosition + Vector3.up * currentLift;
            transform.localRotation = restLocalRotation * Quaternion.Euler(currentTilt.x, 0f, currentTilt.y);
        }

        private void CaptureRestPose()
        {
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
            restCaptured = true;
        }
    }
}
