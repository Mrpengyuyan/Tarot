using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 75, the user's fourth pass on the draw. Runs after the Phase 74 bootstrap. Idempotent.
    /// - The offline reading draws from the real 78-card deck: this copies the backend's seed data
    ///   (Server/data/tarotCards.json) - just the fields the offline reading uses - into
    ///   Resources/TarotDeck/tarot_cards.json. Re-run it when the backend's deck changes; an
    ///   EditMode test fails when the two drift apart.
    /// - The shuffle is stretched from three seconds to four: every beat a third longer again.
    /// </summary>
    public static class Phase75DrawTruthBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string DeckAssetPath = "Assets/Resources/TarotDeck/tarot_cards.json";

        /// <summary>The backend's deck, from the Unity project folder (UnityClient/TarotUnity).</summary>
        public static string ServerDeckPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Server/data/tarotCards.json"));

        [MenuItem("Tools/Tarot Unity/Run Phase 75 Draw Truth Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CopyDeck();
            SlowTheShuffle();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 75 draw truth bootstrap complete.");
        }

        /// <summary>The backend's cards, read through the same fields the client keeps.</summary>
        public static TarotDeckData ReadServerDeck()
        {
            // The seed file is a bare array; JsonUtility needs an object around it.
            return JsonUtility.FromJson<TarotDeckData>("{\"cards\":" + File.ReadAllText(ServerDeckPath) + "}");
        }

        private static void CopyDeck()
        {
            var json = JsonUtility.ToJson(ReadServerDeck(), true);
            var full = Path.GetFullPath(DeckAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            if (!File.Exists(full) || File.ReadAllText(full) != json)
            {
                File.WriteAllText(full, json);
            }

            AssetDatabase.ImportAsset(DeckAssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>About four seconds: the Phase 74 three-second timing with every beat a third longer.</summary>
        private static void SlowTheShuffle()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var shuffle = new SerializedObject(Object.FindFirstObjectByType<DeckShuffleChoreographer>());
            shuffle.FindProperty("anticipationSeconds").floatValue = 0.25f;
            shuffle.FindProperty("cutSeconds").floatValue = 0.72f;
            shuffle.FindProperty("recutSeconds").floatValue = 0.52f;
            shuffle.FindProperty("riffleCardSeconds").floatValue = 0.26f;
            shuffle.FindProperty("riffleStagger").floatValue = 0.088f;
            shuffle.FindProperty("settleSeconds").floatValue = 0.22f;
            shuffle.FindProperty("squareHoldSeconds").floatValue = 0.55f;
            shuffle.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
