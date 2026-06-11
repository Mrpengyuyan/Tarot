using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualAuraController : MonoBehaviour
    {
        [SerializeField] private GameObject auraRoot;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Renderer[] runeRenderers;
        [SerializeField] private Renderer[] particleRenderers;
        [SerializeField] private Light[] auraLights;

        public float CurrentIntensity { get; private set; } = 1f;

        public void SetAuraVisible(bool visible)
        {
            if (auraRoot != null)
            {
                auraRoot.SetActive(visible);
            }

            SetRenderersVisible(glowRenderers, visible);
            SetRenderersVisible(runeRenderers, visible);
            SetRenderersVisible(particleRenderers, visible);
            SetLightsVisible(auraLights, visible);
        }

        public void SetRuneVisible(bool visible)
        {
            SetRenderersVisible(runeRenderers, visible);
        }

        public void SetParticlesVisible(bool visible)
        {
            SetRenderersVisible(particleRenderers, visible);
        }

        public void SetIntensity(float intensity)
        {
            CurrentIntensity = Mathf.Clamp01(intensity);

            if (auraLights == null)
            {
                return;
            }

            foreach (var auraLight in auraLights)
            {
                if (auraLight != null)
                {
                    auraLight.intensity = CurrentIntensity;
                }
            }
        }

        private static void SetRenderersVisible(Renderer[] renderers, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private static void SetLightsVisible(Light[] lights, bool visible)
        {
            if (lights == null)
            {
                return;
            }

            foreach (var light in lights)
            {
                if (light != null)
                {
                    light.enabled = visible;
                }
            }
        }
    }
}
