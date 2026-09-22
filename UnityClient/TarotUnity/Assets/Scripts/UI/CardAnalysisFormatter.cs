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
    /// get none and a surrogate pair gets one. Line feeds count like any other character.
    /// The text arrives from ReadingTextSanitizer.Visible, which has already dropped the characters
    /// TMP lays out inconsistently, so nothing else can shift an entry.
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
            for (var i = 0; i < draws.Length; i++)
            {
                if (i > 0)
                {
                    visibleCount += AppendVisible(builder, "\n\n");
                }

                var heading = ReadingTextSanitizer.Visible(BuildHeading(draws[i]));
                builder.Append(HeadingOpen);
                var headingCount = AppendVisible(builder, heading);
                ranges[i] = new CardBlockRange(i, visibleCount, headingCount);
                visibleCount += headingCount;
                builder.Append(HeadingClose);

                var body = parsed.Bodies[i];
                if (!string.IsNullOrEmpty(body))
                {
                    visibleCount += AppendVisible(builder, "\n");
                    visibleCount += AppendVisible(builder, body);
                }
            }

            foreach (var line in parsed.Leftovers)
            {
                visibleCount += AppendVisible(builder, "\n\n");
                visibleCount += AppendVisible(builder, line);
            }

            return new FormattedCardAnalysis(builder.ToString(), ranges);
        }

        // Appends text the player sees (wrapped in noparse when needed) and returns how many entries TMP
        // gives it in characterInfo: one per UTF-16 unit, except that a valid surrogate pair is one.
        private static int AppendVisible(StringBuilder builder, string visible)
        {
            builder.Append(ReadingTextSanitizer.Wrap(visible));
            var count = 0;
            for (var i = 0; i < visible.Length; i++)
            {
                if (char.IsHighSurrogate(visible[i]) && i + 1 < visible.Length && char.IsLowSurrogate(visible[i + 1]))
                {
                    i++;
                }

                count++;
            }

            return count;
        }
    }
}
