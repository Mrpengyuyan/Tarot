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
            HideImage(transform.Find("Phase12_ResultCardShowcase"));
            var band = transform.Find("MP_ResultSpreadBand");
            if (band != null)
            {
                foreach (Transform cell in band) HideImage(cell.Find("ReversePivot/Frame"));
            }

            StyleAction("BackToMenuButton", Gold, true);
            StyleAction("Phase66_RetryInterpretationButton", Gold, true);
            StyleAction("Phase66_OfflineInterpretationButton", QuietGold, false);
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
            if (!rules) return;
            AddRule(node, "RitualActionRuleTop", 1f);
            AddRule(node, "RitualActionRuleBottom", 0f);
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
