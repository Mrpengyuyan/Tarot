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

        // Phase 71: how much wider the glow grows while the pointer is over the card.
        [SerializeField] private float hoverHaloScale = 1.45f;

        private bool awaitingFlip;
        private bool hovered;
        private bool haloScaleCaptured;
        private Vector3 haloRestScale;
        private float haloBoost = 1f;
        private float? hoverHaloScaleOverride;

        /// <summary>Phase 72: the fan keeps its hover glow at rest size; landed cards use the serialized default.</summary>
        public float HoverHaloScale
        {
            get => hoverHaloScaleOverride ?? hoverHaloScale;
            set
            {
                hoverHaloScaleOverride = value;
                ApplyHalo();
            }
        }

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

            ApplyHalo();   // Phase 71: a face-up card no longer glows

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

        /// <summary>
        /// The deal's "flip me": a face-down card waiting to be turned glows softly under its
        /// edges. The deal and the flip drive this; hovering is separate (<see cref="SetHovered"/>),
        /// so leaving the card no longer puts the invitation out.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            awaitingFlip = highlighted;
            ApplyHalo();
        }

        /// <summary>Phase 71: the pointer over a face-down card widens its glow.</summary>
        public void SetHovered(bool hovered)
        {
            this.hovered = hovered;
            ApplyHalo();
        }

        /// <summary>Phase 72: the pick's hover beat swells the glow (1 = none). A boost shows the glow on its own.</summary>
        public void SetHaloBoost(float multiplier)
        {
            haloBoost = Mathf.Max(0f, multiplier);
            ApplyHalo();
        }

        /// <summary>Phase 72: back to the serialized hover scale once a fan card has left the fan.</summary>
        public void ClearHoverHaloScale()
        {
            hoverHaloScaleOverride = null;
            ApplyHalo();
        }

        private void ApplyHalo()
        {
            var boosted = haloBoost > 1.001f;
            var visible = !IsFaceUp && (awaitingFlip || hovered || boosted);
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = visible;
            }

            if (highlightRoot == null)
            {
                return;
            }

            if (!haloScaleCaptured)
            {
                haloRestScale = highlightRoot.transform.localScale;
                haloScaleCaptured = true;
            }

            var scale = (hovered ? HoverHaloScale : 1f) * haloBoost;
            highlightRoot.transform.localScale = haloRestScale * scale;
            highlightRoot.SetActive(visible);
        }
    }
}
