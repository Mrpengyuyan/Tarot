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
        // the reading reflowed full-width below it. The band, the single hero root,
        // and the reading scroll's two layouts are all wired by the Phase 60
        // bootstrapper, so the presenter only toggles and fills them.
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
        [SerializeField] private Vector2 singleReadingPos = new Vector2(178f, 4f);
        [SerializeField] private Vector2 singleReadingSize = new Vector2(736f, 448f);
        [SerializeField] private Vector2 spreadReadingPos = new Vector2(0f, -150f);
        [SerializeField] private Vector2 spreadReadingSize = new Vector2(1120f, 232f);

        // Phase 62: the band is a pool of cells laid out at runtime, so any card
        // count (not just three) shows every card. Cells are centred as a single
        // row; the pitch shrinks (and the cells scale down with it) once the row
        // would overflow the band width, so a larger spread stays on screen instead
        // of dropping cards past the third.
        [Header("Phase 62: dynamic N-card row")]
        [SerializeField] private float spreadRowWidth = 1180f;
        [SerializeField] private float spreadBasePitch = 348f;
        [SerializeField] private float spreadMinCellScale = 0.34f;
        [SerializeField] private float spreadCellY = 88f;

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

        private float pendingSince = -1f;
        private Coroutine readyFade;

        private CardArtworkCatalog defaultArtworkCatalog;

        public void Present(PredictionDetailResponse detail)
        {
            if (detail == null)
            {
                Clear();
                return;
            }

            SetText(questionText, detail.question);
            SetText(spreadNameText, detail.spread_type?.name);
            SetText(summaryText, detail.interpretation?.summary);
            SetText(overallText, detail.interpretation?.overall_interpretation);
            SetText(cardAnalysisText, detail.interpretation?.card_analysis);
            SetText(adviceText, detail.interpretation?.advice);
            SetText(warningText, detail.interpretation?.warning);
            PresentCards(detail.card_draws);
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
            pendingSince = Time.unscaledTime;
        }

        public void ShowReady(ReadingSessionSnapshot session, bool fadeIn)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ModeLabelFor(session));
            SetInterpretationButtons(false, false);
            pendingSince = -1f;
            SetReadingVisible(true);

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
            pendingSince = -1f;
        }

        public void ShowOffline(ReadingSessionSnapshot session)
        {
            PresentFrame(session);
            SetReadingTexts(session);
            SetStatus(null);
            SetText(modeLabelText, ReleaseUxCopy.ModeOffline);
            SetInterpretationButtons(false, false);
            pendingSince = -1f;
            SetReadingVisible(true);
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

        private void Update()
        {
            if (pendingSince < 0f || interpretationStatusText == null)
            {
                return;
            }

            // A breathing status line while the interpretation is generating; after
            // 20 s the slow-generation notice joins it (spec 7.1).
            var elapsed = Time.unscaledTime - pendingSince;
            var status = BuildPendingStatus(elapsed);
            if (interpretationStatusText.text != status)
            {
                interpretationStatusText.text = status;
            }

            interpretationStatusText.alpha = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2f / 2.4f));
        }

        private IEnumerator FadeInReading()
        {
            readingContentGroup.alpha = 0f;
            var elapsed = 0f;
            while (elapsed < readyFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                readingContentGroup.alpha = Mathf.Clamp01(elapsed / readyFadeSeconds);
                yield return null;
            }

            readingContentGroup.alpha = 1f;
            readyFade = null;
        }

        private void PresentFrame(ReadingSessionSnapshot session)
        {
            SetText(questionText, session.question);
            SetText(spreadNameText, session.spreadName);
            PresentCards(session.cardDraws);
        }

        private void SetReadingTexts(ReadingSessionSnapshot session)
        {
            SetText(summaryText, session?.summary);
            SetText(overallText, session?.overallInterpretation);
            SetText(cardAnalysisText, session?.cardAnalysis);
            SetText(adviceText, session?.advice);
            SetText(warningText, session?.warning);
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
            PresentCards(null);
            SetStatus(null);
            SetText(modeLabelText, string.Empty);
            SetInterpretationButtons(false, false);
            SetReadingVisible(true);
            pendingSince = -1f;
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
            var catalog = ResolveCatalog();
            var count = draws?.Length ?? 0;

            var hasBand = spreadBandRoot != null && spreadCards != null && spreadCards.Length > 0;
            var useSpread = hasBand && count >= 2;

            if (useSpread)
            {
                SetSingleModeActive(false);
                // The warning line is pinned near the footer for the single-card
                // layout; the full-width spread reading reaches into it, so it is
                // hidden here (its copy is still set for a backend that returns one).
                if (warningText != null) warningText.gameObject.SetActive(false);
                spreadBandRoot.SetActive(true);
                ApplyReadingLayout(spreadReadingPos, spreadReadingSize);

                // How many of the pool's cells this reading uses. If a spread ever
                // has more cards than the pool, the extras have nowhere to go - warn
                // rather than drop silently, so it can never regress unnoticed.
                var used = Mathf.Min(count, spreadCards.Length);
                if (count > spreadCards.Length)
                {
                    Debug.LogWarning($"ResultPanelPresenter: {count}-card spread exceeds the " +
                        $"{spreadCards.Length}-cell band; rebuild the band with more cells.");
                }

                var pitch = Mathf.Min(spreadBasePitch, used > 0 ? spreadRowWidth / used : spreadBasePitch);
                var scale = Mathf.Clamp(pitch / spreadBasePitch, spreadMinCellScale, 1f);

                for (var i = 0; i < spreadCards.Length; i++)
                {
                    if (i < used)
                    {
                        PositionSpreadCell(spreadCards[i], i, used, pitch, scale);
                    }

                    FillSpreadCell(spreadCards[i], i < count ? draws[i] : null, catalog);
                }
                return;
            }

            // Single-card (or empty) layout - the original hero showcase.
            if (spreadBandRoot != null)
            {
                spreadBandRoot.SetActive(false);
            }
            SetSingleModeActive(true);
            if (warningText != null) warningText.gameObject.SetActive(true);
            ApplyReadingLayout(singleReadingPos, singleReadingSize);

            var primary = count > 0 && catalog != null ? catalog.FindSprite(draws[0]) : null;
            SetArtwork(primary);
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
                cell.label.text = BuildCellLabel(draw);
            }
        }

        // Centre the used cells as one row: cell i sits at (i - (used-1)/2)*pitch,
        // so any count is symmetric about the middle. The whole cell (frame, art,
        // label, glow) scales together as the pitch tightens for larger spreads.
        private void PositionSpreadCell(SpreadCardCell cell, int index, int used, float pitch, float scale)
        {
            if (cell == null || cell.root == null)
            {
                return;
            }

            var rt = cell.root.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            var x = (index - (used - 1) * 0.5f) * pitch;
            rt.anchoredPosition = new Vector2(x, spreadCellY);
            rt.localScale = Vector3.one * scale;
        }

        private static string BuildCellLabel(CardDrawData draw)
        {
            var position = !string.IsNullOrWhiteSpace(draw.position_name)
                ? draw.position_name
                : (draw.tarot_card != null ? draw.tarot_card.name_zh : string.Empty);
            position ??= string.Empty;
            return draw.is_reversed ? position + "（逆位）" : position;
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
