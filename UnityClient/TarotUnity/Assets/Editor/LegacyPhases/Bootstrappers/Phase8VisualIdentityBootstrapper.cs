using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase8VisualIdentityBootstrapper
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private const string CardIvoryMaterialPath = "Assets/Materials/MAT_Phase8_CardIvory.mat";
        private const string CardInkMaterialPath = "Assets/Materials/MAT_Phase8_CardInk.mat";
        private const string EdgeGoldMaterialPath = "Assets/Materials/MAT_Phase8_EdgeGold.mat";
        private const string TableGreenMaterialPath = "Assets/Materials/MAT_Phase8_TableGreen.mat";
        private const string CandleAmberMaterialPath = "Assets/Materials/MAT_Phase8_CandleAmber.mat";
        private const string ShadowGlassMaterialPath = "Assets/Materials/MAT_Phase8_ShadowGlass.mat";

        private const string TableWeaveTexturePath = "Assets/Art/UI/TX_Phase8_TableWeave.png";

        private static readonly Color Gold = new(0.86f, 0.63f, 0.24f, 1f);
        private static readonly Color DeepVelvet = new(0.028f, 0.020f, 0.042f, 1f);
        private static readonly Color TableGreen = new(0.055f, 0.17f, 0.12f, 1f);
        private static readonly Color CardIvory = new(0.90f, 0.84f, 0.68f, 1f);
        private static readonly Color Ink = new(0.075f, 0.052f, 0.085f, 1f);
        private static readonly Color CandleAmber = new(1.0f, 0.58f, 0.18f, 1f);

        [MenuItem("Tools/Tarot Unity/Run Phase 8 Visual Identity Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 8 Visual Identity Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            Phase7ImmersiveUiBootstrapper.Run();
            CreateAssets();
            UpgradeCardPrefab();
            UpgradeMainMenuScene();
            UpgradeReadingRoomScene();
            UpgradeResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 8 visual identity bootstrap complete.");
        }

        private static void CreateAssets()
        {
            CreateMaterial(CardIvoryMaterialPath, CardIvory, 0.34f);
            CreateMaterial(CardInkMaterialPath, Ink, 0.44f);
            CreateMaterial(EdgeGoldMaterialPath, Gold, 0.62f);
            CreateMaterial(TableGreenMaterialPath, TableGreen, 0.46f);
            CreateMaterial(CandleAmberMaterialPath, CandleAmber, 0.82f);
            CreateMaterial(ShadowGlassMaterialPath, DeepVelvet, 0.18f);
            CreateTableWeaveTexture();
        }

        private static void UpgradeMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);

            var root = EnsureRoot("Phase8_VisualIdentityRoot");
            EnsureTableSurface(root.transform, "Phase8_MenuTableSurface", new Vector3(0f, -0.44f, -1.18f), new Vector3(6.1f, 0.055f, 2.15f));
            EnsureCandle(root.transform, "Phase8_LeftCandle", new Vector3(-2.25f, 0.09f, -1.05f));
            EnsureCandle(root.transform, "Phase8_RightCandle", new Vector3(2.25f, 0.09f, -1.05f));

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.012f, 0.010f, 0.020f, 1f);
                camera.transform.position = new Vector3(0f, 1.78f, -7.35f);
                camera.transform.LookAt(new Vector3(0f, 0.48f, -0.55f));
                EditorUtility.SetDirty(camera);
            }

            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas != null)
            {
                ApplyPhase8Theme(canvas);
                SetText(EnsureText(canvas.transform, "Phase8_TitleConstellation"), "✦       ✧       ✦", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 196f), new Vector2(760f, 34f));
                SetText(EnsureText(canvas.transform, "Phase8_MenuCrest"), "月光牌桌", 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 22f), new Vector2(300f, 28f));

                SetText(canvas.transform.Find("TitleText")?.GetComponent<Text>(), "塔罗仪式", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 132f), new Vector2(780f, 88f));
                SetText(canvas.transform.Find("SubtitleText")?.GetComponent<Text>(), "在安静的牌桌前，把问题交给牌面。", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(820f, 52f));

                var startButton = canvas.transform.Find("StartReadingButton")?.GetComponent<Button>();
                StyleButton(startButton, "开始占卜", new Vector2(0f, -30f), new Vector2(292f, 58f), true);
                if (startButton != null)
                {
                    EnsurePanel(startButton.transform, "Phase8_StartButtonGoldTrim", Vector2.zero, new Vector2(310f, 72f), new Color(Gold.r, Gold.g, Gold.b, 0.40f), 0);
                }
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MainMenuScenePath);
        }

        private static void UpgradeReadingRoomScene()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var root = EnsureRoot("Phase8_ReadingVisualRoot");
            EnsureTableSurface(root.transform, "Phase8_TableClothSurface", new Vector3(0f, -0.52f, 0.15f), new Vector3(7.0f, 0.050f, 3.85f));
            EnsureRing(root.transform, "Phase8_DeckFocusRing", new Vector3(-2.05f, -0.42f, 0.28f), new Vector3(1.10f, 0.018f, 1.10f));

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas != null)
            {
                ApplyPhase8Theme(canvas);
                EnsurePanel(canvas.transform, "Phase8_SpreadChoiceFrame", new Vector2(-142f, 122f), new Vector2(660f, 62f), new Color(0.040f, 0.075f, 0.060f, 0.58f), 2);
                EnsurePanel(canvas.transform, "Phase8_QuestionPanelFrame", new Vector2(0f, 72f), new Vector2(780f, 66f), new Color(0.070f, 0.060f, 0.042f, 0.62f), 3);
                EnsurePanel(canvas.transform, "Phase8_CardSlotsGlow", new Vector2(0f, -24f), new Vector2(860f, 118f), new Color(Gold.r, Gold.g, Gold.b, 0.11f), 4);

                SetText(canvas.transform.Find("SpreadStatusText")?.GetComponent<Text>(), "选择牌阵，让牌面决定回答的节奏", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 174f), new Vector2(820f, 40f));
                SetText(canvas.transform.Find("FlowStatusText")?.GetComponent<Text>(), "写下此刻真正想问的问题，然后洗牌抽取。", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -224f), new Vector2(860f, 46f));

                StyleButton(canvas.transform.Find("OneCardButton")?.GetComponent<Button>(), "一张牌", new Vector2(-275f, 122f), new Vector2(182f, 42f), false);
                StyleButton(canvas.transform.Find("ThreeCardButton")?.GetComponent<Button>(), "三张牌", new Vector2(-68f, 122f), new Vector2(182f, 42f), false);
                StyleButton(canvas.transform.Find("DrawButton")?.GetComponent<Button>(), "洗牌抽取", new Vector2(178f, 122f), new Vector2(218f, 46f), true);
                StyleButton(canvas.transform.Find("RevealResultButton")?.GetComponent<Button>(), "揭示结果", new Vector2(0f, -168f), new Vector2(248f, 52f), true);

                var input = canvas.transform.Find("QuestionInput")?.GetComponent<InputField>();
                StyleInput(input, new Vector2(0f, 72f), new Vector2(740f, 50f), "此刻，你最想向牌面询问什么？");
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                ApplyPhase8Theme(canvas);
                EnsurePanel(canvas.transform, "Phase8_ResultScrollPanel", new Vector2(0f, 18f), new Vector2(990f, 558f), new Color(0.082f, 0.070f, 0.052f, 0.78f), 2);
                SetText(EnsureText(canvas.transform, "Phase8_ResultCrest"), "牌面回声", 17, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 166f), new Vector2(260f, 30f));
                EnsurePanel(canvas.transform, "Phase8_ResultGoldDividerTop", new Vector2(0f, 150f), new Vector2(760f, 3f), Gold, 5);
                EnsurePanel(canvas.transform, "Phase8_ResultGoldDividerBottom", new Vector2(0f, -208f), new Vector2(760f, 3f), Gold, 5);

                SetText(canvas.transform.Find("QuestionText")?.GetComponent<Text>(), "你的问题", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 226f), new Vector2(860f, 36f));
                SetText(canvas.transform.Find("SpreadNameText")?.GetComponent<Text>(), "牌阵", 19, FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0f, 192f), new Vector2(860f, 28f));
                SetText(canvas.transform.Find("SummaryText")?.GetComponent<Text>(), "概要", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(88f, 124f), new Vector2(710f, 52f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("OverallText")?.GetComponent<Text>(), "整体解读", 19, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(88f, 36f), new Vector2(710f, 94f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("CardAnalysisText")?.GetComponent<Text>(), "牌面分析", 16, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(88f, -74f), new Vector2(710f, 96f), VerticalWrapMode.Overflow);
                SetText(canvas.transform.Find("AdviceText")?.GetComponent<Text>(), "建议", 17, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(88f, -184f), new Vector2(710f, 72f), VerticalWrapMode.Overflow);

                StyleButton(canvas.transform.Find("BackToMenuButton")?.GetComponent<Button>(), "回到牌桌", new Vector2(0f, -284f), new Vector2(226f, 46f), false);
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
                    ApplyMaterial(front.gameObject, CardIvoryMaterialPath);
                    var frame = EnsureChild(front, "Phase8_ArcanaFrame");
                    CreateRail(frame.transform, "Top", new Vector3(0f, 0.072f, 0.455f), new Vector3(0.66f, 0.010f, 0.028f), EdgeGoldMaterialPath);
                    CreateRail(frame.transform, "Bottom", new Vector3(0f, 0.072f, -0.455f), new Vector3(0.66f, 0.010f, 0.028f), EdgeGoldMaterialPath);
                    CreateRail(frame.transform, "Left", new Vector3(-0.315f, 0.072f, 0f), new Vector3(0.026f, 0.010f, 0.84f), EdgeGoldMaterialPath);
                    CreateRail(frame.transform, "Right", new Vector3(0.315f, 0.072f, 0f), new Vector3(0.026f, 0.010f, 0.84f), EdgeGoldMaterialPath);
                }

                if (back != null)
                {
                    ApplyMaterial(back.gameObject, CardInkMaterialPath);
                    CreateRail(back, "Phase8_BackPatternTop", new Vector3(0f, 0.074f, 0.26f), new Vector3(0.52f, 0.010f, 0.045f), EdgeGoldMaterialPath);
                    CreateRail(back, "Phase8_BackPatternBottom", new Vector3(0f, 0.074f, -0.26f), new Vector3(0.52f, 0.010f, 0.045f), EdgeGoldMaterialPath);
                    CreateRail(back, "Phase8_CenterGem", new Vector3(0f, 0.079f, 0f), new Vector3(0.16f, 0.014f, 0.16f), CandleAmberMaterialPath);
                }

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureRoot(string name)
        {
            var root = GameObject.Find(name) ?? new GameObject(name);
            EditorUtility.SetDirty(root);
            return root;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            var child = existing != null ? existing.gameObject : new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
            return child;
        }

        private static void EnsureTableSurface(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var table = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            table.transform.localPosition = position;
            table.transform.localRotation = Quaternion.identity;
            table.transform.localScale = scale;
            ApplyMaterial(table, TableGreenMaterialPath);
            EditorUtility.SetDirty(table);
        }

        private static void EnsureRing(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var ring = EnsurePrimitive(parent, name, PrimitiveType.Cylinder);
            ring.transform.localPosition = position;
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = scale;
            ApplyMaterial(ring, EdgeGoldMaterialPath);
            EditorUtility.SetDirty(ring);
        }

        private static void EnsureCandle(Transform parent, string name, Vector3 position)
        {
            var candle = EnsureChild(parent, name);
            candle.transform.localPosition = position;

            var body = EnsurePrimitive(candle.transform, "Body", PrimitiveType.Cylinder);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.14f, 0.42f, 0.14f);
            ApplyMaterial(body, CardIvoryMaterialPath);

            var flame = EnsurePrimitive(candle.transform, "Flame", PrimitiveType.Sphere);
            flame.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            flame.transform.localScale = new Vector3(0.12f, 0.20f, 0.12f);
            ApplyMaterial(flame, CandleAmberMaterialPath);

            var light = candle.GetComponent<Light>();
            if (light == null)
            {
                light = candle.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = CandleAmber;
            light.intensity = 1.65f;
            light.range = 2.25f;
            EditorUtility.SetDirty(candle);
        }

        private static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType primitiveType)
        {
            var existing = parent.Find(name);
            var obj = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitiveType);
            obj.name = name;
            obj.transform.SetParent(parent, false);

            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return obj;
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
            text.color = new Color(0.96f, 0.91f, 0.80f, 1f);
            text.lineSpacing = 1.20f;
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
                    ? new Color(0.20f, 0.12f, 0.16f, 0.96f)
                    : new Color(0.075f, 0.110f, 0.092f, 0.94f);
                image.raycastTarget = true;
            }

            var colors = button.colors;
            colors.normalColor = image != null ? image.color : colors.normalColor;
            colors.highlightedColor = primary
                ? new Color(0.38f, 0.24f, 0.22f, 1f)
                : new Color(0.14f, 0.22f, 0.17f, 1f);
            colors.pressedColor = Gold;
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var labelText = button.transform.Find("Label")?.GetComponent<Text>();
            SetText(labelText, label, primary ? 20 : 17, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, size);
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
                input.targetGraphic.color = new Color(0.030f, 0.052f, 0.044f, 0.96f);
            }

            if (input.textComponent != null)
            {
                SetText(input.textComponent, input.textComponent.text, 18, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(size.x - 34f, size.y));
            }

            if (input.placeholder is Text placeholder)
            {
                SetText(placeholder, placeholderValue, 17, FontStyle.Italic, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(size.x - 34f, size.y));
                placeholder.color = new Color(0.80f, 0.76f, 0.66f, 0.78f);
            }

            EditorUtility.SetDirty(input);
        }

        private static void CreateRail(Transform parent, string name, Vector3 localPosition, Vector3 localScale, string materialPath)
        {
            var rail = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            rail.transform.localPosition = localPosition;
            rail.transform.localRotation = Quaternion.identity;
            rail.transform.localScale = localScale;
            ApplyMaterial(rail, materialPath);
            EditorUtility.SetDirty(rail);
        }

        private static void CreateMaterial(string path, Color color, float smoothness)
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

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
        }

        private static void CreateTableWeaveTexture()
        {
            var absolutePath = Path.Combine(Application.dataPath, "Art/UI/TX_Phase8_TableWeave.png");
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var thread = (x % 8 == 0 || y % 8 == 0) ? 0.050f : 0f;
                    var diagonal = ((x + y) % 17 == 0) ? 0.035f : 0f;
                    texture.SetPixel(x, y, new Color(0.040f + thread, 0.130f + thread + diagonal, 0.095f + diagonal, 1f));
                }
            }

            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TableWeaveTexturePath, ImportAssetOptions.ForceUpdate);
        }

        private static void ApplyMaterial(GameObject target, string materialPath)
        {
            var renderer = target.GetComponent<Renderer>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ApplyPhase8Theme(GameObject canvas)
        {
            var theme = canvas.GetComponent<TarotUiTheme>();
            if (theme == null)
            {
                theme = canvas.AddComponent<TarotUiTheme>();
            }

            var serializedObject = new SerializedObject(theme);
            SetString(serializedObject, "themeName", "Moonlit Tarot");
            SetColor(serializedObject, "textColor", new Color(0.96f, 0.91f, 0.80f, 1f));
            SetColor(serializedObject, "mutedTextColor", new Color(0.74f, 0.72f, 0.76f, 1f));
            SetColor(serializedObject, "buttonColor", new Color(0.12f, 0.09f, 0.16f, 0.96f));
            SetColor(serializedObject, "buttonHighlightColor", new Color(0.24f, 0.18f, 0.26f, 1f));
            SetColor(serializedObject, "inputColor", new Color(0.035f, 0.045f, 0.040f, 0.96f));
            SetColor(serializedObject, "accentGoldColor", Gold);
            SetColor(serializedObject, "tableGreenColor", TableGreen);
            SetColor(serializedObject, "panelIvoryColor", CardIvory);
            SetFloat(serializedObject, "bodyLineSpacing", 1.20f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }
    }
}
