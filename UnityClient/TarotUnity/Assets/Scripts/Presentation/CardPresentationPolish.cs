using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class CardPresentationPolish : MonoBehaviour
    {
        [SerializeField] private GameObject frontFrame;
        [SerializeField] private GameObject backSigil;
        [SerializeField] private GameObject innerGlow;
        [SerializeField] private Renderer frameRenderer;
        [SerializeField] private Renderer sigilRenderer;

        public void SetGlowVisible(bool visible)
        {
            if (innerGlow != null)
            {
                innerGlow.SetActive(visible);
            }
        }
    }
}
