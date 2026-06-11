using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualAuraMotionController : MonoBehaviour
    {
        private const float TwoPi = Mathf.PI * 2f;

        [SerializeField] private RitualAuraController auraController;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private Transform[] glowPulsers;
        [SerializeField] private Transform[] runeRings;
        [SerializeField] private Transform[] particleAnchors;
        [SerializeField] private bool animateOnEnable = true;
        [SerializeField] private float runeRotationSpeedDegrees = 7.5f;
        [SerializeField] private float pulseSpeed = 0.55f;
        [SerializeField] private float pulseAmplitude = 0.08f;
        [SerializeField] private float particleFloatSpeed = 0.75f;
        [SerializeField] private float particleFloatAmplitude = 0.018f;

        private Vector3[] baseGlowScales;
        private Vector3[] baseParticlePositions;
        private bool[] baseGlowScalesCaptured;
        private bool[] baseParticlePositionsCaptured;

        public bool IsAnimating { get; private set; }
        public float CurrentPulse { get; private set; } = 1f;

        private void OnEnable()
        {
            CaptureBaseState();
            SetAnimating(animateOnEnable);
        }

        private void Update()
        {
            if (IsAnimating)
            {
                Tick(Time.deltaTime, Time.time);
            }
        }

        public void SetAnimating(bool animating)
        {
            IsAnimating = animating;
        }

        public void Tick(float deltaSeconds, float elapsedSeconds)
        {
            CaptureBaseState();

            if (!IsAnimating)
            {
                return;
            }

            RotateRuneRings(deltaSeconds);
            PulseGlowPulsers(elapsedSeconds);
            FloatParticleAnchors(elapsedSeconds);
        }

        public void ResetMotion()
        {
            CaptureBaseState();
            RestoreGlowPulsers();
            RestoreParticleAnchors();
            CurrentPulse = 1f;
        }

        private void CaptureBaseState()
        {
            EnsureGlowBaseState();
            EnsureParticleBaseState();
        }

        private void EnsureGlowBaseState()
        {
            if (glowPulsers == null)
            {
                baseGlowScales = null;
                baseGlowScalesCaptured = null;
                return;
            }

            if (baseGlowScales == null || baseGlowScales.Length != glowPulsers.Length)
            {
                baseGlowScales = new Vector3[glowPulsers.Length];
                baseGlowScalesCaptured = new bool[glowPulsers.Length];
            }

            for (var index = 0; index < glowPulsers.Length; index++)
            {
                var pulser = glowPulsers[index];
                if (pulser != null && !baseGlowScalesCaptured[index])
                {
                    baseGlowScales[index] = pulser.localScale;
                    baseGlowScalesCaptured[index] = true;
                }
            }
        }

        private void EnsureParticleBaseState()
        {
            if (particleAnchors == null)
            {
                baseParticlePositions = null;
                baseParticlePositionsCaptured = null;
                return;
            }

            if (baseParticlePositions == null || baseParticlePositions.Length != particleAnchors.Length)
            {
                baseParticlePositions = new Vector3[particleAnchors.Length];
                baseParticlePositionsCaptured = new bool[particleAnchors.Length];
            }

            for (var index = 0; index < particleAnchors.Length; index++)
            {
                var anchor = particleAnchors[index];
                if (anchor != null && !baseParticlePositionsCaptured[index])
                {
                    baseParticlePositions[index] = anchor.localPosition;
                    baseParticlePositionsCaptured[index] = true;
                }
            }
        }

        private void RotateRuneRings(float deltaSeconds)
        {
            if (runeRings == null)
            {
                return;
            }

            var rotation = runeRotationSpeedDegrees * deltaSeconds;
            foreach (var ring in runeRings)
            {
                if (ring != null)
                {
                    ring.Rotate(0f, rotation, 0f, Space.Self);
                }
            }
        }

        private void PulseGlowPulsers(float elapsedSeconds)
        {
            if (glowPulsers == null || baseGlowScales == null)
            {
                return;
            }

            CurrentPulse = 1f + Mathf.Sin(elapsedSeconds * pulseSpeed * TwoPi) * pulseAmplitude;
            for (var index = 0; index < glowPulsers.Length; index++)
            {
                var pulser = glowPulsers[index];
                if (pulser != null)
                {
                    pulser.localScale = baseGlowScales[index] * CurrentPulse;
                }
            }
        }

        private void FloatParticleAnchors(float elapsedSeconds)
        {
            if (particleAnchors == null || baseParticlePositions == null)
            {
                return;
            }

            for (var index = 0; index < particleAnchors.Length; index++)
            {
                var anchor = particleAnchors[index];
                if (anchor == null)
                {
                    continue;
                }

                var phase = elapsedSeconds * particleFloatSpeed * TwoPi + index;
                var offset = Mathf.Sin(phase) * particleFloatAmplitude;
                anchor.localPosition = baseParticlePositions[index] + Vector3.up * offset;
            }
        }

        private void RestoreGlowPulsers()
        {
            if (glowPulsers == null || baseGlowScales == null)
            {
                return;
            }

            for (var index = 0; index < glowPulsers.Length; index++)
            {
                var pulser = glowPulsers[index];
                if (pulser != null)
                {
                    pulser.localScale = baseGlowScales[index];
                }
            }
        }

        private void RestoreParticleAnchors()
        {
            if (particleAnchors == null || baseParticlePositions == null)
            {
                return;
            }

            for (var index = 0; index < particleAnchors.Length; index++)
            {
                var anchor = particleAnchors[index];
                if (anchor != null)
                {
                    anchor.localPosition = baseParticlePositions[index];
                }
            }
        }
    }
}
