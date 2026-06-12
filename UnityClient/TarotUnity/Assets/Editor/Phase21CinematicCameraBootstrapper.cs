using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase21CinematicCameraBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MainMenuChoreographyName = "MainMenuCameraChoreography";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        // Graybox-era TextMesh labels that never had a font assigned. Without a
        // font they render as giant solid ribbons, which the seated camera now
        // exposes. Real RWS artwork replaced their purpose in Phase 13, so they
        // are deactivated (not deleted) here.
        private static readonly string[] FontlessLabelNames = { "TitleLabel", "PositionLabel" };

        // Seated-at-the-table framing. All values are derived from the measured
        // scene geometry: spread center (0, 0.12, 0.15), deck stack (-2.9, 0.12, 0.1).
        private static readonly Vector3 SpreadCenter = new(0f, 0.12f, 0.15f);

        private struct PoseFraming
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Euler;
            public float Fov;
        }

        private static readonly PoseFraming[] ReadingRoomPoses =
        {
            new() { Name = "DefaultPose", Position = new Vector3(0f, 2.45f, -3.55f), Euler = new Vector3(32.2f, 0f, 0f), Fov = 36f },
            new() { Name = "DeckPose", Position = new Vector3(-2.0f, 1.7f, -1.9f), Euler = new Vector3(35.8f, -24.2f, 0f), Fov = 30f },
            new() { Name = "OneCardPose", Position = new Vector3(0f, 1.85f, -2.6f), Euler = new Vector3(32.2f, 0f, 0f), Fov = 32f },
            new() { Name = "ThreeCardPose", Position = new Vector3(0f, 2.2f, -3.2f), Euler = new Vector3(31.8f, 0f, 0f), Fov = 36f },
            new() { Name = "ResultPose", Position = new Vector3(0f, 2.9f, -3.9f), Euler = new Vector3(34.5f, 0f, 0f), Fov = 38f },
        };

        // Camera spawn sits one step behind DefaultPose so PlayOpening eases the
        // player down into the seat instead of starting frozen.
        private static readonly Vector3 OpeningCameraPosition = new(0f, 2.85f, -4.15f);
        private static readonly Vector3 OpeningCameraEuler = new(32.4f, 0f, 0f);
        private const float OpeningCameraFov = 40f;

        [MenuItem("Tools/Tarot Unity/Run Phase 21 Cinematic Camera Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 21 Cinematic Camera Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 21 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            DeactivateFontlessCardLabels();
            UpgradeReadingRoomScene();
            UpgradeResultScene();
            UpgradeMainMenuScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 21 cinematic camera bootstrap complete.");
        }

        private static void DeactivateFontlessCardLabels()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"Phase 21 bootstrap could not open card prefab at {CardPrefabPath}.");
                return;
            }

            try
            {
                var changed = false;
                foreach (var labelName in FontlessLabelNames)
                {
                    var label = FindChildDeep(prefabRoot.transform, labelName);
                    if (label != null && label.gameObject.activeSelf)
                    {
                        label.gameObject.SetActive(false);
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, CardPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform FindChildDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindChildDeep(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var choreographyRoot = GameObject.Find("ReadingRoomCameraChoreography");
            if (choreographyRoot == null)
            {
                Debug.LogWarning("Phase 21 bootstrap skipped ReadingRoom because ReadingRoomCameraChoreography is missing.");
                return;
            }

            foreach (var framing in ReadingRoomPoses)
            {
                var pose = choreographyRoot.transform.Find(framing.Name);
                if (pose == null)
                {
                    Debug.LogWarning($"Phase 21 bootstrap missing pose {framing.Name} in ReadingRoom.");
                    continue;
                }

                pose.localPosition = framing.Position;
                pose.localEulerAngles = framing.Euler;
                EditorUtility.SetDirty(pose);
            }

            var controller = choreographyRoot.GetComponent<CameraChoreographyController>();
            if (controller != null)
            {
                SetFloat(controller, "defaultFov", 36f);
                SetFloat(controller, "deckFov", 30f);
                SetFloat(controller, "oneCardFov", 32f);
                SetFloat(controller, "threeCardFov", 36f);
                SetFloat(controller, "resultFov", 38f);
                SetFloat(controller, "transitionDuration", 0.9f);
                SetBool(controller, "breathingEnabled", true);
                SetFloat(controller, "breathPositionAmplitude", 0.03f);
                SetFloat(controller, "breathRotationAmplitude", 0.3f);
                SetFloat(controller, "breathFrequency", 0.16f);
                SetFloat(controller, "punchTravelDistance", 0.55f);
                SetFloat(controller, "punchFovDelta", -4f);
                SetFloat(controller, "punchInSeconds", 0.26f);
                SetFloat(controller, "punchHoldSeconds", 0.5f);
                SetFloat(controller, "punchReturnSeconds", 0.5f);
                SetFloat(controller, "punchShakeAmplitude", 0.035f);
            }

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(OpeningCameraPosition, Quaternion.Euler(OpeningCameraEuler));
                camera.fieldOfView = OpeningCameraFov;
                EditorUtility.SetDirty(camera);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var controller = Object.FindFirstObjectByType<CameraChoreographyController>();
            if (controller == null)
            {
                Debug.LogWarning("Phase 21 bootstrap skipped Result because no CameraChoreographyController exists.");
                return;
            }

            // Result keeps its tuned framing: the screen-space panels and the 3D
            // card stage were aligned against the existing camera, so only the
            // subtle breathing layer is added here.
            SetFloat(controller, "defaultFov", 60f);
            SetFloat(controller, "deckFov", 60f);
            SetFloat(controller, "oneCardFov", 60f);
            SetFloat(controller, "threeCardFov", 60f);
            SetFloat(controller, "resultFov", 60f);
            SetBool(controller, "breathingEnabled", true);
            SetFloat(controller, "breathPositionAmplitude", 0.015f);
            SetFloat(controller, "breathRotationAmplitude", 0.15f);
            SetFloat(controller, "breathFrequency", 0.12f);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static void UpgradeMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("Phase 21 bootstrap skipped MainMenu because no camera exists.");
                return;
            }

            var choreographyObject = GameObject.Find(MainMenuChoreographyName);
            if (choreographyObject == null)
            {
                choreographyObject = new GameObject(MainMenuChoreographyName);
            }

            var controller = choreographyObject.GetComponent<CameraChoreographyController>();
            if (controller == null)
            {
                controller = choreographyObject.AddComponent<CameraChoreographyController>();
            }

            SetReference(controller, "targetCamera", camera);
            SetBool(controller, "breathingEnabled", true);
            SetFloat(controller, "breathPositionAmplitude", 0.02f);
            SetFloat(controller, "breathRotationAmplitude", 0.2f);
            SetFloat(controller, "breathFrequency", 0.1f);

            EditorUtility.SetDirty(choreographyObject);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MainMenuScenePath);
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

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized bool property {propertyName} on {target.name}");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized reference property {propertyName} on {target.name}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
