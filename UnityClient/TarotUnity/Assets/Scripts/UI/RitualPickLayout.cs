using System;
using UnityEngine;

namespace TarotUnity.UI
{
    public readonly struct RitualPickPose
    {
        public readonly Vector2 Center;
        public readonly Vector2 Size;
        public readonly float Roll;
        public Rect Bounds
        {
            get
            {
                var radians = Roll * Mathf.Deg2Rad;
                var c = Mathf.Abs(Mathf.Cos(radians));
                var s = Mathf.Abs(Mathf.Sin(radians));
                var extent = new Vector2(Size.x * c + Size.y * s, Size.x * s + Size.y * c);
                return new Rect(Center - extent * 0.5f, extent);
            }
        }
        public RitualPickPose(Vector2 center, Vector2 size, float roll = 0f)
        { Center = center; Size = size; Roll = roll; }
    }

    public sealed class RitualPickFrame
    {
        public readonly RitualPickPose[] Fan = new RitualPickPose[78];
        public RitualPickPose[] Selected;
        public Rect[] Slots;
        public Vector2[] Labels;
        public float LabelFont;
        public Rect Progress;
        public float ProgressFont;
        public float NumberFont;
        public Rect[] Ticks;
        public float Scale;
    }

    /// <summary>Measured v6 geometry, in top-left-origin pixels; density is not a layout breakpoint.</summary>
    public static class RitualPickLayout
    {
        public static RitualPickFrame Compute(int count, float width, float height)
        {
            if (count != 1 && count != 3 && count != 10) throw new ArgumentOutOfRangeException(nameof(count));
            var designWidth = Mathf.Lerp(1024f, 1280f, Mathf.InverseLerp(4f / 3f, 16f / 9f, width / height));
            var scale = width / designWidth;
            var w = designWidth - 2f;
            var h = height / scale - 2f;
            var frame = new RitualPickFrame { Scale = scale, Selected = new RitualPickPose[count],
                Slots = new Rect[count], Labels = new Vector2[count], Ticks = new Rect[count] };
            var fanSize = new Vector2(w * 0.081f, w * 0.141f);
            for (var i = 0; i < 78; i++)
            {
                var offset = (i % 39) / 38f - 0.5f;
                var angle = offset * 72f;
                var top = 1f + h * (0.44f + (i / 39) * 0.12f - 0.05f
                    + Mathf.Pow(Mathf.Abs(offset) * 2f, 1.7f) * 0.06f);
                var pivot = new Vector2(designWidth * 0.5f + offset * 0.58f * w, top + 1.6f * fanSize.y);
                var r = angle * Mathf.Deg2Rad;
                var center = pivot + new Vector2(Mathf.Sin(r), -Mathf.Cos(r)) * (fanSize.y * 1.1f);
                frame.Fan[i] = new RitualPickPose(center * scale, fanSize * scale, angle);
            }
            var columns = count == 10 ? 5 : count;
            var size = count == 10 ? new Vector2(w * 0.043f, w * 0.075f)
                : new Vector2(w * 0.065f, Mathf.Min(w * 0.113f, h * 0.23f));
            var gap = w * (count == 10 ? 0.021f : 0.03f);
            var startX = (designWidth - columns * size.x - (columns - 1) * gap) * 0.5f;
            var topY = 1f + h * (count == 10 ? 0.05f : 0.07f);
            frame.LabelFont = (count == 10 ? 9f : Mathf.Max(10f, w * 0.009f)) * scale;
            for (var i = 0; i < count; i++)
            {
                var origin = new Vector2(startX + (i % columns) * (size.x + gap),
                    topY + (i / columns) * (size.y + h * 0.033f));
                frame.Slots[i] = new Rect(origin * scale, size * scale);
                frame.Selected[i] = new RitualPickPose((origin + size * 0.5f) * scale, (size - Vector2.one * 2f) * scale);
                frame.Labels[i] = new Vector2(origin.x + size.x * 0.5f,
                    origin.y + size.y - 1f + h * (count == 10 ? 0.026f : 0.035f)
                    - Mathf.Round(frame.LabelFont / scale * 1.13f) * 0.5f) * scale;
            }
            frame.ProgressFont = Mathf.Max(12f, w * 0.0105f) * scale;
            frame.NumberFont = w * 0.019f * scale;
            var lineHeight = Mathf.Round(frame.NumberFont / scale * 3f) * 0.5f;
            var tick = w * 0.004f;
            var tickGap = w * 0.007f;
            var bottom = 1f + h * 0.97f;
            var textBottom = bottom - tick - h * 0.02f;
            frame.Progress = new Rect(new Vector2(1f, textBottom - lineHeight) * scale, new Vector2(w, lineHeight) * scale);
            for (var i = 0; i < count; i++)
                frame.Ticks[i] = new Rect(new Vector2((designWidth - count * tick - (count - 1) * tickGap) * 0.5f
                    + i * (tick + tickGap), bottom - tick) * scale, Vector2.one * tick * scale);
            return frame;
        }

        public static float FlightEase(float time) => Bezier(time, 0.22f, 0.72f, 0.2f, 1f);
        public static float HoverEase(float time) => Bezier(time, 0.25f, 0.1f, 0.25f, 1f);

        public static RitualPickPose Flight(RitualPickPose from, RitualPickPose to, float time, float height)
        {
            var t = FlightEase(time);
            var mid = new RitualPickPose(Vector2.Lerp(from.Center, to.Center, 0.55f) - Vector2.up * height * 0.1f,
                from.Size * 1.08f, from.Roll * 0.18f);
            var a = t < 0.5f ? from : mid;
            var b = t < 0.5f ? mid : to;
            var k = t < 0.5f ? t * 2f : (t - 0.5f) * 2f;
            return new RitualPickPose(Vector2.Lerp(a.Center, b.Center, k), Vector2.Lerp(a.Size, b.Size, k), Mathf.Lerp(a.Roll, b.Roll, k));
        }

        private static float Bezier(float time, float x1, float y1, float x2, float y2)
        {
            var lo = 0f; var hi = 1f;
            for (var i = 0; i < 22; i++)
            {
                var t = (lo + hi) * 0.5f;
                var x = 3f * (1f - t) * (1f - t) * t * x1 + 3f * (1f - t) * t * t * x2 + t * t * t;
                if (x < Mathf.Clamp01(time)) lo = t; else hi = t;
            }
            var u = (lo + hi) * 0.5f;
            return 3f * (1f - u) * (1f - u) * u * y1 + 3f * (1f - u) * u * u * y2 + u * u * u;
        }
    }
}
