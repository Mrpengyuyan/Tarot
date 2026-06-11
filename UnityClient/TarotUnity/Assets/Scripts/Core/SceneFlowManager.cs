using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarotUnity.Core
{
    public enum GameSceneId
    {
        Boot,
        MainMenu,
        ReadingRoom,
        Result
    }

    public sealed class SceneFlowManager : MonoBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        public event Action<GameSceneId> SceneLoadRequested;
        public event Action<GameSceneId> SceneLoaded;
        public event Action<string> SceneLoadFailed;

        public bool IsLoading { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool LoadScene(GameSceneId sceneId)
        {
            if (IsLoading)
            {
                return FailLoad("Scene load is already in progress.");
            }

            if (!CanLoadScene(sceneId))
            {
                return FailLoad($"Scene '{GetSceneName(sceneId)}' is not a known scene or is missing from Build Settings.");
            }

            StartCoroutine(LoadSceneRoutine(sceneId));
            return true;
        }

        public string GetSceneName(GameSceneId sceneId)
        {
            return sceneId.ToString();
        }

        public bool CanLoadScene(GameSceneId sceneId)
        {
            if (!Enum.IsDefined(typeof(GameSceneId), sceneId))
            {
                return false;
            }

            return Application.CanStreamedLevelBeLoaded(GetSceneName(sceneId));
        }

        private bool FailLoad(string message)
        {
            IsLoading = false;
            LastError = message;
            SceneLoadFailed?.Invoke(message);
            Debug.LogWarning(message);
            return false;
        }

        private IEnumerator LoadSceneRoutine(GameSceneId sceneId)
        {
            IsLoading = true;
            LastError = string.Empty;
            SceneLoadRequested?.Invoke(sceneId);

            var operation = SceneManager.LoadSceneAsync(GetSceneName(sceneId));
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            SceneLoaded?.Invoke(sceneId);
        }
    }
}
