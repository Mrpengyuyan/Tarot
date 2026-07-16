using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class TarotUiTheme : MonoBehaviour
    {
        /// <summary>
        /// Typographic role of a piece of UI text. Display = large mystical
        /// titles, Emphasis = buttons and short call-outs, Body = everything
        /// the reader actually reads (instructions, captions, AI interpretation).
        /// </summary>
        public enum TextRole
        {
            Body,
            Emphasis,
            Display
        }

        [SerializeField] private string themeName = "Moonlit Tarot";
        [SerializeField] private Color textColor = new(0.96f, 0.91f, 0.80f, 1f);
        [SerializeField] private Color mutedTextColor = new(0.74f, 0.72f, 0.76f, 1f);
        [SerializeField] private Color buttonColor = new(0.12f, 0.09f, 0.16f, 0.96f);
        [SerializeField] private Color buttonHighlightColor = new(0.24f, 0.18f, 0.26f, 1f);
        [SerializeField] private Color inputColor = new(0.035f, 0.045f, 0.040f, 0.96f);
        [SerializeField] private Color accentGoldColor = new(0.86f, 0.63f, 0.24f, 1f);
        [SerializeField] private Color tableGreenColor = new(0.06f, 0.18f, 0.13f, 1f);
        [SerializeField] private Color panelIvoryColor = new(0.90f, 0.84f, 0.68f, 1f);
        [SerializeField] private float bodyLineSpacing = 1.18f;

        // Phase 24 typography. The body font carries readable text; the display
        // font is a slightly heavier cut used for titles and button labels so
        // the type hierarchy reads even before colour and size are considered.
        // Both are bundled (LXGW WenKai, SIL OFL) so Chinese glyphs render the
        // same on every machine instead of falling back to the OS sans-serif.
        [SerializeField] private Font bodyFont;
        [SerializeField] private Font displayFont;
        [SerializeField] private int displaySizeThreshold = 30;

        // Phase 43 typography. The same two cuts as SDF font assets. Legacy Text
        // rasterises glyphs into a bitmap atlas at one size, so it softens the
        // moment it is scaled and its outline is four offset copies of the mesh;
        // the SDF assets stay sharp at any resolution and get a real shader
        // outline. Screens migrate one at a time, so both paths live here until
        // the last legacy Text is gone.
        [SerializeField] private TMP_FontAsset tmpBodyFont;
        [SerializeField] private TMP_FontAsset tmpDisplayFont;

        public string ThemeName => themeName;
        public Color AccentGoldColor => accentGoldColor;
        public Color TableGreenColor => tableGreenColor;
        public Color PanelIvoryColor => panelIvoryColor;
        public Font BodyFont => bodyFont;
        public Font DisplayFont => displayFont;
        public TMP_FontAsset TmpBodyFont => tmpBodyFont;
        public TMP_FontAsset TmpDisplayFont => tmpDisplayFont;
        public int DisplaySizeThreshold => displaySizeThreshold;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                ApplyTextStyle(text);
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                ApplyTmpTextStyle(text);
            }

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                ApplyButtonStyle(button);
            }

            foreach (var input in GetComponentsInChildren<InputField>(true))
            {
                ApplyInputStyle(input);
            }
        }

        /// <summary>
        /// Classifies a text element into a typographic role. Shared by the
        /// runtime applier and the Phase 24 editor bootstrap so the role rule
        /// lives in exactly one place.
        /// </summary>
        public static TextRole ClassifyRole(Text text, int displaySizeThreshold)
        {
            if (text == null)
            {
                return TextRole.Body;
            }

            if (text.fontSize >= displaySizeThreshold)
            {
                return TextRole.Display;
            }

            return text.GetComponentInParent<Button>(true) != null
                ? TextRole.Emphasis
                : TextRole.Body;
        }

        /// <summary>
        /// Returns the bundled font for a role, falling back gracefully so the
        /// theme never blanks out text if one font slot is left empty.
        /// </summary>
        public Font FontForRole(TextRole role)
        {
            if (role == TextRole.Body)
            {
                return bodyFont != null ? bodyFont : displayFont;
            }

            return displayFont != null ? displayFont : bodyFont;
        }

        /// <summary>
        /// Classifies a TMP element into a typographic role. Mirrors the legacy
        /// overload so both text systems share one rule during the migration.
        /// </summary>
        public static TextRole ClassifyRole(TMP_Text text, int displaySizeThreshold)
        {
            if (text == null)
            {
                return TextRole.Body;
            }

            if (text.fontSize >= displaySizeThreshold)
            {
                return TextRole.Display;
            }

            return text.GetComponentInParent<Button>(true) != null
                ? TextRole.Emphasis
                : TextRole.Body;
        }

        /// <summary>Returns the bundled SDF font for a role, falling back gracefully.</summary>
        public TMP_FontAsset TmpFontForRole(TextRole role)
        {
            if (role == TextRole.Body)
            {
                return tmpBodyFont != null ? tmpBodyFont : tmpDisplayFont;
            }

            return tmpDisplayFont != null ? tmpDisplayFont : tmpBodyFont;
        }

        private void ApplyTmpTextStyle(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            var font = TmpFontForRole(ClassifyRole(text, displaySizeThreshold));
            if (font != null)
            {
                text.font = font;
            }

            // An authored vertex gradient (the gilded title) is a deliberate
            // treatment; flattening it to a single colour here would undo it the
            // moment the scene wakes.
            if (!text.enableVertexGradient)
            {
                if (text.GetComponent<TarotUiAccentText>() != null)
                {
                    text.color = accentGoldColor;
                }
                else
                {
                    text.color = text.fontSize <= 16f ? mutedTextColor : textColor;
                }
            }

            text.enableWordWrapping = true;
        }

        private void ApplyTextStyle(Text text)
        {
            if (text == null)
            {
                return;
            }

            var font = FontForRole(ClassifyRole(text, displaySizeThreshold));
            if (font != null)
            {
                text.font = font;
            }

            if (text.GetComponent<TarotUiAccentText>() != null)
            {
                text.color = accentGoldColor;
            }
            else
            {
                text.color = text.fontSize <= 16 ? mutedTextColor : textColor;
            }

            text.lineSpacing = Mathf.Max(text.lineSpacing, bodyLineSpacing);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void ApplyButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = accentGoldColor;
            colors.selectedColor = buttonHighlightColor;
            colors.disabledColor = new Color(0.10f, 0.11f, 0.10f, 0.52f);
            button.colors = colors;

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = buttonColor;
            }
        }

        private void ApplyInputStyle(InputField input)
        {
            if (input == null)
            {
                return;
            }

            if (input.targetGraphic != null)
            {
                input.targetGraphic.color = inputColor;
            }

            if (input.textComponent != null)
            {
                if (bodyFont != null)
                {
                    input.textComponent.font = bodyFont;
                }

                input.textComponent.color = textColor;
                input.textComponent.lineSpacing = bodyLineSpacing;
            }

            if (input.placeholder is Text placeholder)
            {
                if (bodyFont != null)
                {
                    placeholder.font = bodyFont;
                }

                placeholder.color = new Color(mutedTextColor.r, mutedTextColor.g, mutedTextColor.b, 0.72f);
                placeholder.lineSpacing = bodyLineSpacing;
            }
        }
    }
}
