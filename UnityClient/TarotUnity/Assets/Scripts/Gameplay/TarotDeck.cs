using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 75: the whole 78-card deck for the offline reading - names, numbers, suits and the
    /// upright and reversed meanings - copied from the backend's seed data (Server/data/
    /// tarotCards.json) into Resources/TarotDeck/tarot_cards.json by Phase75DrawTruthBootstrapper,
    /// and guarded against drifting from it by an EditMode test.
    /// </summary>
    public static class TarotDeck
    {
        public const string ResourcePath = "TarotDeck/tarot_cards";

        private static IReadOnlyList<TarotDeckCard> cards;

        /// <summary>The deck, in the backend's id order; empty if the data is missing.</summary>
        public static IReadOnlyList<TarotDeckCard> Cards
        {
            get
            {
                if (cards == null)
                {
                    var asset = Resources.Load<TextAsset>(ResourcePath);
                    var parsed = asset != null ? JsonUtility.FromJson<TarotDeckData>(asset.text) : null;
                    cards = parsed != null && parsed.cards != null ? parsed.cards : Array.Empty<TarotDeckCard>();
                }

                return cards;
            }
        }
    }

    [Serializable]
    public sealed class TarotDeckData
    {
        public TarotDeckCard[] cards;
    }

    [Serializable]
    public sealed class TarotDeckCard
    {
        public int id;
        public string nameEn;
        public string nameZh;
        public int cardNumber;
        public string type;
        public string suit;
        public string uprightMeaning;
        public string reversedMeaning;
        public string[] keywordsUpright;
        public string[] keywordsReversed;
    }
}
