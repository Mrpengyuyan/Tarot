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
    /// card data, then the body the parser found. HeadingStart counts every character TMP lays
    /// out (line feeds included, rich-text tags excluded), so it indexes
    /// TMP_TextInfo.characterInfo directly.
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
            var parsed = CardAnalysisParser.Parse(cardAnalysis, draws);
            if (!parsed.Success)
            {
                return new FormattedCardAnalysis(ReadingTextSanitizer.Plain(cardAnalysis), Array.Empty<CardBlockRange>());
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
                ranges[i] = new CardBlockRange(i, visibleCount, heading.Length);
                visibleCount += AppendVisible(builder, heading);
                builder.Append(HeadingClose);

                var body = ReadingTextSanitizer.Visible(parsed.Bodies[i]);
                if (body.Length > 0)
                {
                    visibleCount += AppendVisible(builder, "\n");
                    visibleCount += AppendVisible(builder, body);
                }
            }

            foreach (var line in parsed.Leftovers)
            {
                visibleCount += AppendVisible(builder, "\n\n");
                visibleCount += AppendVisible(builder, ReadingTextSanitizer.Visible(line));
            }

            return new FormattedCardAnalysis(builder.ToString(), ranges);
        }

        // Appends text the player sees (wrapped in noparse when needed) and returns how many
        // characters TMP will lay out for it.
        private static int AppendVisible(StringBuilder builder, string visible)
        {
            builder.Append(ReadingTextSanitizer.Wrap(visible));
            return visible.Length;
        }
    }
}
