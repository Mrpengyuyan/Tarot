using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Network;
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
        [SerializeField] private Button retryInterpretationButton;
        [SerializeField] private Button offlineInterpretationButton;

        private InterpretationPoller poller;

        private void Awake()
        {
            backToMenuButton?.onClick.AddListener(BackToMenu);
            retryInterpretationButton?.onClick.AddListener(RetryInterpretation);
            offlineInterpretationButton?.onClick.AddListener(UseOfflineInterpretation);
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
                SubscribeToPoller();
            }

            cameraChoreography?.FocusResult();
            ritualFeedback?.PlayCue(PresentationCueId.ResultReveal);
            revealDirector?.PlayReveal();
        }

        private void OnDestroy()
        {
            backToMenuButton?.onClick.RemoveListener(BackToMenu);
            retryInterpretationButton?.onClick.RemoveListener(RetryInterpretation);
            offlineInterpretationButton?.onClick.RemoveListener(UseOfflineInterpretation);

            if (poller != null)
            {
                poller.StateChanged -= HandleInterpretationStateChanged;
            }
        }

        // Phase 66: follow the persistent poller so a Pending reading switches to its
        // AI text, or to the failure copy, while the player is on this screen.
        private void SubscribeToPoller()
        {
            var instance = InterpretationPoller.Instance;
            if (instance == null)
            {
                return;
            }

            poller = instance;
            poller.StateChanged += HandleInterpretationStateChanged;
        }

        private void HandleInterpretationStateChanged(ReadingSessionSnapshot session)
        {
            if (session == null || session != ReadingSessionStore.Current || resultPanel == null)
            {
                return;
            }

            if (session.source == ReadingSource.Offline)
            {
                resultPanel.ShowOffline(session);
                return;
            }

            switch (session.interpretationState)
            {
                case InterpretationState.Pending:
                    resultPanel.ShowPending(session);
                    break;
                case InterpretationState.Ready:
                    resultPanel.ShowReady(session, true);
                    break;
                default:
                    resultPanel.ShowFailed(session);
                    break;
            }
        }

        private void RetryInterpretation()
        {
            var session = ReadingSessionStore.Current;
            if (poller != null && session != null && session.canRetry && poller.Current == session)
            {
                poller.Retry();
            }
        }

        // Spec 7.1: switch to the offline text for good - polling stops and the
        // screen never flips back to an online result.
        private void UseOfflineInterpretation()
        {
            var session = ReadingSessionStore.Current;
            if (session == null)
            {
                return;
            }

            if (poller != null && poller.Current == session)
            {
                poller.UseOffline();
                return;
            }

            InterpretationPoller.ApplyOffline(session);
            resultPanel?.ShowOffline(session);
        }

        private void BackToMenu()
        {
            var activePoller = poller != null ? poller : InterpretationPoller.Instance;
            if (activePoller != null)
            {
                activePoller.Stop();
            }

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
