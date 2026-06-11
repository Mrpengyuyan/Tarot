using TarotUnity.Core;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase5PolishBootstrapper
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private const string CardFrameMaterialPath = "Assets/Materials/MAT_CardFrame.mat";
        private const string CardInkMaterialPath = "Assets/Materials/MAT_CardInk.mat";
        private const string CardAccentMaterialPath = "Assets/Materials/MAT_CardAccent.mat";
        private const string CardGlowMaterialPath = "Assets/Materials/MAT_CardGlow.mat";

        [MenuItem("Tools/Tarot Unity/Run Phase 5 Polish Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 5 Polish Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            CreateMaterials();
            EnhanceCardPrefab();
            PolishMainMenuScene();
            PolishReadingRoomScene();
            PolishResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 5 polish bootstrap complete.");
        }

        private static void CreateMaterials()
        {
            CreateMaterial(CardFrameMaterialPath, new Color(0.92f, 0.70f, 0.34f, 1f));
            CreateMaterial(CardInkMaterialPath, new Color(0.13f, 0.08f, 0.17f, 1f));
            CreateMaterial(CardAccentMaterialPath, new Color(0.47f, 0.61f, 0.92f, 1f));
            CreateMaterial(CardGlowMaterialPath, new Color(0.95f, 0.78f, 0.36f, 0.78f));
        }

        private static void EnhanceCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var polish = root.GetComponent<CardPresentationPolish>();
                if (polish == null)
                {
                    polish = root.AddComponent<CardPresentationPolish>();
                }

                var front = root.transform.Find("Front");
                var back = root.transform.Find("Back");

                var frontFrame = front != null
                    ? CreateCardPlate("FrontFrame", front, new Vector3(0f, 0.045f, 0f), new Vector3(0.86f, 0.012f, 1.26f), CardFrameMaterialPath)
                    : null;
                var innerGlow = front != null
                    ? CreateCardPlate("InnerGlow", front, new Vector3(0f, 0.052f, 0f), new Vector3(0.64f, 0.009f, 0.94f), CardGlowMaterialPath)
                    : null;
                var backSigil = back != null
                    ? CreateCardPlate("BackSigil", back, new Vector3(0f, 0.045f, 0f), new Vector3(0.32f, 0.016f, 0.32f), CardAccentMaterialPath)
                    : null;
                if (back != null)
                {
                    CreateCardPlate("BackConstellation", back, new Vector3(0f, 0.052f, 0f), new Vector3(0.62f, 0.010f, 0.82f), CardFrameMaterialPath);
                }

                SetObject(polish, "frontFrame", frontFrame);
                SetObject(polish, "backSigil", backSigil);
                SetObject(polish, "innerGlow", innerGlow);
                SetObject(polish, "frameRenderer", frontFrame != null ? frontFrame.GetComponent<Renderer>() : null);
                SetObject(polish, "sigilRenderer", backSigil != null ? backSigil.GetComponent<Renderer>() : null);

                var title = root.transform.Find("Front/TitleLabel")?.GetComponent<TextMesh>();
                if (title != null)
                {
                    title.fontSize = 52;
                    title.characterSize = 0.095f;
                    title.color = new Color(0.10f, 0.06f, 0.12f, 1f);
                }

                var position = root.transform.Find("Front/PositionLabel")?.GetComponent<TextMesh>();
                if (position != null)
                {
                    position.fontSize = 42;
                    position.characterSize = 0.058f;
                    position.color = new Color(0.24f, 0.15f, 0.22f, 1f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void PolishMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas != null)
            {
                AddTheme(canvas);
                AddOverlayPanel(canvas.transform, "MenuBackdrop", new Vector2(0f, 0f), new Vector2(1280f, 720f), new Color(0.015f, 0.012f, 0.026f, 0.42f));
                SetTextStyle(canvas.transform.Find("TitleText")?.GetComponent<Text>(), 46, FontStyle.Bold, 1.08f);
                SetTextStyle(canvas.transform.Find("SubtitleText")?.GetComponent<Text>(), 21, FontStyle.Italic, 1.14f);
                SetTextStyle(canvas.transform.Find("StatusText")?.GetComponent<Text>(), 17, FontStyle.Normal, 1.18f);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MainMenuScenePath);
        }

        private static void PolishReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas != null)
            {
                AddTheme(canvas);
                SetTextStyle(canvas.transform.Find("SpreadStatusText")?.GetComponent<Text>(), 22, FontStyle.Bold, 1.12f);
                SetTextStyle(canvas.transform.Find("FlowStatusText")?.GetComponent<Text>(), 18, FontStyle.Normal, 1.18f);
                SetTextStyle(canvas.transform.Find("QuestionInput/Placeholder")?.GetComponent<Text>(), 16, FontStyle.Italic, 1.16f);
                SetTextStyle(canvas.transform.Find("QuestionInput/Text")?.GetComponent<Text>(), 18, FontStyle.Normal, 1.16f);
            }

            var audioManager = Object.FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                SetBool(audioManager, "proceduralSfxEnabled", true);
                SetFloat(audioManager, "proceduralCueDuration", 0.18f);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void PolishResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                AddTheme(canvas);
                AddOverlayPanel(canvas.transform, "ResultBackdrop", new Vector2(0f, 0f), new Vector2(1280f, 720f), new Color(0.012f, 0.010f, 0.022f, 0.36f));
                AddOverlayPanel(canvas.transform, "ResultReadingFrame", new Vector2(0f, 26f), new Vector2(860f, 430f), new Color(0.06f, 0.045f, 0.08f, 0.36f), 1);

                SetTextStyle(canvas.transform.Find("QuestionText")?.GetComponent<Text>(), 23, FontStyle.Bold, 1.14f);
                SetTextStyle(canvas.transform.Find("SpreadNameText")?.GetComponent<Text>(), 20, FontStyle.Italic, 1.14f);
                SetTextStyle(canvas.transform.Find("SummaryText")?.GetComponent<Text>(), 20, FontStyle.Bold, 1.16f);
                SetTextStyle(canvas.transform.Find("OverallText")?.GetComponent<Text>(), 19, FontStyle.Normal, 1.18f, VerticalWrapMode.Overflow);
                SetTextStyle(canvas.transform.Find("CardAnalysisText")?.GetComponent<Text>(), 17, FontStyle.Normal, 1.20f, VerticalWrapMode.Overflow);
                SetTextStyle(canvas.transform.Find("AdviceText")?.GetComponent<Text>(), 18, FontStyle.Bold, 1.18f, VerticalWrapMode.Overflow);
                SetTextStyle(canvas.transform.Find("WarningText")?.GetComponent<Text>(), 16, FontStyle.Italic, 1.18f, VerticalWrapMode.Overflow);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static TarotUiTheme AddTheme(GameObject canvas)
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            if (theme == null)
            {
                theme = canvas.AddComponent<TarotUiTheme>();
            }

            SetFloat(theme, "bodyLineSpacing", 1.16f);
            return theme;
        }

        private static GameObject CreateCardPlate(string name, Transform parent, Vector3 localPosition, Vector3 localScale, string materialPath)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = localPosition;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = localScale;
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            ApplyMaterial(plate, materialPath);
            return plate;
        }

        private static void AddOverlayPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, int siblingIndex = 0)
        {
            var existing = parent.Find(name);
            var panel = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
        }

        private static void SetTextStyle(Text text, int fontSize, FontStyle fontStyle, float lineSpacing, VerticalWrapMode verticalWrap = VerticalWrapMode.Truncate)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.lineSpacing = lineSpacing;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = verticalWrap;
            text.color = new Color(0.94f, 0.91f, 0.84f, 1f);
            EditorUtility.SetDirty(text);
        }

        private static void CreateMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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

        private static void ApplyMaterial(GameObject target, string materialPath)
        {
            var renderer = target.GetComponent<Renderer>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
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
    }
}
