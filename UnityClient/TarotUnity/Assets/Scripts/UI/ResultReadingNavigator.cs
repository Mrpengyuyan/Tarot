using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.2, 4.4): the reading panel's affordances. The bottom fade shows while
    /// more text is below. Clicking a card scrolls its analysis block so the heading sits 12%
    /// down the viewport, then glows the heading back to its gold over 1.2 s. When the per-card
    /// blocks could not be parsed, a click scrolls to the 牌面分析 heading instead. Nothing
    /// happens while the reading is pending or failed.
    /// </summary>
    public sealed class ResultReadingNavigator : MonoBehaviour
    {
        public const float FadeHideDistance = 8f;

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Graphic bottomFade;
        [SerializeField] private TMP_Text cardAnalysisText;
        [SerializeField] private RectTransform cardSectionHeading;
        [SerializeField] private float focusSeconds = 0.35f;
        [SerializeField] private float focusViewportFraction = 0.12f;
        [SerializeField] private float highlightSeconds = 1.2f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.86f, 0.55f, 1f);

        private readonly List<CardBlockRange> blocks = new List<CardBlockRange>();
        private Coroutine scrollRoutine;
        private Coroutine highlightRoutine;

        public bool IsInteractive { get; private set; }
        public bool IsFadeVisible { get; private set; }
        public int HighlightedCard { get; private set; } = -1;
        public IReadOnlyList<CardBlockRange> Blocks => blocks;

        public static float NormalizedPositionFor(
            float targetOffsetFromTop, float contentHeight, float viewportHeight, float viewportFraction)
        {
            var scrollable = contentHeight - viewportHeight;
            if (scrollable <= 0.5f)
            {
                return 1f;
            }

            var desired = targetOffsetFromTop - viewportFraction * viewportHeight;
            return 1f - Mathf.Clamp(desired, 0f, scrollable) / scrollable;
        }

        public static bool ShouldShowFade(bool interactive, float contentHeight, float viewportHeight, float normalizedPosition)
        {
            if (!interactive)
            {
                return false;
            }

            var scrollable = contentHeight - viewportHeight;
            return scrollable > 1f && normalizedPosition * scrollable > FadeHideDistance;
        }

        public void SetBlocks(IReadOnlyList<CardBlockRange> ranges)
        {
            StopHighlight();
            blocks.Clear();
            if (ranges == null)
            {
                return;
            }

            for (var i = 0; i < ranges.Count; i++)
            {
                blocks.Add(ranges[i]);
            }
        }

        public void SetInteractive(bool interactive)
        {
            IsInteractive = interactive;
            if (!interactive)
            {
                StopScroll();
                StopHighlight();
            }
        }

        public void FocusCard(int cardIndex)
        {
            if (!IsInteractive || scroll == null || scroll.content == null || scroll.viewport == null)
            {
                return;
            }

            StopHighlight();
            Canvas.ForceUpdateCanvases();
            var block = FindBlock(cardIndex);
            var offset = block.HasValue ? HeadingLineOffset(block.Value) : SectionHeadingOffset();
            if (!offset.HasValue)
            {
                return;
            }

            var target = NormalizedPositionFor(
                offset.Value, scroll.content.rect.height, scroll.viewport.rect.height, focusViewportFraction);
            StopScroll();
            scrollRoutine = StartCoroutine(ScrollTo(target));
            if (block.HasValue)
            {
                highlightRoutine = StartCoroutine(Highlight(block.Value));
            }
        }

        private void LateUpdate()
        {
            var visible = scroll != null && scroll.content != null && scroll.viewport != null
                && ShouldShowFade(IsInteractive, scroll.content.rect.height, scroll.viewport.rect.height,
                    scroll.verticalNormalizedPosition);
            IsFadeVisible = visible;
            if (bottomFade == null)
            {
                return;
            }

            var current = bottomFade.canvasRenderer.GetAlpha();
            var next = Mathf.MoveTowards(current, visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (!Mathf.Approximately(current, next))
            {
                bottomFade.canvasRenderer.SetAlpha(next);
            }
        }

        private void OnDisable()
        {
            StopScroll();
            StopHighlight();
        }

        private CardBlockRange? FindBlock(int cardIndex)
        {
            foreach (var block in blocks)
            {
                if (block.CardIndex == cardIndex)
                {
                    return block;
                }
            }

            return null;
        }

        private float? HeadingLineOffset(CardBlockRange block)
        {
            if (cardAnalysisText == null)
            {
                return null;
            }

            cardAnalysisText.ForceMeshUpdate();
            var info = cardAnalysisText.textInfo;
            if (block.HeadingStart < 0 || block.HeadingStart >= info.characterCount)
            {
                return null;
            }

            var line = info.characterInfo[block.HeadingStart].lineNumber;
            if (line < 0 || line >= info.lineCount)
            {
                return null;
            }

            var world = cardAnalysisText.rectTransform.TransformPoint(new Vector3(0f, info.lineInfo[line].ascender, 0f));
            return OffsetFromContentTop(world);
        }

        private float? SectionHeadingOffset()
        {
            if (cardSectionHeading == null)
            {
                return null;
            }

            var world = cardSectionHeading.TransformPoint(new Vector3(0f, cardSectionHeading.rect.yMax, 0f));
            return OffsetFromContentTop(world);
        }

        private float OffsetFromContentTop(Vector3 worldPoint)
        {
            var content = scroll.content;
            return content.rect.yMax - content.InverseTransformPoint(worldPoint).y;
        }

        private IEnumerator ScrollTo(float normalizedTarget)
        {
            scroll.StopMovement();
            var start = scroll.verticalNormalizedPosition;
            var duration = Mathf.Max(0.01f, focusSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - (1f - t) * (1f - t) * (1f - t);
                scroll.verticalNormalizedPosition = Mathf.Lerp(start, normalizedTarget, eased);
                yield return null;
            }

            scroll.verticalNormalizedPosition = normalizedTarget;
            scrollRoutine = null;
        }

        private IEnumerator Highlight(CardBlockRange block)
        {
            HighlightedCard = block.CardIndex;
            var text = cardAnalysisText;
            if (text == null)
            {
                HighlightedCard = -1;
                yield break;
            }

            text.ForceMeshUpdate();
            var info = text.textInfo;
            var characterCount = info.characterCount;
            var end = Mathf.Min(block.HeadingStart + block.HeadingLength, characterCount);
            var vertices = new List<(int material, int index, Color32 original)>();
            for (var i = Mathf.Max(0, block.HeadingStart); i < end; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                var colors = info.meshInfo[character.materialReferenceIndex].colors32;
                for (var v = 0; v < 4 && character.vertexIndex + v < colors.Length; v++)
                {
                    vertices.Add((character.materialReferenceIndex, character.vertexIndex + v, colors[character.vertexIndex + v]));
                }
            }

            Color32 glow = highlightColor;
            var duration = Mathf.Max(0.01f, highlightSeconds);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                if (info.characterCount != characterCount)
                {
                    break; // the text changed under us
                }

                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                foreach (var vertex in vertices)
                {
                    var colors = info.meshInfo[vertex.material].colors32;
                    if (vertex.index < colors.Length)
                    {
                        colors[vertex.index] = Color32.Lerp(glow, vertex.original, t);
                    }
                }

                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }

            text.ForceMeshUpdate();
            HighlightedCard = -1;
            highlightRoutine = null;
        }

        private void StopScroll()
        {
            if (scrollRoutine == null)
            {
                return;
            }

            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        private void StopHighlight()
        {
            if (highlightRoutine == null)
            {
                return;
            }

            StopCoroutine(highlightRoutine);
            highlightRoutine = null;
            HighlightedCard = -1;
            if (cardAnalysisText != null)
            {
                cardAnalysisText.ForceMeshUpdate();
            }
        }
    }
}
