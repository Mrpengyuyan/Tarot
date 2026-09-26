using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: one element of the glass-and-card-stock UI. Emphasised (the current step,
    /// the chosen spread, the next action) it wears ivory card stock with dark ink; otherwise
    /// smoked glass with ivory ink, or no plate at all when it has no glass sprite. Buttons
    /// also get a white-based ColorBlock so the sprite colours survive, and TarotUiTheme leaves
    /// anything carrying this component to it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSkinState : MonoBehaviour
    {
        public static readonly Color GlassLabel = new Color(0.961f, 0.910f, 0.800f, 0.85f);   // #f5e8cc
        public static readonly Color CardStockLabel = new Color(0.165f, 0.114f, 0.090f, 1f);  // #2a1d17

        public static ColorBlock SkinColors => new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 0.96f, 0.86f, 1f),
            pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(1f, 1f, 1f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };

        [SerializeField] private Image plate;
        [SerializeField] private Graphic label;
        [SerializeField] private Sprite glass;
        [SerializeField] private Sprite cardStock;
        [SerializeField] private bool driveLabelColor = true;
        [SerializeField] private bool emphasized;

        public bool IsEmphasized => emphasized;
        public Image Plate => plate;
        public Graphic Label => label;
        public Sprite Glass => glass;
        public Sprite CardStock => cardStock;

        public void Configure(Image plate, Graphic label, Sprite glass, Sprite cardStock, bool driveLabelColor)
        {
            this.plate = plate;
            this.label = label;
            this.glass = glass;
            this.cardStock = cardStock;
            this.driveLabelColor = driveLabelColor;
            Apply();
        }

        public void SetEmphasis(bool on)
        {
            emphasized = on;
            Apply();
        }

        // Start runs after every Awake, so this also settles anything set before the scene woke.
        private void Start()
        {
            Apply();
        }

        private void Apply()
        {
            if (plate != null)
            {
                var sprite = emphasized ? cardStock : glass;
                plate.sprite = sprite;
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 1f;
                plate.enabled = sprite != null;
                plate.color = sprite != null ? Color.white : Color.clear;
            }

            if (driveLabelColor && label != null)
            {
                label.color = emphasized ? CardStockLabel : GlassLabel;
            }

            var button = GetComponent<Button>();
            if (button != null)
            {
                button.colors = SkinColors;
            }
        }
    }
}
