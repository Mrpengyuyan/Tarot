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

        // Target world footprint (width along X, length along Z) the face artwork must fit
        // within on the card, preserving aspect. SetFaceArtwork scales the sprite to this
        // so a card face never depends on the source image's resolution / pixels-per-unit
        // (HD sprites import at PPU 100 and would otherwise render many times oversized).
        [SerializeField] private Vector2 faceArtworkWorldSize = new Vector2(0.64f, 1.0f);

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
            FitFaceArtwork();
        }

        /// <summary>
        /// Scales the face artwork renderer so the assigned sprite fits the target world
        /// footprint, preserving aspect. Bounds are only valid once the renderer's
        /// GameObject is active in the hierarchy, so this is a no-op while the card is
        /// face down; SetFaceUp re-runs it after activating the front so the sized art is
        /// correct whether the sprite is assigned before or after the flip.
        /// </summary>
        private void FitFaceArtwork()
        {
            if (faceArtworkRenderer == null || faceArtworkRenderer.sprite == null)
            {
                return;
            }

            if (!faceArtworkRenderer.gameObject.activeInHierarchy)
            {
                return;
            }

            var target = faceArtworkWorldSize;
            if (target.x < 1e-3f || target.y < 1e-3f)
            {
                target = new Vector2(0.64f, 1.0f);
            }

            var tf = faceArtworkRenderer.transform;
            tf.localScale = Vector3.one;
            var size = faceArtworkRenderer.bounds.size; // world AABB of the sprite at unit local scale
            if (size.x < 1e-5f || size.z < 1e-5f)
            {
                return;
            }

            var fit = Mathf.Min(target.x / size.x, target.y / size.z);
            if (fit <= 0f || float.IsInfinity(fit) || float.IsNaN(fit))
            {
                fit = 1f;
            }

            tf.localScale = new Vector3(fit, fit, tf.localScale.z);
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

            if (faceUp)
            {
                // The front is now active, so the face renderer bounds are valid and the
                // artwork (assigned while the card was face down during the deal) can be
                // sized to the card.
                FitFaceArtwork();
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
