using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class CameraChoreographyController : MonoBehaviour
    {
        // Phase 63: a spread can register its own framing pose (by card count) so a
        // large spread like the Celtic Cross gets a pulled-back shot. The legacy
        // one/three-card poses below stay as the fallback.
        [System.Serializable]
        public sealed class SpreadPose
        {
            public int cardCount;
            public Transform pose;
            public float fov = 60f;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform defaultPose;
        [SerializeField] private Transform deckPose;
        [SerializeField] private Transform oneCardPose;
        [SerializeField] private Transform threeCardPose;
        [SerializeField] private Transform resultPose;
        [SerializeField] private SpreadPose[] spreadPoses = new SpreadPose[0];
        [SerializeField] private float transitionDuration = 0.75f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Phase21 Pose Framing")]
        [SerializeField] private float defaultFov = 60f;
        [SerializeField] private float deckFov = 60f;
        [SerializeField] private float oneCardFov = 60f;
        [SerializeField] private float threeCardFov = 60f;
        [SerializeField] private float resultFov = 60f;

        [Header("Phase21 Idle Breathing")]
        [SerializeField] private bool breathingEnabled;
        [SerializeField] private float breathPositionAmplitude = 0.03f;
        [SerializeField] private float breathRotationAmplitude = 0.3f;
        [SerializeField] private float breathFrequency = 0.16f;

        [Header("Phase21 Flip Punch-In")]
        [SerializeField] private float punchTravelDistance = 0.55f;
        [SerializeField] private float punchFovDelta = -4f;
        [SerializeField] private float punchInSeconds = 0.26f;
        [SerializeField] private float punchHoldSeconds = 0.5f;
        [SerializeField] private float punchReturnSeconds = 0.5f;
        [SerializeField] private float punchShakeAmplitude = 0.035f;
        [SerializeField] private float shakeDecayPerSecond = 0.12f;

        private Coroutine activeMove;
        private Coroutine activePunch;
        private Vector3 basePosition;
        private Quaternion baseRotation = Quaternion.identity;
        private float baseFov = 60f;
        private bool baseInitialized;
        private float punchWeight;
        private Vector3 punchDirection;
        private float shakeEnergy;
        private float breathSeed;

        public bool IsMoving { get; private set; }
        public bool IsPunching => activePunch != null;
        public float CurrentBaseFov => baseFov;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            breathSeed = Random.Range(0f, 100f);
            CaptureBaseFromCamera();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || !baseInitialized)
            {
                return;
            }

            if (shakeEnergy > 0.0001f)
            {
                shakeEnergy = Mathf.MoveTowards(shakeEnergy, 0f, shakeDecayPerSecond * Time.deltaTime);
            }

            ComposeCameraFrame();
        }

        public void PlayOpening()
        {
            MoveTo(defaultPose, defaultFov);
        }

        public void FocusDeck()
        {
            MoveTo(deckPose, deckFov);
        }

        public void FocusSpread(int cardCount)
        {
            foreach (var entry in spreadPoses)
            {
                if (entry != null && entry.cardCount == cardCount && entry.pose != null)
                {
                    MoveTo(entry.pose, entry.fov);
                    return;
                }
            }

            if (cardCount == 1)
            {
                MoveTo(oneCardPose, oneCardFov);
            }
            else
            {
                MoveTo(threeCardPose, threeCardFov);
            }
        }

        public void FocusResult()
        {
            MoveTo(resultPose, resultFov);
        }

        public void SnapToDefault()
        {
            ApplyPose(defaultPose, defaultFov);
        }

        public void PunchToward(Transform focus)
        {
            if (focus == null || targetCamera == null || !baseInitialized || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (activePunch != null)
            {
                StopCoroutine(activePunch);
            }

            punchDirection = (focus.position - basePosition).normalized;
            activePunch = StartCoroutine(PunchRoutine());
        }

        public void Kick(float amplitude)
        {
            shakeEnergy = Mathf.Max(shakeEnergy, Mathf.Abs(amplitude));
        }

        private void MoveTo(Transform pose, float fov)
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

            CancelPunch();

            if (!gameObject.activeInHierarchy)
            {
                ApplyPose(pose, fov);
                return;
            }

            if (activeMove != null)
            {
                StopCoroutine(activeMove);
            }

            activeMove = StartCoroutine(MoveRoutine(pose, fov));
        }

        private IEnumerator MoveRoutine(Transform pose, float fov)
        {
            IsMoving = true;
            if (!baseInitialized)
            {
                CaptureBaseFromCamera();
            }

            var startPosition = basePosition;
            var startRotation = baseRotation;
            var startFov = baseFov;
            var duration = Mathf.Max(0.01f, transitionDuration);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = transitionCurve.Evaluate(elapsed / duration);
                basePosition = Vector3.Lerp(startPosition, pose.position, t);
                baseRotation = Quaternion.Slerp(startRotation, pose.rotation, t);
                baseFov = Mathf.Lerp(startFov, fov, t);
                yield return null;
            }

            ApplyPose(pose, fov);
            IsMoving = false;
            activeMove = null;
        }

        private IEnumerator PunchRoutine()
        {
            var inDuration = Mathf.Max(0.01f, punchInSeconds);
            for (var elapsed = 0f; elapsed < inDuration; elapsed += Time.deltaTime)
            {
                var t = elapsed / inDuration;
                punchWeight = 1f - (1f - t) * (1f - t);
                yield return null;
            }

            punchWeight = 1f;
            Kick(punchShakeAmplitude);

            for (var elapsed = 0f; elapsed < punchHoldSeconds; elapsed += Time.deltaTime)
            {
                yield return null;
            }

            var returnDuration = Mathf.Max(0.01f, punchReturnSeconds);
            for (var elapsed = 0f; elapsed < returnDuration; elapsed += Time.deltaTime)
            {
                var t = elapsed / returnDuration;
                punchWeight = 1f - (t * t * (3f - 2f * t));
                yield return null;
            }

            punchWeight = 0f;
            activePunch = null;
        }

        private void CancelPunch()
        {
            if (activePunch != null)
            {
                StopCoroutine(activePunch);
                activePunch = null;
            }

            punchWeight = 0f;
        }

        private void ApplyPose(Transform pose, float fov)
        {
            if (targetCamera == null || pose == null)
            {
                return;
            }

            basePosition = pose.position;
            baseRotation = pose.rotation;
            baseFov = fov;
            baseInitialized = true;
            ComposeCameraFrame();
        }

        private void CaptureBaseFromCamera()
        {
            if (targetCamera == null)
            {
                return;
            }

            basePosition = targetCamera.transform.position;
            baseRotation = targetCamera.transform.rotation;
            baseFov = targetCamera.fieldOfView;
            baseInitialized = true;
        }

        private void ComposeCameraFrame()
        {
            var position = basePosition;
            var rotation = baseRotation;
            var fov = baseFov;

            if (breathingEnabled && Application.isPlaying)
            {
                var t = Time.time * breathFrequency;
                position += new Vector3(
                    SampleNoise(t, 0.13f) * breathPositionAmplitude,
                    SampleNoise(t, 0.47f) * breathPositionAmplitude * 0.6f,
                    SampleNoise(t, 0.71f) * breathPositionAmplitude * 0.4f);
                rotation *= Quaternion.Euler(
                    SampleNoise(t, 0.29f) * breathRotationAmplitude,
                    SampleNoise(t, 0.83f) * breathRotationAmplitude,
                    0f);
            }

            if (punchWeight > 0f)
            {
                position += punchDirection * (punchTravelDistance * punchWeight);
                fov += punchFovDelta * punchWeight;
            }

            if (shakeEnergy > 0.0001f)
            {
                var shakeTime = Time.time * 23f;
                position += new Vector3(
                    SampleNoise(shakeTime, 0.31f),
                    SampleNoise(shakeTime, 0.67f),
                    0f) * shakeEnergy;
            }

            targetCamera.transform.SetPositionAndRotation(position, rotation);
            targetCamera.fieldOfView = fov;
        }

        private float SampleNoise(float t, float seedOffset)
        {
            return (Mathf.PerlinNoise(t, breathSeed + seedOffset * 37f) - 0.5f) * 2f;
        }
    }
}
