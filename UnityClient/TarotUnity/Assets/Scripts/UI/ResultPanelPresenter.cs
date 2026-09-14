using System;
using System.Collections;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class ResultPanelPresenter : MonoBehaviour
    {
        // Phase 51: the Result screen migrates to TMP SDF like the other two.
        // These seven readouts - including the four that carry arbitrary-length
        // backend AI copy - become TMP_Text. The dynamic SDF atlas resolves any
        // Chinese the backend returns, and TMP_Text exposes the same .text the
        // presenter already set, so only the field types change.
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text spreadNameText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text overallText;
        [SerializeField] private TMP_Text cardAnalysisText;
        [SerializeField] private TMP_Text adviceText;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private Image resultCardArtworkSlot;
        [SerializeField] private CardArtworkCatalog cardArtworkCatalog;

        // Phase 60: a multi-card spread shows every drawn card, not just the first.
        // A one-card reading keeps the original single hero (left third + right
        // reading); a multi-card reading switches to a top band of all N cards with
        // the reading reflowed full-width below it. Phase 67 moves the band and
        // reading geometry into ResultSpreadLayout.
        [Serializable]
        public sealed class SpreadCardCell
        {
            public GameObject root;            // whole cell (toggled per used/unused)
            public RectTransform reversePivot; // 180deg Z for a reversed card (not the foil-driven rect)
            public Image artwork;              // the card face
            public TMP_Text label;             // position name, e.g. 过去 / 现在 / 建议
        }

        [Header("Phase 60: multi-card spread band")]
        // Every object that makes up the one-card hero (the showcase frame, the
        // artwork slot, and the placeholder are separate siblings) - all hidden
        // together for a multi-card spread.
        [SerializeField] private GameObject[] singleModeRoots = Array.Empty<GameObject>();
        [SerializeField] private GameObject spreadBandRoot;
        [SerializeField] private SpreadCardCell[] spreadCards = Array.Empty<SpreadCardCell>();
        [SerializeField] private RectTransform readingScrollRect;
        [SerializeField] private Vector2 singleReadingPos = new Vector2(160f, 4f);
        [SerializeField] private Vector2 singleReadingSize = new Vector2(772f, 448f);

        // Phase 66: an online reading can reach this screen before its AI text exists.
        // The status line, the mode label and the retry / offline buttons are wired by
        // the Phase 66 bootstrapper; readingContentGroup (on the scroll's Viewport)
        // hides the empty sections while the text is pending or has failed.
        [Header("Phase 66: online interpretation states")]
        [SerializeField] private TMP_Text interpretationStatusText;
        [SerializeField] private TMP_Text modeLabelText;
        [SerializeField] private Button retryInterpretationButton;
        [SerializeField] private Button offlineInterpretationButton;
        [SerializeField] private CanvasGroup readingContentGroup;
        [SerializeField] private float readyFadeSeconds = 0.6f;

        // Phase 67: reading experience (spec C). Wired by Phase67ResultReadingBootstrapper.
        [Header("Phase 67: reading experience")]
        [SerializeField] private ResultReadingNavigator readingNavigator;
        [SerializeField] private TMP_Text offlineNoticeText;
        [SerializeField] private GameObject warningHeading;
        [SerializeField] private GameObject bottomDivider;
        [SerializeField] private ResultSpreadCellTarget[] cellTargets;

        public const float PendingSlowNoticeSeconds = 20f;

        private float pendingSince;

        // Explicit flag rather than a negative pendingSince: a shifted start time (tests fake
        // "20 s ago" this way) is still a generating reading.
        private bool isPending;
        private Coroutine readyFade;
        private CardDrawData[] presentedDraws;
        private float laidOutCanvasHeight = -1f;

        private CardArtworkCatalog defaultArtworkCatalog;

        public void Present(PredictionDetailResponse detail)
        {
            if (detail == null)
            {
                Clear();
                return;
            }

            SetText(questionText, ReadingTextSanitizer.Plain(detail.question));
            SetText(spreadNameText, ReadingTextSanitizer.Plain(detail.spread_type?.name));
            SetText(summaryText, ReadingTextSanitizer.Plain(detail.interpretation?.summary));
            SetText(overallText, ReadingTextSanitizer.Plain(detail.interpretation?.overall_interpretation));
            ApplyCardAnalysis(detail.interpretation?.card_analysis, detail.card_draws);
            SetText(adviceText, ReadingTextSanitizer.Plain(detail.interpretation?.advice));
            SetText(warningText, ReadingTextSanitizer.Plain(detail.interpretation?.warning));
            SetOfflineNotice(false);
            SetWarningSection(!string.IsNullOrWhiteSpace(detail.interpretation?.warning));
            PresentCards(detail.card_draws);
            SetNavigatorInteractive(true);
        }

        public void PresentSession(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                Clear();
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                ShowOffline(session);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    ShowPending(session);
                    break;
                case InterpretationState.Failed:
                    ShowFailed(session);
                    break;
                default:
                    ShowReady(session, false);
                    break;
            }
        }

        public void ShowPending(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(null);
            SetReadingVisible(false);
            SetStatus(ReleaseUxCopy.ResultPending);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            SetNavigatorInteractive(false);
            pendingSince = Time.unscaledTime;
            isPending = true;
        }

        public void ShowReady(ReadingSessionSnapshot session, bool fadeIn)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ModeLabelFor(session));
            SetInterpretationButtons(false, false);
            isPending = false;
            SetReadingVisible(true);
            SetNavigatorInteractive(true);

            if (fadeIn && readingContentGroup != null && isActiveAndEnabled && readyFadeSeconds > 0f)
            {
                if (readyFade != null)
                {
                    StopCoroutine(readyFade);
                }

                readyFade = StartCoroutine(FadeInReading());
            }
        }

        public void ShowFailed(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(null);
            SetReadingVisible(false);
            SetStatus(session.failureMessage);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(session.canRetry, true);
            SetNavigatorInteractive(false);
            isPending = false;
        }

        public void ShowOffline(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ReleaseUxCopy.ModeOffline);
            SetInterpretationButtons(false, false);
            isPending = false;
            SetReadingVisible(true);
            SetNavigatorInteractive(true);
        }

        public static string BuildPendingStatus(float elapsedSeconds)
        {
            return elapsedSeconds >= PendingSlowNoticeSeconds
                ? ReleaseUxCopy.ResultPending + "\n" + ReleaseUxCopy.ResultPendingSlow
                : ReleaseUxCopy.ResultPending;
        }

        public static string ModeLabelFor(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            if (session.source == ReadingSource.Offline)
            {
                return ReleaseUxCopy.ModeOffline;
            }

            return string.Equals(session.modelUsed, "mock_ai", StringComparison.OrdinalIgnoreCase)
                ? ReleaseUxCopy.ModeMock
                : string.Empty;
        }

        /// <summary>
        /// Phase 67 (spec C 4.1): lays the spread band and the reading panel out for a canvas
        /// size. The presenter calls it when it presents a spread and whenever the canvas
        /// height changes; the capture builder calls it after resizing the canvas.
        /// </summary>
        public void ApplyLayout(Vector2 canvasSize)
        {
            laidOutCanvasHeight = canvasSize.y;
            var count = presentedDraws?.Length ?? 0;
            if (spreadBandRoot == null || !spreadBandRoot.activeSelf || spreadCards == null || count < 2)
            {
                return;
            }

            var used = Mathf.Min(count, spreadCards.Length);
            var layout = ResultSpreadLayout.Compute(used, canvasSize.y);
            for (var i = 0; i < used; i++)
            {
                PositionSpreadCell(i, layout.Cells[i], layout);
            }

            ApplyReadingLayout(layout.ReadingPosition, layout.ReadingSize);
        }

        private void Update()
        {
            RelayoutIfCanvasChanged();

            if (isPending)
            {
                RefreshPendingState(Time.unscaledTime - pendingSince);
            }
        }

        public static bool ShouldOfferOfflineWhilePending(float elapsedSeconds)
        {
            return elapsedSeconds >= PendingSlowNoticeSeconds;
        }

        /// <summary>
        /// The generating state after <paramref name="elapsedSeconds"/>: a breathing status line
        /// (spec 7.1), and from 20 s the slow notice together with 查看离线解读 (spec C, decision C3).
        /// Update drives it every frame; the capture builder calls it to render the 20-second state.
        /// </summary>
        public void RefreshPendingState(float elapsedSeconds)
        {
            if (interpretationStatusText != null)
            {
                var status = BuildPendingStatus(elapsedSeconds);
                if (interpretationStatusText.text != status)
                {
                    interpretationStatusText.text = status;
                }

                interpretationStatusText.alpha =
                    0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(elapsedSeconds * Mathf.PI * 2f / 2.4f));
            }

            if (ShouldOfferOfflineWhilePending(elapsedSeconds) && offlineInterpretationButton != null
                && !offlineInterpretationButton.gameObject.activeSelf)
            {
                SetInterpretationButtons(false, true);
            }
        }

        private IEnumerator FadeInReading()
        {
            SetReadingAlpha(0f);
            var elapsed = 0f;
            while (elapsed < readyFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetReadingAlpha(Mathf.Clamp01(elapsed / readyFadeSeconds));
                yield return null;
            }

            SetReadingAlpha(1f);
            readyFade = null;
        }

        // Phase 67: the mode label fades in with the reading it describes.
        private void SetReadingAlpha(float alpha)
        {
            readingContentGroup.alpha = alpha;
            if (modeLabelText != null)
            {
                modeLabelText.alpha = alpha;
            }
        }

        private void PresentFrame(ReadingSessionSnapshot session)
        {
            SetText(questionText, ReadingTextSanitizer.Plain(session.question));
            SetText(spreadNameText, ReadingTextSanitizer.Plain(session.spreadName));
            PresentCards(session.cardDraws);
        }

        private void SetReadingTexts(ReadingSessionSnapshot session)
        {
            SetText(summaryText, ReadingTextSanitizer.Plain(session?.summary));
            SetText(overallText, ReadingTextSanitizer.Plain(session?.overallInterpretation));
            ApplyCardAnalysis(session?.cardAnalysis, session?.cardDraws);
            SetText(adviceText, ReadingTextSanitizer.Plain(session?.advice));
            SetText(warningText, ReadingTextSanitizer.Plain(session?.warning));

            // Phase 67 (spec C 4.6): an offline reading says so in its first line. Its warning
            // field holds that same sentence, so the 提醒 section is only for an online warning.
            var offline = session != null && session.source == ReadingSource.Offline;
            SetOfflineNotice(offline);
            SetWarningSection(session != null && !offline && !string.IsNullOrWhiteSpace(session.warning));
        }

        private void ApplyCardAnalysis(string cardAnalysis, CardDrawData[] draws)
        {
            var formatted = CardAnalysisFormatter.Build(cardAnalysis, draws);
            SetText(cardAnalysisText, formatted.RichText);
            if (readingNavigator != null)
            {
                readingNavigator.SetBlocks(formatted.Ranges);
            }
        }

        private void SetOfflineNotice(bool visible)
        {
            if (offlineNoticeText == null)
            {
                return;
            }

            offlineNoticeText.text = ReleaseUxCopy.OfflineWarning;
            offlineNoticeText.gameObject.SetActive(visible);
        }

        private void SetWarningSection(bool visible)
        {
            if (warningHeading != null)
            {
                warningHeading.SetActive(visible);
            }

            if (warningText != null)
            {
                warningText.gameObject.SetActive(visible);
            }
        }

        private void SetNavigatorInteractive(bool interactive)
        {
            if (readingNavigator != null)
            {
                readingNavigator.SetInteractive(interactive);
            }
        }

        private void SetReadingVisible(bool visible)
        {
            if (readingContentGroup == null)
            {
                return;
            }

            if (!visible && readyFade != null)
            {
                StopCoroutine(readyFade);
                readyFade = null;
            }

            readingContentGroup.alpha = visible ? 1f : 0f;
            readingContentGroup.blocksRaycasts = visible;
            if (visible && modeLabelText != null)
            {
                modeLabelText.alpha = 1f;
            }
        }

        private void SetStatus(string message)
        {
            if (interpretationStatusText == null)
            {
                return;
            }

            interpretationStatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            interpretationStatusText.text = message ?? string.Empty;
            interpretationStatusText.alpha = 1f;
        }

        private void SetInterpretationButtons(bool retryVisible, bool offlineVisible)
        {
            if (retryInterpretationButton != null)
            {
                retryInterpretationButton.gameObject.SetActive(retryVisible);
            }

            if (offlineInterpretationButton != null)
            {
                offlineInterpretationButton.gameObject.SetActive(offlineVisible);
            }
        }

        public void Clear()
        {
            SetText(questionText, string.Empty);
            SetText(spreadNameText, string.Empty);
            SetText(summaryText, string.Empty);
            SetText(adviceText, string.Empty);
            SetText(overallText, string.Empty);
            SetText(cardAnalysisText, string.Empty);
            SetText(warningText, string.Empty);
            if (readingNavigator != null)
            {
                readingNavigator.SetBlocks(null);
            }

            SetOfflineNotice(false);
            SetWarningSection(false);
            PresentCards(null);
            SetStatus(null);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            SetNavigatorInteractive(false);
            SetReadingVisible(true);
            isPending = false;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private CardArtworkCatalog ResolveCatalog()
        {
            return cardArtworkCatalog != null
                ? cardArtworkCatalog
                : defaultArtworkCatalog ??= Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
        }

        private void PresentCards(CardDrawData[] draws)
        {
            presentedDraws = draws;
            var catalog = ResolveCatalog();
            var count = draws?.Length ?? 0;

            var hasBand = spreadBandRoot != null && spreadCards != null && spreadCards.Length > 0;
            var useSpread = hasBand && count >= 2;

            if (useSpread)
            {
                SetSingleModeActive(false);
                if (bottomDivider != null)
                {
                    // The single-card footer divider would cross the taller spread reading.
                    bottomDivider.SetActive(false);
                }

                spreadBandRoot.SetActive(true);

                // If a spread ever has more cards than the pool, the extras have nowhere to
                // go - warn rather than drop silently, so it can never regress unnoticed.
                if (count > spreadCards.Length)
                {
                    Debug.LogWarning($"ResultPanelPresenter: {count}-card spread exceeds the " +
                        $"{spreadCards.Length}-cell band; rebuild the band with more cells.");
                }

                for (var i = 0; i < spreadCards.Length; i++)
                {
                    FillSpreadCell(spreadCards[i], i < count ? draws[i] : null, catalog);
                }

                ApplyLayout(CurrentCanvasSize());
                return;
            }

            // Single-card (or empty) layout - the original hero showcase.
            if (spreadBandRoot != null)
            {
                spreadBandRoot.SetActive(false);
            }

            SetSingleModeActive(true);
            if (bottomDivider != null)
            {
                bottomDivider.SetActive(true);
            }

            ApplyReadingLayout(singleReadingPos, singleReadingSize);
            laidOutCanvasHeight = CurrentCanvasSize().y;

            var primary = count > 0 && catalog != null ? catalog.FindSprite(draws[0]) : null;
            SetArtwork(primary);
        }

        // EditMode tests and captures lay out at the reference size; at runtime the canvas
        // rect carries the real size once the scaler has run.
        private Vector2 CurrentCanvasSize()
        {
            var reference = new Vector2(TarotUiSpacing.ReferenceWidth, TarotUiSpacing.ReferenceHeight);
            if (!Application.isPlaying)
            {
                return reference;
            }

            var rect = transform as RectTransform;
            return rect != null && rect.rect.height >= 1f ? rect.rect.size : reference;
        }

        private void RelayoutIfCanvasChanged()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var size = CurrentCanvasSize();
            if (Mathf.Approximately(size.y, laidOutCanvasHeight))
            {
                return;
            }

            if (spreadBandRoot != null && spreadBandRoot.activeSelf)
            {
                ApplyLayout(size);
            }
            else
            {
                laidOutCanvasHeight = size.y;
            }
        }

        private void SetSingleModeActive(bool active)
        {
            if (singleModeRoots == null)
            {
                return;
            }

            foreach (var go in singleModeRoots)
            {
                if (go != null)
                {
                    go.SetActive(active);
                }
            }
        }

        private void FillSpreadCell(SpreadCardCell cell, CardDrawData draw, CardArtworkCatalog catalog)
        {
            if (cell == null || cell.root == null)
            {
                return;
            }

            var used = draw != null;
            cell.root.SetActive(used);
            if (!used)
            {
                return;
            }

            if (cell.artwork != null)
            {
                var sprite = catalog != null ? catalog.FindSprite(draw) : null;
                cell.artwork.sprite = sprite;
                cell.artwork.preserveAspect = true;
                cell.artwork.enabled = sprite != null;
            }

            if (cell.reversePivot != null)
            {
                cell.reversePivot.localRotation = Quaternion.Euler(0f, 0f, draw.is_reversed ? 180f : 0f);
            }

            if (cell.label != null)
            {
                cell.label.text = ReadingTextSanitizer.Plain(BuildCellLabel(draw));
            }
        }

        private void PositionSpreadCell(int index, SpreadCellPlacement placement, SpreadLayoutResult layout)
        {
            var cell = spreadCards[index];
            if (cell == null || cell.root == null)
            {
                return;
            }

            var rt = cell.root.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchoredPosition = placement.Position;
            var target = cellTargets != null && index < cellTargets.Length ? cellTargets[index] : null;
            if (target != null)
            {
                target.SetBaseScale(placement.Scale);
            }
            else
            {
                rt.localScale = Vector3.one * placement.Scale;
            }

            // The label is a child of the scaled cell; compensate so it keeps a fixed on-screen size.
            if (cell.label != null && placement.Scale > 0f)
            {
                cell.label.fontSize = layout.LabelFontSize / placement.Scale;
                cell.label.rectTransform.sizeDelta = new Vector2(
                    ResultSpreadLayout.BasePitch / placement.Scale, layout.LabelHeight / placement.Scale);
            }
        }

        private static string BuildCellLabel(CardDrawData draw)
        {
            var position = !string.IsNullOrWhiteSpace(draw.position_name)
                ? draw.position_name
                : (draw.tarot_card != null ? draw.tarot_card.name_zh : string.Empty);
            position ??= string.Empty;
            return draw.is_reversed ? position + ReleaseUxCopy.CardReversedMark : position;
        }

        private void ApplyReadingLayout(Vector2 pos, Vector2 size)
        {
            if (readingScrollRect == null)
            {
                return;
            }

            readingScrollRect.anchoredPosition = pos;
            readingScrollRect.sizeDelta = size;
        }

        private void SetArtwork(Sprite sprite)
        {
            if (resultCardArtworkSlot == null)
            {
                return;
            }

            resultCardArtworkSlot.sprite = sprite;
            resultCardArtworkSlot.preserveAspect = true;
            resultCardArtworkSlot.enabled = sprite != null;
        }
    }
}
