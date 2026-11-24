using System;
using System.Collections;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed partial class ResultPanelPresenter : MonoBehaviour
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

            // Phase 67: a finished detail ends any generating state, the way ShowReady does.
            SetStatus(null);
            SetInterpretationButtons(false, false);
            isPending = false;
            SetReadingVisible(true);
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
            SetInterpretationButtons(session.canRetry || session.canCheckStatus, true);
            if (retryInterpretationButton != null)
            {
                var label = retryInterpretationButton.transform.Find("Label");
                if (label != null && label.TryGetComponent<TMP_Text>(out var text))
                {
                    text.text = session.canRetry
                        ? ReleaseUxCopy.RetryButtonLabel
                        : ReleaseUxCopy.CheckStatusButtonLabel;
                }
            }
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

        private void Update()
        {
            RelayoutIfCanvasChanged();

            if (isPending)
            {
                RefreshPendingState(Time.unscaledTime - pendingSince);
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
    }
}
