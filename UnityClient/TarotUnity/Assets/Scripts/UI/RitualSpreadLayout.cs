using System;
using UnityEngine;

namespace TarotUnity.UI
{
    public sealed class RitualCardPlacement
    {
        public Vector2 Center;
        public Vector2 Size;
        public float Tilt;
        public float Roll;
        public Rect CardRect;
        public Vector2 LabelCenter;
        public float LabelWidth;
        public float LabelFontSize;
    }

    public sealed class RitualSpreadFrame
    {
        public RitualCardPlacement[] Cards;
        public Vector2 PerspectiveOrigin;
        public float Perspective;
        public Rect ActionRect;
        public float ActionFontSize;
        public Vector2 ProgressCenter;
        public float ProgressFontSize;
    }

    /// <summary>Approved v6 geometry in top-left-origin pixels, excluding browser controls.</summary>
    public static class RitualSpreadLayout
    {
        public const float Perspective = 1300f;

        public static RitualSpreadFrame Compute(int count, float width, float height)
        {
            if (count != 1 && count != 3 && count != 10) throw new ArgumentOutOfRangeException(nameof(count));
            if (width <= 2f || height <= 2f) throw new ArgumentOutOfRangeException(nameof(width));
            var aspectBlend = Mathf.InverseLerp(4f / 3f, 16f / 9f, width / height);
            var designWidth = Mathf.Lerp(1024f, 1280f, aspectBlend);
            var scale = width / designWidth;
            var frame = ComputeDesign(count, designWidth, height / scale, aspectBlend);
            foreach (var card in frame.Cards)
            {
                card.Center *= scale;
                card.Size *= scale;
                card.CardRect = new Rect(card.CardRect.position * scale, card.CardRect.size * scale);
                card.LabelCenter *= scale;
                card.LabelWidth *= scale;
                card.LabelFontSize *= scale;
            }
            frame.PerspectiveOrigin *= scale;
            frame.Perspective = Perspective * scale;
            frame.ActionRect = new Rect(frame.ActionRect.position * scale, frame.ActionRect.size * scale);
            frame.ActionFontSize *= scale;
            frame.ProgressCenter *= scale;
            frame.ProgressFontSize *= scale;
            return frame;
        }

        private static RitualSpreadFrame ComputeDesign(int count, float width, float height, float aspectBlend)
        {
            var w = width - 2f;
            var h = height - 2f;
            var ten = count == 10;
            var top = Mathf.Lerp(0.36f, ten ? 0.27f : 0.29f, aspectBlend);
            var bottom = ten ? 0.21f : 0.20f;
            var origin = new Vector2(width * 0.5f, 1f + (top + (1f - top - bottom) * 0.5f) * h);
            var cardWidth = w * (count == 1 ? 0.15f : ten ? 0.064f : 0.12f);
            var cardHeight = Mathf.Min(w * (count == 1 ? 0.26f : ten ? 0.112f : 0.21f),
                h * (count == 1 ? 0.47f : ten ? 0.20f : 0.40f));
            var pitch = w * (ten ? 0.1152f : 0.18f);
            var columns = ten ? 5 : count;
            var font = w * (ten ? 0.0072f : 0.0105f);
            var lineHeight = Mathf.Round(font * 1.45f * 2f) * 0.5f;
            var gap = h * (ten ? 0.025f : 0.047f);
            var cards = new RitualCardPlacement[count];
            for (var i = 0; i < count; i++)
            {
                var card = new RitualCardPlacement
                {
                    Center = origin + new Vector2((i % columns - (columns - 1) * 0.5f) * pitch,
                        ten ? (i / columns - 0.5f) * (cardHeight + 0.038f * h) : 0f),
                    Size = new Vector2(cardWidth, cardHeight),
                    Tilt = ten ? 12f : 18f,
                    Roll = count == 3 ? (1 - i) * 4f : 0f,
                };
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (var corner = 0; corner < 4; corner++)
                {
                    var point = Project(card, origin, new Vector2((corner % 2 - 0.5f) * cardWidth,
                        (corner / 2 - 0.5f) * cardHeight), out _);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                card.CardRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                card.LabelCenter = Project(card, origin, new Vector2(0f, cardHeight * 0.5f + gap - lineHeight * 0.5f), out var labelScale);
                card.LabelFontSize = font * labelScale;
                card.LabelWidth = cardWidth * 1.6f * labelScale;
                cards[i] = card;
            }
            var actionFont = w * 0.0165f;
            var actionHeight = Mathf.Max(44f, Mathf.Round(actionFont * 1.45f * 2f) * 0.5f + h * 0.024f);
            var actionWidth = actionFont * 5f + w * 0.06f;
            var progressFont = Mathf.Max(12f, w * 0.0105f);
            return new RitualSpreadFrame
            {
                Cards = cards,
                PerspectiveOrigin = origin,
                ActionRect = new Rect((width - actionWidth) * 0.5f, 1f + h * (1f - 0.077f) - actionHeight, actionWidth, actionHeight),
                ActionFontSize = actionFont,
                ProgressCenter = new Vector2(width * 0.5f, 1f + h * 0.9f - progressFont * 0.725f),
                ProgressFontSize = progressFont,
            };
        }

        private static Vector2 Project(RitualCardPlacement card, Vector2 origin, Vector2 local, out float scale)
        {
            var r = card.Roll * Mathf.Deg2Rad;
            var tilt = card.Tilt * Mathf.Deg2Rad;
            var x = local.x * Mathf.Cos(r) - local.y * Mathf.Sin(r);
            var y = local.x * Mathf.Sin(r) + local.y * Mathf.Cos(r);
            scale = Perspective / (Perspective - y * Mathf.Sin(tilt));
            return origin + new Vector2(card.Center.x - origin.x + x,
                card.Center.y - origin.y + y * Mathf.Cos(tilt)) * scale;
        }
    }
}
