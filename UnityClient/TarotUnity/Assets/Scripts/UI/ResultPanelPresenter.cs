using TarotUnity.Data;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class ResultPanelPresenter : MonoBehaviour
    {
        [SerializeField] private Text questionText;
        [SerializeField] private Text spreadNameText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text overallText;
        [SerializeField] private Text cardAnalysisText;
        [SerializeField] private Text adviceText;
        [SerializeField] private Text warningText;
        [SerializeField] private Image resultCardArtworkSlot;
        [SerializeField] private CardArtworkCatalog cardArtworkCatalog;

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
            PresentPrimaryCard(detail.card_draws);
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
            PresentPrimaryCard(session.cardDraws);
        }

        public void Clear()
        {
            SetText(questionText, string.Empty);
            SetText(spreadNameText, string.Empty);
            SetText(summaryText, string.Empty);
            SetText(overallText, string.Empty);
            SetText(cardAnalysisText, string.Empty);
            SetText(adviceText, string.Empty);
            SetText(warningText, string.Empty);
            SetArtwork(null);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private void PresentPrimaryCard(CardDrawData[] draws)
        {
            if (draws == null || draws.Length == 0)
            {
                SetArtwork(null);
                return;
            }

            var catalog = cardArtworkCatalog != null
                ? cardArtworkCatalog
                : defaultArtworkCatalog ??= Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");

            SetArtwork(catalog != null ? catalog.FindSprite(draws[0]) : null);
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
