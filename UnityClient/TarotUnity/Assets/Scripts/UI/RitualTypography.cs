using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class RitualTypography : MonoBehaviour
    {
        private sealed class TextBinding
        {
            public TMP_Text text;
            public TMP_FontAsset font;
            public TextWrappingModes wrapping;
            public TMP_InputField input;
            public bool fixedStyle;
        }

        private sealed class InputBinding
        {
            public TMP_InputField input;
            public TMP_FontAsset font;
        }

        private static readonly Dictionary<Font, TMP_FontAsset> Fonts = new();
        private static readonly HashSet<TMP_FontAsset> Subsets = new();
        private readonly List<TextBinding> texts = new();
        private readonly List<InputBinding> inputs = new();
        private TMP_Text brushTitle;

        public static void ApplyHierarchy(Transform root)
        {
            if (root == null) return;
            var owner = root.GetComponent<RitualTypography>();
            if (owner == null) owner = root.gameObject.AddComponent<RitualTypography>();
            owner.BindHierarchy();
        }

        internal void UseBrushTitle(TMP_Text title)
        {
            brushTitle = title;
            BindHierarchy();
        }

        private void BindHierarchy()
        {
            var theme = GetComponentInParent<TarotUiTheme>(true);
            texts.Clear();
            inputs.Clear();
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                var input = text.GetComponentInParent<TMP_InputField>(true);
                var button = text.GetComponentInParent<Button>(true);
                var quiet = button != null && (button.name == "QuitButton" || button.name == "RitualReturnMenu" || button.name == "RitualChangeSpread");
                var emphasis = text.name == "RitualQuestionLabel" || (input == null && button != null && !quiet && text.name != "RitualSpreadSubtitle");
                var resource = text == brushTitle ? "brush-0" : emphasis ? "noto-1" : "noto-0";
                var font = FontFor(resource, theme, text.font);
                var wrapping = text == brushTitle || emphasis || (input != null && input.lineType == TMP_InputField.LineType.SingleLine)
                    ? TextWrappingModes.NoWrap : text.textWrappingMode;
                var fixedStyle = GetComponent<RitualMenuView>() != null || GetComponent<RitualReadingView>() != null;
                texts.Add(new TextBinding { text = text, font = font, wrapping = wrapping, input = input, fixedStyle = fixedStyle });
            }

            foreach (var input in GetComponentsInChildren<TMP_InputField>(true))
            {
                inputs.Add(new InputBinding { input = input, font = FontFor("noto-0", theme, input.fontAsset) });
            }
            RestoreBindings();
        }

        private void LateUpdate()
        {
            RestoreBindings();
        }

        private void RestoreBindings()
        {
            // Theme.Apply owns the legacy look. Restore only overwritten properties, not the whole hierarchy each frame.
            foreach (var binding in inputs)
            {
                if (binding.input != null && binding.font != null && binding.input.fontAsset != binding.font)
                    binding.input.fontAsset = binding.font;
            }
            foreach (var binding in texts)
            {
                if (binding.text == null) continue;
                if (binding.font != null && binding.text.font != binding.font) binding.text.font = binding.font;
                if (binding.fixedStyle)
                {
                    if (binding.text.fontStyle != FontStyles.Normal) binding.text.fontStyle = FontStyles.Normal;
                    if (binding.text.fontWeight != FontWeight.Regular) binding.text.fontWeight = FontWeight.Regular;
                }
                var input = binding.input;
                var wrapping = input != null
                    ? input.lineType == TMP_InputField.LineType.SingleLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal
                    : binding.wrapping;
                if (binding.text.textWrappingMode != wrapping) binding.text.textWrappingMode = wrapping;
            }
        }

        private static TMP_FontAsset FontFor(string resource, TarotUiTheme theme, TMP_FontAsset previous)
        {
            var source = Resources.Load<Font>("RitualFonts/" + resource);
            var fallback = theme != null ? theme.TmpBodyFont : previous;
            if (source == null) return fallback != null ? fallback : previous;
            var font = RuntimeFont(source);
            if (font == null) return fallback != null ? fallback : previous;
            Subsets.Add(font);
            AddFallback(font, fallback);
            if (theme != null)
            {
                AddFallback(font, theme.TmpDisplayFont);
                if (theme.TmpBodyFont == null && theme.BodyFont != null)
                    AddFallback(font, RuntimeFont(theme.BodyFont));
            }
            return font;
        }

        private static TMP_FontAsset RuntimeFont(Font source)
        {
            if (Fonts.TryGetValue(source, out var cached) && cached != null) return cached;
            var font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (font == null) return null;
            font.name = source.name + " Ritual Runtime";
            font.hideFlags = HideFlags.HideAndDontSave;
            font.material.hideFlags = HideFlags.HideAndDontSave;
            foreach (var texture in font.atlasTextures)
                if (texture != null) texture.hideFlags = HideFlags.HideAndDontSave;
            Fonts[source] = font;
            return font;
        }

        private static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (fallback == null || fallback == font || Subsets.Contains(fallback)) return;
            if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
        }
    }
}
