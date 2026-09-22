using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: external reading text is shown literally, and the per-card analysis is
    /// split into blocks whose headings come from the client's own card data.
    /// </summary>
    public sealed class Phase67ReadingTextTests
    {
        // 过去 / 现在 / 未来: 愚者, 魔术师, 女祭司 (the third card is reversed).
        private static CardDrawData[] ThreeCards()
        {
            return LocalReadingSimulator.CreatePlaceholderDraws(3);
        }

        private static string OfflineAnalysis(CardDrawData[] draws)
        {
            return LocalReadingSimulator.CreateSession(2, "三牌阵", "问题？", "general", draws).cardAnalysis;
        }

        private static string StripTags(string richText)
        {
            return Regex.Replace(richText, "<[^>]+>", string.Empty);
        }

        [Test]
        public void PlainLeavesTagFreeTextUntouched()
        {
            const string text = "过去的积累给了你底气。";
            Assert.That(ReadingTextSanitizer.Plain(text), Is.EqualTo(text));
        }

        [Test]
        public void PlainWrapsTextThatContainsAngleBrackets()
        {
            Assert.That(ReadingTextSanitizer.Plain("<b>粗</b>"), Is.EqualTo("<noparse><b>粗</b></noparse>"));
        }

        [Test]
        public void PlainNeutralisesAnEmbeddedNoparseClose()
        {
            Assert.That(ReadingTextSanitizer.Plain("a</NOPARSE><size=200>b"),
                Is.EqualTo("<noparse>a＜/NOPARSE><size=200>b</noparse>"));
        }

        [Test]
        public void PlainTruncatesPastTheFieldCapAndLogs()
        {
            var text = new string('字', ReadingTextSanitizer.MaxFieldLength + 1);
            LogAssert.Expect(LogType.Warning, new Regex("truncated a 4001-character field"));

            var plain = ReadingTextSanitizer.Plain(text);

            Assert.That(plain.Length,
                Is.EqualTo(ReadingTextSanitizer.MaxFieldLength + ReadingTextSanitizer.TruncationMark.Length));
            Assert.That(plain, Does.EndWith(ReadingTextSanitizer.TruncationMark));
            Assert.That(ReadingTextSanitizer.Plain(new string('字', ReadingTextSanitizer.MaxFieldLength)).Length,
                Is.EqualTo(ReadingTextSanitizer.MaxFieldLength), "control: text at the cap is kept whole");
        }

        [Test]
        public void PlainTurnsNullIntoEmpty()
        {
            Assert.That(ReadingTextSanitizer.Plain(null), Is.Empty);
            Assert.That(ReadingTextSanitizer.Wrap(null), Is.Empty);
        }

        [Test]
        public void ParsesTheOfflineFormat()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse(OfflineAnalysis(draws), draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "新的开始、信任、迈出第一步", "专注、意志、能力", "直觉、沉默、隐藏的知识" }));
            Assert.That(result.Leftovers, Is.Empty);
        }

        [Test]
        public void ParsesTheBackendMockFormatWithEmptyBodies()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse("1. 过去：愚者（正位）\n2. 现在：魔术师（正位）\n3. 未来：女祭司（逆位）", draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { string.Empty, string.Empty, string.Empty }));
        }

        [Test]
        public void ParsesOrdinalsSeparatorsAndPositionOnlyPrefixes()
        {
            var draws = ThreeCards();
            const string analysis = "（1）过去——旧的节奏正在松动。\r\n② 现在：魔术师 资源齐备。\n三、未来 · 女祭司（逆位）：别只听外界的声音。";

            var result = CardAnalysisParser.Parse(analysis, draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "旧的节奏正在松动。", "资源齐备。", "别只听外界的声音。" }));
        }

        [Test]
        public void FallsBackToLineOrderWhenCountsMatch()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse("1. 旧的节奏正在松动。\n2. 资源齐备。\n3. 别只听外界的声音。", draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "旧的节奏正在松动。", "资源齐备。", "别只听外界的声音。" }));
        }

        [Test]
        public void FailsWhenLinesCannotBeMatched()
        {
            var result = CardAnalysisParser.Parse("整组牌讲的是节奏。\n也讲专注。", ThreeCards());
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void KeepsUnmatchedLinesAsLeftovers()
        {
            var draws = ThreeCards();
            var result = CardAnalysisParser.Parse(OfflineAnalysis(draws) + "\n三张牌合起来看，节奏在变。", draws);

            Assert.That(result.Success, Is.True, "control: every card still found its line");
            Assert.That(result.Leftovers, Is.EqualTo(new[] { "三张牌合起来看，节奏在变。" }));
        }

        [Test]
        public void LongerPositionNamesClaimTheirLinesBeforeShorterPrefixes()
        {
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(2, new[] { "过去", "过去的影响" }, null);
            const string analysis = "过去的影响：魔术师 — 旧习惯仍在拉扯。\n过去：愚者 — 敢于开始的勇气仍在。";

            var result = CardAnalysisParser.Parse(analysis, draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { "敢于开始的勇气仍在。", "旧习惯仍在拉扯。" }),
                "过去 must not take the line that belongs to 过去的影响");
        }

        [Test]
        public void FailsOnEmptyInput()
        {
            Assert.That(CardAnalysisParser.Parse(null, ThreeCards()).Success, Is.False);
            Assert.That(CardAnalysisParser.Parse("  \n ", ThreeCards()).Success, Is.False);
            Assert.That(CardAnalysisParser.Parse("过去：愚者 — 勇气", null).Success, Is.False);
        }

        [Test]
        public void FormatsBlocksWithClientHeadingsAndRecordsTheirPositions()
        {
            var draws = ThreeCards();
            var formatted = CardAnalysisFormatter.Build(OfflineAnalysis(draws), draws);
            var visible = StripTags(formatted.RichText);
            var expected = new[] { "过去 · 愚者（正位）", "现在 · 魔术师（正位）", "未来 · 女祭司（逆位）" };

            Assert.That(formatted.Ranges.Length, Is.EqualTo(3));
            Assert.That(formatted.RichText, Does.Contain("<color=" + CardAnalysisFormatter.HeadingColorHex + ">"));
            for (var i = 0; i < 3; i++)
            {
                var range = formatted.Ranges[i];
                Assert.That(range.CardIndex, Is.EqualTo(i));
                Assert.That(CardAnalysisFormatter.BuildHeading(draws[i]), Is.EqualTo(expected[i]));
                Assert.That(visible.Substring(range.HeadingStart, range.HeadingLength), Is.EqualTo(expected[i]));
            }

            Assert.That(visible, Does.Contain("新的开始、信任、迈出第一步"));
            Assert.That(visible, Does.Not.Contain("过去：愚者"), "the line prefix is replaced by the client heading");
        }

        [Test]
        public void FormatsTheRawTextWhenParsingFailed()
        {
            const string analysis = "整组牌讲的是节奏。\n也讲专注。";
            var formatted = CardAnalysisFormatter.Build(analysis, ThreeCards());

            Assert.That(formatted.RichText, Is.EqualTo(analysis));
            Assert.That(formatted.Ranges, Is.Empty);
        }

        // Final review I1, M2, M3 and D1: TMP converts escape sequences before it parses tags, and acts on a
        // few '<' sequences even inside noparse, so the sanitiser must neutralise both; the card analysis is
        // capped once; line order never overrides a name match.

        private const string BodyFontPath = "Assets/Fonts/LXGWWenKai-Regular SDF.asset";

        private GameObject realTextRoot;

        [TearDown]
        public void DestroyRealText()
        {
            if (realTextRoot != null)
            {
                Object.DestroyImmediate(realTextRoot);
                realTextRoot = null;
            }
        }

        // What TMP really lays out for Plain(input), with the Result body font, rich text on and control
        // characters parsed (as every Result text and TMP Settings have it), must be exactly Visible(input):
        // nothing converted, nothing parsed as a tag.
        private void AssertTmpShowsExactlyTheVisibleText(string input)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
            Assert.That(font, Is.Not.Null, "control: the Result body font");
            realTextRoot = new GameObject("Phase67_RealTmpCanvas", typeof(Canvas));
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(realTextRoot.transform, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.richText = true;
            text.parseCtrlCharacters = true;
            text.rectTransform.sizeDelta = new Vector2(4000f, 200f);

            var visible = ReadingTextSanitizer.Visible(input);
            text.text = ReadingTextSanitizer.Plain(input);
            text.ForceMeshUpdate();
            var info = text.textInfo;

            Assert.That(info.characterCount, Is.EqualTo(visible.Length), $"TMP lays out every visible character of {text.text}");
            for (var i = 0; i < visible.Length; i++)
            {
                Assert.That(info.characterInfo[i].character, Is.EqualTo(visible[i]), $"character {i} of {text.text}");
            }
        }

        [Test]
        public void TmpShowsAnEscapedAngleBracketLiterally()
        {
            AssertTmpShowsExactlyTheVisibleText("\\u003Csize=200>大");
        }

        [Test]
        public void TmpKeepsNoparseOpenPastAnEscapedNoparseClose()
        {
            AssertTmpShowsExactlyTheVisibleText("a<b>\\u003C/noparse><size=200>b");
        }

        [Test]
        public void TmpShowsALinkTagLiterally()
        {
            AssertTmpShowsExactlyTheVisibleText("a<b><a href=\"x\">链接</a>");
        }

        [Test]
        public void TmpKeepsNoparseOpenPastANoparseCloseWithASpace()
        {
            AssertTmpShowsExactlyTheVisibleText("a<b></noparse ><size=200>b");
        }

        [Test]
        public void VisibleTurnsAnEscapedLineBreakIntoALineFeed()
        {
            Assert.That(ReadingTextSanitizer.Visible("第一行\\n第二行"), Is.EqualTo("第一行\n第二行"));
        }

        [Test]
        public void VisibleTurnsAnEscapedCrLfIntoOneLineFeed()
        {
            Assert.That(ReadingTextSanitizer.Visible("a\\r\\nb"), Is.EqualTo("a\nb"));
        }

        [Test]
        public void VisibleShowsAnEscapedTabWithAFullWidthBackslash()
        {
            Assert.That(ReadingTextSanitizer.Visible("a\\tb"), Is.EqualTo("a＼tb"));
        }

        [Test]
        public void VisibleShowsAPathBackslashFullWidth()
        {
            Assert.That(ReadingTextSanitizer.Visible("C:\\x"), Is.EqualTo("C:＼x"));
        }

        [Test]
        public void VisibleNeutralisesLinkTags()
        {
            Assert.That(ReadingTextSanitizer.Visible("<a href=\"x\">链接</a>"), Is.EqualTo("＜a href=\"x\">链接＜/a>"));
        }

        [Test]
        public void VisibleDropsAVariationSelector()
        {
            Assert.That(ReadingTextSanitizer.Visible("a\uFE0Fb"), Is.EqualTo("ab"),
                "TMP gives a selector after a character no entry, but keeps it after a sprite, so it never reaches the text");
        }

        [Test]
        public void VisibleDropsASupplementaryVariationSelector()
        {
            Assert.That(ReadingTextSanitizer.Visible("a\U000E0101b"), Is.EqualTo("ab"));
        }

        [Test]
        public void VisibleDropsANul()
        {
            Assert.That(ReadingTextSanitizer.Visible("a\0b"), Is.EqualTo("ab"),
                "U+0000 stops TMP's text processing and would hide the rest of the field");
        }

        [Test]
        public void VisibleKeepsASurrogatePairWholeWhileDroppingASelector()
        {
            Assert.That(ReadingTextSanitizer.Visible("\U0001F319\uFE0F"), Is.EqualTo("\U0001F319"),
                "control: only the selector goes, the pair stays whole");
        }

        [Test]
        public void PlainKeepsOtherTagsLiteralInsideNoparse()
        {
            Assert.That(ReadingTextSanitizer.Plain("<abbr>"), Is.EqualTo("<noparse><abbr></noparse>"),
                "control: a tag TMP does not act on inside noparse keeps its ASCII '<'");
        }

        [Test]
        public void FormatterSplitsAnAnalysisWhoseLineBreaksAreEscaped()
        {
            var draws = ThreeCards();
            const string analysis = "过去：愚者 — 旧的节奏正在松动。\\n现在：魔术师 — 资源齐备。\\n未来：女祭司（逆位）— 别只听外界的声音。";
            Assert.That(analysis, Does.Not.Contain("\n"), "control: no real line feed anywhere");

            var formatted = CardAnalysisFormatter.Build(analysis, draws);

            Assert.That(formatted.Ranges.Length, Is.EqualTo(3), "double-escaped line breaks still split the analysis per card");
        }

        [Test]
        public void VisibleCapKeepsASurrogatePairWhole()
        {
            var text = new string('字', ReadingTextSanitizer.MaxFieldLength - 1) + "\U0001F319";
            Assert.That(text.Length, Is.EqualTo(ReadingTextSanitizer.MaxFieldLength + 1), "control: 4001 UTF-16 units");
            Assert.That(char.IsHighSurrogate(text[ReadingTextSanitizer.MaxFieldLength - 1]), Is.True,
                "control: the pair sits at indices 3999-4000");
            LogAssert.Expect(LogType.Warning, new Regex("truncated a 4001-character field to 3999 characters"));

            Assert.That(ReadingTextSanitizer.Visible(text),
                Is.EqualTo(text.Substring(0, ReadingTextSanitizer.MaxFieldLength - 1) + ReadingTextSanitizer.TruncationMark));
        }

        [Test]
        public void FormatterCapsTheCardAnalysisOnceBeforeSplitting()
        {
            var draws = ThreeCards();
            var filler = new string('甲', 1500);
            var analysis = string.Join("\n",
                draws[0].position_name + "：" + filler,
                draws[1].position_name + "：" + filler,
                draws[2].position_name + "：" + filler);
            Assert.That(analysis.Length, Is.EqualTo(4511), "control: the field is over the 4000 cap, each line under it");
            LogAssert.Expect(LogType.Warning, new Regex("truncated a 4511-character field"));

            var formatted = CardAnalysisFormatter.Build(analysis, draws);

            var mark = ReadingTextSanitizer.TruncationMark;
            var marks = (formatted.RichText.Length - formatted.RichText.Replace(mark, string.Empty).Length) / mark.Length;
            Assert.That(marks, Is.EqualTo(1), "the whole card analysis is capped once");
            Assert.That(formatted.Ranges.Length, Is.EqualTo(3), "the cap lands inside the third line, which still starts with its name");
        }

        // 过去 / 现在 / 未来: the backend's three-card position names.
        private static CardDrawData[] PastPresentFuture()
        {
            return LocalReadingSimulator.CreatePlaceholderDraws(3, new[] { "过去", "现在", "未来" }, null);
        }

        [Test]
        public void LineOrderNeverOverridesANameMatch()
        {
            // 未來 is traditional, so the third line matches no card; the two name matches are out of order.
            const string analysis = "1. 现在：资源齐备。\n2. 过去：旧的节奏正在松动。\n3. 未來：别只听外界的声音。";

            var result = CardAnalysisParser.Parse(analysis, PastPresentFuture());

            Assert.That(result.Success, Is.False, "the 现在 line must not be shown under 过去's heading");
        }

        [Test]
        public void LineOrderFillsOnlyTheCardsNoNameMatched()
        {
            const string analysis = "1. 过去：旧的节奏正在松动。\n2. 现在：资源齐备。\n3. 未來：别只听外界的声音。";

            var result = CardAnalysisParser.Parse(analysis, PastPresentFuture());

            Assert.That(result.Success, Is.True, "control: both name matches sit at their own lines");
            Assert.That(result.Bodies[0], Does.Not.StartWith("过去"), "a name-matched card keeps its name-stripped body");
            Assert.That(result.Bodies[1], Does.Not.StartWith("现在"), "a name-matched card keeps its name-stripped body");
            Assert.That(result.Bodies[2], Is.EqualTo("未來：别只听外界的声音。"), "the unmatched card takes its own line without the ordinal");
        }

        [Test]
        public void ResultReadingCopyHasNoAsciiLetters()
        {
            var copies = new[]
            {
                ReleaseUxCopy.ResultSectionWarning, ReleaseUxCopy.CardHeadingSeparator,
                ReleaseUxCopy.CardUprightMark, ReleaseUxCopy.CardReversedMark,
            };

            foreach (var copy in copies)
            {
                Assert.That(string.IsNullOrWhiteSpace(copy), Is.False, "control: the constant has content");
                Assert.That(Regex.IsMatch(copy, "[A-Za-z]"), Is.False, $"'{copy}' should not contain ASCII letters");
            }
        }
    }
}
