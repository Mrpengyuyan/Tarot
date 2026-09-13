using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67 (spec C 4.1): the top card band is sized by card count - one row up to five,
    /// two rows from six - and the reading panel takes everything between the band and the
    /// button row, including any extra height a taller canvas brings.
    /// </summary>
    public sealed class Phase67ResultLayoutTests
    {
        private const float ReferenceHeight = 720f;
        private const float DividerBottom = 231f;   // Phase8_ResultGoldDividerTop: y 238, 14 tall
        private const float ButtonTop = -276f;      // BackToMenuButton: y -300, 48 tall

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void OneRowReadingIsAtLeast326AtSixteenByNine(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);

            Assert.That(layout.Rows, Is.EqualTo(1));
            Assert.That(layout.Cells.Length, Is.EqualTo(cardCount), "control: every card is placed");
            Assert.That(layout.ReadingSize.y, Is.GreaterThanOrEqualTo(326f));
            Assert.That(layout.ReadingSize.x, Is.EqualTo(1180f));
        }

        [Test]
        public void TenCardsUseTwoRowsOfFiveWithReadableLabels()
        {
            var layout = ResultSpreadLayout.Compute(10, ReferenceHeight);

            Assert.That(layout.Rows, Is.EqualTo(2));
            var firstRowY = layout.Cells[0].Position.y;
            for (var i = 0; i < 5; i++)
            {
                Assert.That(layout.Cells[i].Position.y, Is.EqualTo(firstRowY).Within(0.01f), $"card {i} is in the first row");
            }

            for (var i = 5; i < 10; i++)
            {
                Assert.That(layout.Cells[i].Position.y, Is.LessThan(firstRowY - 1f), $"card {i} is in the second row");
            }

            Assert.That(layout.LabelFontSize, Is.GreaterThanOrEqualTo(14f));
        }

        [Test]
        public void TenCardReadingIsAtLeast282AtSixteenByNine()
        {
            Assert.That(ResultSpreadLayout.Compute(10, ReferenceHeight).ReadingSize.y, Is.GreaterThanOrEqualTo(282f));
        }

        [TestCase(3, 800f)]
        [TestCase(3, 960f)]
        [TestCase(10, 800f)]
        [TestCase(10, 960f)]
        public void TallerCanvasesGiveEveryExtraUnitToTheReading(int cardCount, float canvasHeight)
        {
            var reference = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            var taller = ResultSpreadLayout.Compute(cardCount, canvasHeight);

            Assert.That(taller.ReadingSize.y - reference.ReadingSize.y,
                Is.EqualTo(canvasHeight - ReferenceHeight).Within(0.01f));
            Assert.That(taller.Cells[0].Position.y - reference.Cells[0].Position.y,
                Is.EqualTo((canvasHeight - ReferenceHeight) * 0.5f).Within(0.01f), "the band stays pinned under the header");
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void BandClearsTheReadingAndTheReadingClearsTheButtons(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            var firstFrameTop = layout.Cells[0].Position.y + ResultSpreadLayout.CellFrameTop * layout.Cells[0].Scale;

            Assert.That(firstFrameTop, Is.LessThanOrEqualTo(DividerBottom), "the first row stays under the header divider");
            Assert.That(layout.BandBottom - layout.ReadingTop, Is.GreaterThanOrEqualTo(ResultSpreadLayout.BandReadingGap - 0.01f));
            Assert.That(layout.ReadingBottom - ButtonTop, Is.GreaterThanOrEqualTo(10f - 0.01f));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void CellsStayInsideTheReferenceWidth(int cardCount)
        {
            var layout = ResultSpreadLayout.Compute(cardCount, ReferenceHeight);
            foreach (var cell in layout.Cells)
            {
                var halfWidth = ResultSpreadLayout.CellFrameHalfWidth * cell.Scale;
                Assert.That(Mathf.Abs(cell.Position.x) + halfWidth, Is.LessThanOrEqualTo(640f));
            }
        }

        [Test]
        public void FiveCardRowIsCentredAndDistinct()
        {
            var cells = ResultSpreadLayout.Compute(5, ReferenceHeight).Cells;

            Assert.That(cells[2].Position.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(cells[0].Position.x, Is.EqualTo(-cells[4].Position.x).Within(0.01f));
            for (var i = 1; i < 5; i++)
            {
                Assert.That(cells[i].Position.x - cells[i - 1].Position.x,
                    Is.GreaterThan(2f * ResultSpreadLayout.CellFrameHalfWidth * cells[i].Scale), "cards must not overlap");
            }
        }
    }
}
