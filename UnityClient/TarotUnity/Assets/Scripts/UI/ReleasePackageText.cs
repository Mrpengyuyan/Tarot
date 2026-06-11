using TarotUnity.Core;

namespace TarotUnity.UI
{
    public static class ReleasePackageText
    {
        public static string BuildReadme()
        {
            return
                "Tarot Unity Windows x64\n" +
                "\n" +
                "快速开始\n" +
                "1. 解压整个 TarotUnity-Windows-x64 文件夹。\n" +
                "2. 双击 TarotUnity.exe 开始游戏。\n" +
                "3. 如果 Windows Defender 弹窗，选择“更多信息”，再选择“仍要运行”。\n" +
                "\n" +
                "当前可玩流程\n" +
                "Main Menu -> Spread Select -> Question Input -> Shuffle/Draw -> Flip Cards -> Result\n" +
                "\n" +
                "后端配置\n" +
                $"运行时配置文件位于 TarotUnity_Data/StreamingAssets/{DesktopConfigLoader.ConfigFileName}。\n" +
                $"默认 backendBaseUrl 是 {DesktopRuntimeConfig.DefaultBackendBaseUrl}。\n" +
                "如果你把 FastAPI 后端部署到别的机器，请编辑 tarot_desktop_config.json 的 backendBaseUrl。\n" +
                "也可以设置环境变量 TAROT_BACKEND_URL 覆盖配置文件。\n" +
                "\n" +
                "本地模式\n" +
                "没有启动后端时，游戏会使用本地模式完成垂直切片，方便先体验前端流程。\n" +
                "需要真实账号、真实抽牌记录和 AI 解读时，请先启动后端。\n" +
                "\n" +
                "后端启动参考\n" +
                "cd /path/to/Tarot\n" +
                "uvicorn app.main:app --reload\n";
        }

        public static string BuildConfigExample()
        {
            return
                "{\n" +
                $"  \"backendBaseUrl\": \"{DesktopRuntimeConfig.DefaultBackendBaseUrl}\",\n" +
                $"  \"requestTimeoutSeconds\": {DesktopRuntimeConfig.DefaultRequestTimeoutSeconds}\n" +
                "}\n";
        }
    }
}
