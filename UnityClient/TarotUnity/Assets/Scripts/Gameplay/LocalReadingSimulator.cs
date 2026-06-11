using System;
using System.Text;
using TarotUnity.Data;

namespace TarotUnity.Gameplay
{
    public static class LocalReadingSimulator
    {
        private static readonly (string Zh, string En, string Meaning)[] PlaceholderCards =
        {
            ("The Fool", "The Fool", "new beginning, trust, first step"),
            ("The Magician", "The Magician", "focus, will, skill"),
            ("The High Priestess", "The High Priestess", "intuition, silence, hidden knowledge"),
            ("The Empress", "The Empress", "growth, care, embodied abundance"),
            ("The Chariot", "The Chariot", "direction, discipline, momentum"),
            ("The Star", "The Star", "healing, hope, quiet guidance"),
        };

        public static CardDrawData[] CreatePlaceholderDraws(int cardCount)
        {
            var safeCount = Math.Max(1, cardCount);
            var draws = new CardDrawData[safeCount];

            for (var i = 0; i < safeCount; i++)
            {
                var card = PlaceholderCards[i % PlaceholderCards.Length];
                draws[i] = new CardDrawData
                {
                    id = i + 1,
                    prediction_id = 1,
                    tarot_card_id = i + 1,
                    position = i + 1,
                    is_reversed = i % 3 == 2,
                    drawn_at = DateTime.UtcNow.ToString("O"),
                    tarot_card = new TarotCardSimple
                    {
                        id = i + 1,
                        name_zh = card.Zh,
                        name_en = card.En,
                        arcana = "major",
                        suit = string.Empty,
                        number = i,
                        image_url = string.Empty,
                    },
                    card_meaning = new TarotCardMeaningData
                    {
                        id = i + 1,
                        name_zh = card.Zh,
                        name_en = card.En,
                        is_reversed = i % 3 == 2,
                        meaning = card.Meaning,
                        keywords = card.Meaning.Split(", "),
                        position = i + 1,
                        position_name = PositionName(i, safeCount),
                        position_meaning = PositionMeaning(i, safeCount),
                    },
                    position_name = PositionName(i, safeCount),
                    position_meaning = PositionMeaning(i, safeCount),
                };
            }

            return draws;
        }

        public static ReadingSessionSnapshot CreateSession(
            int spreadId,
            string spreadName,
            string question,
            string questionType,
            CardDrawData[] draws)
        {
            return new ReadingSessionSnapshot
            {
                spreadId = spreadId,
                spreadName = spreadName,
                cardCount = draws?.Length ?? 0,
                question = string.IsNullOrWhiteSpace(question) ? "What should I notice now?" : question,
                questionType = string.IsNullOrWhiteSpace(questionType) ? "general" : questionType,
                cardDraws = draws ?? Array.Empty<CardDrawData>(),
                summary = "A placeholder reading is ready for the Unity graybox slice.",
                overallInterpretation = BuildOverall(draws),
                cardAnalysis = BuildCardAnalysis(draws),
                advice = "Move slowly, choose one next action, and notice what changes after the reveal.",
                warning = "This is local placeholder text. Backend AI interpretation starts in a later phase.",
            };
        }

        private static string BuildOverall(CardDrawData[] draws)
        {
            if (draws == null || draws.Length == 0)
            {
                return "The table is quiet. Draw cards to reveal the first message.";
            }

            return draws.Length == 1
                ? "One card narrows the moment into a single clear signal."
                : "The spread moves from context to tension to advice, giving the reading a simple beginning, middle, and next step.";
        }

        private static string BuildCardAnalysis(CardDrawData[] draws)
        {
            if (draws == null || draws.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var draw in draws)
            {
                builder.Append(draw.position_name);
                builder.Append(": ");
                builder.Append(draw.tarot_card?.name_zh ?? "Unknown Card");
                if (draw.is_reversed)
                {
                    builder.Append(" reversed");
                }

                builder.Append(" - ");
                builder.Append(draw.card_meaning?.meaning ?? "placeholder meaning");
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string PositionName(int index, int count)
        {
            if (count == 1)
            {
                return "Focus";
            }

            return index switch
            {
                0 => "Past",
                1 => "Present",
                2 => "Advice",
                _ => $"Position {index + 1}",
            };
        }

        private static string PositionMeaning(int index, int count)
        {
            if (count == 1)
            {
                return "The clearest signal for this question.";
            }

            return index switch
            {
                0 => "What brought this question here.",
                1 => "What is active now.",
                2 => "The next useful posture.",
                _ => "Additional context.",
            };
        }
    }
}

