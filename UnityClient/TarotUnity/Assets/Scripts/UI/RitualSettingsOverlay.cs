using TarotUnity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>Minimalist gold-hairline settings drawer for desktop audio, display, and shuffle speed.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1050)]
    public sealed class RitualSettingsOverlay : MonoBehaviour
    {
        private static readonly Color Gold = new(240f / 255f, 214f / 255f, 165f / 255f, 1f);
        private static readonly Color QuietGold = new(194f / 255f, 173f / 255f, 138f / 255f, 1f);
        private static readonly Color Hairline = new(208f / 255f, 180f / 255f, 124f / 255f, 0.55f);
        private static readonly Color BackdropColor = new(0.03f, 0.025f, 0.04f, 0.72f);
        private static readonly Color PanelColor = new(0.06f, 0.05f, 0.08f, 0.96f);

        private RectTransform canvas;
        private Button triggerButton;
        private CanvasGroup triggerGroup;
        private GameObject modalRoot;
        private Slider musicSlider;
        private Slider sfxSlider;
        private TMP_Text musicValueLabel;
        private TMP_Text sfxValueLabel;
        private TMP_Text muteValueLabel;
        private TMP_Text fastShuffleValueLabel;
        private TMP_Text fullScreenValueLabel;
        private bool triggerAllowed = true;
        private bool escapeHeld;

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
        public Button TriggerButton => triggerButton;

        public static RitualSettingsOverlay Attach(GameObject host)
        {
            if (host == null || host.transform is not RectTransform) return null;
            var existing = host.GetComponent<RitualSettingsOverlay>();
            if (existing != null) return existing;
            var owner = host.AddComponent<RitualSettingsOverlay>();
            owner.Build();
            return owner;
        }

        public void SetTriggerVisible(bool visible)
        {
            triggerAllowed = visible;
            if (!visible && IsOpen) Close();
            if (triggerGroup == null) return;
            triggerGroup.alpha = visible ? 1f : 0f;
            triggerGroup.interactable = visible;
            triggerGroup.blocksRaycasts = visible;
        }

        public void Open()
        {
            if (modalRoot == null || !triggerAllowed) return;
            SyncControlsFromPreferences();
            modalRoot.transform.SetAsLastSibling();
            modalRoot.SetActive(true);
        }

        public void Close()
        {
            if (modalRoot != null && modalRoot.activeSelf)
            {
                modalRoot.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Build()
        {
            canvas = (RectTransform)transform;
            triggerButton = CreateTrigger();
            triggerGroup = triggerButton.gameObject.AddComponent<CanvasGroup>();
            modalRoot = CreateModal();
            modalRoot.SetActive(false);
            UserPreferences.Changed += SyncControlsFromPreferences;
            SyncControlsFromPreferences();
            RitualTypography.ApplyHierarchy(triggerButton.transform);
            RitualTypography.ApplyHierarchy(modalRoot.transform);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                escapeHeld = false;
                return;
            }

            var pressed = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            if (pressed && !escapeHeld)
            {
                Close();
            }

            escapeHeld = pressed;
        }

        private void OnDestroy()
        {
            UserPreferences.Changed -= SyncControlsFromPreferences;
            if (triggerButton != null) Destroy(triggerButton.gameObject);
            if (modalRoot != null) Destroy(modalRoot);
        }

        private Button CreateTrigger()
        {
            var go = new GameObject("RitualSettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-36f, -24f);
            rect.sizeDelta = new Vector2(88f, 38f);

            var plate = go.GetComponent<Image>();
            plate.color = Color.clear;
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Toggle);

            var label = CreateText(go.transform, "Label", "\u8bbe\u7f6e", 16f, QuietGold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.characterSpacing = 12f;

            if (Application.isPlaying)
            {
                RitualControlFeedback.Attach(button, RitualControlFeedback.Kind.Quiet);
            }

            return button;
        }

        private GameObject CreateModal()
        {
            var root = new GameObject("RitualSettingsModal", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas, false);
            Stretch((RectTransform)root.transform);

            var backdrop = root.GetComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            var backdropButton = root.GetComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };
            backdropButton.onClick.AddListener(Close);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 388f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;

            CreateRule(panel.transform, "TopBorder", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 1f));
            CreateRule(panel.transform, "BottomBorder", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f));

            var heading = CreateText(panel.transform, "Heading", "\u504f\u597d\u8bbe\u7f6e", 24f, Gold, TextAlignmentOptions.Center);
            PlaceTop(heading.rectTransform, 22f, 400f, 34f);
            heading.characterSpacing = 10f;

            CreateRule(panel.transform, "HeadingDivider", new Vector2(0.12f, 1f), new Vector2(0.88f, 1f), new Vector2(0f, -66f), new Vector2(0f, 1f));

            musicSlider = CreateSliderRow(panel.transform, "MusicRow", "\u80cc\u666f\u97f3\u4e50", 92f, out musicValueLabel,
                v => UserPreferences.MusicVolume = v);
            sfxSlider = CreateSliderRow(panel.transform, "SfxRow", "\u4eea\u5f0f\u97f3\u6548", 142f, out sfxValueLabel,
                v => UserPreferences.SfxVolume = v);

            muteValueLabel = CreateToggleRow(panel.transform, "MuteRow", "\u5168\u90e8\u9759\u97f3", 196f,
                () => UserPreferences.Muted = !UserPreferences.Muted);
            fastShuffleValueLabel = CreateToggleRow(panel.transform, "FastShuffleRow", "\u5feb\u901f\u6d17\u724c (2.5x)", 244f,
                () => UserPreferences.FastShuffle = !UserPreferences.FastShuffle);
            fullScreenValueLabel = CreateToggleRow(panel.transform, "FullScreenRow", "\u663e\u793a\u6a21\u5f0f", 292f,
                () => UserPreferences.FullScreen = !UserPreferences.FullScreen);

            var closeButton = CreateActionButton(panel.transform, "CloseSettingsButton", "\u5173\u95ed", 338f, Close);
            if (Application.isPlaying)
            {
                RitualControlFeedback.Attach(closeButton, RitualControlFeedback.Kind.Primary);
            }

            return root;
        }

        private Slider CreateSliderRow(Transform parent, string name, string title, float topOffset,
            out TMP_Text valueLabel, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            PlaceTop((RectTransform)row.transform, topOffset, 392f, 36f);

            var label = CreateText(row.transform, "Label", title, 16f, QuietGold, TextAlignmentOptions.Left);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.32f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.anchorMin = new Vector2(0.34f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.82f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(0f, 20f);
            sliderRect.anchoredPosition = Vector2.zero;

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(sliderGo.transform, false);
            var trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.sizeDelta = new Vector2(0f, 2f);
            track.GetComponent<Image>().color = new Color(Hairline.r, Hairline.g, Hairline.b, 0.35f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch((RectTransform)fillArea.transform);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 2f);
            fill.GetComponent<Image>().color = Gold;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch((RectTransform)handleArea.transform);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(12f, 12f);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = Gold;

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(onChanged);

            valueLabel = CreateText(row.transform, "Value", "100%", 15f, Gold, TextAlignmentOptions.Right);
            var valueRect = valueLabel.rectTransform;
            valueRect.anchorMin = new Vector2(0.84f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;
            return slider;
        }

        private TMP_Text CreateToggleRow(Transform parent, string name, string title, float topOffset,
            UnityEngine.Events.UnityAction onClick)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            PlaceTop((RectTransform)row.transform, topOffset, 392f, 36f);

            var label = CreateText(row.transform, "Label", title, 16f, QuietGold, TextAlignmentOptions.Left);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.68f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var buttonGo = new GameObject("ToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(row.transform, false);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = new Vector2(0.70f, 0.1f);
            buttonRect.anchorMax = new Vector2(1f, 0.9f);
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;

            var plate = buttonGo.GetComponent<Image>();
            plate.color = new Color(Hairline.r, Hairline.g, Hairline.b, 0.12f);
            plate.raycastTarget = true;

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);

            var valueLabel = CreateText(buttonGo.transform, "Value", "", 15f, Gold, TextAlignmentOptions.Center);
            Stretch(valueLabel.rectTransform);
            return valueLabel;
        }

        private Button CreateActionButton(Transform parent, string name, string text, float topOffset,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            PlaceTop((RectTransform)go.transform, topOffset, 150f, 38f);

            var plate = go.GetComponent<Image>();
            plate.color = Color.clear;
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);

            var label = CreateText(go.transform, "Label", text, 17f, Gold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.characterSpacing = 14f;

            CreateRule(go.transform, "RitualRuleTop", new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), Vector2.zero, new Vector2(0f, 1f));
            CreateRule(go.transform, "RitualRuleBottom", new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), Vector2.zero, new Vector2(0f, 1f));
            return button;
        }

        private void SyncControlsFromPreferences()
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(UserPreferences.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(UserPreferences.SfxVolume);
            if (musicValueLabel != null) musicValueLabel.text = $"{Mathf.RoundToInt(UserPreferences.MusicVolume * 100f)}%";
            if (sfxValueLabel != null) sfxValueLabel.text = $"{Mathf.RoundToInt(UserPreferences.SfxVolume * 100f)}%";
            if (muteValueLabel != null) muteValueLabel.text = UserPreferences.Muted ? "\u5df2\u9759\u97f3" : "\u5f00\u542f";
            if (fastShuffleValueLabel != null) fastShuffleValueLabel.text = UserPreferences.FastShuffle ? "\u5f00\u542f" : "\u5173\u95ed";
            if (fullScreenValueLabel != null) fullScreenValueLabel.text = UserPreferences.FullScreen ? "\u5168\u5c4f" : "\u7a97\u53e3";
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            go.AddComponent<TarotUiPreserveColor>();
            return text;
        }

        private static void CreateRule(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = Hairline;
            image.raycastTarget = false;
        }

        private static void PlaceTop(RectTransform rect, float topOffset, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
