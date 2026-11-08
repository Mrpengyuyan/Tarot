using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>Retain deck-owned cards if their UI adapter is removed during an active reading.</summary>
    public sealed class RitualSpreadCardLease : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (GetComponentInChildren<CardView>(true) == null) Destroy(gameObject);
        }
    }
}
