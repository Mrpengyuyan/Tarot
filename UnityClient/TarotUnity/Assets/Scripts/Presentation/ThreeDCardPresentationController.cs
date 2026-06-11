using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ThreeDCardPresentationController : MonoBehaviour
    {
        [SerializeField] private Transform cardMeshRoot;
        [SerializeField] private Renderer cardFaceRenderer;
        [SerializeField] private Renderer cardBackRenderer;
        [SerializeField] private Renderer cardDropShadowRenderer;

        public void SetFaceVisible(bool faceVisible)
        {
            if (cardFaceRenderer != null)
            {
                cardFaceRenderer.enabled = faceVisible;
            }

            if (cardBackRenderer != null)
            {
                cardBackRenderer.enabled = !faceVisible;
            }
        }

        public void SetDropShadowVisible(bool visible)
        {
            if (cardDropShadowRenderer != null)
            {
                cardDropShadowRenderer.enabled = visible;
            }
        }

        public void SetShellVisible(bool visible)
        {
            if (cardMeshRoot != null)
            {
                cardMeshRoot.gameObject.SetActive(visible);
            }
        }
    }
}
