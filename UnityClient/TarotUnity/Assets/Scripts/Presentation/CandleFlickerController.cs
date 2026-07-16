using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Makes a candle burn. A still flame is the clearest tell that a lit scene
    /// is a render: real candlelight never holds one value, and the flame body
    /// breathes and leans as it does.
    ///
    /// Two Perlin bands are layered - a slow wander and a faster shimmer - so the
    /// motion never falls into an audible loop the way a single sine does. Each
    /// candle gets its own seed, because two candles flickering in unison is its
    /// own kind of wrong.
    ///
    /// Allocation-free per frame (Perlin sampling and struct math only), per the
    /// Phase 36 baseline.
    /// </summary>
    public sealed class CandleFlickerController : MonoBehaviour
    {
        [SerializeField] private Light flameLight;
        [SerializeField] private Transform flameBillboard;

        [Tooltip("Light intensity the flicker rides around. Captured on Awake if left at 0.")]
        [SerializeField] private float baseIntensity;

        [Range(0f, 0.6f)]
        [SerializeField] private float intensityFlicker = 0.16f;

        [Range(0f, 0.4f)]
        [SerializeField] private float flameScaleFlicker = 0.11f;

        [SerializeField] private float flickerSpeed = 4.5f;

        private float seed;
        private Vector3 flameBaseScale = Vector3.one;

        private void Awake()
        {
            if (flameLight == null)
            {
                flameLight = GetComponent<Light>();
            }

            if (baseIntensity <= 0f && flameLight != null)
            {
                baseIntensity = flameLight.intensity;
            }

            if (flameBillboard != null)
            {
                flameBaseScale = flameBillboard.localScale;
            }

            // Position in the noise field, not a time offset: two candles started
            // on the same frame would otherwise share a waveform.
            seed = Random.Range(0f, 128f);
        }

        private void Update()
        {
            var t = Time.time * flickerSpeed;

            // Slow wander plus a faster shimmer, weighted so the flame mostly
            // drifts and only occasionally gutters.
            var wander = Mathf.PerlinNoise(seed, t * 0.35f);
            var shimmer = Mathf.PerlinNoise(seed + 37f, t);
            var n = wander * 0.65f + shimmer * 0.35f;

            // Perlin centres near 0.5; remap so the flicker sits around the base.
            var signed = (n - 0.5f) * 2f;

            if (flameLight != null)
            {
                flameLight.intensity = baseIntensity * (1f + signed * intensityFlicker);
            }

            if (flameBillboard != null)
            {
                // The flame stretches taller as it brightens and squats as it dips.
                var stretch = 1f + signed * flameScaleFlicker;
                flameBillboard.localScale = new Vector3(
                    flameBaseScale.x * (1f - signed * flameScaleFlicker * 0.35f),
                    flameBaseScale.y * stretch,
                    flameBaseScale.z);
            }
        }
    }
}
