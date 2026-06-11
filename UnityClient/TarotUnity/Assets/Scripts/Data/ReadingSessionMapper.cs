using System;

namespace TarotUnity.Data
{
    public static class ReadingSessionMapper
    {
        public static ReadingSessionSnapshot FromBackendDetail(
            PredictionDetailResponse detail,
            CardDrawData[] fallbackCards = null)
        {
            if (detail == null)
            {
                return null;
            }

            var cards = detail.card_draws ?? fallbackCards ?? Array.Empty<CardDrawData>();
            var spread = detail.spread_type;
            var interpretation = detail.interpretation;

            return new ReadingSessionSnapshot
            {
                spreadId = spread?.id ?? detail.spread_type_id,
                spreadName = FirstNonEmpty(spread?.name, spread?.name_en, $"Spread {detail.spread_type_id}"),
                cardCount = cards.Length > 0 ? cards.Length : spread?.card_count ?? 0,
                question = detail.question,
                questionType = detail.question_type,
                cardDraws = cards,
                summary = interpretation?.summary ?? string.Empty,
                overallInterpretation = interpretation?.overall_interpretation ?? string.Empty,
                cardAnalysis = interpretation?.card_analysis ?? string.Empty,
                advice = interpretation?.advice ?? string.Empty,
                warning = interpretation?.warning ?? string.Empty,
            };
        }

        public static ReadingSessionSnapshot FromBackendParts(
            PredictionResponse prediction,
            SpreadSummary spread,
            CardDrawData[] cardDraws,
            InterpretationResponse interpretation)
        {
            if (prediction == null)
            {
                return null;
            }

            var detail = new PredictionDetailResponse
            {
                id = prediction.id,
                user_id = prediction.user_id,
                spread_type_id = prediction.spread_type_id,
                question = prediction.question,
                question_type = prediction.question_type,
                status = prediction.status,
                created_at = prediction.created_at,
                completed_at = prediction.completed_at,
                is_favorite = prediction.is_favorite,
                user_rating = prediction.user_rating,
                user_notes = prediction.user_notes,
                spread_type = spread,
                card_draws = cardDraws,
                interpretation = interpretation,
            };

            return FromBackendDetail(detail, cardDraws);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
