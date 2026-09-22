using System.Text.RegularExpressions;
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.5): text from the server or the player - the AI interpretation, the
    /// question, spread and card names - is shown literally. TMP rich text stays on for the
    /// client's own styling, so external text that contains '&lt;' is wrapped in noparse.
    /// TMP converts escape sequences before it parses tags (a backslash-u003C becomes a '&lt;' that
    /// can open a tag or close noparse), and it acts on a few '&lt;' sequences even inside noparse.
    /// Visible neutralises both: a literal backslash-n becomes a line feed, every other backslash
    /// becomes '＼', and the '&lt;' of '&lt;/noparse', '&lt;a' and '&lt;/a' becomes '＜'. It also drops the
    /// characters TMP does not lay out, so what is left is exactly what the player sees.
    /// </summary>
    public static class ReadingTextSanitizer
    {
        public const int MaxFieldLength = 4000;
        public const string TruncationMark = "……";

        private const string NoparseOpen = "<noparse>";
        private const string NoparseClose = "</noparse>";

        // TMP names a tag up to '>', '=' or a space, so "</noparse" closes the wrapper whatever follows
        // the name, and its pre-pass inserts the default style sheet's "A" style for "<a" and "</a"
        // without checking noparse (TMP_Text.PopulateTextProcessingArray).
        private static readonly Regex TagsTmpActsOnInsideNoparse =
            new Regex("<(?=/noparse|/?a[ =>])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // TMP lays neither of these out, and both move what follows: it stops processing at U+0000, and it
        // gives no characterInfo entry to a variation selector that follows a font character - but keeps one
        // for a selector after an emoji it draws from a sprite asset. Dropping them keeps every offset exact.
        private static readonly Regex CharactersTmpDoesNotLayOut =
            new Regex("[\\u0000\\uFE00-\\uFE0F]|\\uDB40[\\uDD00-\\uDDEF]", RegexOptions.CultureInvariant);

        /// <summary>
        /// The characters the player will see: escapes neutralised, truncated, and no '&lt;' that TMP
        /// would act on inside noparse.
        /// </summary>
        public static string Visible(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // A double-escaped line break still breaks the line, so the card analysis can still split;
            // any other backslash could start an escape TMP converts, so it shows full-width instead.
            var text = value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace('\\', '＼');
            text = CharactersTmpDoesNotLayOut.Replace(text, string.Empty);
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

            // A full-width bracket keeps these sequences visible and inert inside the noparse wrapper.
            return text.IndexOf('<') < 0 ? text : TagsTmpActsOnInsideNoparse.Replace(text, "＜");
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
