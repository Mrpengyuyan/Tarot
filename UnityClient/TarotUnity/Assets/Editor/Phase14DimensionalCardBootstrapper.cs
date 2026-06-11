using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase14DimensionalCardBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 14 Dimensional Card Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 14 Dimensional Card Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 14 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 14 dimensional card bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var dimensionalRoot = EnsureChild(root.transform, "Phase14_DimensionalRoot");
                dimensionalRoot.transform.localPosition = Vector3.zero;
                dimensionalRoot.transform.localRotation = Quaternion.identity;
                dimensionalRoot.transform.localScale = Vector3.one;

                EnsureQuad(dimensionalRoot.transform, "Phase14_CardEdge", new Vector3(0f, 0.058f, -0.041f), new Vector3(0.74f, 1.05f, 1f), new Color(0.16f, 0.095f, 0.050f, 1f), 0);
                EnsureQuad(dimensionalRoot.transform, "Phase14_CastShadow", new Vector3(0.030f, -0.014f, 0.030f), new Vector3(0.82f, 1.12f, 1f), new Color(0f, 0f, 0f, 0.36f), 1);
                EnsureQuad(dimensionalRoot.transform, "Phase14_FaceRimLight", new Vector3(0f, 0.088f, -0.044f), new Vector3(0.62f, 0.84f, 1f), new Color(1f, 0.80f, 0.43f, 0.30f), 2);
                EnsureQuad(dimensionalRoot.transform, "Phase14_ArtworkGlass", new Vector3(0f, 0.091f, -0.046f), new Vector3(0.48f, 0.64f, 1f), new Color(1f, 0.96f, 0.82f, 0.10f), 3);
                var glow = EnsureQuad(dimensionalRoot.transform, "Phase14_RevealGlow", new Vector3(0f, 0.094f, -0.050f), new Vector3(0.70f, 0.94f, 1f), new Color(1f, 0.70f, 0.26f, 0.22f), 4);

                var glowRenderer = glow.GetComponent<MeshRenderer>();
                if (glowRenderer != null)
                {
                    glowRenderer.enabled = false;
                }

                var controller = root.GetComponent<DimensionalCardRevealController>();
                if (controller == null)
                {
                    controller = root.AddComponent<DimensionalCardRevealController>();
                }

                SetSerializedReference(controller, "cardRoot", dimensionalRoot.transform);
                SetSerializedReference(controller, "revealGlowRenderer", glowRenderer);

                var cardView = root.GetComponent<CardView>();
                if (cardView != null)
                {
                    SetSerializedReference(cardView, "dimensionalRevealController", controller);
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

            var table = EnsureQuad(null, "Phase14_TableDepthPlane", new Vector3(0f, -0.54f, 0.03f), new Vector3(5.65f, 2.12f, 1f), new Color(0.026f, 0.016f, 0.034f, 0.62f), 0);
            table.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var pool = EnsureQuad(null, "Phase14_CardRevealPool", new Vector3(0f, -0.51f, 0.16f), new Vector3(4.20f, 1.30f, 1f), new Color(0.91f, 0.62f, 0.26f, 0.16f), 0);
            pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var lightObject = GameObject.Find("Phase14_RevealLightWarm") ?? new GameObject("Phase14_RevealLightWarm");
            lightObject.transform.position = new Vector3(0f, 1.92f, -1.38f);
            lightObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.38f, 1f);
            light.intensity = 0.58f;
            light.range = 4.8f;
            EditorUtility.SetDirty(lightObject);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                EnsurePanel(canvas.transform, "Phase14_ResultCardShadow", new Vector2(-330f, 10f), new Vector2(172f, 256f), new Color(0f, 0f, 0f, 0.34f), 4);
                EnsurePanel(canvas.transform, "Phase14_ResultCardHalo", new Vector2(-330f, 22f), new Vector2(204f, 304f), new Color(1f, 0.72f, 0.28f, 0.14f), 5);
                EnsurePanel(canvas.transform, "Phase14_ResultTextBridge", new Vector2(-72f, 10f), new Vector2(116f, 324f), new Color(0.80f, 0.54f, 0.20f, 0.10f), 6);
            }

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

        private static GameObject EnsureQuad(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int siblingIndex)
        {
            var existing = parent != null ? parent.Find(name)?.gameObject : GameObject.Find(name);
            var quad = existing ?? GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;

            if (parent != null)
            {
                quad.transform.SetParent(parent, false);
                quad.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            }

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = localScale;

            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = EnsurePreviewMaterial(name, color);
            }

            EditorUtility.SetDirty(quad);
            return quad;
        }

        private static Image EnsurePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, int siblingIndex)
        {
            var existing = parent.Find(name);
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));

            panel.transform.SetParent(parent, false);
            panel.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(image);
            return image;
        }

        private static Material EnsurePreviewMaterial(string sourceName, Color color)
        {
            Directory.CreateDirectory(MaterialFolder);

            var materialPath = $"{MaterialFolder}/MAT_{sourceName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            ConfigureTransparentMaterial(material);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
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
