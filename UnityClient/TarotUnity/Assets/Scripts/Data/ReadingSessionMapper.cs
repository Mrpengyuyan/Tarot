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
                predictionId = detail.id,
                source = ReadingSource.Online,
                interpretationState = IsRealInterpretation(interpretation)
                    ? InterpretationState.Ready
                    : InterpretationState.Pending,
                modelUsed = IsRealInterpretation(interpretation)
                    ? interpretation.model_used ?? string.Empty
                    : string.Empty,
            };
        }

        // Phase 66: the start of an online reading - the record and its drawn cards,
        // with no interpretation yet. The caller fills in spreadName; the poller
        // fills in the text once the background generation finishes.
        public static ReadingSessionSnapshot FromBackendStart(PredictionResponse prediction, CardDrawData[] cards)
        {
            if (prediction == null)
            {
                return null;
            }

            var draws = cards ?? Array.Empty<CardDrawData>();
            return new ReadingSessionSnapshot
            {
                spreadId = prediction.spread_type_id,
                spreadName = string.Empty,
                cardCount = draws.Length,
                question = prediction.question,
                questionType = prediction.question_type,
                cardDraws = draws,
                summary = string.Empty,
                overallInterpretation = string.Empty,
                cardAnalysis = string.Empty,
                advice = string.Empty,
                warning = string.Empty,
                predictionId = prediction.id,
                source = ReadingSource.Online,
                interpretationState = InterpretationState.Pending,
            };
        }

        // Phase 66: JsonUtility may turn a JSON "interpretation": null into an empty
        // instance rather than null, so "has an interpretation" means a stored row
        // (id > 0) or body text - never merely a non-null reference.
        public static bool HasInterpretation(PredictionDetailResponse detail)
        {
            return IsRealInterpretation(detail?.interpretation);
        }

        public static bool IsRealInterpretation(InterpretationResponse interpretation)
        {
            return interpretation != null
                && (interpretation.id > 0 || !string.IsNullOrWhiteSpace(interpretation.overall_interpretation));
        }

        public static void ApplyInterpretation(ReadingSessionSnapshot target, InterpretationResponse interpretation)
        {
            if (target == null || interpretation == null)
            {
                return;
            }

            target.summary = interpretation.summary ?? string.Empty;
            target.overallInterpretation = interpretation.overall_interpretation ?? string.Empty;
            target.cardAnalysis = interpretation.card_analysis ?? string.Empty;
            target.advice = interpretation.advice ?? string.Empty;
            target.warning = interpretation.warning ?? string.Empty;
            target.modelUsed = interpretation.model_used ?? string.Empty;
            target.source = ReadingSource.Online;
            target.interpretationState = InterpretationState.Ready;
            target.failureMessage = string.Empty;
            target.canRetry = false;
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
