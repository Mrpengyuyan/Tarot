using System;
using TarotUnity.Data;
using TarotUnity.Presentation;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer frontRenderer;
        [SerializeField] private SpriteRenderer backRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private SpriteRenderer faceArtworkRenderer;
        [SerializeField] private DimensionalCardRevealController dimensionalRevealController;
        [SerializeField] private ThreeDCardPresentationController threeDPresentationController;
        [SerializeField] private GameObject frontRoot;
        [SerializeField] private GameObject backRoot;
        [SerializeField] private GameObject highlightRoot;
        [SerializeField] private TextMesh titleLabel;
        [SerializeField] private TextMesh positionLabel;

        public event Action<CardView, bool> FaceChanged;

        public CardDrawData DrawData { get; private set; }
        public bool IsFaceUp { get; private set; }

        public void Bind(CardDrawData drawData)
        {
            DrawData = drawData;

            if (titleLabel != null)
            {
                titleLabel.text = drawData?.tarot_card?.name_zh ?? "Unknown Card";
            }

            if (positionLabel != null)
            {
                var reversedSuffix = drawData != null && drawData.is_reversed ? " (Reversed)" : string.Empty;
                positionLabel.text = $"{drawData?.position_name ?? $"Position {drawData?.position ?? 0}"}{reversedSuffix}";
            }

            SetFaceArtwork(null);
            SetFaceUp(false);
        }

        public void SetFaceArtwork(Sprite sprite)
        {
            if (faceArtworkRenderer == null)
            {
                return;
            }

            faceArtworkRenderer.sprite = sprite;
            faceArtworkRenderer.enabled = sprite != null && IsFaceUp;
        }

        public void SetFaceUp(bool faceUp)
        {
            var wasFaceUp = IsFaceUp;
            IsFaceUp = faceUp;

            if (frontRenderer != null)
            {
                frontRenderer.enabled = faceUp;
            }

            if (backRenderer != null)
            {
                backRenderer.enabled = !faceUp;
            }

            if (faceArtworkRenderer != null)
            {
                faceArtworkRenderer.enabled = faceUp && faceArtworkRenderer.sprite != null;
            }

            if (frontRoot != null)
            {
                frontRoot.SetActive(faceUp);
            }

            if (backRoot != null)
            {
                backRoot.SetActive(!faceUp);
            }

            if (dimensionalRevealController != null)
            {
                dimensionalRevealController.SetGlowVisible(faceUp);
                if (faceUp && !wasFaceUp)
                {
                    dimensionalRevealController.PlayReveal();
                }
            }

            if (threeDPresentationController != null)
            {
                threeDPresentationController.SetFaceVisible(faceUp);
                threeDPresentationController.SetDropShadowVisible(true);
            }

            FaceChanged?.Invoke(this, faceUp);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = highlighted;
            }

            if (highlightRoot != null)
            {
                highlightRoot.SetActive(highlighted);
            }
        }
    }
}
