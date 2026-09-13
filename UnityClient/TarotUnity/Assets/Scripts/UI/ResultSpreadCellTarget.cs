using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.4): one spread cell on the Result screen. While the reading is shown,
    /// hovering lifts the card 4% and brightens its glow, and clicking scrolls the reading to the
    /// card's block. It sits on the cell root: clicks bubble up from the artwork (whose
    /// holographic driver only handles enter/exit/move) and from the label.
    /// </summary>
    public sealed class ResultSpreadCellTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private int cardIndex;
        [SerializeField] private ResultReadingNavigator navigator;
        [SerializeField] private Image glow;
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float hoverGlowAlpha = 0.42f;

        private float baseScale = 1f;
        private float baseGlowAlpha = -1f;

        public int CardIndex => cardIndex;
        public bool IsHovered { get; private set; }

        /// <summary>The layout scale for this card; the hover lift multiplies it.</summary>
        public void SetBaseScale(float scale)
        {
            baseScale = scale;
            ApplyVisual();
        }

        public void Activate()
        {
            if (navigator != null)
            {
                navigator.FocusCard(cardIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Activate();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = navigator != null && navigator.IsInteractive;
            ApplyVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            ApplyVisual();
        }

        private void OnDisable()
        {
            IsHovered = false;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            transform.localScale = Vector3.one * (baseScale * (IsHovered ? hoverScale : 1f));
            if (glow == null)
            {
                return;
            }

            if (baseGlowAlpha < 0f)
            {
                baseGlowAlpha = glow.color.a;
            }

            var color = glow.color;
            color.a = IsHovered ? hoverGlowAlpha : baseGlowAlpha;
            glow.color = color;
        }
    }
}
