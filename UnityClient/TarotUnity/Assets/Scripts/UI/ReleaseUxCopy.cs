using TarotUnity.Data;
using TarotUnity.Network;

namespace TarotUnity.UI
{
    public static class ReleaseUxCopy
    {
        // Phase 66: reading-room flow copy (spec 6.6). Guarded to contain no ASCII letters.
        public const string DefaultQuestion = "此刻我最需要留意什么？";
        public const string FlowShuffling = "正在洗牌……";
        public const string FlowDealing = "正在发牌……";
        public const string FlowFlipPrompt = "点击每张牌，把它翻开。";
        public const string FlowAllRevealed = "牌已全部揭开。";
        public const string FlowResultReady = "可以查看结果了。";
        public const string InterpretationGenerating = "解读正在生成……";
        public const string InterpretationReadyHint = "解读已就绪";
        public const string OnlineReady = "已连上占卜服务：抽牌后会在线生成解读。";

        // Phase 66: a failed start (spec 7.3, create-record rows) - the reading goes offline.
        public const string OfflineBecauseNetwork = "暂时连不上占卜服务，这一局使用离线解读。";
        public const string OfflineBecauseSession = "访客会话连接失败，这一局使用离线解读。";
        public const string OfflineBecauseUnavailable = "在线占卜暂时不可用，这一局使用离线解读。";
        public const string OfflineBecauseSpread = "这个牌阵暂时无法在线占卜，这一局使用离线解读。";

        // Phase 66: interpretation failures (spec 7.3, interpretation rows).
        public const string InterpretationBackendFailed = "这次解读没有顺利生成，可以再试一次。";
        public const string InterpretationTimedOut = "解读花的时间比预期长，可以再试一次。";
        public const string InterpretationConnectionLost = "与占卜服务的连接中断了，可以再试一次。";
        public const string InterpretationAttemptsExhausted = "这一局已经尝试多次仍未成功，先看看离线解读吧。";
        public const string InterpretationSessionExpired = "连接已过期，这次解读无法取回。";
        public const string InterpretationUnavailable = "这次解读无法生成，先看看离线解读吧。";

        // Phase 66: Result screen (spec 7.1).
        public const string ResultPending = "牌意正在汇聚……";
        public const string ResultPendingSlow = "这次解读比平时慢一些，请再稍候。";
        public const string ModeOffline = "离线解读";
        public const string ModeMock = "模拟解读";
        public const string RetryButtonLabel = "重新解读";
        public const string OfflineButtonLabel = "查看离线解读";
        public const string OfflineWarning = "这是离线解读，由本地牌义生成，未经过 AI。";

        public static string GuestQuotaExhausted(int retryAfterSeconds)
        {
            const string head = "今天的访客占卜次数已用完，";
            const string tail = "这一局使用离线解读。";
            if (retryAfterSeconds < 0)
            {
                return head + tail;
            }

            if (retryAfterSeconds < 3600)
            {
                return head + "不到 1 小时后恢复。" + tail;
            }

            var hours = (retryAfterSeconds + 3599) / 3600;
            return head + $"约 {hours} 小时后恢复。" + tail;
        }

        // A 401 reaching this point means BackendReadingService.RecoverSession
        // already failed (spec 6.6 step 2).
        public static string ForStartReadingFailure(ApiError error)
        {
            if (error == null)
            {
                return OfflineBecauseUnavailable;
            }

            switch (error.Kind)
            {
                case ApiErrorKind.Network:
                case ApiErrorKind.Timeout:
                case ApiErrorKind.Server:
                    return OfflineBecauseNetwork;
                case ApiErrorKind.RateLimited:
                    return GuestQuotaExhausted(error.RetryAfterSeconds);
                case ApiErrorKind.Unauthorized:
                    return OfflineBecauseSession;
                default:
                    return OfflineBecauseUnavailable;
            }
        }

        public static string ForInterpretationFailure(InterpretationFailure failure)
        {
            switch (failure)
            {
                case InterpretationFailure.BackendFailed:
                    return InterpretationBackendFailed;
                case InterpretationFailure.TimedOut:
                    return InterpretationTimedOut;
                case InterpretationFailure.ConnectionLost:
                    return InterpretationConnectionLost;
                case InterpretationFailure.AttemptsExhausted:
                    return InterpretationAttemptsExhausted;
                case InterpretationFailure.SessionExpired:
                    return InterpretationSessionExpired;
                default:
                    return InterpretationUnavailable;
            }
        }

        public static bool CanRetry(InterpretationFailure failure)
        {
            return failure == InterpretationFailure.BackendFailed
                || failure == InterpretationFailure.TimedOut
                || failure == InterpretationFailure.ConnectionLost;
        }


        public static string LocalModeReady =>
            "本地模式已准备好：即使没有启动后端，也可以完成一局塔罗流程。";

        public static string BackendFallback(string reason)
        {
            var detail = string.IsNullOrWhiteSpace(reason) ? "未收到后端响应" : reason.Trim();
            return $"后端暂时不可用，已切换到本地模式。原因：{detail}";
        }

        public static string BackendOnlyFailure(string reason)
        {
            var detail = string.IsNullOrWhiteSpace(reason) ? "未收到后端响应" : reason.Trim();
            return $"后端连接失败：{detail}。请检查 tarot_desktop_config.json 中的 backendBaseUrl，或先启动 FastAPI 后端。";
        }
    }
}
