using UnityEngine;

namespace TarotUnity.UI
{
    public readonly struct SpreadCellPlacement
    {
        public SpreadCellPlacement(Vector2 position, float scale)
        {
            Position = position;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public float Scale { get; }
    }

    public sealed class SpreadLayoutResult
    {
        public SpreadCellPlacement[] Cells;
        public int Rows;
        public float LabelFontSize;
        public float LabelHeight;
        public float BandBottom;
        public Vector2 ReadingPosition;
        public Vector2 ReadingSize;

        public float ReadingTop => ReadingPosition.y + ReadingSize.y * 0.5f;
        public float ReadingBottom => ReadingPosition.y - ReadingSize.y * 0.5f;
    }

    /// <summary>
    /// Phase 67 (spec C 4.1): geometry of the multi-card Result screen, in centre-anchored
    /// canvas units. The band hangs from the header (136 below the canvas top), rows are
    /// centred at a pitch of min(348, 1180 / cards in the row), and the reading panel fills the
    /// rest down to 10 above the button row - so every extra unit of canvas height goes to the
    /// reading. Cell constants come from the Phase 60 cell (frame 164x234 centred 18 above the
    /// cell centre, label centred at -118).
    /// </summary>
    public static class ResultSpreadLayout
    {
        public const float RowWidth = 1180f;
        public const float BasePitch = 348f;
        public const float ReadingWidth = 1180f;
        public const float BandTopFromCanvasTop = 136f;
        public const float ButtonTopFromCanvasBottom = 84f;
        public const float ReadingBottomFromCanvasBottom = 94f;
        public const float BandReadingGap = 12f;
        public const float RowGap = 6f;
        public const float CellFrameTop = 135f;
        public const float CellFrameHalfWidth = 82f;
        public const float CellLabelCentre = -118f;
        public const int TwoRowThreshold = 6;
        public const float OneRowScale = 0.52f;
        public const float TwoRowScale = 0.31f;
        public const float OneRowLabelSize = 18f;
        public const float TwoRowLabelSize = 14f;
        public const float OneRowLabelHeight = 28f;
        public const float TwoRowLabelHeight = 22f;

        public static SpreadLayoutResult Compute(int cardCount, float canvasHeight)
        {
            var count = Mathf.Max(0, cardCount);
            var height = canvasHeight >= 1f ? canvasHeight : TarotUiSpacing.ReferenceHeight;
            var twoRows = count >= TwoRowThreshold;
            var rows = count == 0 ? 0 : (twoRows ? 2 : 1);
            var scale = twoRows ? TwoRowScale : OneRowScale;
            var labelHeight = twoRows ? TwoRowLabelHeight : OneRowLabelHeight;
            var firstRowCount = twoRows ? (count + 1) / 2 : count;

            var cells = new SpreadCellPlacement[count];
            var rowTop = height * 0.5f - BandTopFromCanvasTop;
            var bandBottom = rowTop;
            var placed = 0;
            for (var row = 0; row < rows; row++)
            {
                var inRow = row == 0 ? firstRowCount : count - firstRowCount;
                var centreY = rowTop - CellFrameTop * scale;
                var labelBottom = centreY + CellLabelCentre * scale - labelHeight * 0.5f;
                var pitch = Mathf.Min(BasePitch, inRow > 0 ? RowWidth / inRow : BasePitch);
                for (var i = 0; i < inRow; i++)
                {
                    var x = (i - (inRow - 1) * 0.5f) * pitch;
                    cells[placed++] = new SpreadCellPlacement(new Vector2(x, centreY), scale);
                }

                bandBottom = labelBottom;
                rowTop = labelBottom - RowGap;
            }

            var readingTop = bandBottom - BandReadingGap;
            var readingBottom = -height * 0.5f + ReadingBottomFromCanvasBottom;
            return new SpreadLayoutResult
            {
                Cells = cells,
                Rows = rows,
                LabelFontSize = twoRows ? TwoRowLabelSize : OneRowLabelSize,
                LabelHeight = labelHeight,
                BandBottom = bandBottom,
                ReadingPosition = new Vector2(0f, (readingTop + readingBottom) * 0.5f),
                ReadingSize = new Vector2(ReadingWidth, Mathf.Max(0f, readingTop - readingBottom)),
            };
        }
    }
}
