using System.Collections;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualPickSlot : MonoBehaviour
    {
        public const float FlightSeconds = 0.9f;
        private RitualPickPresentation owner;
        private int index;
        private CardView landedCard;
        public void Initialize(RitualPickPresentation presentation, int slotIndex) { owner = presentation; index = slotIndex; }
        public bool HasLanded(CardView card) => card != null && landedCard == card;
        internal void ConfirmLanding(CardView card) => landedCard = card;
        public IEnumerator Fly(CardView card)
        {
            landedCard = null;
            if (owner != null) yield return owner.Fly(card, index);
        }
    }
}
