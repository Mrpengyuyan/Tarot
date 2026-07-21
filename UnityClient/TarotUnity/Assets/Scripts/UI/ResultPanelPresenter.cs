using System;
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

            SetText(questionText, session.question);
            SetText(spreadNameText, session.spreadName);
            SetText(summaryText, session.summary);
            SetText(overallText, session.overallInterpretation);
            SetText(cardAnalysisText, session.cardAnalysis);
            SetText(adviceText, session.advice);
            SetText(warningText, session.warning);
            PresentCards(session.cardDraws);
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

                for (var i = 0; i < spreadCards.Length; i++)
                {
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
