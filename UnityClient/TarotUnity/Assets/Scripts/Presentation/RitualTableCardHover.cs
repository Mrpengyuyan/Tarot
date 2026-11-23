using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TarotUnity.Presentation
{
    /// <summary>Native table-card hover state for cards docked in a <see cref="RitualSpreadSlot"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class RitualTableCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverSeconds = 0.3f;

        private CardView card;
        private CardFlipController flip;
        private RitualPaperSurface surface;
        private bool hovering;
        private float from;
        private float target;
        private float since;
        private float lastEvaluated;

        internal CardFlipController Flip => flip;
        internal bool IsHovering => hovering;
        internal bool NeedsUpdate => hovering || lastEvaluated > 0.0001f || !Mathf.Approximately(from, target);

        internal static RitualTableCardHover Attach(CardView card)
        {
            if (card == null) return null;
            var owner = card.GetComponent<RitualTableCardHover>();
            if (owner == null) owner = card.gameObject.AddComponent<RitualTableCardHover>();
            owner.Bind(card);
            return owner;
        }

        private void Bind(CardView value)
        {
            card = value;
            flip = value.GetComponent<CardFlipController>();
            surface = value.GetComponent<RitualPaperSurface>();
            ResetState();
        }

        public void OnPointerEnter(PointerEventData eventData) => hovering = true;
        public void OnPointerExit(PointerEventData eventData) => hovering = false;

        internal float Evaluate(bool allowed, bool immediateReset)
        {
            if (!allowed)
            {
                if (immediateReset)
                {
                    from = 0f;
                    target = 0f;
                    since = Time.time;
                    lastEvaluated = 0f;
                    return 0f;
                }
            }

            var next = allowed && hovering ? 1f : 0f;
            var current = Sample(Time.time);
            if (!Mathf.Approximately(target, next))
            {
                from = current;
                target = next;
                since = Time.time;
            }

            lastEvaluated = Sample(Time.time);
            return lastEvaluated;
        }

        private float Sample(float now)
        {
            var progress = (now - since) / HoverSeconds;
            if (progress <= 0f) return from;
            if (progress >= 1f) return target;
            return Mathf.Lerp(from, target, RitualPickLayout.HoverEase(progress));
        }

        internal void ApplyTint(float amount, bool inspecting)
        {
            if (card == null) return;
            if (surface == null) surface = card.GetComponent<RitualPaperSurface>();

            if (inspecting)
            {
                if (surface != null && surface.Back != null && surface.Back.color != Color.white)
                {
                    surface.Back.color = Color.white;
                }

                return;
            }

            var lit = amount <= 0.0001f ? Color.white : new Color(1f + 0.1f * amount, 1f + 0.1f * amount, 1f + 0.1f * amount, 1f);
            if (card.IsFaceUp)
            {
                if (card.FaceArtwork != null && card.FaceArtwork.color != lit)
                {
                    card.FaceArtwork.color = lit;
                }

                if (surface != null && surface.Back != null && surface.Back.color != Color.white)
                {
                    surface.Back.color = Color.white;
                }
            }
            else
            {
                if (surface != null && surface.Back != null && surface.Back.color != lit)
                {
                    surface.Back.color = lit;
                }

                if (card.FaceArtwork != null && card.FaceArtwork.color != Color.white)
                {
                    card.FaceArtwork.color = Color.white;
                }
            }
        }

        private void ResetState()
        {
            hovering = false;
            from = 0f;
            target = 0f;
            since = Time.time;
        }

        private void OnDisable()
        {
            ResetState();
            ApplyTint(0f, false);
        }

        private void OnDestroy()
        {
            ApplyTint(0f, false);
        }
    }
}
