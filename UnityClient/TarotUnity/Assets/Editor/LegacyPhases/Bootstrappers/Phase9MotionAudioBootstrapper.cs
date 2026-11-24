using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase9MotionAudioBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        [MenuItem("Tools/Tarot Unity/Run Phase 9 Motion Audio Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 9 Motion Audio Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            Phase8VisualIdentityBootstrapper.Run();
            UpgradeReadingRoomScene();
            UpgradeResultScene();
            UpgradeCardPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 9 motion and audio rhythm bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = GameObject.Find("Phase9_RhythmDirectorRoot") ?? new GameObject("Phase9_RhythmDirectorRoot");
            var director = root.GetComponent<RitualRhythmDirector>();
            if (director == null)
            {
                director = root.AddComponent<RitualRhythmDirector>();
            }

            SetFloat(director, "shuffleBreathSeconds", 0.66f);
            SetFloat(director, "dealSettleSeconds", 0.26f);
            SetFloat(director, "resultBreathSeconds", 0.42f);
            SetCueOrder(director);

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            if (controller != null)
            {
                SetObject(controller, "rhythmDirector", director);
            }

            var deck = Object.FindFirstObjectByType<DeckController>();
            if (deck != null)
            {
                SetFloat(deck, "dealDuration", 0.56f);
                SetFloat(deck, "dealInterval", 0.18f);
                SetFloat(deck, "dealArcHeight", 0.72f);
                SetFloat(deck, "dealTiltDegrees", 10.5f);
                SetFloat(deck, "postDealSettleSeconds", 0.10f);
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var reveal = Object.FindFirstObjectByType<ResultRevealDirector>();
            if (reveal != null)
            {
                SetFloat(reveal, "firstDelay", 0.34f);
                SetFloat(reveal, "groupInterval", 0.22f);
                SetFloat(reveal, "fadeDuration", 0.40f);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var flip = root.GetComponent<CardFlipController>();
                if (flip == null)
                {
                    flip = root.AddComponent<CardFlipController>();
                }

                SetFloat(flip, "flipDuration", 0.66f);
                SetFloat(flip, "anticipationPause", 0.16f);
                SetFloat(flip, "faceRevealPause", 0.12f);
                SetFloat(flip, "liftDuringFlip", 0.14f);
                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetCueOrder(Object target)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty("cueOrder");
            if (property == null)
            {
                return;
            }

            var cues = new[]
            {
                PresentationCueId.ShuffleStarted,
                PresentationCueId.CardDealt,
                PresentationCueId.CardFlipped,
                PresentationCueId.ResultReady,
                PresentationCueId.ResultReveal,
            };

            property.arraySize = cues.Length;
            for (var i = 0; i < cues.Length; i++)
            {
                property.GetArrayElementAtIndex(i).enumValueIndex = (int)cues[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
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
