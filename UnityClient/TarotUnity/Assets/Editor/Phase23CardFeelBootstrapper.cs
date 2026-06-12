using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 23 adds Hearthstone-style card physicality (hover lift and tilt)
    /// and applies research-backed rhythm tuning: interaction feedback near
    /// 100ms, flip punch aftermath shortened to match the flip animation, and
    /// a slightly wider deal interval so each landing card reads as a beat.
    /// </summary>
    public static class Phase23CardFeelBootstrapper
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 23 Card Feel Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 23 Card Feel Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 23 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeCardPrefab();
            TuneReadingRoomRhythm();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 23 card feel bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"Phase 23 bootstrap could not open card prefab at {CardPrefabPath}.");
                return;
            }

            try
            {
                var hover = prefabRoot.GetComponent<CardHoverTiltController>();
                if (hover == null)
                {
                    hover = prefabRoot.AddComponent<CardHoverTiltController>();
                }

                SetFloat(hover, "hoverLift", 0.045f);
                SetFloat(hover, "maxTiltDegrees", 7f);
                SetFloat(hover, "responseSeconds", 0.06f);
                SetFloat(hover, "cardHalfWidth", 0.35f);
                SetFloat(hover, "cardHalfLength", 0.49f);

                var flip = prefabRoot.GetComponent<CardFlipController>();
                if (flip != null)
                {
                    SetFloat(flip, "liftDuringFlip", 0.16f);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void TuneReadingRoomRhythm()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var choreographyRoot = GameObject.Find("ReadingRoomCameraChoreography");
            var choreography = choreographyRoot != null
                ? choreographyRoot.GetComponent<CameraChoreographyController>()
                : null;
            if (choreography != null)
            {
                // Aftermath shortened so the punch settles with the flip
                // instead of lingering past it; the return eases out slightly
                // faster than the lean-in, matching exit-vs-enter guidance.
                SetFloat(choreography, "punchHoldSeconds", 0.32f);
                SetFloat(choreography, "punchReturnSeconds", 0.4f);
            }
            else
            {
                Debug.LogWarning("Phase 23 bootstrap found no CameraChoreographyController in ReadingRoom.");
            }

            var deck = GameObject.Find("DeckStack");
            var deckController = deck != null ? deck.GetComponent<DeckController>() : null;
            if (deckController != null)
            {
                // Phase 9 set a deliberate "weightier deal" floor of 0.16; this
                // widens the beat slightly so each landing card reads on its own.
                SetFloat(deckController, "dealInterval", 0.18f);
            }
            else
            {
                Debug.LogWarning("Phase 23 bootstrap found no DeckController on DeckStack.");
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized float property {propertyName} on {target.name}");
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
