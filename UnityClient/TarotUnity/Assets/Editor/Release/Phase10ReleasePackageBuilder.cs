using System.IO;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase10ReleasePackageBuilder
    {
        public const string WindowsReleaseFolder = "Builds/Desktop/Release/TarotUnity-Windows-x64";
        public const string WindowsReleaseZip = "Builds/Desktop/Release/TarotUnity-Windows-x64.zip";

        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 10 Release UX Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 10 Release UX Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            Phase9MotionAudioBootstrapper.Run();
            UpgradeReadingRoomScene();
            WriteReleaseSupportFiles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 10 release UX bootstrap complete.");
        }

        [MenuItem("Tools/Tarot Unity/Write Windows Release Support Files")]
        public static void WriteReleaseSupportFiles()
        {
            Directory.CreateDirectory(WindowsReleaseFolder);
            File.WriteAllText(Path.Combine(WindowsReleaseFolder, "README_FIRST.txt"), ReleasePackageText.BuildReadme());
            File.WriteAllText(Path.Combine(WindowsReleaseFolder, "tarot_desktop_config.example.json"), ReleasePackageText.BuildConfigExample());
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas == null)
            {
                return;
            }

            var text = EnsureText(canvas.transform, "Phase10_ReleaseStatusText");
            text.text = ReleaseUxCopy.LocalModeReady;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Italic;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.78f, 0.74f, 0.64f, 0.92f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(860f, 34f);
            rect.anchoredPosition = new Vector2(0f, -262f);

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            if (controller != null)
            {
                SetObject(controller, "releaseStatusText", text);
            }

            EditorUtility.SetDirty(text);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static Text EnsureText(Transform parent, string name)
        {
            var existing = parent.Find(name);
            var target = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Text));

            target.transform.SetParent(parent, false);
            var text = target.GetComponent<Text>();
            if (text == null)
            {
                text = target.AddComponent<Text>();
            }

            return text;
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
    }
}
