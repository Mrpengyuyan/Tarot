using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ThreeDCardPresentationController : MonoBehaviour
    {
        [SerializeField] private Transform cardMeshRoot;
        [SerializeField] private Renderer cardFaceRenderer;
        [SerializeField] private Renderer cardBackRenderer;
        [SerializeField] private Renderer cardDropShadowRenderer;
        private Renderer artwork;
        private Renderer[] shellParts;
        private bool[] shellStates;
        private bool printedFaceOnly;
        private bool faceIsVisible;
        private Renderer paperBack;

        public void UsePaperSurface(Renderer back)
        {
            paperBack = back;
            UsePrintedFace();
            ApplyPrintedFace();
        }

        public void UsePrintedFace()
        {
            if (printedFaceOnly) return;
            printedFaceOnly = true;
            var card = GetComponent<CardView>();
            artwork = card != null ? card.FaceArtwork : null;
            shellParts = GetComponentsInChildren<Renderer>(true);
            shellStates = new bool[shellParts.Length];
            for (var i = 0; i < shellParts.Length; i++) shellStates[i] = shellParts[i].enabled;
            ApplyPrintedFace();
        }

        public void SetFaceVisible(bool faceVisible)
        {
            faceIsVisible = faceVisible;
            if (cardFaceRenderer != null)
            {
                cardFaceRenderer.enabled = faceVisible;
            }

            if (cardBackRenderer != null)
            {
                cardBackRenderer.enabled = !faceVisible;
            }
            ApplyPrintedFace();
        }

        private void ApplyPrintedFace()
        {
            if (!printedFaceOnly) return;
            for (var i = 0; i < shellParts.Length; i++)
                if (shellParts[i] != null && shellParts[i] != artwork)
                    shellParts[i].enabled = paperBack == null && !faceIsVisible && shellStates[i];
            if (paperBack != null) paperBack.enabled = !faceIsVisible;
        }

        public void SetDropShadowVisible(bool visible)
        {
            if (cardDropShadowRenderer != null)
            {
                cardDropShadowRenderer.enabled = visible && paperBack == null && (!printedFaceOnly || !faceIsVisible);
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
