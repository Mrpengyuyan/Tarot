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

        public void OnPointerEnter(PointerEventData eventData)
        {
            cardView.SetHighlighted(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cardView.SetHighlighted(false);
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
