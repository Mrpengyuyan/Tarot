using System;
using System.Collections.Generic;
using TarotUnity.Data;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    [CreateAssetMenu(menuName = "Tarot Unity/Card Artwork Catalog", fileName = "CardArtworkCatalog")]
    public sealed class CardArtworkCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "TarotArt/RWS1909_CardArtworkCatalog";

        private static CardArtworkCatalog sharedDefault;

        [SerializeField] private CardArtworkEntry[] entries = Array.Empty<CardArtworkEntry>();

        private Dictionary<string, Sprite> spriteLookup;

        public IReadOnlyList<CardArtworkEntry> Entries => entries ?? Array.Empty<CardArtworkEntry>();

        public static CardArtworkCatalog LoadDefault()
        {
            if (sharedDefault == null)
            {
                sharedDefault = Resources.Load<CardArtworkCatalog>(DefaultResourcePath);
            }

            return sharedDefault;
        }

        public static void ReleaseDefault()
        {
            sharedDefault = null;
        }

        private void OnEnable()
        {
            spriteLookup = null;
        }

        public Sprite FindSprite(CardDrawData drawData)
        {
            return drawData == null ? null : FindSprite(ResolveKey(drawData.tarot_card));
        }

        public Sprite FindSprite(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || entries == null || entries.Length == 0)
            {
                return null;
            }

            EnsureLookup();
            return spriteLookup.TryGetValue(key.Trim(), out var sprite) ? sprite : null;
        }

        public void SetEntries(CardArtworkEntry[] nextEntries)
        {
            entries = nextEntries ?? Array.Empty<CardArtworkEntry>();
            spriteLookup = null;
        }

        private void EnsureLookup()
        {
            if (spriteLookup != null)
            {
                return;
            }

            spriteLookup = new Dictionary<string, Sprite>(entries.Length, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                var trimmedKey = entry.key.Trim();
                if (!spriteLookup.ContainsKey(trimmedKey))
                {
                    spriteLookup[trimmedKey] = entry.artwork;
                }
            }
        }

        public static string ResolveKey(TarotCardSimple card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            var suit = ResolveCanonicalSuit(card.suit);
            if (!string.IsNullOrEmpty(suit) && card.number >= 1 && card.number <= 14)
            {
                return $"{suit}_{card.number:00}";
            }

            if (IsMajorArcana(card.arcana, suit) && card.number >= 0 && card.number <= 21)
            {
                return $"major_{card.number:00}";
            }

            return ResolveNameKey(card.name_en);
        }

        private static string ResolveCanonicalSuit(string rawSuit)
        {
            if (string.IsNullOrWhiteSpace(rawSuit))
            {
                return string.Empty;
            }

            var trimmed = rawSuit.Trim();
            for (var i = 0; i < MinorSuits.Length; i++)
            {
                if (string.Equals(trimmed, MinorSuits[i], StringComparison.OrdinalIgnoreCase))
                {
                    return MinorSuits[i];
                }
            }

            return Normalize(trimmed);
        }

        private static bool IsMajorArcana(string rawArcana, string resolvedSuit)
        {
            if (string.IsNullOrEmpty(resolvedSuit))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(rawArcana))
            {
                return false;
            }

            var trimmed = rawArcana.Trim();
            if (string.Equals(trimmed, "major", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "major_arcana", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var normalized = Normalize(trimmed);
            return normalized == "major" || normalized == "major_arcana";
        }

        private static string ResolveNameKey(string nameEn)
        {
            var normalized = Normalize(nameEn);
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            if (MajorNameKeys.TryGetValue(normalized, out var majorKey))
            {
                return majorKey;
            }

            foreach (var suit in MinorSuits)
            {
                var suffix = "_of_" + suit;
                if (!normalized.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                var rank = normalized.Substring(0, normalized.Length - suffix.Length);
                var number = rank switch
                {
                    "ace" => 1,
                    "page" => 11,
                    "knight" => 12,
                    "queen" => 13,
                    "king" => 14,
                    _ => int.TryParse(rank, out var parsed) ? parsed : 0,
                };

                if (number >= 1 && number <= 14)
                {
                    return $"{suit}_{number:00}";
                }
            }

            return string.Empty;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private static readonly string[] MinorSuits =
        {
            "cups",
            "pentacles",
            "swords",
            "wands",
        };

        private static readonly Dictionary<string, string> MajorNameKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            ["fool"] = "major_00",
            ["the_fool"] = "major_00",
            ["magician"] = "major_01",
            ["the_magician"] = "major_01",
            ["high_priestess"] = "major_02",
            ["the_high_priestess"] = "major_02",
            ["empress"] = "major_03",
            ["the_empress"] = "major_03",
            ["emperor"] = "major_04",
            ["the_emperor"] = "major_04",
            ["hierophant"] = "major_05",
            ["the_hierophant"] = "major_05",
            ["lovers"] = "major_06",
            ["the_lovers"] = "major_06",
            ["chariot"] = "major_07",
            ["the_chariot"] = "major_07",
            ["strength"] = "major_08",
            ["hermit"] = "major_09",
            ["the_hermit"] = "major_09",
            ["wheel_of_fortune"] = "major_10",
            ["justice"] = "major_11",
            ["hanged_man"] = "major_12",
            ["the_hanged_man"] = "major_12",
            ["death"] = "major_13",
            ["temperance"] = "major_14",
            ["devil"] = "major_15",
            ["the_devil"] = "major_15",
            ["tower"] = "major_16",
            ["the_tower"] = "major_16",
            ["star"] = "major_17",
            ["the_star"] = "major_17",
            ["moon"] = "major_18",
            ["the_moon"] = "major_18",
            ["sun"] = "major_19",
            ["the_sun"] = "major_19",
            ["judgement"] = "major_20",
            ["judgment"] = "major_20",
            ["world"] = "major_21",
            ["the_world"] = "major_21",
        };
    }

    [Serializable]
    public sealed class CardArtworkEntry
    {
        public string key;
        public string cardNameEn;
        public string arcana;
        public string suit;
        public int number;
        public Sprite artwork;
        public string sourceTitle;
        public string sourceUrl;
    }
}
