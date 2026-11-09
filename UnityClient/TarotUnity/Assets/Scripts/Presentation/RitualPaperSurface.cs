using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>The printed surface belongs to the physical card, not a temporary destination.</summary>
    [DisallowMultipleComponent]
    public sealed class RitualPaperSurface : MonoBehaviour
    {
        public const float PaperWidth = 0.64f;
        private static Sprite backSprite;
        private CardView card;
        private SpriteRenderer back;
        private ThreeDCardPresentationController presentation;
        private MaterialPropertyBlock paperInk;
        private static readonly int Glare = Shader.PropertyToID("_GlareIntensity");
        private static readonly int Iridescence = Shader.PropertyToID("_Iridescence");

        public SpriteRenderer Back => back;

        public static RitualPaperSurface Attach(CardView card)
        {
            var surface = card.GetComponent<RitualPaperSurface>();
            if (surface != null) { surface.RefreshFace(card, card.IsFaceUp); return surface; }
            surface = card.gameObject.AddComponent<RitualPaperSurface>();
            surface.card = card;
            if (backSprite == null)
            {
                foreach (var renderer in card.GetComponentsInChildren<Renderer>(true))
                {
                    var material = renderer.sharedMaterial;
                    if (material == null || material.mainTexture is not Texture2D texture || texture.name != "TarotCardBack") continue;
                    backSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 100f);
                    backSprite.name = "RitualPrintedBack";
                    backSprite.hideFlags = HideFlags.HideAndDontSave;
                    break;
                }
            }
            var node = new GameObject("RitualPaperBack", typeof(SpriteRenderer));
            node.transform.SetParent(card.transform, false);
            surface.back = node.GetComponent<SpriteRenderer>();
            surface.back.sprite = backSprite;
            Fit(surface.back, false);
            surface.presentation = card.GetComponent<ThreeDCardPresentationController>();
            surface.EnforcePaper();
            card.FaceChanged += surface.RefreshFace;
            surface.RefreshFace(card, card.IsFaceUp);
            return surface;
        }

        public void EnforcePaper()
        {
            if (presentation != null) presentation.UsePaperSurface(back);
        }

        private void RefreshFace(CardView value, bool faceUp)
        {
            if (value != card) return;
            Fit(card.FaceArtwork, card.ShowsReversed);
            if (card.FaceArtwork != null)
            {
                card.FaceArtwork.color = Color.white;
                paperInk ??= new MaterialPropertyBlock();
                card.FaceArtwork.GetPropertyBlock(paperInk);
                paperInk.SetFloat(Glare, 0f);
                paperInk.SetFloat(Iridescence, 0f);
                card.FaceArtwork.SetPropertyBlock(paperInk);
            }
            if (back != null) back.enabled = !faceUp;
            EnforcePaper();
        }

        private static void Fit(SpriteRenderer image, bool reversed)
        {
            if (image == null || image.sprite == null) return;
            var bounds = image.sprite.bounds;
            var scale = new Vector3(PaperWidth / bounds.size.x, 1f / bounds.size.y, 1f);
            var rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.AngleAxis(reversed ? 180f : 0f, Vector3.forward);
            image.transform.localScale = scale;
            image.transform.localRotation = rotation;
            image.transform.localPosition = -(rotation * Vector3.Scale(scale, bounds.center));
        }

        private void OnDestroy() { if (card != null) card.FaceChanged -= RefreshFace; }
    }
}
