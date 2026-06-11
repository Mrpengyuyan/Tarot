using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class CameraChoreographyController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform defaultPose;
        [SerializeField] private Transform deckPose;
        [SerializeField] private Transform oneCardPose;
        [SerializeField] private Transform threeCardPose;
        [SerializeField] private Transform resultPose;
        [SerializeField] private float transitionDuration = 0.75f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine activeMove;

        public bool IsMoving { get; private set; }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        public void PlayOpening()
        {
            MoveTo(defaultPose);
        }

        public void FocusDeck()
        {
            MoveTo(deckPose);
        }

        public void FocusSpread(int cardCount)
        {
            MoveTo(cardCount == 1 ? oneCardPose : threeCardPose);
        }

        public void FocusResult()
        {
            MoveTo(resultPose);
        }

        public void SnapToDefault()
        {
            ApplyPose(defaultPose);
        }

        private void MoveTo(Transform pose)
        {
            if (pose == null)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                ApplyPose(pose);
                return;
            }

            if (activeMove != null)
            {
                StopCoroutine(activeMove);
            }

            activeMove = StartCoroutine(MoveRoutine(pose));
        }

        private IEnumerator MoveRoutine(Transform pose)
        {
            IsMoving = true;
            var cameraTransform = targetCamera.transform;
            var startPosition = cameraTransform.position;
            var startRotation = cameraTransform.rotation;
            var duration = Mathf.Max(0.01f, transitionDuration);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = transitionCurve.Evaluate(elapsed / duration);
                cameraTransform.position = Vector3.Lerp(startPosition, pose.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, pose.rotation, t);
                yield return null;
            }

            ApplyPose(pose);
            IsMoving = false;
            activeMove = null;
        }

        private void ApplyPose(Transform pose)
        {
            if (targetCamera == null || pose == null)
            {
                return;
            }

            targetCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
