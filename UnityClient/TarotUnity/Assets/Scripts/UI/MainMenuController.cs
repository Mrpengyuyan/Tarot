using TarotUnity.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startReadingButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text statusText;

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
            if (statusText != null)
            {
                statusText.text = "准备开始一场安静的占卜。";
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
