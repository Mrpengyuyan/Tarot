using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TarotUnity.Data;

namespace TarotUnity.UI
{
    public sealed class CardAnalysisParseResult
    {
        public static readonly CardAnalysisParseResult Failed =
            new CardAnalysisParseResult(false, Array.Empty<string>(), Array.Empty<string>());

        public CardAnalysisParseResult(bool success, string[] bodies, string[] leftovers)
        {
            Success = success;
            Bodies = bodies;
            Leftovers = leftovers;
        }

        public bool Success { get; }

        /// <summary>Bodies[i] belongs to draws[i]; a line that only named the card gives an empty body.</summary>
        public string[] Bodies { get; }

        /// <summary>Lines no card claimed, in their original order.</summary>
        public string[] Leftovers { get; }
    }

    /// <summary>
    /// Phase 67 (spec C 4.3): split card_analysis into one body per drawn card.
    /// 1) Each card takes the first unused line that starts, after an ordinal, with its position name.
    /// 2) If not every card matched but the line count equals the card count, match by order.
    /// 3) Otherwise fail; the caller then shows the whole text as before.
    /// </summary>
    public static class CardAnalysisParser
    {
        private static readonly Regex Ordinal = new Regex(
            @"^\s*(?:[\(（]\s*\d{1,2}\s*[\)）]|\d{1,2}\s*[\.．、:：\)）]|[①②③④⑤⑥⑦⑧⑨⑩]|[一二三四五六七八九十]{1,3}\s*[、\.．])\s*",
            RegexOptions.CultureInvariant);

        private static readonly Regex LeadingSeparators =
            new Regex(@"^[\s：:、,，·\-—–]+", RegexOptions.CultureInvariant);

        private static readonly Regex LeadingOrientation =
            new Regex(@"^[\(（]?\s*(?:正位|逆位)\s*[\)）]?", RegexOptions.CultureInvariant);

        public static CardAnalysisParseResult Parse(string cardAnalysis, CardDrawData[] draws)
        {
            if (string.IsNullOrWhiteSpace(cardAnalysis) || draws == null || draws.Length == 0)
            {
                return CardAnalysisParseResult.Failed;
            }

            var lines = SplitLines(cardAnalysis);
            if (lines.Count == 0)
            {
                return CardAnalysisParseResult.Failed;
            }

            var bodies = new string[draws.Length];
            var used = new bool[lines.Count];
            var matched = 0;

            // Longer position names claim their lines first, so a name that is a prefix of another
            // (过去 / 过去的影响) cannot take the other card's line.
            var order = new int[draws.Length];
            for (var i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (a, b) =>
            {
                var byLength = PositionLength(draws[b]).CompareTo(PositionLength(draws[a]));
                return byLength != 0 ? byLength : a.CompareTo(b);
            });

            foreach (var d in order)
            {
                var position = draws[d]?.position_name?.Trim();
                if (string.IsNullOrEmpty(position))
                {
                    continue;
                }

                for (var l = 0; l < lines.Count; l++)
                {
                    var line = StripOrdinal(lines[l]);
                    if (used[l] || !line.StartsWith(position, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    used[l] = true;
                    bodies[d] = StripHeading(line.Substring(position.Length), draws[d]);
                    matched++;
                    break;
                }
            }

            if (matched == draws.Length)
            {
                var leftovers = new List<string>();
                for (var l = 0; l < lines.Count; l++)
                {
                    if (!used[l])
                    {
                        leftovers.Add(lines[l]);
                    }
                }

                return new CardAnalysisParseResult(true, bodies, leftovers.ToArray());
            }

            if (lines.Count == draws.Length)
            {
                var ordered = new string[lines.Count];
                for (var l = 0; l < lines.Count; l++)
                {
                    ordered[l] = StripOrdinal(lines[l]);
                }

                return new CardAnalysisParseResult(true, ordered, Array.Empty<string>());
            }

            return CardAnalysisParseResult.Failed;
        }

        private static List<string> SplitLines(string text)
        {
            var lines = new List<string>();
            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static string StripOrdinal(string line)
        {
            return Ordinal.Replace(line, string.Empty, 1).TrimStart();
        }

        private static string StripHeading(string rest, CardDrawData draw)
        {
            var text = LeadingSeparators.Replace(rest, string.Empty, 1);
            var name = draw?.tarot_card?.name_zh?.Trim();
            if (!string.IsNullOrEmpty(name) && text.StartsWith(name, StringComparison.Ordinal))
            {
                text = text.Substring(name.Length).TrimStart();
            }

            text = LeadingOrientation.Replace(text, string.Empty, 1);
            text = LeadingSeparators.Replace(text, string.Empty, 1);
            return text.Trim();
        }

        private static int PositionLength(CardDrawData draw)
        {
            return draw?.position_name?.Trim().Length ?? 0;
        }
    }
}
