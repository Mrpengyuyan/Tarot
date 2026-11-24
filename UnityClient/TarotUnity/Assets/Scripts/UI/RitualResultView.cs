using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    public sealed class RitualResultView : MonoBehaviour
    {
        private static readonly Color Gold = new(0.94f, 0.84f, 0.65f, 1f);
        private static readonly Color QuietGold = new(0.76f, 0.68f, 0.54f, 1f);
        private const string ReadAgainLabelText = "\u518d\u5360\u4e00\u5c40";
        private const string CopyReadingLabelText = "\u590d\u5236\u89e3\u8bfb";
        private const string CopiedLabelText = "\u5df2\u590d\u5236";

        public Button ReadAgainButton { get; private set; }
        public Button CopyReadingButton { get; private set; }

        private TMP_Text copyLabel;
        private Coroutine copiedFeedbackRoutine;
        private RectTransform pendingSeal;
        private Graphic pendingSealGraphic;
        private TMP_Text statusText;
        private CanvasGroup readingViewportGroup;

        public static RitualResultView Attach(GameObject host)
        {
            if (host == null || host.transform is not RectTransform) return null;
            var view = host.GetComponent<RitualResultView>();
            if (view == null)
            {
                view = host.AddComponent<RitualResultView>();
                view.Build();
            }
            RitualTypography.ApplyHierarchy(host.transform);
            return view;
        }

        public void SetCopyAvailable(bool available)
        {
            if (CopyReadingButton == null) return;
            CopyReadingButton.gameObject.SetActive(available);
            if (!available && copyLabel != null)
            {
                copyLabel.text = CopyReadingLabelText;
            }
        }

        public void ShowCopiedFeedback()
        {
            if (copyLabel == null) return;
            if (copiedFeedbackRoutine != null)
            {
                StopCoroutine(copiedFeedbackRoutine);
                copiedFeedbackRoutine = null;
            }

            copyLabel.text = CopiedLabelText;
            if (isActiveAndEnabled)
            {
                copiedFeedbackRoutine = StartCoroutine(ResetCopiedLabelAfterDelay());
            }
        }

        private IEnumerator ResetCopiedLabelAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (copyLabel != null)
            {
                copyLabel.text = CopyReadingLabelText;
            }
            copiedFeedbackRoutine = null;
        }

        private void Update()
        {
            if (pendingSeal == null || pendingSealGraphic == null) return;

            var showPending = statusText != null
                && statusText.gameObject.activeInHierarchy
                && readingViewportGroup != null
                && readingViewportGroup.alpha < 0.05f
                && !string.IsNullOrEmpty(statusText.text)
                && statusText.text.Contains(ReleaseUxCopy.ResultPending);

            if (pendingSeal.gameObject.activeSelf != showPending)
            {
                pendingSeal.gameObject.SetActive(showPending);
            }

            if (!showPending) return;

            var t = Time.unscaledTime;
            var pulse = 0.45f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 2.4f));
            var c = pendingSealGraphic.color;
            c.a = pulse;
            pendingSealGraphic.color = c;
            var scale = 0.94f + 0.10f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 2.4f));
            pendingSeal.localScale = new Vector3(scale, scale, 1f);
        }

        private void Build()
        {
            // Keep the scroll hit area and its viewport mask; only the visible plate goes away.
            var reading = transform.Find("ResultReadingScroll");
            if (reading != null && reading.TryGetComponent<Image>(out var plate))
            {
                plate.sprite = null;
                plate.color = Color.clear;
                plate.enabled = true;
                plate.raycastTarget = true;
            }

            if (reading != null)
            {
                var viewport = reading.Find("Viewport");
                if (viewport != null)
                {
                    readingViewportGroup = viewport.GetComponent<CanvasGroup>();
                }
            }

            HideImage(transform.Find("Phase12_ResultCardShowcase"));
            var band = transform.Find("MP_ResultSpreadBand");
            if (band != null)
            {
                foreach (Transform cell in band) HideImage(cell.Find("ReversePivot/Frame"));
            }

            StyleAction("BackToMenuButton", Gold, true);
            StyleAction("Phase66_RetryInterpretationButton", Gold, true);
            StyleAction("Phase66_OfflineInterpretationButton", QuietGold, false);

            BuildSecondaryFooterActions();
            BuildPendingSeal();
        }

        private void BuildSecondaryFooterActions()
        {
            var backNode = transform.Find("BackToMenuButton") as RectTransform;
            if (backNode == null) return;

            var backLabel = backNode.GetComponentInChildren<TMP_Text>(true);
            var font = backLabel != null ? backLabel.font : null;
            var baseY = backNode.anchoredPosition.y;

            ReadAgainButton = CreateQuietFooterButton("RitualReadAgainButton", ReadAgainLabelText, new Vector2(-230f, baseY), font, out _);
            CopyReadingButton = CreateQuietFooterButton("RitualCopyReadingButton", CopyReadingLabelText, new Vector2(230f, baseY), font, out copyLabel);
        }

        private Button CreateQuietFooterButton(string name, string text, Vector2 anchoredPosition, TMP_FontAsset font, out TMP_Text label)
        {
            var existing = transform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(164f, 48f);

            var plate = go.GetComponent<Image>();
            plate.sprite = null;
            plate.type = Image.Type.Simple;
            plate.color = Color.clear;
            plate.enabled = true;
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;

            var labelNode = go.transform.Find("Label");
            if (labelNode == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                labelNode = labelGo.transform;
            }

            var labelRect = (RectTransform)labelNode;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelNode.GetComponent<TMP_Text>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = 18f;
            label.color = QuietGold;
            label.characterSpacing = 3f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
            if (label.GetComponent<TarotUiPreserveColor>() == null)
            {
                label.gameObject.AddComponent<TarotUiPreserveColor>();
            }

            var skin = go.GetComponent<UiSkinState>();
            if (skin == null) skin = go.AddComponent<UiSkinState>();
            skin.Configure(null, label, null, null, false);

            if (Application.isPlaying)
            {
                RitualControlFeedback.Attach(button, RitualControlFeedback.Kind.Quiet);
            }

            return button;
        }

        private void BuildPendingSeal()
        {
            foreach (var candidate in GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate.name == "Phase66_InterpretationStatusText" || candidate.name == "InterpretationStatusText")
                {
                    statusText = candidate;
                    break;
                }
            }

            if (statusText == null || statusText.rectTransform == null) return;

            var existing = transform.Find("RitualPendingSeal");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("RitualPendingSeal", typeof(RectTransform), typeof(RitualOrnamentGraphic));
                go.transform.SetParent(statusText.transform.parent, false);
            }

            pendingSeal = (RectTransform)go.transform;
            pendingSeal.anchorMin = statusText.rectTransform.anchorMin;
            pendingSeal.anchorMax = statusText.rectTransform.anchorMax;
            pendingSeal.pivot = new Vector2(0.5f, 0.5f);
            pendingSeal.anchoredPosition = statusText.rectTransform.anchoredPosition + new Vector2(0f, 44f);
            pendingSeal.sizeDelta = new Vector2(36f, 22f);

            var ornament = go.GetComponent<RitualOrnamentGraphic>();
            ornament.Seal = true;
            ornament.color = new Color(Gold.r, Gold.g, Gold.b, 0.72f);
            ornament.raycastTarget = false;
            pendingSealGraphic = ornament;
            go.SetActive(false);
        }

        private void StyleAction(string name, Color ink, bool rules)
        {
            var node = transform.Find(name);
            if (node == null || !node.TryGetComponent<Button>(out var button)) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var skin = button.GetComponent<UiSkinState>();
            if (skin == null) skin = button.gameObject.AddComponent<UiSkinState>();
            // The existing theme must not restore card-stock ink or a visible plate on these actions.
            skin.Configure(null, label, null, null, false);
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic is Image plate)
            {
                plate.sprite = null;
                plate.type = Image.Type.Simple;
                plate.color = Color.clear;
                plate.enabled = true;
                plate.raycastTarget = true;
            }
            if (label != null)
            {
                label.color = ink;
                label.characterSpacing = 3f;
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                label.raycastTarget = false;
                if (label.GetComponent<TarotUiPreserveColor>() == null)
                    label.gameObject.AddComponent<TarotUiPreserveColor>();
            }
            if (!rules)
            {
                if (Application.isPlaying) RitualControlFeedback.Attach(button, RitualControlFeedback.Kind.Quiet);
                return;
            }
            AddRule(node, "RitualActionRuleTop", 1f);
            AddRule(node, "RitualActionRuleBottom", 0f);
            if (Application.isPlaying) RitualControlFeedback.Attach(button, RitualControlFeedback.Kind.Primary);
        }

        private static void HideImage(Transform node)
        {
            if (node != null && node.TryGetComponent<Image>(out var image)) image.enabled = false;
        }

        private static void AddRule(Transform parent, string name, float y)
        {
            var rule = new GameObject(name, typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(parent, false);
            var rect = (RectTransform)rule.transform;
            rect.anchorMin = new Vector2(0.08f, y);
            rect.anchorMax = new Vector2(0.92f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 1f);
            var image = rule.GetComponent<Image>();
            image.color = new Color(0.78f, 0.65f, 0.42f, 0.55f);
            image.raycastTarget = false;
        }
    }
}
