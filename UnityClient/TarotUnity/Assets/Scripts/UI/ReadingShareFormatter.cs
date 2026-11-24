using System.Text;
using TarotUnity.Data;

namespace TarotUnity.UI
{
    /// <summary>
    /// Formats a completed reading session into clean plain text suitable for copying or sharing.
    /// </summary>
    public static class ReadingShareFormatter
    {
        public static string Format(ReadingSessionSnapshot session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(512);
            var spread = ReadingTextSanitizer.Plain(session.spreadName);
            var question = ReadingTextSanitizer.Plain(session.question);
            var modeTag = session.source == ReadingSource.Offline ? "（离线牌义）" : string.Empty;

            sb.Append("【塔罗占卜记录");
            sb.Append(modeTag);
            sb.AppendLine("】");

            if (!string.IsNullOrWhiteSpace(spread))
            {
                sb.Append("牌阵：").AppendLine(spread);
            }

            if (!string.IsNullOrWhiteSpace(question))
            {
                sb.Append("问题：").AppendLine(question);
            }

            if (session.cardDraws != null && session.cardDraws.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("抽到的牌：");
                for (var i = 0; i < session.cardDraws.Length; i++)
                {
                    var draw = session.cardDraws[i];
                    if (draw == null)
                    {
                        continue;
                    }

                    var position = ReadingTextSanitizer.Plain(draw.position_name);
                    var cardName = ReadingTextSanitizer.Plain(draw.tarot_card != null ? draw.tarot_card.name_zh : string.Empty);
                    var orientation = draw.is_reversed ? ReleaseUxCopy.CardReversedMark : ReleaseUxCopy.CardUprightMark;

                    sb.Append(i + 1).Append(". ");
                    if (!string.IsNullOrWhiteSpace(position))
                    {
                        sb.Append(position).Append(" — ");
                    }

                    sb.Append(string.IsNullOrWhiteSpace(cardName) ? "未知牌面" : cardName);
                    sb.AppendLine(orientation);
                }
            }

            AppendSection(sb, "摘要", session.summary);
            AppendSection(sb, "整体解读", session.overallInterpretation);
            AppendSection(sb, "逐牌解析", session.cardAnalysis);
            AppendSection(sb, "行动建议", session.advice);

            if (session.source != ReadingSource.Offline)
            {
                AppendSection(sb, "提醒", session.warning);
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder sb, string heading, string body)
        {
            var clean = ReadingTextSanitizer.Plain(body);
            if (string.IsNullOrWhiteSpace(clean))
            {
                return;
            }

            sb.AppendLine();
            sb.Append(heading).AppendLine("：");
            sb.AppendLine(clean);
        }
    }
}
