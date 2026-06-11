using System.Linq;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class Phase3PresentationBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string DeckPrefabPath = "Assets/Prefabs/Gameplay/PF_DeckStack.prefab";
        private const string SparkMaterialPath = "Assets/Materials/MAT_RitualSpark.mat";
        private const string GlowMaterialPath = "Assets/Materials/MAT_RitualGlow.mat";

        [MenuItem("Tools/Tarot Unity/Run Phase 3 Presentation Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 3 Presentation Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            EnsureFolders();
            CreatePresentationMaterials();
            EnhanceCardPrefab();
            EnhanceDeckPrefab();
            EnhanceReadingRoomScene();
            EnhanceResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 3 presentation bootstrap complete.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "VFX");
            EnsureFolder("Assets", "Audio");
            EnsureFolder("Assets/Scripts", "Presentation");
        }

        private static void CreatePresentationMaterials()
        {
            CreateMaterial(SparkMaterialPath, new Color(1f, 0.78f, 0.34f, 1f));
            CreateMaterial(GlowMaterialPath, new Color(0.36f, 0.58f, 0.94f, 1f));
        }

        private static void EnhanceCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var flip = root.GetComponent<CardFlipController>();
                if (flip == null)
                {
                    flip = root.AddComponent<CardFlipController>();
                }
                SetFloat(flip, "flipDuration", 0.68f);
                SetFloat(flip, "anticipationPause", 0.12f);
                SetFloat(flip, "faceRevealPause", 0.08f);
                SetEnum(flip, "flipCue", (int)PresentationCueId.CardFlipped);

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnhanceDeckPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DeckPrefabPath);
            try
            {
                var deck = root.GetComponent<DeckController>();
                if (deck != null)
                {
                    SetFloat(deck, "dealDuration", 0.52f);
                    SetFloat(deck, "dealInterval", 0.18f);
                    SetFloat(deck, "dealArcHeight", 0.55f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, DeckPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnhanceReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.012f, 0.01f, 0.022f);
            }

            RenderSettings.ambientLight = new Color(0.10f, 0.08f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.012f, 0.01f, 0.022f);
            RenderSettings.fogDensity = 0.018f;

            var deck = Object.FindFirstObjectByType<DeckController>();
            var cameraChoreography = EnsureCameraChoreography(
                "ReadingRoomCameraChoreography",
                camera,
                new[]
                {
                    CreatePoseSpec("DefaultPose", new Vector3(0f, 6.1f, -5.8f), new Vector3(0f, 0f, 0.15f)),
                    CreatePoseSpec("DeckPose", new Vector3(-1.7f, 4.4f, -4.1f), new Vector3(-2.6f, 0.1f, 0.08f)),
                    CreatePoseSpec("OneCardPose", new Vector3(0f, 4.3f, -4.2f), new Vector3(0f, 0.08f, 0.15f)),
                    CreatePoseSpec("ThreeCardPose", new Vector3(0f, 4.8f, -4.8f), new Vector3(0f, 0.08f, 0.15f)),
                    CreatePoseSpec("ResultPose", new Vector3(0f, 5.2f, -4.9f), new Vector3(0f, 0.15f, 0.15f)),
                });

            var feedback = EnsureRitualFeedback("ReadingRoomRitualFeedback");
            var deckPosition = deck != null ? deck.transform.position : new Vector3(-2.9f, 0.2f, 0.1f);
            var shuffleParticles = ConfigureParticles(
                "ShuffleParticles",
                feedback.transform,
                deckPosition + Vector3.up * 0.3f,
                0.9f,
                34,
                0.22f,
                new Color(0.93f, 0.72f, 0.32f, 0.95f));
            var dealParticles = ConfigureParticles(
                "DealParticles",
                feedback.transform,
                new Vector3(0f, 0.35f, 0.15f),
                0.7f,
                18,
                0.14f,
                new Color(0.48f, 0.70f, 1f, 0.85f));
            var flipParticles = ConfigureParticles(
                "FlipParticles",
                feedback.transform,
                new Vector3(0f, 0.45f, 0.15f),
                0.55f,
                24,
                0.12f,
                new Color(1f, 0.86f, 0.42f, 0.95f));
            var resultParticles = ConfigureParticles(
                "ResultReadyParticles",
                feedback.transform,
                new Vector3(0f, 0.55f, 0.15f),
                1.2f,
                44,
                0.2f,
                new Color(0.62f, 0.86f, 1f, 0.9f));
            ConfigureFeedback(feedback, shuffleParticles, dealParticles, flipParticles, resultParticles);

            ConfigureLight("Ritual Ember Light", new Vector3(-2.6f, 1.3f, -0.3f), new Color(1f, 0.64f, 0.31f), 1.8f, 4.2f);
            ConfigureLight("Moon Fill Light", new Vector3(2.4f, 2.2f, -1.2f), new Color(0.45f, 0.62f, 1f), 1.1f, 5.2f);

            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            if (room != null)
            {
                SetObject(room, "cameraChoreography", cameraChoreography);
                SetObject(room, "ritualFeedback", feedback);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void EnhanceResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 1.6f, -7.6f);
                camera.transform.LookAt(new Vector3(0f, 0.25f, 0f));
                camera.backgroundColor = new Color(0.014f, 0.012f, 0.026f);
            }

            RenderSettings.ambientLight = new Color(0.12f, 0.10f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.014f, 0.012f, 0.026f);
            RenderSettings.fogDensity = 0.012f;

            var cameraChoreography = EnsureCameraChoreography(
                "ResultCameraChoreography",
                camera,
                new[]
                {
                    CreatePoseSpec("DefaultPose", new Vector3(0f, 1.6f, -7.6f), new Vector3(0f, 0.25f, 0f)),
                    CreatePoseSpec("DeckPose", new Vector3(0f, 1.6f, -7.6f), new Vector3(0f, 0.25f, 0f)),
                    CreatePoseSpec("OneCardPose", new Vector3(0f, 1.6f, -7.6f), new Vector3(0f, 0.25f, 0f)),
                    CreatePoseSpec("ThreeCardPose", new Vector3(0f, 1.6f, -7.6f), new Vector3(0f, 0.25f, 0f)),
                    CreatePoseSpec("ResultPose", new Vector3(0f, 1.42f, -7.1f), new Vector3(0f, 0.2f, 0f)),
                });

            var feedback = EnsureRitualFeedback("ResultRitualFeedback");
            var resultParticles = ConfigureParticles(
                "ResultRevealParticles",
                feedback.transform,
                new Vector3(0f, 0.55f, -0.5f),
                1.4f,
                55,
                0.24f,
                new Color(0.78f, 0.90f, 1f, 0.9f));
            ConfigureFeedback(feedback, resultParticles, resultParticles, resultParticles, resultParticles);

            ConfigureLight("Result Halo Light", new Vector3(0f, 1.4f, -2.2f), new Color(0.72f, 0.84f, 1f), 1.6f, 6f);

            var canvas = GameObject.Find("ResultCanvas");
            ResultRevealDirector revealDirector = null;
            if (canvas != null)
            {
                revealDirector = canvas.GetComponent<ResultRevealDirector>();
                if (revealDirector == null)
                {
                    revealDirector = canvas.AddComponent<ResultRevealDirector>();
                }
            }

            if (canvas != null && revealDirector != null)
            {
                var groups = new[]
                    {
                        "QuestionText",
                        "SpreadNameText",
                        "SummaryText",
                        "OverallText",
                        "CardAnalysisText",
                        "AdviceText",
                        "WarningText",
                    }
                    .Select(name => canvas.transform.Find(name))
                    .Where(child => child != null)
                    .Select(EnsureCanvasGroup)
                    .ToArray();

                SetObjectArray(revealDirector, "revealGroups", groups);
                SetFloat(revealDirector, "firstDelay", 0.15f);
                SetFloat(revealDirector, "groupInterval", 0.12f);
                SetFloat(revealDirector, "fadeDuration", 0.22f);
            }

            var controller = Object.FindFirstObjectByType<ResultSceneController>();
            if (controller != null)
            {
                SetObject(controller, "revealDirector", revealDirector);
                SetObject(controller, "ritualFeedback", feedback);
                SetObject(controller, "cameraChoreography", cameraChoreography);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static CameraChoreographyController EnsureCameraChoreography(string name, Camera camera, PoseSpec[] poses)
        {
            var root = FindOrCreateRoot(name);
            var choreography = root.GetComponent<CameraChoreographyController>();
            if (choreography == null)
            {
                choreography = root.AddComponent<CameraChoreographyController>();
            }
            var defaultPose = EnsurePose(root.transform, poses[0]);
            var deckPose = EnsurePose(root.transform, poses[1]);
            var oneCardPose = EnsurePose(root.transform, poses[2]);
            var threeCardPose = EnsurePose(root.transform, poses[3]);
            var resultPose = EnsurePose(root.transform, poses[4]);

            SetObject(choreography, "targetCamera", camera);
            SetObject(choreography, "defaultPose", defaultPose);
            SetObject(choreography, "deckPose", deckPose);
            SetObject(choreography, "oneCardPose", oneCardPose);
            SetObject(choreography, "threeCardPose", threeCardPose);
            SetObject(choreography, "resultPose", resultPose);
            SetFloat(choreography, "transitionDuration", 0.72f);
            return choreography;
        }

        private static RitualFeedbackController EnsureRitualFeedback(string name)
        {
            var root = FindOrCreateRoot(name);
            var feedback = root.GetComponent<RitualFeedbackController>();
            if (feedback == null)
            {
                feedback = root.AddComponent<RitualFeedbackController>();
            }

            return feedback;
        }

        private static ParticleSystem ConfigureParticles(
            string name,
            Transform parent,
            Vector3 position,
            float lifetime,
            int burstCount,
            float radius,
            Color color)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var child = new GameObject(name, typeof(ParticleSystem));
            child.transform.SetParent(parent);
            child.transform.position = position;
            var particles = child.GetComponent<ParticleSystem>();

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.2f, lifetime);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(64, burstCount * 2);

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return particles;
        }

        private static void ConfigureFeedback(
            RitualFeedbackController feedback,
            ParticleSystem shuffleParticles,
            ParticleSystem dealParticles,
            ParticleSystem flipParticles,
            ParticleSystem resultParticles)
        {
            SetObject(feedback, "shuffleParticles", shuffleParticles);
            SetObject(feedback, "dealParticles", dealParticles);
            SetObject(feedback, "flipParticles", flipParticles);
            SetObject(feedback, "resultParticles", resultParticles);
            SetBool(feedback, "moveParticlesToAnchor", true);
        }

        private static void ConfigureLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = FindOrCreateRoot(name);
            lightObject.transform.position = position;
            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static Transform EnsurePose(Transform parent, PoseSpec poseSpec)
        {
            var pose = FindOrCreateChild(poseSpec.name, parent);
            pose.transform.position = poseSpec.position;
            pose.transform.LookAt(poseSpec.lookAt);
            return pose.transform;
        }

        private static PoseSpec CreatePoseSpec(string name, Vector3 position, Vector3 lookAt)
        {
            return new PoseSpec(name, position, lookAt);
        }

        private static GameObject FindOrCreateRoot(string name)
        {
            var root = GameObject.Find(name);
            return root != null ? root : new GameObject(name);
        }

        private static CanvasGroup EnsureCanvasGroup(Transform child)
        {
            var group = child.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = child.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static GameObject FindOrCreateChild(string name, Transform parent)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            var childObject = new GameObject(name);
            childObject.transform.SetParent(parent);
            return childObject;
        }

        private static void CreateMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }

            EditorUtility.SetDirty(material);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private readonly struct PoseSpec
        {
            public readonly string name;
            public readonly Vector3 position;
            public readonly Vector3 lookAt;

            public PoseSpec(string name, Vector3 position, Vector3 lookAt)
            {
                this.name = name;
                this.position = position;
                this.lookAt = lookAt;
            }
        }
    }
}
