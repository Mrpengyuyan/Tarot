using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class RitualMenuView : MonoBehaviour
    {
        private static readonly Color Gold = new(240f/255f, 214f/255f, 165f/255f, 1f);
        private static readonly Color QuietGold = new(194f/255f, 173f/255f, 138f/255f, 1f);
        private readonly List<GameObject> retired = new();
        private RectTransform canvas;
        private TMP_Text title;
        private Button start;
        private Button quit;
        private TMP_Text startLabel;
        private TMP_Text quitLabel;
        private RectTransform titleRule;
        private RectTransform seal;
        private TMP_Text caption;
        private Vector2 lastSize;

        public static RitualMenuView Attach(GameObject host)
        {
            if (host == null || host.transform is not RectTransform) return null;
            var view = host.GetComponent<RitualMenuView>();
            if (view == null)
            {
                view = host.AddComponent<RitualMenuView>();
                view.Build();
            }
            view.RefreshLayout();
            view.RestoreInk();
            return view;
        }

        private void Build()
        {
            canvas = (RectTransform)transform;
            title = Find<TMP_Text>("TitleText");
            start = Find<Button>("StartReadingButton");
            quit = Find<Button>("QuitButton");
            foreach (var node in GetComponentsInChildren<Transform>(true))
            {
                switch (node.name)
                {
                    case "SubtitleText":
                    case "StatusText":
                    case "Phase8_MenuCrest":
                    case "Phase8_TitleConstellation":
                    case "Phase11_MenuDepthFrame":
                    case "Phase11_ActionRail":
                    case "Phase8_StartButtonGoldTrim":
                    case "MP_TitleRule":
                        retired.Add(node.gameObject);
                        node.gameObject.SetActive(false);
                        break;
                }
            }
            // Keep only the original button hit targets; neither title ornament nor old framing can cover them.
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            startLabel = PrepareAction(start, true);
            quitLabel = PrepareAction(quit, false);
            titleRule = Rule(transform, "RitualTitleRule", 0.5f);
            titleRule.GetComponent<RitualOrnamentGraphic>().CenterMark = true;
            seal = Rule(transform, "RitualCoverSeal", 0.5f);
            var ornament = seal.GetComponent<RitualOrnamentGraphic>();
            ornament.Seal = true;
            ornament.color = new Color(208f/255f, 180f/255f, 124f/255f, 0.75f);
            var captionNode = new GameObject("RitualCoverCaption", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionNode.transform.SetParent(canvas, false);
            caption = captionNode.GetComponent<TMP_Text>();
            caption.text = "T A R O T \u00b7 R I T U A L";
            caption.alignment = TextAlignmentOptions.Center;
            caption.color = new Color(158f/255f, 139f/255f, 108f/255f, 0.6f);
            caption.raycastTarget = false;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            captionNode.AddComponent<TarotUiPreserveColor>();
            if (title != null)
            {
                title.text = "\u5854\u7f57\u4eea\u5f0f";
                title.enableAutoSizing = false;
                title.alignment = TextAlignmentOptions.Center;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Overflow;
                title.margin = Vector4.zero;
                title.characterSpacing = 11f;
                title.fontStyle = FontStyles.Normal;
                title.fontWeight = FontWeight.Regular;
                title.color = Color.white;
                title.enableVertexGradient = true;
                title.colorGradient = new VertexGradient(new Color(1f, 0.94f, 0.81f),
                    new Color(0.89f, 0.77f, 0.55f), new Color(0.73f, 0.55f, 0.30f),
                    new Color(0.96f, 0.85f, 0.65f));
            }
            RitualTypography.ApplyHierarchy(transform);
            GetComponent<RitualTypography>().UseBrushTitle(title);
            if (Application.isPlaying) RitualCoverFinish.Attach(gameObject,title);
        }

        private TMP_Text PrepareAction(Button button, bool rules)
        {
            if (button == null) return null;
            var skin = button.GetComponent<UiSkinState>();
            if (skin != null) skin.enabled = false;
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic is Image plate)
            {
                plate.sprite = null;
                plate.type = Image.Type.Simple;
                plate.color = Color.clear;
                plate.enabled = true;
                plate.raycastTarget = true;
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = false;
                label.characterSpacing = rules ? 20f : 22f;
                label.fontStyle = FontStyles.Normal;
                label.fontWeight = FontWeight.Regular;
                label.margin = Vector4.zero;
                if (label.GetComponent<TarotUiPreserveColor>() == null)
                    label.gameObject.AddComponent<TarotUiPreserveColor>();
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            if (rules)
            {
                Rule(button.transform, "RitualActionRuleTop", 1f);
                Rule(button.transform, "RitualActionRuleBottom", 0f);
            }
            return label;
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            if (canvas.rect.size != lastSize) RefreshLayout();
            RestoreInk();
        }

        private void RefreshLayout()
        {
            if (canvas == null) return;
            lastSize = canvas.rect.size;
            if (lastSize.x <= 2f || lastSize.y <= 2f) return;
            var frame = RitualStaticLayout.Compute("cover", lastSize.x, lastSize.y);
            foreach (var entry in frame)
            {
                var node = transform.Find(entry.Key) as RectTransform;
                if (node == null) continue;
                RitualStaticLayout.Apply(node, entry.Value, canvas);
                var text = node.GetComponent<TMP_Text>() ?? node.GetComponentInChildren<TMP_Text>(true);
                if (text == null || entry.Value.FontSize <= 0f) continue;
                text.fontSize = entry.Value.FontSize;
                text.characterSpacing = entry.Value.Tracking * 100f;
            }
        }

        private void RestoreInk()
        {
            foreach (var node in retired)
                if (node != null && node.activeSelf) node.SetActive(false);
            RestoreAction(start, startLabel, Gold);
            RestoreAction(quit, quitLabel, QuietGold);
        }

        private static void RestoreAction(Button button, TMP_Text label, Color ink)
        {
            if (button == null) return;
            if (button.targetGraphic is Image plate)
            {
                if (plate.sprite != null) plate.sprite = null;
                if (plate.color != Color.clear) plate.color = Color.clear;
                if (!plate.enabled) plate.enabled = true;
                if (!plate.raycastTarget) plate.raycastTarget = true;
            }
            if (label != null && label.color != ink) label.color = ink;
        }

        private static RectTransform Rule(Transform parent, string name, float y)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(RitualOrnamentGraphic));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = new Vector2(0.05f, y);
            rect.anchorMax = new Vector2(0.95f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 1f);
            var image = node.GetComponent<Image>();
            image.color = new Color(0.78f, 0.65f, 0.42f, 0.65f);
            image.raycastTarget = false;
            return rect;
        }

        private T Find<T>(string name) where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true))
                if (component.name == name) return component;
            return null;
        }
    }
}
