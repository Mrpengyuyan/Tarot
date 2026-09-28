using System.Linq;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 72 wires the draw ritual: the fan the player picks from (MP_SpreadFan with its
    /// FanCenter), the draw camera poses (near for one and three cards, far for the Celtic
    /// Cross), the reading-room controller's new references and the UI it hides while the
    /// player picks, and the flight trail on the card prefab. Idempotent.
    /// </summary>
    public static class Phase72DrawRitualBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string WarmGlowPath = "Assets/Art/MidnightParlor/Materials/MP_WarmGlow.mat";

        // The fan lies between the player and the lowest Celtic slot (z -1.2, bottom edge -1.75).
        public static readonly Vector3 FanCenterPosition = new Vector3(0f, 0.13f, -2.45f);
        public static readonly Vector3 NearPosePosition = new Vector3(0f, 5.0f, -6.3f);
        public static readonly Vector3 NearPoseEuler = new Vector3(50f, 0f, 0f);
        public const float NearPoseFov = 45f;
        public static readonly Vector3 CelticPosePosition = new Vector3(0.7f, 6.8f, -7.0f);
        public static readonly Vector3 CelticPoseEuler = new Vector3(45f, 0f, 0f);
        public const float CelticPoseFov = 50f;

        // The fan's own light pool, in the table pool's colour; the fan fades it in and out.
        public static readonly Vector3 FanLightPosition = new Vector3(0f, 4.2f, -3.2f);
        public static readonly Vector3 FanLightTarget = new Vector3(0f, 0.13f, -2.7f);

        private static readonly string[] PickHiddenNames =
        {
            "Phase11_ActionDock", "QuestionInput", "OneCardButton", "ThreeCardButton", "CelticCrossButton", "DrawButton",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 72 Draw Ritual Bootstrap")]
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

            var glow = AssetDatabase.LoadAssetAtPath<Material>(WarmGlowPath);
            if (glow == null)
            {
                Debug.LogError("Phase 72: MP_WarmGlow missing; run the Phase 37 bootstrap first.");
                return;
            }

            AddFlightTrail(glow);
            WireScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 72 draw ritual bootstrap complete.");
        }

        private static void AddFlightTrail(Material glow)
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var existing = root.transform.Find("FlightTrail");
                var go = existing != null ? existing.gameObject : new GameObject("FlightTrail");
                if (existing == null)
                {
                    go.transform.SetParent(root.transform, false);
                }

                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var trail = go.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = go.AddComponent<TrailRenderer>();
                }

                trail.emitting = false;
                trail.time = 0.3f;
                trail.minVertexDistance = 0.02f;
                trail.widthMultiplier = 1f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0f));
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.78f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
                    new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = gradient;
                trail.sharedMaterial = glow;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.alignment = LineAlignment.View;

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>(FindObjectsInactive.Include);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (room == null || choreography == null || indicator == null || canvas == null)
            {
                Debug.LogError("Phase 72: ReadingRoom is missing its controller, camera choreography, step indicator or canvas.");
                return;
            }

            // The fan.
            var fanGo = GameObject.Find("MP_SpreadFan");
            if (fanGo == null)
            {
                fanGo = new GameObject("MP_SpreadFan");
            }

            fanGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var fan = fanGo.GetComponent<SpreadFanController>();
            if (fan == null)
            {
                fan = fanGo.AddComponent<SpreadFanController>();
            }

            var center = Child(fanGo.transform, "FanCenter");
            center.SetPositionAndRotation(FanCenterPosition, Quaternion.identity);
            var fanSo = new SerializedObject(fan);
            fanSo.FindProperty("cardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
            fanSo.FindProperty("fanCenter").objectReferenceValue = center;
            fanSo.FindProperty("fanLight").objectReferenceValue = FanLight(fanGo.transform);
            fanSo.ApplyModifiedPropertiesWithoutUndo();

            // The draw camera poses.
            var near = Child(choreography.transform, "DrawPoseNear");
            near.SetPositionAndRotation(NearPosePosition, Quaternion.Euler(NearPoseEuler));
            var celtic = Child(choreography.transform, "DrawPoseCeltic");
            celtic.SetPositionAndRotation(CelticPosePosition, Quaternion.Euler(CelticPoseEuler));
            var camSo = new SerializedObject(choreography);
            var poses = camSo.FindProperty("drawPoses");
            poses.arraySize = 3;
            SetPose(poses.GetArrayElementAtIndex(0), 1, near, NearPoseFov);
            SetPose(poses.GetArrayElementAtIndex(1), 3, near, NearPoseFov);
            SetPose(poses.GetArrayElementAtIndex(2), 10, celtic, CelticPoseFov);
            camSo.ApplyModifiedPropertiesWithoutUndo();

            // The controller's references and the UI hidden while picking.
            var groups = PickHiddenNames.Select(n =>
            {
                var t = canvas.transform.Find(n);
                var group = t.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = t.gameObject.AddComponent<CanvasGroup>();
                }

                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
                EditorUtility.SetDirty(group);
                return group;
            }).ToArray();

            var roomSo = new SerializedObject(room);
            roomSo.FindProperty("spreadFan").objectReferenceValue = fan;
            roomSo.FindProperty("stepIndicator").objectReferenceValue = indicator;
            var hidden = roomSo.FindProperty("pickHiddenUi");
            hidden.arraySize = groups.Length;
            for (var i = 0; i < groups.Length; i++)
            {
                hidden.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];
            }

            roomSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Light FanLight(Transform fan)
        {
            var t = Child(fan, "FanLight");
            t.position = FanLightPosition;
            t.rotation = Quaternion.LookRotation(FanLightTarget - FanLightPosition, Vector3.forward);
            var light = t.GetComponent<Light>();
            if (light == null)
            {
                light = t.gameObject.AddComponent<Light>();
            }

            light.type = LightType.Spot;
            light.color = new Color(1f, 0.8f, 0.58f);
            light.intensity = 0f;
            light.range = 8f;
            light.spotAngle = 70f;
            light.innerSpotAngle = 36f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            EditorUtility.SetDirty(light);
            return light;
        }

        private static void SetPose(SerializedProperty entry, int cardCount, Transform pose, float fov)
        {
            entry.FindPropertyRelative("cardCount").intValue = cardCount;
            entry.FindPropertyRelative("pose").objectReferenceValue = pose;
            entry.FindPropertyRelative("fov").floatValue = fov;
        }

        private static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }

            return t;
        }
    }
}
