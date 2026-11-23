using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1450)]
    public sealed class RitualStageEntrance : MonoBehaviour
    {
        private sealed class Item
        {
            internal CanvasGroup Group;
            internal RectTransform Rect;
            internal Vector2 Rest,Applied;
            internal bool Visible;
            internal float Opacity=1;
        }
        private const float EntranceDuration = 0.6f;
        private readonly Dictionary<Transform, Item> items = new();
        private float began;
        private bool settled;
        private RectTransform canvas;

        internal static RitualStageEntrance Attach(GameObject host)
        {
            if (host == null) return null;
            var owner = host.GetComponent<RitualStageEntrance>();
            if (owner == null) owner = host.AddComponent<RitualStageEntrance>();
            owner.canvas = host.transform as RectTransform;
            return owner;
        }

        private void OnEnable() => Restart();
        internal void Restart()
        {
            began = Time.unscaledTime;
            settled = false;
        }

        internal void Show(Transform node, bool visible, float opacity = 1f)
        {
            if (node == null) return;
            if (!items.TryGetValue(node, out var item))
            {
                var rect = node as RectTransform;
                var group = node.GetComponent<CanvasGroup>();
                if (group == null) group = node.gameObject.AddComponent<CanvasGroup>();
                item = new Item
                {
                    Group = group,
                    Rect = rect,
                    Rest = rect != null ? rect.anchoredPosition : Vector2.zero,
                    Applied = rect != null ? rect.anchoredPosition : Vector2.zero,
                    Visible = visible,
                    Opacity = opacity,
                };
                items.Add(node, item);
                item.Group.interactable = visible;
                item.Group.blocksRaycasts = visible;
                Apply(item, CurrentAmount());
                return;
            }

            var changed = item.Visible != visible || !Mathf.Approximately(item.Opacity, opacity);
            item.Visible = visible;
            item.Opacity = opacity;
            if (item.Group != null)
            {
                if (item.Group.interactable != visible) item.Group.interactable = visible;
                if (item.Group.blocksRaycasts != visible) item.Group.blocksRaycasts = visible;
                if (changed || !settled) Apply(item, CurrentAmount());
            }
        }

        private void LateUpdate()
        {
            if (settled) return;
            var amount = CurrentAmount();
            foreach (var item in items.Values)
            {
                if (item.Group != null) Apply(item, amount);
            }
            if (amount >= 1f) settled = true;
        }

        private float CurrentAmount()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return 1f;
            var progress = (Time.unscaledTime - began) / EntranceDuration;
            return progress >= 1f ? 1f : RitualPickLayout.HoverEase(progress);
        }

        private void Apply(Item item, float amount)
        {
            var alpha = item.Visible ? Mathf.Max(1f / 255f, amount) * item.Opacity : 0f;
            if (!Mathf.Approximately(item.Group.alpha, alpha)) item.Group.alpha = alpha;
            if (item.Rect == null || canvas == null) return;
            if (item.Rect.anchoredPosition != item.Applied) item.Rest = item.Rect.anchoredPosition;
            var d = canvas.rect.width > 2f && canvas.rect.height > 2f ? RitualDeckLayout.Density(canvas.rect.width, canvas.rect.height) : 1f;
            var nextPos = item.Rest + (item.Visible ? Vector2.down * (7f * d * (1f - amount)) : Vector2.zero);
            item.Applied = nextPos;
            if (item.Rect.anchoredPosition != nextPos) item.Rect.anchoredPosition = nextPos;
        }

        private void OnDisable()
        {
            settled = true;
            foreach (var item in items.Values)
            {
                if (item.Group != null) Apply(item, 1f);
            }
        }
    }
}
