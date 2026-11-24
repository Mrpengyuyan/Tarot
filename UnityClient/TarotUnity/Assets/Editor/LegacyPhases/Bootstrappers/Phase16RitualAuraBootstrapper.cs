using System.IO;
using System.Linq;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Editor
{
    public static class Phase16RitualAuraBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 16 Ritual Aura Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 16 Ritual Aura Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 16 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 16 ritual aura bootstrap complete.");
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var glowMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraGlowPool", new Color(0.42f, 0.95f, 0.78f, 0.22f));
            var runeMaterial = EnsureTransparentMaterial("MAT_Phase16_RuneRing", new Color(1.0f, 0.78f, 0.32f, 0.34f));
            var particleMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraParticle", new Color(1.0f, 0.86f, 0.46f, 0.58f));

            var parent = FindSceneObject("Phase15_ThreeDTableRoot");
            var root = FindSceneObject("Phase16_RitualAuraRoot") ?? new GameObject("Phase16_RitualAuraRoot");
            root.transform.SetParent(parent != null ? parent.transform : null, false);
            root.transform.localPosition = new Vector3(0f, 0.038f, 0.08f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var glow = EnsureQuad(root.transform, "Phase16_GlowPool", new Vector3(0f, 0.012f, 0.02f), new Vector3(2.75f, 0.86f, 1f), glowMaterial);
            var outerRing = EnsureQuad(root.transform, "Phase16_RuneRingOuter", new Vector3(0f, 0.018f, 0.02f), new Vector3(3.25f, 1.02f, 1f), runeMaterial);
            var innerRing = EnsureQuad(root.transform, "Phase16_RuneRingInner", new Vector3(0f, 0.024f, 0.02f), new Vector3(2.35f, 0.72f, 1f), runeMaterial);
            var north = EnsureSphere(root.transform, "Phase16_ParticleAnchorNorth", new Vector3(0f, 0.06f, 0.68f), new Vector3(0.055f, 0.055f, 0.055f), particleMaterial);
            var east = EnsureSphere(root.transform, "Phase16_ParticleAnchorEast", new Vector3(1.24f, 0.055f, 0.02f), new Vector3(0.048f, 0.048f, 0.048f), particleMaterial);
            var south = EnsureSphere(root.transform, "Phase16_ParticleAnchorSouth", new Vector3(0f, 0.052f, -0.62f), new Vector3(0.05f, 0.05f, 0.05f), particleMaterial);
            var west = EnsureSphere(root.transform, "Phase16_ParticleAnchorWest", new Vector3(-1.24f, 0.055f, 0.02f), new Vector3(0.048f, 0.048f, 0.048f), particleMaterial);
            EnsureEmptyChild(root.transform, "Phase16_AuraFocusAnchor", new Vector3(0f, 0.16f, 0.02f));

            var controller = EnsureAuraController(root);
            SetSerializedReference(controller, "auraRoot", root);
            SetSerializedArrayReferences(controller, "glowRenderers", glow.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "runeRenderers", outerRing.GetComponent<MeshRenderer>(), innerRing.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "particleRenderers", north.GetComponent<MeshRenderer>(), east.GetComponent<MeshRenderer>(), south.GetComponent<MeshRenderer>(), west.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "auraLights");

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var glowMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraGlowPool", new Color(0.42f, 0.95f, 0.78f, 0.22f));
            var runeMaterial = EnsureTransparentMaterial("MAT_Phase16_RuneRing", new Color(1.0f, 0.78f, 0.32f, 0.34f));
            var particleMaterial = EnsureTransparentMaterial("MAT_Phase16_AuraParticle", new Color(1.0f, 0.86f, 0.46f, 0.58f));

            var parent = FindSceneObject("Phase15_ResultCardStageRoot");
            var root = FindSceneObject("Phase16_ResultAuraRoot") ?? new GameObject("Phase16_ResultAuraRoot");
            root.transform.SetParent(parent != null ? parent.transform : null, false);
            root.transform.localPosition = new Vector3(0f, 0.08f, -0.02f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var glow = EnsureQuad(root.transform, "Phase16_ResultGlowPool", new Vector3(0f, 0.012f, 0f), new Vector3(1.28f, 0.52f, 1f), glowMaterial);
            var ring = EnsureQuad(root.transform, "Phase16_ResultRuneRing", new Vector3(0f, 0.018f, 0f), new Vector3(1.45f, 0.58f, 1f), runeMaterial);
            var left = EnsureSphere(root.transform, "Phase16_ResultParticleAnchorLeft", new Vector3(-0.62f, 0.06f, 0f), new Vector3(0.042f, 0.042f, 0.042f), particleMaterial);
            var right = EnsureSphere(root.transform, "Phase16_ResultParticleAnchorRight", new Vector3(0.62f, 0.06f, 0f), new Vector3(0.042f, 0.042f, 0.042f), particleMaterial);
            EnsureEmptyChild(root.transform, "Phase16_ResultAuraFocusAnchor", new Vector3(0f, 0.20f, 0f));

            var controller = EnsureAuraController(root);
            SetSerializedReference(controller, "auraRoot", root);
            SetSerializedArrayReferences(controller, "glowRenderers", glow.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "runeRenderers", ring.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "particleRenderers", left.GetComponent<MeshRenderer>(), right.GetComponent<MeshRenderer>());
            SetSerializedArrayReferences(controller, "auraLights");

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static GameObject EnsureEmptyChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = EnsureChild(parent, name);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
            return child;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static GameObject EnsureQuad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var quad = EnsurePrimitive(parent, name, PrimitiveType.Quad);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = localScale;
            SetMaterial(quad, material);
            return quad;
        }

        private static GameObject EnsureSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var sphere = EnsurePrimitive(parent, name, PrimitiveType.Sphere);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = localScale;
            SetMaterial(sphere, material);
            return sphere;
        }

        private static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType type)
        {
            var existingTransform = parent != null ? parent.Find(name) : null;
            var target = existingTransform != null ? existingTransform.gameObject : null;

            if (target == null)
            {
                target = GameObject.CreatePrimitive(type);
                target.name = name;
                target.transform.SetParent(parent, false);
            }
            else
            {
                EnsurePrimitiveComponents(target, type);
            }

            RemoveCollider(target);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static void EnsurePrimitiveComponents(GameObject target, PrimitiveType type)
        {
            var meshFilter = target.GetComponent<MeshFilter>();
            var meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshFilter != null && meshRenderer != null)
            {
                return;
            }

            var template = GameObject.CreatePrimitive(type);
            try
            {
                if (meshFilter == null)
                {
                    meshFilter = target.AddComponent<MeshFilter>();
                }

                var templateFilter = template.GetComponent<MeshFilter>();
                if (templateFilter != null)
                {
                    meshFilter.sharedMesh = templateFilter.sharedMesh;
                }

                if (meshRenderer == null)
                {
                    target.AddComponent<MeshRenderer>();
                }
            }
            finally
            {
                Object.DestroyImmediate(template);
            }
        }

        private static RitualAuraController EnsureAuraController(GameObject root)
        {
            var controller = root.GetComponent<RitualAuraController>();
            if (controller == null)
            {
                controller = root.AddComponent<RitualAuraController>();
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Material EnsureTransparentMaterial(string materialName, Color color)
        {
            var material = EnsureMaterial(materialName, color, Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");

            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);

            DisableKeywords(material, "_ALPHAPREMULTIPLY_ON", "_ALPHAMODULATE_ON", "_ALPHATEST_ON");
            EnableKeyword(material, "_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMaterial(string materialName, Color color, Shader shader)
        {
            Directory.CreateDirectory(MaterialFolder);
            var materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            foreach (var collider in target.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void DisableKeywords(Material material, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                material.DisableKeyword(keyword);
                SetLocalKeywordIfPresent(material, keyword, false);
            }

#pragma warning disable 618
            material.shaderKeywords = material.shaderKeywords
                .Where(keyword => !keywords.Contains(keyword))
                .ToArray();
#pragma warning restore 618
        }

        private static void EnableKeyword(Material material, string keyword)
        {
            material.EnableKeyword(keyword);
            SetLocalKeywordIfPresent(material, keyword, true);
        }

        private static void SetLocalKeywordIfPresent(Material material, string keyword, bool enabled)
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
    }
}
