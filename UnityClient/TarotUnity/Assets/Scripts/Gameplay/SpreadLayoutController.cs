using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed class SpreadLayoutController : MonoBehaviour
    {
        [SerializeField] private Transform[] oneCardSlots;
        [SerializeField] private Transform[] threeCardSlots;
        [SerializeField] private Transform[] fallbackSlots;

        public List<Transform> GetSlots(int cardCount)
        {
            return cardCount switch
            {
                1 => CollectSlots(oneCardSlots),
                3 => CollectSlots(threeCardSlots),
                _ => CollectSlots(fallbackSlots),
            };
        }

        public void ConfigureSlots(Transform[] oneCard, Transform[] threeCard, Transform[] fallback)
        {
            oneCardSlots = oneCard;
            threeCardSlots = threeCard;
            fallbackSlots = fallback;
        }

        private static List<Transform> CollectSlots(IEnumerable<Transform> source)
        {
            var slots = new List<Transform>();
            if (source == null)
            {
                return slots;
            }

            foreach (var slot in source)
            {
                if (slot != null)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }
    }
}
