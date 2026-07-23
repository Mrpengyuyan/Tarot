using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Holds the table slots for each spread, keyed by card count. Phase 63 replaced
    /// the fixed one/three/fallback fields with a list so any spread (e.g. the
    /// ten-card Celtic Cross) can register its own slots; the Phase 63 bootstrapper
    /// configures each set from the spread catalog.
    /// </summary>
    public sealed class SpreadLayoutController : MonoBehaviour
    {
        [Serializable]
        public sealed class SpreadSlotSet
        {
            public int cardCount;
            public Transform[] slots = Array.Empty<Transform>();
        }

        [SerializeField] private List<SpreadSlotSet> spreadSlots = new();

        public List<Transform> GetSlots(int cardCount)
        {
            foreach (var set in spreadSlots)
            {
                if (set != null && set.cardCount == cardCount)
                {
                    return CollectSlots(set.slots);
                }
            }

            return new List<Transform>();
        }

        public void ConfigureSpread(int cardCount, Transform[] slots)
        {
            foreach (var set in spreadSlots)
            {
                if (set != null && set.cardCount == cardCount)
                {
                    set.slots = slots ?? Array.Empty<Transform>();
                    return;
                }
            }

            spreadSlots.Add(new SpreadSlotSet { cardCount = cardCount, slots = slots ?? Array.Empty<Transform>() });
        }

        /// <summary>Back-compat entry for the Phase 2 graybox bootstrapper (one- and three-card).</summary>
        public void ConfigureSlots(Transform[] oneCard, Transform[] threeCard, Transform[] fallback)
        {
            ConfigureSpread(1, oneCard);
            ConfigureSpread(3, threeCard);
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
