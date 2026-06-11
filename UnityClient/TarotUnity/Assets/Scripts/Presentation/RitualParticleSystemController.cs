using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualParticleSystemController : MonoBehaviour
    {
        [SerializeField] private RitualAuraController auraController;
        [SerializeField] private ParticleSystem[] ambientParticles;
        [SerializeField] private ParticleSystem[] focusParticles;
        [SerializeField] private ParticleSystem[] revealParticles;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float baseEmissionMultiplier = 1f;
        [SerializeField] private float focusEmissionMultiplier = 0.85f;
        [SerializeField] private float revealEmissionMultiplier = 1.35f;
        [SerializeField] private float intensity = 0.65f;

        private readonly Dictionary<ParticleSystem, float> baseEmissionRates = new Dictionary<ParticleSystem, float>();

        public bool IsPlaying { get; private set; }
        public float CurrentIntensity { get; private set; } = 0.65f;

        private void OnEnable()
        {
            SetIntensity(intensity);

            if (playOnEnable)
            {
                PlayAmbient();
            }
        }

        public void SetParticlesVisible(bool visible)
        {
            if (visible)
            {
                SetObjectsVisible(ambientParticles, true);
                SetObjectsVisible(focusParticles, true);
                SetObjectsVisible(revealParticles, true);
                SetIntensity(CurrentIntensity);
                PlayAmbient();
            }
            else
            {
                StopAll(false);
                SetObjectsVisible(ambientParticles, false);
                SetObjectsVisible(focusParticles, false);
                SetObjectsVisible(revealParticles, false);
            }
        }

        public void SetIntensity(float value)
        {
            CurrentIntensity = Mathf.Clamp01(value);
            ApplyEmission(ambientParticles, baseEmissionMultiplier);
            ApplyEmission(focusParticles, focusEmissionMultiplier);
            ApplyEmission(revealParticles, revealEmissionMultiplier);
        }

        public void PlayAmbient()
        {
            PlayAll(ambientParticles);
            PlayAll(focusParticles);
            IsPlaying = HasAnyValidParticle(ambientParticles) || HasAnyValidParticle(focusParticles);
        }

        public void StopAll(bool clear)
        {
            StopParticles(ambientParticles, clear);
            StopParticles(focusParticles, clear);
            StopParticles(revealParticles, clear);
            IsPlaying = false;
        }

        public void TriggerRevealBurst()
        {
            foreach (var particles in Enumerate(revealParticles))
            {
                EnsureParticleObjectVisible(particles);
                var burstCount = Mathf.Max(1, Mathf.RoundToInt(12f * Mathf.Max(0.15f, CurrentIntensity) * revealEmissionMultiplier));
                particles.Emit(burstCount);
            }
        }

        public void SimulateTick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            SimulateParticles(ambientParticles, deltaSeconds);
            SimulateParticles(focusParticles, deltaSeconds);
            SimulateParticles(revealParticles, deltaSeconds);
        }

        private void ApplyEmission(ParticleSystem[] particles, float multiplier)
        {
            foreach (var system in Enumerate(particles))
            {
                var emission = system.emission;
                if (!emission.enabled)
                {
                    emission.enabled = true;
                }

                var baseRate = CaptureBaseEmissionRate(system);
                emission.rateOverTime = Mathf.Max(0f, baseRate * Mathf.Max(0f, multiplier) * CurrentIntensity);
            }
        }

        private float CaptureBaseEmissionRate(ParticleSystem particles)
        {
            if (baseEmissionRates.TryGetValue(particles, out var rate))
            {
                return rate;
            }

            rate = Mathf.Max(0f, particles.emission.rateOverTime.constantMax);
            baseEmissionRates[particles] = rate;
            return rate;
        }

        private static void SetObjectsVisible(ParticleSystem[] particles, bool visible)
        {
            foreach (var system in Enumerate(particles))
            {
                system.gameObject.SetActive(visible);
            }
        }

        private static void PlayAll(ParticleSystem[] particles)
        {
            foreach (var system in Enumerate(particles))
            {
                EnsureParticleObjectVisible(system);
                system.Play(true);
            }
        }

        private static void StopParticles(ParticleSystem[] particles, bool clear)
        {
            var behavior = clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;

            foreach (var system in Enumerate(particles))
            {
                system.Stop(true, behavior);
            }
        }

        private static void SimulateParticles(ParticleSystem[] particles, float deltaSeconds)
        {
            foreach (var system in Enumerate(particles))
            {
                system.Simulate(deltaSeconds, true, false, false);
            }
        }

        private static bool HasAnyValidParticle(ParticleSystem[] particles)
        {
            foreach (var unused in Enumerate(particles))
            {
                return true;
            }

            return false;
        }

        private static void EnsureParticleObjectVisible(ParticleSystem particles)
        {
            if (!particles.gameObject.activeSelf)
            {
                particles.gameObject.SetActive(true);
            }
        }

        private static IEnumerable<ParticleSystem> Enumerate(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                yield break;
            }

            foreach (var system in particles)
            {
                if (system != null)
                {
                    yield return system;
                }
            }
        }
    }
}
