using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
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
        // 过去 / 现在 / 建议: 愚者, 魔术师, 女祭司 (the third card is reversed).
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
            var result = CardAnalysisParser.Parse("1. 过去：愚者（正位）\n2. 现在：魔术师（正位）\n3. 建议：女祭司（逆位）", draws);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Bodies, Is.EqualTo(new[] { string.Empty, string.Empty, string.Empty }));
        }

        [Test]
        public void ParsesOrdinalsSeparatorsAndPositionOnlyPrefixes()
        {
            var draws = ThreeCards();
            const string analysis = "（1）过去——旧的节奏正在松动。\r\n② 现在：魔术师 资源齐备。\n三、建议 · 女祭司（逆位）：别只听外界的声音。";

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
            var expected = new[] { "过去 · 愚者（正位）", "现在 · 魔术师（正位）", "建议 · 女祭司（逆位）" };

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
