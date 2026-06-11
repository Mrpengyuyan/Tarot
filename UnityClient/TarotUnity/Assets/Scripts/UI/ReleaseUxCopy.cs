namespace TarotUnity.UI
{
    public static class ReleaseUxCopy
    {
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
