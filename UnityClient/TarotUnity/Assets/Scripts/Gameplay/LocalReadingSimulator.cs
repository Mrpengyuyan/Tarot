using System;
using System.Text;
using TarotUnity.Data;

namespace TarotUnity.Gameplay
{
    public static class LocalReadingSimulator
    {
        // The offline placeholder reading. The real reading comes from the backend
        // AI (Chinese); this stands in when playing offline, so it is Chinese too,
        // to match the rest of the interface. name_en is kept for traceability.
        private static readonly (string Zh, string En, string Meaning)[] PlaceholderCards =
        {
            ("愚者", "The Fool", "新的开始、信任、迈出第一步"),
            ("魔术师", "The Magician", "专注、意志、能力"),
            ("女祭司", "The High Priestess", "直觉、沉默、隐藏的知识"),
            ("皇后", "The Empress", "成长、滋养、丰盛"),
            ("战车", "The Chariot", "方向、自律、势头"),
            ("星星", "The Star", "疗愈、希望、安静的指引"),
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
                        keywords = card.Meaning.Split('、'),
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
                question = string.IsNullOrWhiteSpace(question) ? "我现在该留意什么？" : question,
                questionType = string.IsNullOrWhiteSpace(questionType) ? "general" : questionType,
                cardDraws = draws ?? Array.Empty<CardDrawData>(),
                summary = "牌面已经铺开，先看整体的走向。",
                overallInterpretation = BuildOverall(draws),
                cardAnalysis = BuildCardAnalysis(draws),
                advice = "放慢脚步，只选定一个下一步，然后留意揭示之后有什么改变。",
                warning = "这是本地占位文本，后端 AI 解读将在后续阶段接入。",
            };
        }

        private static string BuildOverall(CardDrawData[] draws)
        {
            if (draws == null || draws.Length == 0)
            {
                return "牌桌还很安静。抽牌来揭开第一条讯息。";
            }

            return draws.Length == 1
                ? "一张牌，把此刻收拢成一个清晰的信号。"
                : "这组牌从过去走到现在，再落到给你的建议——一个清晰的开始、当下与下一步。";
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
                builder.Append("：");
                builder.Append(draw.tarot_card?.name_zh ?? "未知牌");
                if (draw.is_reversed)
                {
                    builder.Append("（逆位）");
                }

                builder.Append(" — ");
                builder.Append(draw.card_meaning?.meaning ?? "占位含义");
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string PositionName(int index, int count)
        {
            if (count == 1)
            {
                return "核心";
            }

            return index switch
            {
                0 => "过去",
                1 => "现在",
                2 => "建议",
                _ => $"第 {index + 1} 位",
            };
        }

        private static string PositionMeaning(int index, int count)
        {
            if (count == 1)
            {
                return "针对这个问题最清晰的信号。";
            }

            return index switch
            {
                0 => "是什么把这个问题带到了这里。",
                1 => "此刻正在起作用的是什么。",
                2 => "接下来更有用的姿态。",
                _ => "额外的背景。",
            };
        }
    }
}
