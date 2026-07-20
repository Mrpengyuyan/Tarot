using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Shared render step for every visual-review capture.
    ///
    /// Why this exists: the wax and the other emissive materials are marked
    /// RealtimeEmissive, and realtime GI does not resolve on the frame a scene is
    /// loaded. Every capture builder in this project rendered exactly once after
    /// opening a scene, so every review image showed lighting that had not settled.
    /// It was measured on the menu's front candle, which reads
    ///
    ///     render 1  -> RGB(246,  77, 23)  saturation 0.91   (a red plastic tube)
    ///     render 2+ -> RGB(233, 177, 78)  saturation 0.66   (warm amber wax)
    ///
    /// and stays there through 24 renders. The "saturated red candles" that this
    /// pass was opened to fix were never in the game - only in the captures used to
    /// review it. Warming up is therefore not a nicety: a review artefact that lies
    /// about the product is worse than no artefact.
    /// </summary>
    public static class CaptureRig
    {
        /// <summary>
        /// Renders enough times for realtime GI to settle before the caller reads
        /// pixels. Measured convergence is at the second render; the default leaves
        /// headroom for heavier scenes at a cost of a few milliseconds.
        /// </summary>
        public const int DefaultWarmupRenders = 4;

        public static void RenderConverged(Camera camera, int warmupRenders = DefaultWarmupRenders)
        {
            if (camera == null)
            {
                return;
            }

            for (var i = 0; i < Mathf.Max(1, warmupRenders); i++)
            {
                camera.Render();
            }
        }
    }
}
