using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class TarotUnityEditorShortcuts
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Tools/Tarot Unity/Open Boot Scene")]
        public static void OpenBootScene()
        {
            if (EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("Exit Play Mode before opening Boot from the editor shortcut.");
                return;
            }

            EditorSceneManager.OpenScene(BootScenePath);
        }

        [MenuItem("Tools/Tarot Unity/Play From Boot")]
        public static void PlayFromBoot()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
