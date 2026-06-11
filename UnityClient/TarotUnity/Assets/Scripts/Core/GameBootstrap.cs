using System.Collections;
using TarotUnity.Network;
using UnityEngine;

namespace TarotUnity.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSceneId firstScene = GameSceneId.MainMenu;
        [SerializeField] private bool loadFirstSceneOnStart = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureService<SceneFlowManager>();
            EnsureService<AudioManager>();
            EnsureService<ApiClient>();
            EnsureService<DesktopConfigLoader>();
            GetComponent<DesktopConfigLoader>()?.LoadAndApply();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (loadFirstSceneOnStart && SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(firstScene);
            }
        }

        private void EnsureService<T>() where T : Component
        {
            if (GetComponent<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }
    }
}
