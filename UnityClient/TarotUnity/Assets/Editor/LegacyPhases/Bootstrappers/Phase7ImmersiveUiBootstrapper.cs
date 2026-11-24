using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase7ImmersiveUiBootstrapper
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private const string MoonGoldMaterialPath = "Assets/Materials/MAT_Phase7_MoonGold.mat";
        private const string DeepVelvetMaterialPath = "Assets/Materials/MAT_Phase7_DeepVelvet.mat";

        [MenuItem("Tools/Tarot Unity/Run Phase 7 Immersive UI Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 7 Immersive UI Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            CreateMaterials();
            UpgradeCardPrefab();
            UpgradeMainMenuScene();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 7 immersive UI bootstrap complete.");
        }

        private static void CreateMaterials()
        {
            CreateMaterial(MoonGoldMaterialPath, new Color(0.92f, 0.72f, 0.34f, 1f));
            CreateMaterial(DeepVelvetMaterialPath, new Color(0.08f, 0.045f, 0.12f, 1f));
        }

        private static void UpgradeMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);

            EnsureWorldRoot("Phase7_ImmersiveMenuRoot");
            EnsureWorldCardHalo();

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.014f, 0.030f, 1f);
                camera.transform.position = new Vector3(0f, 1.7f, -7.2f);
                camera.transform.LookAt(new Vector3(0f, 0.8f, 0f));
                EditorUtility.SetDirty(camera);
            }

            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas != null)
            {
                EnsureTheme(canvas);
                EnsurePanel(canvas.transform, "Phase7_MenuBackdrop", Vector2.zero, new Vector2(1280f, 720f), new Color(0.018f, 0.012f, 0.030f, 0.60f), 0);
                EnsurePanel(canvas.transform, "Phase7_MenuVignette", new Vector2(0f, -190f), new Vector2(980f, 280f), new Color(0.12f, 0.07f, 0.14f, 0.42f), 1);

                SetText(canvas.transform.Find("TitleText")?.GetComponent<Text>(), "塔罗仪式", 54, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 132f), new Vector2(760f, 78f));
                SetText(canvas.transform.Find("SubtitleText")?.GetComponent<Text>(), "在安静的牌桌前，把问题交给牌面。", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 72f), new Vector2(780f, 48f));
                SetText(canvas.transform.Find("StatusText")?.GetComponent<Text>(), "准备开始一场安静的占卜。", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -96f), new Vector2(720f, 42f));

                var startButton = canvas.transform.Find("StartReadingButton")?.GetComponent<Button>();
                StyleButton(startButton, "开始占卜", new Vector2(0f, -22f), new Vector2(280f, 56f), true);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MainMenuScenePath);
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            EnsureWorldRoot("Phase7_TableVignette");

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas != null)
            {
                EnsureTheme(canvas);
                EnsurePanel(canvas.transform, "Phase7_TableVignette", new Vector2(0f, -235f), new Vector2(1040f, 150f), new Color(0.02f, 0.012f, 0.032f, 0.48f), 0);
                var hud = EnsureRectRoot(canvas.transform, "Phase7_RitualHudRoot", new Vector2(0f, 232f), new Vector2(980f, 104f), 2);
                EnsurePanel(hud.transform, "Phase7_HudPlate", Vector2.zero, new Vector2(980f, 104f), new Color(0.045f, 0.032f, 0.070f, 0.74f), 0);
                EnsureProgressMarker(hud.transform, "Phase7_Progress_ChooseSpread", "选牌阵", -388f);
                EnsureProgressMarker(hud.transform, "Phase7_Progress_AskQuestion", "写问题", -194f);
                EnsureProgressMarker(hud.transform, "Phase7_Progress_DrawCards", "抽牌", 0f);
                EnsureProgressMarker(hud.transform, "Phase7_Progress_FlipCards", "翻牌", 194f);
                EnsureProgressMarker(hud.transform, "Phase7_Progress_RevealResult", "解读", 388f);

                SetText(canvas.transform.Find("SpreadStatusText")?.GetComponent<Text>(), "一张牌 · 当下指引", 23, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 170f), new Vector2(760f, 38f));
                SetText(canvas.transform.Find("FlowStatusText")?.GetComponent<Text>(), "选择牌阵，写下问题，然后让牌面回应。", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -222f), new Vector2(820f, 44f));

                StyleButton(canvas.transform.Find("OneCardButton")?.GetComponent<Button>(), "一张牌", new Vector2(-245f, 122f), new Vector2(180f, 42f), false);
                StyleButton(canvas.transform.Find("ThreeCardButton")?.GetComponent<Button>(), "三张牌", new Vector2(-40f, 122f), new Vector2(180f, 42f), false);
                StyleButton(canvas.transform.Find("DrawButton")?.GetComponent<Button>(), "洗牌抽取", new Vector2(190f, 122f), new Vector2(210f, 46f), true);
                StyleButton(canvas.transform.Find("RevealResultButton")?.GetComponent<Button>(), "揭示结果", new Vector2(0f, -168f), new Vector2(240f, 50f), true);

                var input = canvas.transform.Find("QuestionInput")?.GetComponent<InputField>();
                StyleInput(input, new Vector2(0f, 72f), new Vector2(680f, 48f), "此刻，你最想向牌面询问什么？");
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                EnsureTheme(canvas);
                EnsurePanel(canvas.transform, "Phase7_ResultBackdrop", Vector2.zero, new Vector2(1280f, 720f), new Color(0.015f, 0.010f, 0.028f, 0.68f), 0);
                EnsurePanel(canvas.transform, "Phase7_ResultOracleFrame", new Vector2(0f, 20f), new Vector2(940f, 520f), new Color(0.055f, 0.038f, 0.074f, 0.78f), 1);
                EnsureSectionLabel(canvas.transform, "Phase7_ResultSectionSummary", "概要", new Vector2(-390f, 132f));
                EnsureSectionLabel(canvas.transform, "Phase7_ResultSectionOverall", "整体解读", new Vector2(-390f, 48f));
                EnsureSectionLabel(canvas.transform, "Phase7_ResultSectionCards", "牌面分析", new Vector2(-390f, -58f));
                EnsureSectionLabel(canvas.transform, "Phase7_ResultSectionAdvice", "建议", new Vector2(-390f, -170f));

                SetText(canvas.transform.Find("QuestionText")?.GetComponent<Text>(), "你的问题", 23, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 224f), new Vector2(840f, 34f));
                SetText(canvas.transform.Find("SpreadNameText")?.GetComponent<Text>(), "牌阵", 19, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 190f), new Vector2(840f, 28f));
                SetText(canvas.transform.Find("SummaryText")?.GetComponent<Text>(), "概要", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(70f, 132f), new Vector2(650f, 52f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("OverallText")?.GetComponent<Text>(), "整体解读", 19, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(70f, 46f), new Vector2(650f, 82f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("CardAnalysisText")?.GetComponent<Text>(), "牌面分析", 16, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(70f, -62f), new Vector2(650f, 94f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("AdviceText")?.GetComponent<Text>(), "建议", 17, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(70f, -174f), new Vector2(650f, 68f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("WarningText")?.GetComponent<Text>(), string.Empty, 15, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, -244f), new Vector2(760f, 34f), VerticalWrapMode.Overflow);

                StyleButton(canvas.transform.Find("BackToMenuButton")?.GetComponent<Button>(), "回到牌桌", new Vector2(0f, -282f), new Vector2(220f, 44f), false);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var front = root.transform.Find("Front");
                var back = root.transform.Find("Back");
                if (front != null)
                {
                    CreateCardPlate("Phase7_TitleBand", front, new Vector3(0f, 0.063f, 0.36f), new Vector3(0.64f, 0.010f, 0.16f), MoonGoldMaterialPath);
                    var title = front.Find("TitleLabel")?.GetComponent<TextMesh>();
                    if (title != null)
                    {
                        title.fontSize = 60;
                        title.characterSize = 0.082f;
                        title.color = new Color(0.09f, 0.045f, 0.10f, 1f);
                        EditorUtility.SetDirty(title);
                    }
                }

                if (back != null)
                {
                    CreateCardPlate("Phase7_MoonSigil", back, new Vector3(0f, 0.065f, 0f), new Vector3(0.42f, 0.012f, 0.42f), MoonGoldMaterialPath);
                    CreateCardPlate("Phase7_BackVeil", back, new Vector3(0f, 0.058f, 0f), new Vector3(0.68f, 0.006f, 0.98f), DeepVelvetMaterialPath);
                }

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureWorldRoot(string name)
        {
            var root = GameObject.Find(name);
            if (root == null)
            {
                root = new GameObject(name);
            }

            EditorUtility.SetDirty(root);
        }

        private static void EnsureWorldCardHalo()
        {
            var root = GameObject.Find("Phase7_ImmersiveMenuRoot") ?? new GameObject("Phase7_ImmersiveMenuRoot");
            var halo = root.transform.Find("Phase7_MenuCardHalo");
            var haloObject = halo != null ? halo.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObject.name = "Phase7_MenuCardHalo";
            haloObject.transform.SetParent(root.transform, false);
            haloObject.transform.position = new Vector3(0f, 0.44f, -1.4f);
            haloObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            haloObject.transform.localScale = new Vector3(2.8f, 0.025f, 2.8f);
            var collider = haloObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            ApplyMaterial(haloObject, MoonGoldMaterialPath);
            EditorUtility.SetDirty(haloObject);
        }

        private static TarotUiTheme EnsureTheme(GameObject canvas)
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            if (theme == null)
            {
                theme = canvas.AddComponent<TarotUiTheme>();
            }

            SetFloat(theme, "bodyLineSpacing", 1.18f);
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static GameObject EnsureRectRoot(Transform parent, string name, Vector2 position, Vector2 size, int siblingIndex)
        {
            var existing = parent.Find(name);
            var root = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            EditorUtility.SetDirty(root);
            return root;
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
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(panel);
            return image;
        }

        private static void EnsureProgressMarker(Transform parent, string name, string label, float x)
        {
            var marker = EnsureRectRoot(parent, name, new Vector2(x, 0f), new Vector2(154f, 44f), parent.childCount);
            EnsurePanel(marker.transform, "Plate", Vector2.zero, new Vector2(154f, 42f), new Color(0.11f, 0.075f, 0.15f, 0.88f), 0);
            var text = EnsureText(marker.transform, "Label");
            SetText(text, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(142f, 34f));
        }

        private static void EnsureSectionLabel(Transform parent, string name, string label, Vector2 position)
        {
            var text = EnsureText(parent, name);
            SetText(text, label, 16, FontStyle.Bold, TextAnchor.UpperLeft, position, new Vector2(180f, 28f));
            text.color = new Color(0.92f, 0.72f, 0.34f, 1f);
        }

        private static Text EnsureText(Transform parent, string name)
        {
            var existing = parent.Find(name);
            var textObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            return text;
        }

        private static void SetText(
            Text text,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size,
            VerticalWrapMode verticalWrap = VerticalWrapMode.Truncate)
        {
            if (text == null)
            {
                return;
            }

            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.91f, 0.82f, 1f);
            text.lineSpacing = 1.18f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = verticalWrap;
            EditorUtility.SetDirty(text);
        }

        private static void StyleButton(Button button, string label, Vector2 position, Vector2 size, bool primary)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = primary
                    ? new Color(0.32f, 0.18f, 0.32f, 0.96f)
                    : new Color(0.15f, 0.10f, 0.20f, 0.92f);
                image.raycastTarget = true;
            }

            var colors = button.colors;
            colors.normalColor = image != null ? image.color : colors.normalColor;
            colors.highlightedColor = primary
                ? new Color(0.48f, 0.29f, 0.47f, 1f)
                : new Color(0.27f, 0.19f, 0.34f, 1f);
            colors.pressedColor = new Color(0.62f, 0.40f, 0.46f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var labelText = button.transform.Find("Label")?.GetComponent<Text>();
            SetText(labelText, label, primary ? 19 : 17, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, size);
            EditorUtility.SetDirty(button);
        }

        private static void StyleInput(InputField input, Vector2 position, Vector2 size, string placeholderValue)
        {
            if (input == null)
            {
                return;
            }

            var rect = input.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            if (input.targetGraphic != null)
            {
                input.targetGraphic.color = new Color(0.035f, 0.025f, 0.055f, 0.96f);
            }

            if (input.textComponent != null)
            {
                SetText(input.textComponent, input.textComponent.text, 18, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(size.x - 32f, size.y));
            }

            if (input.placeholder is Text placeholder)
            {
                SetText(placeholder, placeholderValue, 17, FontStyle.Italic, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(size.x - 32f, size.y));
                placeholder.color = new Color(0.74f, 0.68f, 0.78f, 0.76f);
            }

            EditorUtility.SetDirty(input);
        }

        private static GameObject CreateCardPlate(string name, Transform parent, Vector3 localPosition, Vector3 localScale, string materialPath)
        {
            var existing = parent.Find(name);
            var plate = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = localPosition;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = localScale;

            var collider = plate.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            ApplyMaterial(plate, materialPath);
            EditorUtility.SetDirty(plate);
            return plate;
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
    }
}
