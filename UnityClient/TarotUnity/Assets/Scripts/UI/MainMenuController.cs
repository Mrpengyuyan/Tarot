using TarotUnity.Core;
using TarotUnity.Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startReadingButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text statusText;

        private BackendSessionBootstrap backendSessionBootstrap;

        private void Awake()
        {
            if (startReadingButton != null)
            {
                startReadingButton.onClick.AddListener(StartReading);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void Start()
        {
            SetStatus("烛火已燃，牌已洗过。\n正在准备在线解读……");

            backendSessionBootstrap = FindFirstObjectByType<BackendSessionBootstrap>();
            if (backendSessionBootstrap != null)
            {
                backendSessionBootstrap.StatusChanged += HandleBackendSessionStatusChanged;
                HandleBackendSessionStatusChanged(
                    backendSessionBootstrap.Status,
                    backendSessionBootstrap.LastError);
            }
        }

        private void OnDestroy()
        {
            if (startReadingButton != null)
            {
                startReadingButton.onClick.RemoveListener(StartReading);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }

            if (backendSessionBootstrap != null)
            {
                backendSessionBootstrap.StatusChanged -= HandleBackendSessionStatusChanged;
            }
        }

        private void HandleBackendSessionStatusChanged(BackendSessionStatus status, string error)
        {
            switch (status)
            {
                case BackendSessionStatus.Connecting:
                    SetStatus("正在连接在线解读……");
                    break;
                case BackendSessionStatus.Online:
                    SetStatus("在线解读已连接。\n烛火已燃，牌已洗过。");
                    break;
                case BackendSessionStatus.Offline:
                    SetStatus("在线解读暂不可用，已准备离线模式。\n烛火已燃，牌已洗过。");
                    break;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartReading()
        {
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(GameSceneId.ReadingRoom);
                return;
            }

            SceneManager.LoadScene(GameSceneId.ReadingRoom.ToString());
        }
    }
}
