using System.Linq;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase17RitualAuraMotionBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 17 Ritual Aura Motion Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 17 Ritual Aura Motion Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 17 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 17 ritual aura motion bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = FindSceneObject("Phase16_RitualAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 17 bootstrap skipped ReadingRoom because Phase16_RitualAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var motion = EnsureMotionController(root);
            SetSerializedReference(motion, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedReference(motion, "motionRoot", root.transform);
            SetSerializedArrayReferences(motion, "glowPulsers", FindChild(root, "Phase16_GlowPool"));
            SetSerializedArrayReferences(motion, "runeRings", FindChild(root, "Phase16_RuneRingOuter"), FindChild(root, "Phase16_RuneRingInner"));
            SetSerializedArrayReferences(
                motion,
                "particleAnchors",
                FindChild(root, "Phase16_ParticleAnchorNorth"),
                FindChild(root, "Phase16_ParticleAnchorEast"),
                FindChild(root, "Phase16_ParticleAnchorSouth"),
                FindChild(root, "Phase16_ParticleAnchorWest"));
            SetSerializedBool(motion, "animateOnEnable", true);
            SetSerializedFloat(motion, "runeRotationSpeedDegrees", 7.5f);
            SetSerializedFloat(motion, "pulseSpeed", 0.55f);
            SetSerializedFloat(motion, "pulseAmplitude", 0.08f);
            SetSerializedFloat(motion, "particleFloatSpeed", 0.75f);
            SetSerializedFloat(motion, "particleFloatAmplitude", 0.018f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = FindSceneObject("Phase16_ResultAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 17 bootstrap skipped Result because Phase16_ResultAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var motion = EnsureMotionController(root);
            SetSerializedReference(motion, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedReference(motion, "motionRoot", root.transform);
            SetSerializedArrayReferences(motion, "glowPulsers", FindChild(root, "Phase16_ResultGlowPool"));
            SetSerializedArrayReferences(motion, "runeRings", FindChild(root, "Phase16_ResultRuneRing"));
            SetSerializedArrayReferences(motion, "particleAnchors", FindChild(root, "Phase16_ResultParticleAnchorLeft"), FindChild(root, "Phase16_ResultParticleAnchorRight"));
            SetSerializedBool(motion, "animateOnEnable", true);
            SetSerializedFloat(motion, "runeRotationSpeedDegrees", 4.5f);
            SetSerializedFloat(motion, "pulseSpeed", 0.45f);
            SetSerializedFloat(motion, "pulseAmplitude", 0.06f);
            SetSerializedFloat(motion, "particleFloatSpeed", 0.65f);
            SetSerializedFloat(motion, "particleFloatAmplitude", 0.012f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static RitualAuraMotionController EnsureMotionController(GameObject root)
        {
            var controller = root.GetComponent<RitualAuraMotionController>();
            if (controller == null)
            {
                controller = root.AddComponent<RitualAuraMotionController>();
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Transform FindChild(GameObject root, string childName)
        {
            return root != null ? root.transform.Find(childName) : null;
        }

        private static GameObject FindSceneObject(string name)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == name && candidate.scene == activeScene && !EditorUtility.IsPersistent(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

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

        private static void SetSerializedArrayReferences(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"Missing serialized array property {propertyName} on {target.name}");
                return;
            }

            var references = values.Where(value => value != null).ToArray();
            property.arraySize = references.Length;
            for (var i = 0; i < references.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedBool(UnityEngine.Object target, string propertyName, bool value)
        {
            if (target == null)
            {
                return;
            }

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

        private static void SetSerializedFloat(UnityEngine.Object target, string propertyName, float value)
        {
            if (target == null)
            {
                return;
            }

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
