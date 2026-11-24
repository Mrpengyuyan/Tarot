using System.Collections.Generic;
using TarotUnity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1500)]
    public sealed class RitualControlFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,
        IPointerDownHandler,ISelectHandler,IDeselectHandler,IMoveHandler
    {
        internal enum Kind { Primary,Quiet,Spread,Input }
        private sealed class Visual
        {
            internal RectTransform Rect;
            internal Vector2 Rest,Applied;
        }
        private Selectable control;
        private TMP_Text label;
        private TMP_InputField input;
        private RectTransform canvas;
        private Kind kind;
        private Color normal;
        private Visual visual;
        private readonly List<Graphic> rules=new();
        private readonly List<CanvasGroup> groups=new();
        private RitualFocusInk ring,halo;
        private Image underline;
        private bool hovering,keyboardFocus;
        private float hoverFrom,hoverTarget,hoverSince,haloFrom,haloTarget,haloSince;

        internal float HoverAmount { get; private set; }

        internal static void Attach(Button button,Kind kind)
        {
            if(button==null || button.GetComponent<RitualControlFeedback>()!=null) return;
            var owner=button.gameObject.AddComponent<RitualControlFeedback>();
            owner.control=button; owner.kind=kind;
            owner.label=button.GetComponentInChildren<TMP_Text>(true);
            owner.Build();
        }

        internal static void Attach(TMP_InputField input)
        {
            if(input==null || input.GetComponent<RitualControlFeedback>()!=null) return;
            var owner=input.gameObject.AddComponent<RitualControlFeedback>();
            owner.control=owner.input=input; owner.kind=Kind.Input;
            owner.Build();
        }

        private void Build()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            canvas = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
            if (label != null) normal = label.color;
            if (kind == Kind.Input)
            {
                underline = Node<Image>("RitualInputFocus");
                var rect = underline.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.right;
                rect.pivot = new Vector2(.5f, .5f);
            }
            else
            {
                ring = Node<RitualFocusInk>("RitualFocusRing");
                ring.rectTransform.anchorMin = Vector2.zero; ring.rectTransform.anchorMax = Vector2.one;
                if (kind == Kind.Spread)
                {
                    halo = Node<RitualFocusInk>("RitualSpreadHalo"); halo.Halo = true;
                    halo.rectTransform.anchorMin = new Vector2(-.12f, -.2f);
                    halo.rectTransform.anchorMax = new Vector2(1.12f, 1.2f);
                    halo.rectTransform.offsetMin = halo.rectTransform.offsetMax = Vector2.zero;
                    halo.transform.SetAsFirstSibling();
                }
                var rect = kind == Kind.Spread ? transform.Find("RitualSpreadSymbol") as RectTransform : label != null ? label.rectTransform : null;
                if (rect != null) visual = new Visual { Rect = rect, Rest = rect.anchoredPosition, Applied = rect.anchoredPosition };
                if (kind == Kind.Primary)
                    foreach (var image in GetComponentsInChildren<Graphic>(true))
                        if (image.name.Contains("Rule") && image != ring) rules.Add(image);
            }
            Refresh();
        }

        private T Node<T>(string name) where T:Graphic
        {
            var node=new GameObject(name,typeof(RectTransform),typeof(T));
            node.transform.SetParent(transform,false);
            var graphic=node.GetComponent<T>();
            graphic.raycastTarget=false;
            graphic.color=Color.clear;
            return graphic;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!hovering && kind != Kind.Input && control != null && control.IsInteractable() && Visible())
            {
                AudioManager.Instance?.PlayCue(PresentationCueId.UiHoverSoft, 0.35f);
            }

            hovering = true;
        }
        public void OnPointerExit(PointerEventData data) => hovering=false;
        public void OnPointerDown(PointerEventData data)
        {
            keyboardFocus = false;
            if (kind != Kind.Input && control != null && control.IsInteractable() && Visible())
            {
                AudioManager.Instance?.PlayCue(PresentationCueId.UiClickSoft, 0.4f);
            }
        }
        public void OnSelect(BaseEventData data) => keyboardFocus=data is not PointerEventData;
        public void OnDeselect(BaseEventData data) => keyboardFocus=false;
        public void OnMove(AxisEventData data) => keyboardFocus=true;
        private void LateUpdate() => Refresh();

        private bool Visible()
        {
            if(control==null || !control.IsActive()) return false;
            groups.Clear(); GetComponentsInParent(true,groups);
            foreach(var group in groups)
            {
                if(!group.interactable || group.alpha<.001f) return false;
                if(group.ignoreParentGroups) break;
            }
            return true;
        }

        private static float Blend(ref float from,ref float target,ref float since,float next,float seconds)
        {
            var value=seconds<=0 ? target : Mathf.Lerp(from,target,RitualPickLayout.HoverEase((Time.unscaledTime-since)/seconds));
            if(target!=next) { from=value; target=next; since=Time.unscaledTime; }
            return seconds<=0 ? next : Mathf.Lerp(from,target,RitualPickLayout.HoverEase((Time.unscaledTime-since)/seconds));
        }

        private void Refresh()
        {
            if (control == null || canvas == null) return;
            var visible = Visible();
            var usable = visible && control.IsInteractable();
            if (!visible) hovering = false;
            if (!usable) { keyboardFocus = false; hoverFrom = hoverTarget = haloFrom = haloTarget = 0f; }
            var focus = usable && keyboardFocus && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            var amount = Blend(ref hoverFrom, ref hoverTarget, ref hoverSince, usable && hovering ? 1f : 0f, kind == Kind.Spread ? .4f : kind == Kind.Quiet ? 0f : .2f);
            HoverAmount = amount;
            var d = canvas.rect.width > 2f && canvas.rect.height > 2f ? RitualDeckLayout.Density(canvas.rect.width, canvas.rect.height) : 1f;
            if (ring != null)
            {
                var ringColor = new Color(240f / 255f, 213f / 255f, 160f / 255f, focus ? 1f : 0f);
                var stroke = 2f * d;
                var min = Vector2.one * (-7f * d);
                var max = Vector2.one * (7f * d);
                if (ring.color != ringColor) ring.color = ringColor;
                if (!Mathf.Approximately(ring.Stroke, stroke))
                {
                    ring.Stroke = stroke;
                    ring.SetVerticesDirty();
                }
                if (ring.rectTransform.offsetMin != min) ring.rectTransform.offsetMin = min;
                if (ring.rectTransform.offsetMax != max) ring.rectTransform.offsetMax = max;
            }
            if (halo != null)
            {
                var opacity = Blend(ref haloFrom, ref haloTarget, ref haloSince, usable && (hovering || focus) ? 1f : 0f, .35f);
                var haloColor = new Color(195f / 255f, 152f / 255f, 60f / 255f, 28f / 255f * opacity);
                if (halo.color != haloColor) halo.color = haloColor;
            }
            if (underline != null)
            {
                var underlineColor = new Color(229f / 255f, 195f / 255f, 136f / 255f, usable && input.isFocused ? 1f : 0f);
                var pos = new Vector2(0f, -.5f * d);
                var size = new Vector2(0f, d);
                if (underline.color != underlineColor) underline.color = underlineColor;
                if (underline.rectTransform.anchoredPosition != pos) underline.rectTransform.anchoredPosition = pos;
                if (underline.rectTransform.sizeDelta != size) underline.rectTransform.sizeDelta = size;
            }
            if (label != null && kind != Kind.Spread)
            {
                var nextLabelColor = kind == Kind.Quiet
                    ? Color.Lerp(normal, new Color(246f / 255f, 221f / 255f, 176f / 255f), amount)
                    : new Color(Mathf.Min(1f, normal.r * (1f + .25f * amount)),
                        Mathf.Min(1f, normal.g * (1f + .25f * amount)),
                        Mathf.Min(1f, normal.b * (1f + .25f * amount)), normal.a);
                if (label.color != nextLabelColor) label.color = nextLabelColor;
            }
            if (visual != null && visual.Rect != null)
            {
                if (visual.Rect.anchoredPosition != visual.Applied) visual.Rest = visual.Rect.anchoredPosition;
                var lift = kind == Kind.Spread ? (canvas.rect.height - 2f * d) * .014f : 2f * d;
                var nextPos = visual.Rest + Vector2.up * (lift * amount);
                visual.Applied = nextPos;
                if (visual.Rect.anchoredPosition != nextPos) visual.Rect.anchoredPosition = nextPos;
            }
            foreach (var rule in rules)
            {
                if (rule == null) continue;
                var brightness = 1f + .25f * amount;
                var nextRuleColor = new Color(Mathf.Min(1f, 199f / 255f * brightness),
                    Mathf.Min(1f, 165f / 255f * brightness),
                    Mathf.Min(1f, 107f / 255f * brightness),
                    usable ? Mathf.Lerp(.65f, 1f, amount) : .2f);
                var nextScale = new Vector3(1f + .08f * amount, 1f, 1f);
                if (rule.color != nextRuleColor) rule.color = nextRuleColor;
                if (rule.rectTransform.localScale != nextScale) rule.rectTransform.localScale = nextScale;
            }
        }

        private void OnDisable()
        {
            hovering=keyboardFocus=false; hoverFrom=hoverTarget=haloFrom=haloTarget=0;
            if(label!=null && kind!=Kind.Spread) label.color=normal;
            if(visual?.Rect!=null && visual.Rect.anchoredPosition==visual.Applied) visual.Rect.anchoredPosition=visual.Rest;
            foreach(var rule in rules) if(rule!=null) { rule.color=new Color(199f/255,165f/255,107f/255,.65f); rule.rectTransform.localScale=Vector3.one; }
            if(ring!=null) ring.color=Color.clear;
            if(halo!=null) halo.color=Color.clear;
            if(underline!=null) underline.color=Color.clear;
        }

        private void OnDestroy()
        {
            if(ring!=null) Destroy(ring.gameObject);
            if(halo!=null) Destroy(halo.gameObject);
            if(underline!=null) Destroy(underline.gameObject);
        }
    }
}
