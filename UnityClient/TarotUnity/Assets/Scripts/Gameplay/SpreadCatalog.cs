using System;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 63: the single source of truth for the game's spreads. The reading
    /// room, camera, socket glows and offline reader all resolve a spread through
    /// this catalog (loaded from Resources), so a new spread is one more asset here
    /// rather than a new branch in every system.
    /// </summary>
    [CreateAssetMenu(menuName = "Tarot Unity/Spread Catalog", fileName = "SpreadCatalog")]
    public sealed class SpreadCatalog : ScriptableObject
    {
        public const string ResourcePath = "Spreads/SpreadCatalog";

        public SpreadDefinition[] spreads = Array.Empty<SpreadDefinition>();

        public SpreadDefinition GetById(int spreadId)
        {
            if (spreads == null)
            {
                return null;
            }

            foreach (var spread in spreads)
            {
                if (spread != null && spread.spreadId == spreadId)
                {
                    return spread;
                }
            }

            return null;
        }

        public SpreadDefinition GetByCardCount(int cardCount)
        {
            if (spreads == null)
            {
                return null;
            }

            foreach (var spread in spreads)
            {
                if (spread != null && spread.cardCount == cardCount)
                {
                    return spread;
                }
            }

            return null;
        }
    }
}
