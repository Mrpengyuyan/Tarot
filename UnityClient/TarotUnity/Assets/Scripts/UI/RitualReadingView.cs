using System.Collections.Generic;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>Presentation only: the existing room still owns draws, network calls and results.</summary>
    [DisallowMultipleComponent]
    public sealed class RitualReadingView : MonoBehaviour
    {
        public enum ViewStage { Spread, Question, Shuffle, Pick, Reveal, Ready }
        public ViewStage Stage { get; private set; }

        private ReadingFlowController flow;
        private TMP_InputField question;
        private Button draw;
        private Button reveal;
        private Button[] spreads;
        private RectTransform canvas;
        private Button changeSpread;
        private Button back;
        private TMP_Text spreadCaption;
        private TMP_Text questionLabel;
        private TMP_Text feedback;
        private TMP_Text progress;
        private TMP_Text oldStatus;
        private DeckController deck;
        private SpreadSocketVisibility sockets;
        private bool choosingSpread = true;
        private bool initialized;
        private Vector2 lastSize;
        private readonly Dictionary<Transform, CanvasGroup> groups = new();
        private readonly List<Transform> retired = new();

        public static RitualReadingView Attach(ReadingRoomController room, ReadingFlowController flow,
            TMP_InputField question, Button draw, Button reveal, Button[] spreads)
        {
            var host = question != null ? question.GetComponentInParent<Canvas>() : null;
            if (host == null) return null;
            var view = host.GetComponent<RitualReadingView>();
            if (view != null) return view;
            view = host.gameObject.AddComponent<RitualReadingView>();
            view.flow = flow;
            view.question = question;
            view.draw = draw;
            view.reveal = reveal;
            view.spreads = spreads;
            view.canvas = (RectTransform)host.transform;
            view.Build();
            return view;
        }

        private void Build()
        {
            // Keep serialized controls and their listeners, but retire the old, always-visible dock.
            foreach (Transform child in canvas)
            {
                Show(child, false);
                if (child != question.transform && child != draw.transform && child != reveal.transform
                    && System.Array.Find(spreads, button => button != null && button.transform == child) == null)
                    retired.Add(child);
            }
            oldStatus = canvas.Find("FlowStatusText")?.GetComponent<TMP_Text>();
            deck = FindFirstObjectByType<DeckController>();
            sockets = FindFirstObjectByType<SpreadSocketVisibility>();
            if (sockets != null) sockets.Apply(0);
            spreadCaption = Text("RitualSpreadCaption", "选择牌阵");
            questionLabel = Text("RitualQuestionLabel", "此刻，你想问什么？");
            feedback = Text("RitualQuestionFeedback", "");
            progress = Text("RitualProgress", "");
            changeSpread = Action("RitualChangeSpread", "更换牌阵", false);
            changeSpread.onClick.AddListener(ChangeSpread);
            back = Action("RitualReturnMenu", "返回", false);
            back.onClick.AddListener(ReturnToMenu);

            for (var i = 0; i < spreads.Length; i++)
            {
                var button = spreads[i];
                if (button == null) continue;
                StyleAction(button, false);
                button.onClick.AddListener(ChooseSpread);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.text = i == 0 ? "一张牌" : i == 1 ? "三张牌" : "凯尔特十字";
                var subtitle = Text("RitualSpreadSubtitle", i == 0 ? "洞察此刻" : i == 1 ? "过去·现在·未来" : "洞察全貌", button.transform);
                subtitle.textWrappingMode = TextWrappingModes.NoWrap;
                BuildSpreadSymbol(button.transform, i == 0 ? 1 : i == 1 ? 3 : 4);
            }
            StyleAction(draw, true);
            StyleAction(reveal, true);
            if (draw != null) draw.GetComponentInChildren<TMP_Text>(true).text = "开始洗牌";
            if (reveal != null) reveal.GetComponentInChildren<TMP_Text>(true).text = "揭示结果";

            var ground = question.targetGraphic;
            if (ground != null) ground.color = Color.clear;
            if (question.GetComponent<TarotUiPreserveColor>() == null) question.gameObject.AddComponent<TarotUiPreserveColor>();
            question.lineType = TMP_InputField.LineType.SingleLine;
            question.textComponent.alignment = TextAlignmentOptions.Center;
            question.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            if (question.placeholder is TMP_Text placeholder)
            {
                placeholder.text = ReleaseUxCopy.DefaultQuestion;
                placeholder.alignment = TextAlignmentOptions.Center;
                placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            }
            if (question.textViewport != null) Stretch(question.textViewport, 0f);
            Stretch(question.textComponent.rectTransform, 0f);
            if (question.placeholder is TMP_Text hint) Stretch(hint.rectTransform, 0f);
            foreach (var graphic in question.GetComponentsInChildren<Image>(true))
            {
                if (graphic != ground) graphic.enabled = false;
            }
            Rule(question.transform, "RitualInputUnderline", 0f);
            question.onSubmit.AddListener(SubmitQuestion);
            RitualTypography.ApplyHierarchy(canvas);
            RefreshLayout();
            initialized = true;
            Refresh();
        }

        private void ChooseSpread()
        {
            choosingSpread = false;
            if (sockets != null) sockets.Apply(0);
            Refresh();
        }

        private void ChangeSpread()
        {
            choosingSpread = true;
            Refresh();
        }

        private void ReturnToMenu() => SceneManager.LoadScene("MainMenu");

        private void SubmitQuestion(string value)
        {
            if (Stage == ViewStage.Question && !string.IsNullOrWhiteSpace(value) && draw != null && draw.interactable)
                draw.onClick.Invoke();
        }

        private void LateUpdate()
        {
            if (canvas == null || !initialized) return;
            if (lastSize != canvas.rect.size) RefreshLayout();
            Refresh();
        }

        private void Refresh()
        {
            if (flow == null) return;
            foreach (var node in retired) if (node != null) Show(node, false);
            switch (flow.State)
            {
                case ReadingFlowState.Shuffling: Stage = ViewStage.Shuffle; break;
                case ReadingFlowState.Drawing: Stage = ViewStage.Pick; break;
                case ReadingFlowState.WaitingForFlip: Stage = ViewStage.Reveal; break;
                case ReadingFlowState.ResultReady: Stage = ViewStage.Ready; break;
                default: Stage = choosingSpread ? ViewStage.Spread : ViewStage.Question; break;
            }
            foreach (var button in spreads) if (button != null) Show(button.transform, Stage == ViewStage.Spread);
            Show(spreadCaption.transform, Stage == ViewStage.Spread);
            Show(back.transform, Stage == ViewStage.Spread);
            Show(questionLabel.transform, Stage == ViewStage.Question);
            Show(question.transform, Stage == ViewStage.Question);
            Show(changeSpread.transform, Stage == ViewStage.Question);
            Show(draw.transform, Stage == ViewStage.Question);
            Show(reveal.transform, Stage == ViewStage.Ready);
            question.interactable = Stage == ViewStage.Question;
            draw.interactable = Stage == ViewStage.Question && !string.IsNullOrWhiteSpace(question.text);
            if (Stage == ViewStage.Question && !draw.interactable) groups[draw.transform].alpha = 0.35f;

            var message = oldStatus != null ? oldStatus.text : "";
            var error = Stage == ViewStage.Question && ((message.Contains("先输入") && string.IsNullOrWhiteSpace(question.text)) || message.Contains("中断") || message.Contains("打断")
                || message.Contains("失败") || message.Contains("无法") || message.Contains("不可用"));
            feedback.text = error ? message : "";
            Show(feedback.transform, error);
            var total = flow.SelectedSpreadCardCount;
            var picked = deck != null ? deck.ActiveCards.Count : 0;
            var flipped = 0;
            if (deck != null) foreach (var card in deck.ActiveCards) if (card != null && card.IsFaceUp) flipped++;
            progress.text = Stage == ViewStage.Pick && picked < total ? $"还可选 {total - picked} 张"
                : Stage == ViewStage.Reveal ? $"逐张揭开 {flipped} / {total}" : "";
            Show(progress.transform, progress.text.Length > 0);
        }

        public void RefreshLayout()
        {
            if (canvas == null) return;
            lastSize = canvas.rect.size;
            var width = lastSize.x;
            var height = lastSize.y;
            var scale = width / 1280f;
            if (width <= 2f || height <= 2f) return;
            foreach (var stage in new[] { "spread", "question" })
            {
                foreach (var entry in RitualStaticLayout.Compute(stage, width, height))
                {
                    var key = entry.Key;
                    var node = key.EndsWith("/Title")
                        ? canvas.Find(key.Substring(0, key.Length - 6)).GetComponentInChildren<TMP_Text>(true).rectTransform
                        : canvas.Find(key) as RectTransform;
                    if (node == null) continue;
                    RitualStaticLayout.Apply(node, entry.Value, canvas);
                    if (entry.Value.FontSize <= 0f) continue;
                    if (node == question.transform)
                    {
                        ApplyText(question.textComponent, entry.Value, new Color(242f/255f, 223f/255f, 190f/255f));
                        if (question.placeholder is TMP_Text hint) ApplyText(hint, entry.Value, new Color(203f/255f, 182f/255f, 150f/255f, 0.85f));
                        continue;
                    }
                    var text = node.GetComponent<TMP_Text>() ?? node.GetComponentInChildren<TMP_Text>(true);
                    var ink = key.EndsWith("/Title") ? new Color(228f/255f, 204f/255f, 164f/255f)
                        : key.EndsWith("RitualSpreadSubtitle") ? new Color(173f/255f, 150f/255f, 116f/255f)
                        : key == "RitualQuestionLabel" ? new Color(234f/255f, 214f/255f, 179f/255f)
                        : key == "RitualSpreadCaption" ? new Color(179f/255f, 157f/255f, 125f/255f)
                        : key == "RitualReturnMenu" || key == "RitualChangeSpread" ? new Color(187f/255f, 164f/255f, 132f/255f)
                        : new Color(240f/255f, 214f/255f, 165f/255f);
                    ApplyText(text, entry.Value, ink);
                }
            }
            Place((RectTransform)reveal.transform, 0.5f, 0.10f, width * 0.22f, 72f * scale);
            var revealLabel = reveal.GetComponentInChildren<TMP_Text>(true);
            revealLabel.fontSize = 27f * scale;
            Stretch(revealLabel.rectTransform, 0f);
            Place(progress.rectTransform, 0.5f, 0.075f, width * 0.6f, 40f * scale);
            progress.fontSize = 20f * scale;
        }

        private static void ApplyText(TMP_Text text, RitualStaticElement element, Color ink)
        {
            if (text == null) return;
            text.fontSize = element.FontSize;
            text.characterSpacing = element.Tracking * 100f;
            text.margin = Vector4.zero;
            text.color = ink;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (text.GetComponent<TarotUiPreserveColor>() == null) text.gameObject.AddComponent<TarotUiPreserveColor>();
        }

        private void Show(Transform node, bool visible)
        {
            if (node == null) return;
            if (!groups.TryGetValue(node, out var group) || group == null)
            {
                group = node.GetComponent<CanvasGroup>();
                if (group == null) group = node.gameObject.AddComponent<CanvasGroup>();
                groups[node] = group;
            }
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private TMP_Text Text(string name, string value, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent != null ? parent : canvas, false);
            var label = go.GetComponent<TMP_Text>();
            label.font = question.textComponent.font;
            label.text = value;
            label.color = new Color(0.91f, 0.82f, 0.66f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            go.AddComponent<TarotUiPreserveColor>();
            return label;
        }

        private Button Action(string name, string value, bool rules)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var label = Text("Label", value, go.transform);
            Stretch(label.rectTransform, 0f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            StyleAction(button, rules);
            return button;
        }

        private static void StyleAction(Button button, bool rules)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var skin = button.GetComponent<UiSkinState>();
            if (skin == null) skin = button.gameObject.AddComponent<UiSkinState>();
            skin.Configure(null, label, null, null, false);
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                if (image == button.targetGraphic)
                {
                    image.enabled = true;
                    image.sprite = null;
                    image.color = Color.clear;
                    image.raycastTarget = true;
                }
                else image.enabled = false;
            }
            foreach (var effect in button.GetComponentsInChildren<Shadow>(true)) effect.enabled = false;
            label.color = new Color(0.94f, 0.84f, 0.65f);
            label.fontStyle = FontStyles.Normal;
            label.enableAutoSizing = false;
            label.characterSpacing = 8f;
            label.raycastTarget = false;
            button.transition = Selectable.Transition.None;
            if (rules)
            {
                Rule(button.transform, "RitualRuleTop", 1f);
                Rule(button.transform, "RitualRuleBottom", 0f);
            }
        }

        private static void Rule(Transform parent, string name, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RitualOrnamentGraphic));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.72f, 0.55f, 0.30f, 0.55f);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(name == "RitualInputUnderline" ? 0f : 0.05f, y);
            rect.anchorMax = new Vector2(name == "RitualInputUnderline" ? 1f : 0.95f, y);
            rect.sizeDelta = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void BuildSpreadSymbol(Transform button, int count)
        {
            var root = new GameObject("RitualSpreadSymbol", typeof(RectTransform));
            root.transform.SetParent(button, false);
            var artwork = DeckBackArtwork();
            for (var i = 0; i < count; i++)
            {
                var card = new GameObject("Back_" + i, typeof(RectTransform), typeof(RawImage), typeof(Outline));
                card.transform.SetParent(root.transform, false);
                var image = card.GetComponent<RawImage>();
                image.texture = artwork;
                image.color = artwork != null ? Color.white : new Color(0.055f, 0.035f, 0.035f, 0.97f);
                image.raycastTarget = false;
                var outline = card.GetComponent<Outline>();
                outline.effectColor = new Color(0.68f, 0.48f, 0.23f, 0.9f);
                outline.effectDistance = Vector2.one;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(66f, 112f);
                var offset = count == 1 ? 0f : i == 0 ? -32f : i == 1 ? 32f : 0f;
                rect.anchoredPosition = new Vector2(offset, 0f);
                rect.localRotation = Quaternion.Euler(0f, 0f, count == 1 ? 0f : i == 0 ? 15f : i == 1 ? -15f : i == 3 ? 90f : 0f);
            }
        }

        private static Texture DeckBackArtwork()
        {
            var stack = FindFirstObjectByType<DeckShuffleChoreographer>();
            if (stack == null) return null;
            foreach (var renderer in stack.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material != null && material.mainTexture != null && material.mainTexture.name == "TarotCardBack")
                    return material.mainTexture;
            }
            return null;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void OnDestroy()
        {
            if (question != null) question.onSubmit.RemoveListener(SubmitQuestion);
            if (changeSpread != null) changeSpread.onClick.RemoveListener(ChangeSpread);
            if (back != null) back.onClick.RemoveListener(ReturnToMenu);
            if (spreads != null) foreach (var button in spreads) if (button != null) button.onClick.RemoveListener(ChooseSpread);
        }
    }
}
