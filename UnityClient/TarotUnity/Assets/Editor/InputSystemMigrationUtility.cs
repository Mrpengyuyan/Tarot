using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace TarotUnity.Editor
{
    public static class InputSystemMigrationUtility
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        [MenuItem("Tarot/Tools/Migrate UI Input To Input System")]
        public static void MigrateUiInputToInputSystem()
        {
            foreach (var scenePath in ScenePaths)
            {
                MigrateScene(scenePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Migrated tarot UI scenes to InputSystemUIInputModule.");
        }

        private static void MigrateScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var eventSystem in eventSystems)
            {
                foreach (var legacyModule in eventSystem.GetComponents<StandaloneInputModule>())
                {
                    Object.DestroyImmediate(legacyModule);
                }

                var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule == null)
                {
                    inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                if (inputModule.actionsAsset == null)
                {
                    inputModule.AssignDefaultActions();
                }

                EditorUtility.SetDirty(eventSystem);
                EditorUtility.SetDirty(inputModule);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
