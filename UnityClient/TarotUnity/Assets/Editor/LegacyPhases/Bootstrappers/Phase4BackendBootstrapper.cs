using TarotUnity.Network;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase4BackendBootstrapper
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 4 Backend Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 4 Backend Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            EnhanceBootScene();
            EnhanceReadingRoomScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 4 backend bootstrap complete.");
        }

        private static void EnhanceBootScene()
        {
            EditorSceneManager.OpenScene(BootScenePath);

            var bootstrapper = GameObject.Find("Bootstrapper") ?? new GameObject("Bootstrapper");
            var apiClient = bootstrapper.GetComponent<ApiClient>();
            if (apiClient == null)
            {
                apiClient = bootstrapper.AddComponent<ApiClient>();
            }

            SetString(apiClient, "baseUrl", "http://localhost:8000/api/v1");
            EditorUtility.SetDirty(bootstrapper);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), BootScenePath);
        }

        private static void EnhanceReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var networkRoot = GameObject.Find("BackendServices") ?? new GameObject("BackendServices");
            var apiClient = networkRoot.GetComponent<ApiClient>();
            if (apiClient == null)
            {
                apiClient = networkRoot.AddComponent<ApiClient>();
            }

            SetString(apiClient, "baseUrl", "http://localhost:8000/api/v1");

            var backendReadingService = networkRoot.GetComponent<BackendReadingService>();
            if (backendReadingService == null)
            {
                backendReadingService = networkRoot.AddComponent<BackendReadingService>();
            }

            SetObject(backendReadingService, "apiClient", apiClient);

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            if (room != null)
            {
                SetObject(room, "apiClient", apiClient);
                SetObject(room, "backendReadingService", backendReadingService);
                SetEnum(room, "backendMode", (int)BackendIntegrationMode.BackendWithLocalFallback);
            }

            EditorUtility.SetDirty(networkRoot);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
