using System.IO;
using System.Linq;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase15ThreeDTableBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 15 3D Table Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 15 3D Table Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 15 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 15 3D table bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var bodyMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardBody", new Color(0.20f, 0.12f, 0.055f, 1f));
                var faceMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardFacePlane", new Color(0.92f, 0.82f, 0.62f, 1f));
                var backMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardBackPlane", new Color(0.10f, 0.09f, 0.18f, 1f));
                var edgeMaterial = EnsureOpaqueMaterial("MAT_Phase15_CardSideEdge", new Color(0.12f, 0.075f, 0.038f, 1f));
                var shadowMaterial = EnsureTransparentMaterial("MAT_Phase15_CardDropShadow", new Color(0f, 0f, 0f, 0.38f));

                var meshRoot = EnsureChild(root.transform, "Phase15_CardMeshRoot");
                meshRoot.transform.localPosition = new Vector3(0f, -0.018f, 0f);
                meshRoot.transform.localRotation = Quaternion.identity;
                meshRoot.transform.localScale = Vector3.one;

                var body = EnsureCube(meshRoot.transform, "Phase15_CardBody", Vector3.zero, new Vector3(0.74f, 0.035f, 1.05f), bodyMaterial);
                var face = EnsureQuad(meshRoot.transform, "Phase15_CardFacePlane", new Vector3(0f, 0.022f, -0.002f), new Vector3(0.64f, 0.90f, 1f), faceMaterial);
                var back = EnsureQuad(meshRoot.transform, "Phase15_CardBackPlane", new Vector3(0f, -0.022f, 0.002f), new Vector3(0.70f, 0.98f, 1f), backMaterial);
                back.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var edge = EnsureCube(meshRoot.transform, "Phase15_CardSideEdge", new Vector3(0.385f, 0f, 0f), new Vector3(0.035f, 0.042f, 1.05f), edgeMaterial);
                var shadow = EnsureQuad(meshRoot.transform, "Phase15_CardDropShadow", new Vector3(0.045f, -0.036f, 0.045f), new Vector3(0.82f, 1.14f, 1f), shadowMaterial);
                shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                RemoveCollider(body);
                RemoveCollider(face);
                RemoveCollider(back);
                RemoveCollider(edge);
                RemoveCollider(shadow);

                var controller = root.GetComponent<ThreeDCardPresentationController>();
                if (controller == null)
                {
                    controller = root.AddComponent<ThreeDCardPresentationController>();
                }

                SetSerializedReference(controller, "cardMeshRoot", meshRoot.transform);
                SetSerializedReference(controller, "cardFaceRenderer", face.GetComponent<MeshRenderer>());
                SetSerializedReference(controller, "cardBackRenderer", back.GetComponent<MeshRenderer>());
                SetSerializedReference(controller, "cardDropShadowRenderer", shadow.GetComponent<MeshRenderer>());

                var cardView = root.GetComponent<CardView>();
                if (cardView != null)
                {
                    SetSerializedReference(cardView, "threeDPresentationController", controller);
                }

                var phase14 = root.GetComponent<DimensionalCardRevealController>();
                var phase14Root = root.transform.Find("Phase14_DimensionalRoot");
                if (phase14 != null && phase14Root != null)
                {
                    SetSerializedReference(phase14, "cardRoot", phase14Root);
                }

                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = FindSceneObject("Phase15_ThreeDTableRoot") ?? new GameObject("Phase15_ThreeDTableRoot");
            root.transform.position = new Vector3(0f, -0.56f, 0.08f);
            root.transform.rotation = Quaternion.identity;

            var table = EnsureQuad(root.transform, "Phase15_RitualTableSurface", Vector3.zero, new Vector3(5.90f, 2.45f, 1f), EnsureOpaqueMaterial("MAT_Phase15_RitualTableSurface", new Color(0.045f, 0.028f, 0.045f, 1f)));
            table.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ring = EnsureQuad(root.transform, "Phase15_TableDepthRing", new Vector3(0f, 0.012f, 0.08f), new Vector3(4.65f, 1.74f, 1f), EnsureTransparentMaterial("MAT_Phase15_TableDepthRing", new Color(1f, 0.68f, 0.26f, 0.16f)));
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            EnsureEmptyChild(root.transform, "Phase15_DeckFocusAnchor", new Vector3(-1.72f, 0.10f, -0.30f));
            EnsureEmptyChild(root.transform, "Phase15_SpreadFocusAnchor", new Vector3(0f, 0.12f, 0.08f));
            EnsureEmptyChild(root.transform, "Phase15_FlipFocusAnchor", new Vector3(0.42f, 0.20f, -0.10f));

            var warm = EnsureLight("Phase15_WarmKeyLight", new Vector3(-1.25f, 1.70f, -1.10f), Quaternion.Euler(57f, 28f, 0f), new Color(1f, 0.70f, 0.36f, 1f), 1.02f, 5.4f);
            var cool = EnsureLight("Phase15_CoolRimLight", new Vector3(1.70f, 1.32f, 0.78f), Quaternion.Euler(52f, -35f, 0f), new Color(0.38f, 0.58f, 1f, 1f), 0.64f, 4.2f);

            var controller = root.GetComponent<Phase15TableStageController>();
            if (controller == null)
            {
                controller = root.AddComponent<Phase15TableStageController>();
            }

            SetSerializedReference(controller, "tableRoot", root);
            SetSerializedReference(controller, "warmKeyLight", warm);
            SetSerializedReference(controller, "coolRimLight", cool);

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var root = FindSceneObject("Phase15_ResultCardStageRoot") ?? new GameObject("Phase15_ResultCardStageRoot");
            root.transform.position = new Vector3(-1.74f, -0.42f, 0.06f);
            root.transform.rotation = Quaternion.identity;

            EnsureCube(root.transform, "Phase15_ResultCardPedestal", Vector3.zero, new Vector3(1.18f, 0.055f, 1.62f), EnsureOpaqueMaterial("MAT_Phase15_ResultPedestal", new Color(0.12f, 0.075f, 0.052f, 1f)));
            EnsureEmptyChild(root.transform, "Phase15_ResultFocusAnchor", new Vector3(0f, 0.28f, -0.10f));

            EnsureLight("Phase15_ResultWarmFocusLight", new Vector3(-2.24f, 1.28f, -0.82f), Quaternion.Euler(58f, 18f, 0f), new Color(1f, 0.68f, 0.32f, 1f), 0.82f, 4.3f);
            EnsureLight("Phase15_ResultCoolEdgeLight", new Vector3(-0.82f, 1.06f, 0.82f), Quaternion.Euler(54f, -30f, 0f), new Color(0.42f, 0.62f, 1f, 1f), 0.48f, 3.2f);

            var canvas = FindSceneObject("ResultCanvas");
            if (canvas != null)
            {
                foreach (var image in canvas.GetComponentsInChildren<Image>(true))
                {
                    if (image.name.StartsWith("Phase15_", System.StringComparison.Ordinal))
                    {
                        image.raycastTarget = false;
                        EditorUtility.SetDirty(image);
                    }
                }
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
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

        private static GameObject EnsureEmptyChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = EnsureChild(parent, name);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
            return child;
        }

        private static GameObject EnsureCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cube = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            SetMaterial(cube, material);
            return cube;
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

        private static Light EnsureLight(string name, Vector3 position, Quaternion rotation, Color color, float intensity, float range)
        {
            var lightObject = FindSceneObject(name) ?? new GameObject(name);
            lightObject.transform.position = position;
            lightObject.transform.rotation = rotation;

            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;

            EditorUtility.SetDirty(lightObject);
            EditorUtility.SetDirty(light);
            return light;
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

        private static Material EnsureOpaqueMaterial(string materialName, Color color)
        {
            var material = EnsureMaterial(materialName, color, Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");

            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.Zero);
            SetFloatIfPresent(material, "_ZWrite", 1f);

            DisableKeywords(material, "_SURFACE_TYPE_TRANSPARENT", "_ALPHAPREMULTIPLY_ON", "_ALPHAMODULATE_ON", "_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            return material;
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
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
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
            var collider = target.GetComponent<Collider>();
            if (collider != null)
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
    }
}
