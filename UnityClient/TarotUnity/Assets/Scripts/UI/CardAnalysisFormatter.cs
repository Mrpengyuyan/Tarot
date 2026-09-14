using System;
using System.Text;
using TarotUnity.Data;

namespace TarotUnity.UI
{
    /// <summary>Where one card's heading sits in the formatted card analysis.</summary>
    [Serializable]
    public struct CardBlockRange
    {
        public int CardIndex;
        public int HeadingStart;
        public int HeadingLength;

        public CardBlockRange(int cardIndex, int headingStart, int headingLength)
        {
            CardIndex = cardIndex;
            HeadingStart = headingStart;
            HeadingLength = headingLength;
        }
    }

    public sealed class FormattedCardAnalysis
    {
        public FormattedCardAnalysis(string richText, CardBlockRange[] ranges)
        {
            RichText = richText;
            Ranges = ranges;
        }

        public string RichText { get; }
        public CardBlockRange[] Ranges { get; }
    }

    /// <summary>
    /// Phase 67 (spec C 4.3): one block per card - a gold heading built from the client's own
    /// card data, then the body the parser found. HeadingStart and HeadingLength count the entries
    /// TMP gives the text in TMP_TextInfo.characterInfo, so they index it directly: rich-text tags
    /// get none, a surrogate pair gets one, and a variation selector straight after a character
    /// gets none (TMP skips it). Line feeds count like any other character.
    /// </summary>
    public static class CardAnalysisFormatter
    {
        public const string HeadingColorHex = "#DBA13D";

        private const string HeadingOpen = "<color=" + HeadingColorHex + "><b><size=95%>";
        private const string HeadingClose = "</size></b></color>";

        public static string BuildHeading(CardDrawData draw)
        {
            var position = draw?.position_name?.Trim() ?? string.Empty;
            var name = draw?.tarot_card?.name_zh?.Trim() ?? string.Empty;
            var mark = draw != null && draw.is_reversed ? ReleaseUxCopy.CardReversedMark : ReleaseUxCopy.CardUprightMark;
            return position.Length == 0 ? name + mark : position + ReleaseUxCopy.CardHeadingSeparator + name + mark;
        }

        public static FormattedCardAnalysis Build(string cardAnalysis, CardDrawData[] draws)
        {
            // The field is neutralised and capped once, then split. Bodies and leftovers are pieces of
            // this visible text, so they do not go through Visible again (it is not idempotent at the cap).
            var visible = ReadingTextSanitizer.Visible(cardAnalysis);
            var parsed = CardAnalysisParser.Parse(visible, draws);
            if (!parsed.Success)
            {
                return new FormattedCardAnalysis(ReadingTextSanitizer.Wrap(visible), Array.Empty<CardBlockRange>());
            }

            var builder = new StringBuilder();
            var ranges = new CardBlockRange[draws.Length];
            var visibleCount = 0;
            var afterCharacter = false; // whether TMP's last element so far is a character rather than a tag
            for (var i = 0; i < draws.Length; i++)
            {
                if (i > 0)
                {
                    visibleCount += AppendVisible(builder, "\n\n", ref afterCharacter);
                }

                var heading = ReadingTextSanitizer.Visible(BuildHeading(draws[i]));
                builder.Append(HeadingOpen);
                afterCharacter = false;
                var headingCount = AppendVisible(builder, heading, ref afterCharacter);
                ranges[i] = new CardBlockRange(i, visibleCount, headingCount);
                visibleCount += headingCount;
                builder.Append(HeadingClose);
                afterCharacter = false;

                var body = parsed.Bodies[i];
                if (!string.IsNullOrEmpty(body))
                {
                    visibleCount += AppendVisible(builder, "\n", ref afterCharacter);
                    visibleCount += AppendVisible(builder, body, ref afterCharacter);
                }
            }

            foreach (var line in parsed.Leftovers)
            {
                visibleCount += AppendVisible(builder, "\n\n", ref afterCharacter);
                visibleCount += AppendVisible(builder, line, ref afterCharacter);
            }

            return new FormattedCardAnalysis(builder.ToString(), ranges);
        }

        // Appends text the player sees (wrapped in noparse when needed) and returns how many entries TMP
        // gives it in characterInfo: one per UTF-16 unit, except that a valid surrogate pair is one, and a
        // variation selector (U+FE00-FE0F, or U+E0100-E01EF) gets none when it directly follows a
        // character - TextMeshProUGUI.SetArraySizes skips it, and a skipped selector does not skip the next
        // one. afterCharacter carries TMP's previous element across segments: the noparse wrapper and the
        // heading tags are tags, so a selector straight after them keeps its entry. A selector after an
        // emoji TMP draws from its default sprite asset also keeps its entry, which this count does not
        // model; such text leaves later offsets short by one per selector.
        private static int AppendVisible(StringBuilder builder, string visible, ref bool afterCharacter)
        {
            var wrapped = ReadingTextSanitizer.Wrap(visible);
            builder.Append(wrapped);
            var isWrapped = wrapped.Length > visible.Length;
            var previousIsCharacter = afterCharacter && !isWrapped;
            var count = 0;
            for (var i = 0; i < visible.Length; i++)
            {
                int codePoint = visible[i];
                if (char.IsHighSurrogate(visible[i]) && i + 1 < visible.Length && char.IsLowSurrogate(visible[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(visible[i], visible[i + 1]);
                    i++;
                }

                if (previousIsCharacter && IsVariationSelector(codePoint))
                {
                    previousIsCharacter = false;
                    continue;
                }

                count++;
                previousIsCharacter = true;
            }

            afterCharacter = previousIsCharacter && !isWrapped;
            return count;
        }

        private static bool IsVariationSelector(int codePoint)
        {
            return (codePoint >= 0xFE00 && codePoint <= 0xFE0F) || (codePoint >= 0xE0100 && codePoint <= 0xE01EF);
        }
    }
}
