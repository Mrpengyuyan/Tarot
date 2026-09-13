using System.Text.RegularExpressions;
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.5): text from the server or the player - the AI interpretation, the
    /// question, spread and card names - is shown literally. TMP rich text stays on for the
    /// client's own styling, so external text that contains '&lt;' is wrapped in noparse; text
    /// without '&lt;' cannot form a tag and is returned unchanged.
    /// </summary>
    public static class ReadingTextSanitizer
    {
        public const int MaxFieldLength = 4000;
        public const string TruncationMark = "……";

        private const string NoparseOpen = "<noparse>";
        private const string NoparseClose = "</noparse>";

        private static readonly Regex EmbeddedNoparseClose =
            new Regex("</noparse>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The characters the player will see: truncated, neutralised, no tags.</summary>
        public static string Visible(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var text = value;
            if (text.Length > MaxFieldLength)
            {
                var cut = MaxFieldLength;
                if (char.IsHighSurrogate(text[cut - 1]))
                {
                    cut--;
                }

                Debug.LogWarning($"ReadingTextSanitizer: truncated a {value.Length}-character field to {cut} characters.");
                text = text.Substring(0, cut) + TruncationMark;
            }

            // A literal "</noparse>" would close the wrapper early; a full-width bracket keeps it visible and inert.
            return text.IndexOf('<') < 0
                ? text
                : EmbeddedNoparseClose.Replace(text, match => "＜" + match.Value.Substring(1));
        }

        /// <summary>Wraps already-visible text so TMP shows it literally.</summary>
        public static string Wrap(string visible)
        {
            if (string.IsNullOrEmpty(visible))
            {
                return string.Empty;
            }

            return visible.IndexOf('<') < 0 ? visible : NoparseOpen + visible + NoparseClose;
        }

        public static string Plain(string value)
        {
            return Wrap(Visible(value));
        }
    }
}
