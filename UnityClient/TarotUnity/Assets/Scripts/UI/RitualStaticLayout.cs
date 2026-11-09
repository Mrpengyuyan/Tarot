using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.UI
{
    public sealed class RitualStaticElement
    {
        public Rect Rect;
        public float FontSize;
        public float Tracking;
        public float Rotation;
    }

    /// <summary>Settled v6 static stages in top-left-origin design pixels.</summary>
    public static class RitualStaticLayout
    {
        public static Dictionary<string, RitualStaticElement> Compute(string stage, float width, float height)
        {
            if (width <= 2f || height <= 2f) throw new ArgumentOutOfRangeException(nameof(width));
            var blend = Mathf.InverseLerp(4f / 3f, 16f / 9f, width / height);
            var designWidth = Mathf.Lerp(1024f, 1280f, blend);
            var scale = width / designWidth;
            var w = designWidth - 2f;
            var h = height / scale - 2f;
            var frame = new Dictionary<string, RitualStaticElement>();
            void Add(string key, float x, float y, float rw, float rh, float font = 0f, float tracking = 0f, float rotation = 0f)
            {
                frame[key] = new RitualStaticElement { Rect = new Rect(x, y, rw, rh), FontSize = font, Tracking = tracking, Rotation = rotation };
            }
            void Center(string key, float y, float rw, float rh, float font = 0f, float tracking = 0f, float rotation = 0f)
                => Add(key, (designWidth - rw) * 0.5f, y, rw, rh, font, tracking, rotation);
            var quiet = Mathf.Max(12f, w * 0.0105f);
            var quietLine = Mathf.Lerp(17.5f, 19.5f, blend);
            var roomBackY = 1f + h * (1f - 0.063f) - quietLine - 20f;
            if (stage == "cover")
            {
                var top = 1f + h * Mathf.Lerp(0.19f, 0.11f, blend);
                var seal = w * 0.037f;
                Center("RitualCoverSeal", top, seal, seal);
                var titleFont = w * 0.111f;
                var titleY = top + seal + h * 0.012f;
                Center("TitleText", titleY, titleFont * 4.58f, titleFont * 1.28f, titleFont, 0.11f, 2f);
                Center("RitualTitleRule", titleY + titleFont * 1.28f + h * 0.025f, w * 0.8f * 0.26f, 1f);
                var captionLine = Mathf.Lerp(12f, 15.5f, blend);
                Center("RitualCoverCaption", 1f + h * 0.962f - captionLine, w, captionLine, w * 0.008f, 0.5f);
                var quitY = 1f + h * (1f - Mathf.Lerp(0.15f, 0.13f, blend)) - quietLine - 20f;
                Center("QuitButton", quitY, quiet * 2.44f + 36f, quietLine + 20f, quiet, 0.22f);
                var font = w * 0.018f;
                var buttonHeight = Mathf.Max(44f, Mathf.Lerp(26.5f, 33f, blend) + h * 0.024f);
                Center("StartReadingButton", quitY - h * 0.03f - buttonHeight, font * 5f + w * 0.06f, buttonHeight, font, 0.2f);
            }
            else if (stage == "spread")
            {
                var y = 1f + h * Mathf.Lerp(0.41f, 0.36f, blend);
                var column = w * 0.62f / 3f;
                var titleLine = Mathf.Lerp(27.5f, 34f, blend);
                var symbolHeight = h * 0.17f;
                var optionHeight = Mathf.Max(h * 0.31f, h * 0.259f + titleLine + quietLine + 0.5f);
                var names = new[] { "OneCardButton", "ThreeCardButton", "CelticCrossButton" };
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    var x = 1f + w * 0.16f + i * (column + w * 0.03f);
                    Add(name, x, y, column, optionHeight);
                    var symbolY = y + h * 0.012f + (i == 1 ? 0f : 0.25f);
                    Add(name + "/RitualSpreadSymbol", x + (column - w * 0.13f) / 2f, symbolY, w * 0.13f, symbolHeight);
                    var labelY = symbolY + symbolHeight + h * 0.035f;
                    Add(name + "/Title", x, labelY, column, titleLine, w * 0.0185f, 0.17f);
                    Add(name + "/RitualSpreadSubtitle", x, labelY + titleLine + h * 0.012f,
                        column, quietLine + (i == 1 ? 0.5f : 0f), quiet, 0.06f);
                    var cards = i == 0 ? 1 : i == 1 ? 3 : 4;
                    for (var j = 0; j < cards; j++)
                    {
                        var angle = cards == 1 || j == 2 ? 0f : j == 3 ? 90f : (j == 0 ? -1f : 1f) * (i == 1 ? 15f : 25f);
                        var offset = j > 1 || cards == 1 ? 0f : (j == 0 ? -1f : 1f) * w * (i == 1 ? 0.027f : 0.028f);
                        var center = new Vector2(x + column / 2f, symbolY + symbolHeight / 2f)
                            + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * offset;
                        var size = new Vector2(w * 0.055f, Mathf.Min(w * 0.095f, h * 0.17f));
                        Add(name + "/RitualSpreadSymbol/Back_" + j, center.x - size.x/2f, center.y - size.y/2f, size.x, size.y, rotation: -angle);
                    }
                }
                Center("RitualSpreadCaption", 1f + h * 0.91f - quietLine, w, quietLine, quiet, 0.18f);
                Add("RitualReturnMenu", 1f + w * 0.05f, roomBackY, quiet * 2.2f + 20f, quietLine + 20f, quiet, 0.1f);
            }
            else if (stage == "question")
            {
                var titleY = 1f + h * Mathf.Lerp(0.42f, 0.4f, blend);
                var labelLine = Mathf.Lerp(42.5f, 53f, blend);
                var inputY = titleY + labelLine + h * 0.04f;
                var inputHeight = Mathf.Lerp(30.5f, 38.5f, blend) + h * 0.056f;
                Center("RitualQuestionLabel", titleY, w * 0.64f, labelLine, Mathf.Clamp(w * 0.028f, 20f, 42f), 0.12f);
                Center("QuestionInput", inputY, w * 0.64f, inputHeight, Mathf.Clamp(w * 0.021f, 16f, 30f));
                var font = Mathf.Clamp(w * 0.02f, 16f, 28f);
                Center("DrawButton", inputY + inputHeight + h * 0.06f, font * 5f + w * 0.06f,
                    Mathf.Max(44f, Mathf.Lerp(29.5f, 37f, blend) + h * 0.024f), font, 0.2f);
                Center("RitualQuestionFeedback", inputY + inputHeight + h * 0.01f, w * 0.64f, quietLine, quiet, 0.04f);
                Add("RitualChangeSpread", 1f + w * 0.05f, roomBackY, quiet * 4.4f + 20f, quietLine + 20f, quiet, 0.1f);
            }
            else throw new ArgumentOutOfRangeException(nameof(stage));
            foreach (var element in frame.Values)
            {
                element.Rect = new Rect(element.Rect.position * scale, element.Rect.size * scale);
                element.FontSize *= scale;
            }
            return frame;
        }

        public static void Apply(RectTransform rect, RitualStaticElement element, RectTransform canvas)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.localScale = Vector3.one;
            rect.sizeDelta = element.Rect.size;
            rect.position = canvas.TransformPoint(new Vector3(element.Rect.center.x - canvas.rect.width/2f,
                canvas.rect.height/2f - element.Rect.center.y, 0f));
            rect.localRotation = Quaternion.Euler(0f, 0f, element.Rotation);
        }
    }
}
