using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase19CardActionVfxBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 19 Card Action VFX Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 19 Card Action VFX Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 19 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 19 card action VFX bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            WireActionVfx(
                feedbackName: "ReadingRoomRitualFeedback",
                particleRootName: "Phase16_RitualAuraRoot",
                playAmbientOnShuffle: true,
                playAmbientOnDeal: true,
                burstOnFlip: true,
                burstOnResult: true,
                shuffleIntensity: 0.76f,
                dealIntensity: 0.66f,
                flipIntensity: 0.95f,
                resultIntensity: 0.72f);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            WireActionVfx(
                feedbackName: "ResultRitualFeedback",
                particleRootName: "Phase16_ResultAuraRoot",
                playAmbientOnShuffle: false,
                playAmbientOnDeal: false,
                burstOnFlip: false,
                burstOnResult: true,
                shuffleIntensity: 0.44f,
                dealIntensity: 0.44f,
                flipIntensity: 0.52f,
                resultIntensity: 0.58f);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static void WireActionVfx(
            string feedbackName,
            string particleRootName,
            bool playAmbientOnShuffle,
            bool playAmbientOnDeal,
            bool burstOnFlip,
            bool burstOnResult,
            float shuffleIntensity,
            float dealIntensity,
            float flipIntensity,
            float resultIntensity)
        {
            var feedbackObject = FindSceneObject(feedbackName);
            if (feedbackObject == null)
            {
                Debug.LogWarning($"Phase 19 bootstrap skipped {feedbackName} because it is missing.");
                return;
            }

            var feedback = feedbackObject.GetComponent<RitualFeedbackController>();
            if (feedback == null)
            {
                Debug.LogWarning($"Phase 19 bootstrap skipped {feedbackName} because RitualFeedbackController is missing.");
                return;
            }

            var particleRoot = FindSceneObject(particleRootName);
            if (particleRoot == null)
            {
                Debug.LogWarning($"Phase 19 bootstrap skipped {feedbackName} because {particleRootName} is missing. Run Phase 16 and Phase 18 bootstraps first.");
                return;
            }

            var particleController = particleRoot.GetComponent<RitualParticleSystemController>();
            if (particleController == null)
            {
                Debug.LogWarning($"Phase 19 bootstrap skipped {feedbackName} because {particleRootName} has no RitualParticleSystemController. Run Phase 18 bootstrap first.");
                return;
            }

            var actionVfx = feedbackObject.GetComponent<RitualActionVfxController>();
            if (actionVfx == null)
            {
                actionVfx = feedbackObject.AddComponent<RitualActionVfxController>();
            }

            SetSerializedReference(feedback, "actionVfxController", actionVfx);
            SetSerializedBool(feedback, "forwardCuesToActionVfx", true);

            SetSerializedReference(actionVfx, "particleSystemController", particleController);
            SetSerializedBool(actionVfx, "playAmbientOnShuffle", playAmbientOnShuffle);
            SetSerializedBool(actionVfx, "playAmbientOnDeal", playAmbientOnDeal);
            SetSerializedBool(actionVfx, "burstOnFlip", burstOnFlip);
            SetSerializedBool(actionVfx, "burstOnResult", burstOnResult);
            SetSerializedFloat(actionVfx, "shuffleIntensity", shuffleIntensity);
            SetSerializedFloat(actionVfx, "dealIntensity", dealIntensity);
            SetSerializedFloat(actionVfx, "flipIntensity", flipIntensity);
            SetSerializedFloat(actionVfx, "resultIntensity", resultIntensity);

            EditorUtility.SetDirty(feedbackObject);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(actionVfx);
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
