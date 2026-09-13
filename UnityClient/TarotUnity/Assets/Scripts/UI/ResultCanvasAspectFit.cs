using System;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.7): the Result canvas keeps the whole 1280-wide design on narrow
    /// screens (match width) and the whole 720-tall design on wide ones (match height). The
    /// header and the button row stay centre-anchored in the scene (EditMode guards measure
    /// them against an unsized overlay canvas) and are pinned to the canvas top and bottom at
    /// runtime instead, so a taller canvas opens space in the middle for the reading.
    /// </summary>
    public sealed class ResultCanvasAspectFit : MonoBehaviour
    {
        public enum Edge
        {
            Top,
            Bottom,
        }

        [Serializable]
        public sealed class PinnedElement
        {
            public RectTransform target;
            public Edge edge;
            public float offsetFromEdge;
        }

        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private PinnedElement[] pinned = Array.Empty<PinnedElement>();

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private float lastCanvasHeight = -1f;

        public static float MatchFor(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return 1f;
            }

            var referenceAspect = TarotUiSpacing.ReferenceWidth / TarotUiSpacing.ReferenceHeight;
            return (float)width / height >= referenceAspect - 0.0001f ? 1f : 0f;
        }

        public static float PinnedY(float canvasHeight, Edge edge, float offsetFromEdge)
        {
            var half = (canvasHeight >= 1f ? canvasHeight : TarotUiSpacing.ReferenceHeight) * 0.5f;
            return edge == Edge.Top ? half - offsetFromEdge : -half + offsetFromEdge;
        }

        private void Awake()
        {
            Apply(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                Apply(Screen.width, Screen.height);
                return;
            }

            // The scaler resizes the canvas a frame after the match factor changes.
            var canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            if (!Mathf.Approximately(canvasHeight, lastCanvasHeight))
            {
                Repin(canvasHeight);
            }
        }

        public void Apply(int screenWidth, int screenHeight)
        {
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
            if (scaler != null)
            {
                scaler.matchWidthOrHeight = MatchFor(screenWidth, screenHeight);
            }

            Repin(canvasRect != null ? canvasRect.rect.height : 0f);
        }

        public void Repin(float canvasHeight)
        {
            lastCanvasHeight = canvasHeight;
            foreach (var element in pinned)
            {
                if (element == null || element.target == null)
                {
                    continue;
                }

                var position = element.target.anchoredPosition;
                element.target.anchoredPosition =
                    new Vector2(position.x, PinnedY(canvasHeight, element.edge, element.offsetFromEdge));
            }
        }
    }
}
