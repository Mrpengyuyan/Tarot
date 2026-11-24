using TarotUnity.Data;
using TarotUnity.Gameplay;
using TMPro;
using UnityEngine;

namespace TarotUnity.UI
{
    public sealed partial class ResultPanelPresenter
    {
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
                : CardArtworkCatalog.LoadDefault();
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

            // The label is a child of the scaled cell; compensate so it keeps a fixed on-screen size. Its
            // width (the hover and click area, P5) stops at the row pitch so neighbouring labels never overlap.
            if (cell.label != null && placement.Scale > 0f)
            {
                cell.label.fontSize = layout.LabelFontSize / placement.Scale;
                cell.label.rectTransform.sizeDelta = new Vector2(
                    Mathf.Min(ResultSpreadLayout.BasePitch, layout.RowPitch) / placement.Scale, layout.LabelHeight / placement.Scale);
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
