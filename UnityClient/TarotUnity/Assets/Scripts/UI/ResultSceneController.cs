using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private ResultPanelPresenter resultPanel;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private ResultRevealDirector revealDirector;
        [SerializeField] private RitualFeedbackController ritualFeedback;
        [SerializeField] private CameraChoreographyController cameraChoreography;

        private void Awake()
        {
            backToMenuButton?.onClick.AddListener(BackToMenu);
        }

        private void Start()
        {
            if (resultPanel == null)
            {
                resultPanel = FindFirstObjectByType<ResultPanelPresenter>();
            }

            if (revealDirector == null)
            {
                revealDirector = FindFirstObjectByType<ResultRevealDirector>();
            }

            if (ritualFeedback == null)
            {
                ritualFeedback = FindFirstObjectByType<RitualFeedbackController>();
            }

            if (cameraChoreography == null)
            {
                cameraChoreography = FindFirstObjectByType<CameraChoreographyController>();
            }

            if (ReadingSessionStore.HasCurrent)
            {
                resultPanel?.PresentSession(ReadingSessionStore.Current);
            }

            cameraChoreography?.FocusResult();
            ritualFeedback?.PlayCue(PresentationCueId.ResultReveal);
            revealDirector?.PlayReveal();
        }

        private void OnDestroy()
        {
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
        }

        private void BackToMenu()
        {
            ReadingSessionStore.Clear();

            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(GameSceneId.MainMenu);
                return;
            }

            SceneManager.LoadScene(GameSceneId.MainMenu.ToString());
        }
    }
}
