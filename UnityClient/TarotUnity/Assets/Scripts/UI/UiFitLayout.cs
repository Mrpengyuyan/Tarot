using System;
using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: the sizing rules of the glass-and-card-stock UI. Panels are sized by their
    /// text: a TMP preferred size plus padding that scales with the font, plus the transparent
    /// or shadow ring baked into both nine-slice sprites. Pure maths, so the editor bootstrapper
    /// and the tests share one source.
    /// </summary>
    public static class UiFitLayout
    {
        /// <summary>Transparent (glass) or shadow (card stock) ring inside both sprites, in canvas units.</summary>
        public const float SkinMargin = 8f;
        public const float TypeScale = 1.15f;
        public const float PadXPerFont = 1.4f;
        public const float PadYPerFont = 0.5f;

        /// <summary>16pt, the theme's muted-text limit, after the type scale.</summary>
        public const float MutedSizeThresholdScaled = 18.5f;

        // Step bar.
        public const float StepGap = 20f;
        public const float StepDotSize = 12f;
        public static readonly Vector2 HudPad = new Vector2(20f, 4f);

        // Action dock.
        public const float RowGap = 20f;
        public static readonly Vector2 DockPad = new Vector2(24f, 10f);
        public const float DockRowGap = 2f;
        public const float DockTop = -122f;
        public const float BelowDockGap = 4f;
        public const float InputWidthRatio = 0.62f;

        // Stars.
        public const float CornerStarSize = 18f;
        public const float FlankStarPerFont = 0.6f;
        public const float FlankGapPerFont = 0.35f;

        public const float CanvasHalfHeight = 360f;

        /// <summary>Scales a font size and rounds it to the nearest half point, halves rounding up.</summary>
        public static float ScaledFontSize(float size, float factor)
        {
            if (Mathf.Abs(factor - 1f) < 1e-4f)
            {
                return size;
            }

            return (float)(Math.Floor((double)size * factor * 2.0 + 0.5 + 1e-4) / 2.0);
        }

        public static Vector2 Padding(float fontSize)
        {
            return new Vector2(Mathf.Round(PadXPerFont * fontSize), Mathf.Round(PadYPerFont * fontSize));
        }

        /// <summary>The rect that holds text of this preferred size without touching the frame.</summary>
        public static Vector2 FitSize(Vector2 preferred, float fontSize)
        {
            var pad = Padding(fontSize);
            return new Vector2(
                Mathf.Ceil(preferred.x) + 2f * pad.x + 2f * SkinMargin,
                Mathf.Ceil(preferred.y) + 2f * pad.y + 2f * SkinMargin);
        }

        /// <summary>Centres of items laid edge to edge with a fixed gap, the row centred on zero.</summary>
        public static float[] RowCenters(float[] widths, float gap)
        {
            var total = 0f;
            foreach (var w in widths)
            {
                total += w;
            }

            total += gap * Mathf.Max(0, widths.Length - 1);
            var centers = new float[widths.Length];
            var x = -total / 2f;
            for (var i = 0; i < widths.Length; i++)
            {
                centers[i] = x + widths[i] / 2f;
                x += widths[i] + gap;
            }

            return centers;
        }
    }
}
