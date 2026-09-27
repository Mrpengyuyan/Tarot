using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TarotUnity.Gameplay
{
    [RequireComponent(typeof(CardView))]
    public sealed class CardClickHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private CardView cardView;

        public event Action<CardView> Clicked;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        // Phase 71: hover is its own state, so leaving a card keeps the deal's glow.
        public void OnPointerEnter(PointerEventData eventData)
        {
            cardView.SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cardView.SetHovered(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(cardView);
        }
    }
}
