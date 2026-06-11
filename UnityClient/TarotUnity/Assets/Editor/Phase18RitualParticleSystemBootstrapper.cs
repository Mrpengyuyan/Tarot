using System.IO;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Editor
{
    public static class Phase18RitualParticleSystemBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const string ParticleMaterialPath = "Assets/Materials/MAT_Phase18_RitualParticles.mat";

        [MenuItem("Tools/Tarot Unity/Run Phase 18 Ritual Particle Systems Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 18 Ritual Particle Systems Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 18 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            var particleMaterial = EnsureParticleMaterial();

            UpgradeReadingRoomScene(particleMaterial);
            UpgradeResultScene(particleMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 18 ritual particle systems bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene(Material particleMaterial)
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = FindSceneObject("Phase16_RitualAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 18 bootstrap skipped ReadingRoom because Phase16_RitualAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var ambientParent = RequireChild(root, "Phase16_ParticleAnchorNorth");
            var focusParent = RequireChild(root, "Phase16_ParticleAnchorEast");
            var revealParent = RequireChild(root, "Phase16_AuraFocusAnchor");
            if (ambientParent == null || focusParent == null || revealParent == null)
            {
                return;
            }

            var ambient = EnsureParticles(
                ambientParent,
                "Phase18_AmbientDustParticles",
                Vector3.zero,
                loop: true,
                duration: 4.8f,
                rateOverTime: 9f,
                startLifetimeMin: 1.6f,
                startLifetimeMax: 3.2f,
                startSpeedMin: 0.015f,
                startSpeedMax: 0.09f,
                startSizeMin: 0.012f,
                startSizeMax: 0.035f,
                maxParticles: 96,
                shapeRadius: 0.48f,
                new Color(0.92f, 0.82f, 0.48f, 0.42f),
                particleMaterial);
            var focus = EnsureParticles(
                focusParent,
                "Phase18_DeckFocusParticles",
                Vector3.zero,
                loop: true,
                duration: 3.6f,
                rateOverTime: 6f,
                startLifetimeMin: 1.0f,
                startLifetimeMax: 2.2f,
                startSpeedMin: 0.01f,
                startSpeedMax: 0.07f,
                startSizeMin: 0.014f,
                startSizeMax: 0.045f,
                maxParticles: 72,
                shapeRadius: 0.28f,
                new Color(0.44f, 0.98f, 0.86f, 0.38f),
                particleMaterial);
            var reveal = EnsureParticles(
                revealParent,
                "Phase18_FlipSparkParticles",
                Vector3.zero,
                loop: false,
                duration: 0.95f,
                rateOverTime: 0f,
                startLifetimeMin: 0.32f,
                startLifetimeMax: 0.76f,
                startSpeedMin: 0.16f,
                startSpeedMax: 0.55f,
                startSizeMin: 0.018f,
                startSizeMax: 0.055f,
                maxParticles: 64,
                shapeRadius: 0.18f,
                new Color(1.0f, 0.82f, 0.34f, 0.72f),
                particleMaterial);

            var controller = EnsureParticleController(root);
            SetSerializedReference(controller, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedArrayReferences(controller, "ambientParticles", ambient);
            SetSerializedArrayReferences(controller, "focusParticles", focus);
            SetSerializedArrayReferences(controller, "revealParticles", reveal);
            SetSerializedBool(controller, "playOnEnable", true);
            SetSerializedFloat(controller, "baseEmissionMultiplier", 1f);
            SetSerializedFloat(controller, "focusEmissionMultiplier", 0.86f);
            SetSerializedFloat(controller, "revealEmissionMultiplier", 1.35f);
            SetSerializedFloat(controller, "intensity", 0.62f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene(Material particleMaterial)
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = FindSceneObject("Phase16_ResultAuraRoot");
            if (root == null)
            {
                Debug.LogWarning("Phase 18 bootstrap skipped Result because Phase16_ResultAuraRoot is missing. Run Phase 16 bootstrap first.");
                return;
            }

            var motesParent = RequireChild(root, "Phase16_ResultParticleAnchorLeft");
            var glowParent = RequireChild(root, "Phase16_ResultAuraFocusAnchor");
            if (motesParent == null || glowParent == null)
            {
                return;
            }

            var motes = EnsureParticles(
                motesParent,
                "Phase18_ResultCardMotes",
                Vector3.zero,
                loop: true,
                duration: 4.2f,
                rateOverTime: 5f,
                startLifetimeMin: 1.2f,
                startLifetimeMax: 2.8f,
                startSpeedMin: 0.01f,
                startSpeedMax: 0.06f,
                startSizeMin: 0.012f,
                startSizeMax: 0.034f,
                maxParticles: 64,
                shapeRadius: 0.26f,
                new Color(0.96f, 0.84f, 0.48f, 0.38f),
                particleMaterial);
            var glow = EnsureParticles(
                glowParent,
                "Phase18_ResultInterpretationGlow",
                Vector3.zero,
                loop: true,
                duration: 5.2f,
                rateOverTime: 3f,
                startLifetimeMin: 2.0f,
                startLifetimeMax: 4.5f,
                startSpeedMin: 0.005f,
                startSpeedMax: 0.035f,
                startSizeMin: 0.04f,
                startSizeMax: 0.12f,
                maxParticles: 48,
                shapeRadius: 0.34f,
                new Color(0.42f, 0.95f, 0.78f, 0.24f),
                particleMaterial);

            var controller = EnsureParticleController(root);
            SetSerializedReference(controller, "auraController", root.GetComponent<RitualAuraController>());
            SetSerializedArrayReferences(controller, "ambientParticles", motes);
            SetSerializedArrayReferences(controller, "focusParticles", glow);
            SetSerializedArrayReferences(controller, "revealParticles");
            SetSerializedBool(controller, "playOnEnable", true);
            SetSerializedFloat(controller, "baseEmissionMultiplier", 0.82f);
            SetSerializedFloat(controller, "focusEmissionMultiplier", 0.72f);
            SetSerializedFloat(controller, "revealEmissionMultiplier", 1f);
            SetSerializedFloat(controller, "intensity", 0.44f);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static ParticleSystem EnsureParticles(
            Transform parent,
            string name,
            Vector3 localPosition,
            bool loop,
            float duration,
            float rateOverTime,
            float startLifetimeMin,
            float startLifetimeMax,
            float startSpeedMin,
            float startSpeedMax,
            float startSizeMin,
            float startSizeMax,
            int maxParticles,
            float shapeRadius,
            Color color,
            Material material)
        {
            var existing = parent.Find(name);
            var target = existing != null ? existing.gameObject : new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            target.SetActive(true);

            var particles = target.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = target.AddComponent<ParticleSystem>();
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = loop;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.4f, duration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(startLifetimeMin, startLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedMin, startSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Clamp(maxParticles, 12, 160);

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(0f, rateOverTime);
            if (loop)
            {
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }
            else
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            }

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.01f, shapeRadius);

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 0.1f;

            RemoveColliders(target);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(particles);
            EditorUtility.SetDirty(renderer);
            return particles;
        }

        private static RitualParticleSystemController EnsureParticleController(GameObject root)
        {
            var controller = root.GetComponent<RitualParticleSystemController>();
            if (controller == null)
            {
                controller = root.AddComponent<RitualParticleSystemController>();
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Material EnsureParticleMaterial()
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ParticleMaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            var color = new Color(1f, 0.86f, 0.46f, 0.58f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetKeywordIfPresent(material, "_ALPHATEST_ON", false);
            SetKeywordIfPresent(material, "_ALPHAPREMULTIPLY_ON", false);
            SetKeywordIfPresent(material, "_SURFACE_TYPE_TRANSPARENT", true);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemoveColliders(GameObject target)
        {
            foreach (var collider in target.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
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

        private static Transform RequireChild(GameObject root, string childName)
        {
            var child = root != null ? FindChildDeep(root.transform, childName) : null;
            if (child == null)
            {
                Debug.LogWarning($"Phase 18 bootstrap skipped {root?.scene.name ?? "scene"} because {childName} is missing. Run Phase 16 bootstrap first.");
            }

            return child;
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

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetKeywordIfPresent(Material material, string keyword, bool enabled)
        {
            if (material.shader == null)
            {
                return;
            }

            var keywordSpace = material.shader.keywordSpace;
            for (var i = 0; i < keywordSpace.keywordCount; i++)
            {
                var localKeyword = keywordSpace.keywords[i];
                if (localKeyword.name == keyword)
                {
                    material.SetKeyword(localKeyword, enabled);
                    return;
                }
            }
        }
    }
}
