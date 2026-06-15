namespace TarotUnity.UI
{
    /// <summary>
    /// Canonical cross-screen layout constants - the single source of truth for the
    /// secondary spacing/alignment rhythm shared by MainMenu, ReadingRoom, and Result.
    /// The three core canvases were composed across many phases yet already share these
    /// values (verified by the Phase 30 layout audit); centralising them here keeps any
    /// future screen consistent and gives the Phase 30 consistency tests a definition to
    /// assert against.
    /// </summary>
    public static class TarotUiSpacing
    {
        /// <summary>Reference resolution every core canvas scales against.</summary>
        public const float ReferenceWidth = 1280f;

        /// <summary>Reference resolution every core canvas scales against.</summary>
        public const float ReferenceHeight = 720f;

        /// <summary>
        /// CanvasScaler match factor (0 = match width, 1 = match height). 0.5 balances
        /// both so the layout degrades symmetrically on off-aspect windows.
        /// </summary>
        public const float ScreenMatchWidthOrHeight = 0.5f;

        /// <summary>
        /// Tolerance (canvas units) within which a centered heading/footer is considered
        /// horizontally symmetric. Small float drift from layout math is expected.
        /// </summary>
        public const float CenterSymmetryTolerance = 1.5f;

        /// <summary>
        /// A heading and the sub-label directly beneath it may abut, but their rects must
        /// not overlap by more than this (canvas units). Text leading keeps a visible gap
        /// even when the rects touch; this guards against a sub-label crashing into the
        /// heading if copy grows.
        /// </summary>
        public const float HeadingSubLabelMaxOverlap = 3f;
    }
}
