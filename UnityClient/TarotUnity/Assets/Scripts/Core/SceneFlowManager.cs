using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private static readonly Color VeilColor = new(0.025f, 0.02f, 0.035f, 1f);

        public static SceneFlowManager Instance { get; private set; }

        public event Action<GameSceneId> SceneLoadRequested;
        public event Action<GameSceneId> SceneLoaded;
        public event Action<string> SceneLoadFailed;

        public bool IsLoading { get; private set; }
        public bool IsFading { get; private set; }
        public float FadeDuration { get; set; } = 0.22f;
        public float FadeAlpha => fadeGroup != null ? fadeGroup.alpha : 0f;
        public string LastError { get; private set; } = string.Empty;

        private GameObject fadeRoot;
        private CanvasGroup fadeGroup;
        private Coroutine fadeInRoutine;

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

        private void OnDestroy()
        {
            if (fadeRoot != null)
            {
                Destroy(fadeRoot);
                fadeRoot = null;
                fadeGroup = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
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

            if (fadeInRoutine != null)
            {
                StopCoroutine(fadeInRoutine);
                fadeInRoutine = null;
            }

            var activeSceneName = SceneManager.GetActiveScene().name;
            var useFade = FadeDuration > 0.001f
                && !string.Equals(activeSceneName, GameSceneId.Boot.ToString(), StringComparison.OrdinalIgnoreCase);

            if (useFade)
            {
                yield return FadeCurtain(0f, 1f, FadeDuration);
            }

            var operation = SceneManager.LoadSceneAsync(GetSceneName(sceneId));
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            // Let the newly loaded scene run its initial Start/layout frame under the veil.
            if (useFade)
            {
                yield return null;
            }

            IsLoading = false;
            SceneLoaded?.Invoke(sceneId);

            if (useFade)
            {
                fadeInRoutine = StartCoroutine(FadeInAndHide(FadeDuration));
            }
        }

        private void EnsureFadeCurtain()
        {
            if (fadeRoot != null && fadeGroup != null)
            {
                return;
            }

            fadeRoot = new GameObject("RitualSceneFadeCanvas", typeof(Canvas), typeof(CanvasGroup));
            fadeRoot.transform.SetParent(transform, false);
            var canvas = fadeRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            fadeGroup = fadeRoot.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = true;

            var veil = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(fadeRoot.transform, false);
            var rect = (RectTransform)veil.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = veil.GetComponent<Image>();
            image.color = VeilColor;
            image.raycastTarget = true;

            fadeRoot.SetActive(false);
        }

        private IEnumerator FadeCurtain(float from, float to, float duration)
        {
            EnsureFadeCurtain();
            fadeRoot.SetActive(true);
            fadeGroup.blocksRaycasts = true;
            fadeGroup.alpha = from;
            IsFading = true;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);
                fadeGroup.alpha = Mathf.Lerp(from, to, eased);
                yield return null;
            }

            fadeGroup.alpha = to;
        }

        private IEnumerator FadeInAndHide(float duration)
        {
            yield return FadeCurtain(1f, 0f, duration);
            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = false;
            }
            if (fadeRoot != null)
            {
                fadeRoot.SetActive(false);
            }
            IsFading = false;
            fadeInRoutine = null;
        }
    }
}
