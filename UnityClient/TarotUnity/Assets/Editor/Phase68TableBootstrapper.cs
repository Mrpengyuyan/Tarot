using System;
using System.Collections.Generic;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 68 stages the reading-room table for the poses the player actually sits at.
    /// 1. Only the selected spread's sockets show (SpreadSocketVisibility). The scene is saved
    ///    in the state the room opens in - one card selected - so the editor matches the game.
    /// 2. MP_TablePool, a soft spot straight above the card row, makes the play area the
    ///    brightest part of the cloth. It reads as a lamp hanging above the table, out of
    ///    frame: a deliberate, single exception to Phase 49's candles-only room, which removed
    ///    seven lights that washed the table flat. MP_RoomFill stays as it is; it is what lets
    ///    gold read as gold at the edges of the frame, the deck included.
    /// Re-runnable. Running Phase 49 again does not touch MP_TablePool.
    /// </summary>
    public static class Phase68TableBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string PoolName = "MP_TablePool";

        // Spec section 4 starting values; tuned against the Phase 68 captures.
        private static readonly Vector3 PoolPosition = new Vector3(0f, 4.0f, 0.3f);
        private const float PoolInnerAngle = 40f;
        private const float PoolOuterAngle = 75f;
        private const float PoolRange = 8f;
        private const float PoolIntensity = 60f;
        private static readonly Color PoolColor = new Color(1f, 0.8f, 0.58f, 1f);

        private static readonly (int CardCount, string[] Sockets)[] SocketSets =
        {
            (1, new[] { "MP_Socket_OneCardSlot" }),
            (3, new[] { "MP_Socket_PastSlot", "MP_Socket_PresentSlot", "MP_Socket_AdviceSlot" }),
            (10, new[]
            {
                "MP_Socket_Celtic_00", "MP_Socket_Celtic_01", "MP_Socket_Celtic_02", "MP_Socket_Celtic_03",
                "MP_Socket_Celtic_04", "MP_Socket_Celtic_05", "MP_Socket_Celtic_06", "MP_Socket_Celtic_07",
                "MP_Socket_Celtic_08", "MP_Socket_Celtic_09",
            }),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 68 Table Bootstrap")]
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

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var stage = GameObject.Find("MP_TableStage");
            if (stage == null)
            {
                throw new InvalidOperationException("MP_TableStage is missing - run the Phase 38 bootstrapper first.");
            }

            WireSocketVisibility(scene, stage);
            StageTablePool(stage);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 68 table bootstrap complete.");
        }

        private static void WireSocketVisibility(Scene scene, GameObject stage)
        {
            var flow = UnityEngine.Object.FindFirstObjectByType<ReadingFlowController>();
            if (flow == null)
            {
                throw new InvalidOperationException("No ReadingFlowController in the reading room.");
            }

            var byName = IndexScene(scene);
            var visibility = stage.GetComponent<SpreadSocketVisibility>();
            if (visibility == null)
            {
                visibility = stage.AddComponent<SpreadSocketVisibility>();
            }

            var so = new SerializedObject(visibility);
            so.FindProperty("flowController").objectReferenceValue = flow;
            var sets = so.FindProperty("socketSets");
            sets.arraySize = SocketSets.Length;
            for (var i = 0; i < SocketSets.Length; i++)
            {
                var (cardCount, names) = SocketSets[i];
                var element = sets.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cardCount").intValue = cardCount;
                var sockets = element.FindPropertyRelative("sockets");
                sockets.arraySize = names.Length;
                for (var j = 0; j < names.Length; j++)
                {
                    if (!byName.TryGetValue(names[j], out var socket))
                    {
                        throw new InvalidOperationException($"{names[j]} is missing from the reading room.");
                    }

                    sockets.GetArrayElementAtIndex(j).objectReferenceValue = socket;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save the scene the way the room opens: ReadingRoomController.Start selects one card.
            visibility.Apply(1);
            EditorUtility.SetDirty(visibility);
        }

        // Sockets hidden by an earlier run are inactive, and GameObject.Find skips inactive objects.
        private static Dictionary<string, GameObject> IndexScene(Scene scene)
        {
            var byName = new Dictionary<string, GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!byName.ContainsKey(t.name))
                    {
                        byName[t.name] = t.gameObject;
                    }
                }
            }

            return byName;
        }

        private static void StageTablePool(GameObject stage)
        {
            var existing = stage.transform.Find(PoolName);
            var go = existing != null ? existing.gameObject : new GameObject(PoolName);
            go.transform.SetParent(stage.transform, false);
            go.transform.localPosition = PoolPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = go.GetComponent<Light>();
            if (light == null)
            {
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Spot;
            light.innerSpotAngle = PoolInnerAngle;
            light.spotAngle = PoolOuterAngle;
            light.range = PoolRange;
            light.intensity = PoolIntensity;
            light.color = PoolColor;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(go);
        }
    }
}
