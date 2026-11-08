using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>A real 3D destination; only opted-in cards use the rectangular paper surface.</summary>
    public sealed class RitualSpreadSlot : MonoBehaviour
    {
        public const float PaperWidth = 0.64f;
        private static Sprite backSprite;
        private CardView card;
        private SpriteRenderer back;
        private MaterialPropertyBlock paperInk;
        private static readonly int Glare = Shader.PropertyToID("_GlareIntensity");
        private static readonly int Iridescence = Shader.PropertyToID("_Iridescence");

        public Vector3 ScaleFor(Transform moving)
        {
            var parent = moving.parent != null ? moving.parent.lossyScale : Vector3.one;
            var scale = transform.lossyScale;
            return new Vector3(scale.x / parent.x, scale.y / parent.y, scale.z / parent.z);
        }

        public void Dock(CardView value)
        {
            if (value == null) return;
            if (card != value)
            {
                if (card != null) card.FaceChanged -= RefreshFace;
                card = value;
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
                back = node.GetComponent<SpriteRenderer>();
                back.sprite = backSprite;
                Fit(back, false);
                var presentation = card.GetComponent<ThreeDCardPresentationController>();
                if (presentation != null) presentation.UsePaperSurface(back);
                card.FaceChanged += RefreshFace;
            }
            card.transform.SetParent(transform, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = Vector3.one;
            if (card.FaceArtwork != null) card.FaceArtwork.transform.SetParent(card.transform, false);
            RefreshFace(card, card.IsFaceUp);
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

        private void OnDestroy()
        {
            if (card != null) card.FaceChanged -= RefreshFace;
        }
    }
}
