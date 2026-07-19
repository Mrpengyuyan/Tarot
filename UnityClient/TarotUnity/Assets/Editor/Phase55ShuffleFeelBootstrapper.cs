using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 55 completes the card's motion-language chain. The flip got its weight
    /// in Phase 52 and the deal its landing in Phase 54, but the shuffle - the very
    /// first beat of the ritual - was still sound and dust over a perfectly still
    /// deck. This adds DeckShuffleChoreographer to the Midnight Parlor deck stack
    /// (MP_DeckStack, Phase 38's staggered pile) and wires it into
    /// ReadingRoomController so the draw plays it on the shuffle cue: press down,
    /// riffle ripple up the stack, square-up squash with a camera kick, exact settle.
    /// </summary>
    public static class Phase55ShuffleFeelBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string StackPath = "DeckStack/MP_DeckStack";

        [MenuItem("Tools/Tarot Unity/Run Phase 55 - Shuffle Feel")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var stack = GameObject.Find(StackPath);
            if (stack == null)
            {
                Debug.LogError($"Phase 55: {StackPath} missing.");
                return;
            }

            var choreographer = stack.GetComponent<DeckShuffleChoreographer>();
            if (choreographer == null)
            {
                choreographer = stack.AddComponent<DeckShuffleChoreographer>();
                Debug.Log("Phase 55: added DeckShuffleChoreographer to MP_DeckStack.");
            }

            var room = Object.FindFirstObjectByType<ReadingRoomController>(FindObjectsInactive.Include);
            if (room == null)
            {
                Debug.LogError("Phase 55: ReadingRoomController missing.");
                return;
            }

            var so = new SerializedObject(room);
            var field = so.FindProperty("deckShuffle");
            if (field == null)
            {
                Debug.LogError("Phase 55: ReadingRoomController.deckShuffle field missing.");
                return;
            }

            field.objectReferenceValue = choreographer;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 55: shuffle choreographer wired into the ReadingRoom draw flow.");
        }
    }
}
