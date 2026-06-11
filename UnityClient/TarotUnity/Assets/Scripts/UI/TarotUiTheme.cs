using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class TarotUiTheme : MonoBehaviour
    {
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

        public string ThemeName => themeName;
        public Color AccentGoldColor => accentGoldColor;
        public Color TableGreenColor => tableGreenColor;
        public Color PanelIvoryColor => panelIvoryColor;

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

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                ApplyButtonStyle(button);
            }

            foreach (var input in GetComponentsInChildren<InputField>(true))
            {
                ApplyInputStyle(input);
            }
        }

        private void ApplyTextStyle(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.color = text.fontSize <= 16 ? mutedTextColor : textColor;
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
                input.textComponent.color = textColor;
                input.textComponent.lineSpacing = bodyLineSpacing;
            }

            if (input.placeholder is Text placeholder)
            {
                placeholder.color = new Color(mutedTextColor.r, mutedTextColor.g, mutedTextColor.b, 0.72f);
                placeholder.lineSpacing = bodyLineSpacing;
            }
        }
    }
}
