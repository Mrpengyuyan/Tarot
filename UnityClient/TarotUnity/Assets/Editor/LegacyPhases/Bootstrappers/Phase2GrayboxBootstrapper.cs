using System.IO;
using System.Linq;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase2GrayboxBootstrapper
    {
        private static readonly string[] BuildSceneNames =
        {
            "Boot",
            "MainMenu",
            "ReadingRoom",
            "Result",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 2 Graybox Bootstrap")]
        public static void Run()
        {
            EnsureFolders();
            CreateMaterials();
            CreateCardPrefab();
            CreateDeckPrefab();
            CreateSpreadSlotPrefab();
            CreateResultPanelPrefab();
            CreateBootScene();
            CreateMainMenuScene();
            CreateReadingRoomScene();
            CreateResultScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 2 graybox bootstrap complete.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Cards");
            EnsureFolder("Assets/Prefabs", "Gameplay");
            EnsureFolder("Assets/Prefabs", "UI");
            EnsureFolder("Assets", "Scenes");
        }

        private static void CreateMaterials()
        {
            CreateMaterial("Assets/Materials/MAT_Table.mat", new Color(0.10f, 0.12f, 0.11f));
            CreateMaterial("Assets/Materials/MAT_CardBack.mat", new Color(0.16f, 0.08f, 0.28f));
            CreateMaterial("Assets/Materials/MAT_CardFront.mat", new Color(0.88f, 0.82f, 0.66f));
            CreateMaterial("Assets/Materials/MAT_CardHighlight.mat", new Color(0.95f, 0.72f, 0.20f));
            CreateMaterial("Assets/Materials/MAT_DeckStack.mat", new Color(0.11f, 0.07f, 0.17f));
            CreateMaterial("Assets/Materials/MAT_SpreadSlot.mat", new Color(0.28f, 0.24f, 0.18f));
        }

        private static void CreateCardPrefab()
        {
            var path = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
            var root = new GameObject("PF_TarotCard");
            root.transform.localScale = Vector3.one;

            var cardView = root.AddComponent<CardView>();
            root.AddComponent<CardFlipController>();
            root.AddComponent<CardClickHandler>();
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.82f, 0.16f, 1.22f);

            var back = CreateCardBody("Back", root.transform, "Assets/Materials/MAT_CardBack.mat", new Vector3(0f, 0f, 0f));
            var front = CreateCardBody("Front", root.transform, "Assets/Materials/MAT_CardFront.mat", new Vector3(0f, 0.02f, 0f));
            var highlight = CreateCardBody("Highlight", root.transform, "Assets/Materials/MAT_CardHighlight.mat", new Vector3(0f, 0.04f, 0f));
            highlight.transform.localScale = new Vector3(0.9f, 0.03f, 1.3f);
            highlight.SetActive(false);

            var title = CreateWorldLabel("TitleLabel", front.transform, new Vector3(0f, 0.08f, 0.12f), 0.1f, "Card");
            var position = CreateWorldLabel("PositionLabel", front.transform, new Vector3(0f, 0.08f, -0.22f), 0.065f, "Position");

            SetObject(cardView, "frontRoot", front);
            SetObject(cardView, "backRoot", back);
            SetObject(cardView, "highlightRoot", highlight);
            SetObject(cardView, "titleLabel", title);
            SetObject(cardView, "positionLabel", position);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateDeckPrefab()
        {
            var path = "Assets/Prefabs/Gameplay/PF_DeckStack.prefab";
            var root = new GameObject("PF_DeckStack");
            var deck = root.AddComponent<DeckController>();

            var stack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stack.name = "Graybox Deck";
            stack.transform.SetParent(root.transform);
            stack.transform.localPosition = Vector3.zero;
            stack.transform.localScale = new Vector3(0.9f, 0.28f, 1.25f);
            ApplyMaterial(stack, "Assets/Materials/MAT_DeckStack.mat");

            var cardParent = new GameObject("CardParent");
            cardParent.transform.SetParent(root.transform);
            cardParent.transform.localPosition = Vector3.zero;

            var cardPrefab = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            SetObject(deck, "cardPrefab", cardPrefab);
            SetObject(deck, "cardParent", cardParent.transform);
            SetFloat(deck, "dealDuration", 0.38f);
            SetFloat(deck, "dealInterval", 0.16f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateSpreadSlotPrefab()
        {
            var path = "Assets/Prefabs/Gameplay/PF_SpreadSlot.prefab";
            var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slot.name = "PF_SpreadSlot";
            slot.transform.localScale = new Vector3(0.9f, 0.03f, 1.3f);
            ApplyMaterial(slot, "Assets/Materials/MAT_SpreadSlot.mat");

            PrefabUtility.SaveAsPrefabAsset(slot, path);
            Object.DestroyImmediate(slot);
        }

        private static void CreateResultPanelPrefab()
        {
            var path = "Assets/Prefabs/UI/PF_ResultPanel.prefab";
            var canvas = CreateResultCanvas("PF_ResultPanel", out _);
            PrefabUtility.SaveAsPrefabAsset(canvas, path);
            Object.DestroyImmediate(canvas);
        }

        private static void CreateBootScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapper = new GameObject("Bootstrapper");
            bootstrapper.AddComponent<GameBootstrap>();
            bootstrapper.AddComponent<SceneFlowManager>();
            bootstrapper.AddComponent<AudioManager>();
            CreateCamera("Boot Camera", new Vector3(0f, 1.5f, -8f), Vector3.zero);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Boot.unity");
        }

        private static void CreateMainMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", new Vector3(0f, 1.5f, -8f), Vector3.zero);
            CreateLight("Menu Key Light", 0.95f);
            EnsureEventSystem();

            var canvas = CreateCanvas("MainMenuCanvas");
            CreateText(canvas.transform, "TitleText", new Vector2(0f, 120f), new Vector2(760f, 72f), "Tarot Unity", 42, TextAnchor.MiddleCenter);
            CreateText(canvas.transform, "SubtitleText", new Vector2(0f, 66f), new Vector2(760f, 40f), "Graybox vertical slice", 22, TextAnchor.MiddleCenter);
            var startButton = CreateButton(canvas.transform, "StartReadingButton", new Vector2(0f, -10f), new Vector2(260f, 52f), "Start Reading");
            var status = CreateText(canvas.transform, "StatusText", new Vector2(0f, -82f), new Vector2(620f, 34f), "Local graybox mode", 18, TextAnchor.MiddleCenter);

            var controller = canvas.AddComponent<MainMenuController>();
            SetObject(controller, "startReadingButton", startButton);
            SetObject(controller, "statusText", status);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/MainMenu.unity");
        }

        private static void CreateReadingRoomScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera("Main Camera", new Vector3(0f, 6.4f, -6.2f), Vector3.zero);
            camera.transform.LookAt(new Vector3(0f, 0f, 0.15f));
            CreateLight("Table Key Light", 1.2f);
            EnsureEventSystem();

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Graybox Tarot Table";
            table.transform.localScale = new Vector3(7.5f, 0.18f, 4.4f);
            table.transform.position = new Vector3(0f, -0.08f, 0f);
            ApplyMaterial(table, "Assets/Materials/MAT_Table.mat");

            var spreadRoot = new GameObject("SpreadLayout");
            var spreadLayout = spreadRoot.AddComponent<SpreadLayoutController>();

            var oneCardSlot = CreateSceneSlot("OneCardSlot", spreadRoot.transform, new Vector3(0f, 0.12f, 0.15f));
            var threeSlots = new[]
            {
                CreateSceneSlot("PastSlot", spreadRoot.transform, new Vector3(-1.45f, 0.12f, 0.15f)),
                CreateSceneSlot("PresentSlot", spreadRoot.transform, new Vector3(0f, 0.12f, 0.15f)),
                CreateSceneSlot("AdviceSlot", spreadRoot.transform, new Vector3(1.45f, 0.12f, 0.15f)),
            };
            spreadLayout.ConfigureSlots(new[] { oneCardSlot }, threeSlots, threeSlots);

            var deckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/PF_DeckStack.prefab");
            var deckObject = (GameObject)PrefabUtility.InstantiatePrefab(deckPrefab);
            deckObject.name = "DeckStack";
            deckObject.transform.position = new Vector3(-2.9f, 0.12f, 0.1f);
            var deckController = deckObject.GetComponent<DeckController>();

            var flowObject = new GameObject("ReadingFlow");
            var flowController = flowObject.AddComponent<ReadingFlowController>();
            SetObject(flowController, "deckController", deckController);
            SetObject(flowController, "spreadLayoutController", spreadLayout);

            var canvas = CreateCanvas("ReadingRoomCanvas");
            var spreadStatus = CreateText(canvas.transform, "SpreadStatusText", new Vector2(0f, 190f), new Vector2(720f, 34f), "One Card Focus", 20, TextAnchor.MiddleCenter);
            var flowStatus = CreateText(canvas.transform, "FlowStatusText", new Vector2(0f, -214f), new Vector2(760f, 36f), "Choose a spread, ask a question, then draw.", 18, TextAnchor.MiddleCenter);
            var oneButton = CreateButton(canvas.transform, "OneCardButton", new Vector2(-210f, 140f), new Vector2(180f, 42f), "One Card");
            var threeButton = CreateButton(canvas.transform, "ThreeCardButton", new Vector2(0f, 140f), new Vector2(180f, 42f), "Three Cards");
            var drawButton = CreateButton(canvas.transform, "DrawButton", new Vector2(210f, 140f), new Vector2(180f, 42f), "Draw");
            var input = CreateInputField(canvas.transform, "QuestionInput", new Vector2(0f, 86f), new Vector2(620f, 42f), "What should I notice now?");
            var revealButton = CreateButton(canvas.transform, "RevealResultButton", new Vector2(0f, -166f), new Vector2(220f, 48f), "Reveal Result");

            var roomController = canvas.AddComponent<ReadingRoomController>();
            SetObject(roomController, "flowController", flowController);
            SetObject(roomController, "deckController", deckController);
            SetObject(roomController, "oneCardButton", oneButton);
            SetObject(roomController, "threeCardButton", threeButton);
            SetObject(roomController, "drawButton", drawButton);
            SetObject(roomController, "revealResultButton", revealButton);
            SetObject(roomController, "questionInput", input);
            SetObject(roomController, "spreadStatusText", spreadStatus);
            SetObject(roomController, "flowStatusText", flowStatus);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/ReadingRoom.unity");
        }

        private static void CreateResultScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", new Vector3(0f, 1.5f, -8f), Vector3.zero);
            CreateLight("Result Key Light", 0.9f);
            EnsureEventSystem();

            var canvas = CreateResultCanvas("ResultCanvas", out var presenter);
            var backButton = CreateButton(canvas.transform, "BackToMenuButton", new Vector2(0f, -218f), new Vector2(220f, 44f), "Back To Menu");
            var controller = canvas.AddComponent<ResultSceneController>();
            SetObject(controller, "resultPanel", presenter);
            SetObject(controller, "backToMenuButton", backButton);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Result.unity");
        }

        private static GameObject CreateResultCanvas(string name, out ResultPanelPresenter presenter)
        {
            var canvas = CreateCanvas(name);
            presenter = canvas.AddComponent<ResultPanelPresenter>();

            var question = CreateText(canvas.transform, "QuestionText", new Vector2(0f, 182f), new Vector2(760f, 34f), "Question", 22, TextAnchor.MiddleCenter);
            var spread = CreateText(canvas.transform, "SpreadNameText", new Vector2(0f, 142f), new Vector2(760f, 30f), "Spread", 20, TextAnchor.MiddleCenter);
            var summary = CreateText(canvas.transform, "SummaryText", new Vector2(0f, 96f), new Vector2(760f, 34f), "Summary", 19, TextAnchor.MiddleCenter);
            var overall = CreateText(canvas.transform, "OverallText", new Vector2(0f, 44f), new Vector2(780f, 46f), "Overall interpretation", 18, TextAnchor.MiddleCenter);
            var analysis = CreateText(canvas.transform, "CardAnalysisText", new Vector2(0f, -34f), new Vector2(780f, 82f), "Card analysis", 16, TextAnchor.MiddleCenter);
            var advice = CreateText(canvas.transform, "AdviceText", new Vector2(0f, -110f), new Vector2(760f, 38f), "Advice", 17, TextAnchor.MiddleCenter);
            var warning = CreateText(canvas.transform, "WarningText", new Vector2(0f, -156f), new Vector2(760f, 34f), "Warning", 15, TextAnchor.MiddleCenter);

            SetObject(presenter, "questionText", question);
            SetObject(presenter, "spreadNameText", spread);
            SetObject(presenter, "summaryText", summary);
            SetObject(presenter, "overallText", overall);
            SetObject(presenter, "cardAnalysisText", analysis);
            SetObject(presenter, "adviceText", advice);
            SetObject(presenter, "warningText", warning);

            return canvas;
        }

        private static Transform CreateSceneSlot(string name, Transform parent, Vector3 position)
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/PF_SpreadSlot.prefab");
            var slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab);
            slot.name = name;
            slot.transform.SetParent(parent);
            slot.transform.position = position;
            return slot.transform;
        }

        private static GameObject CreateCardBody(string name, Transform parent, string materialPath, Vector3 localPosition)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name;
            body.transform.SetParent(parent);
            body.transform.localPosition = localPosition;
            body.transform.localScale = new Vector3(0.78f, 0.035f, 1.18f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, materialPath);
            return body;
        }

        private static TextMesh CreateWorldLabel(string name, Transform parent, Vector3 localPosition, float size, string value)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = size;
            label.fontSize = 42;
            label.color = new Color(0.10f, 0.07f, 0.12f);
            return label;
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.15f, 0.23f, 0.96f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(buttonObject.transform, "Label", Vector2.zero, size, label, 18, TextAnchor.MiddleCenter);
            labelText.color = Color.white;
            return button;
        }

        private static InputField CreateInputField(Transform parent, string name, Vector2 position, Vector2 size, string placeholderText)
        {
            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent);
            var rect = inputObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = inputObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.04f, 0.08f, 0.92f);

            var text = CreateText(inputObject.transform, "Text", Vector2.zero, new Vector2(size.x - 24f, size.y), string.Empty, 17, TextAnchor.MiddleLeft);
            var placeholder = CreateText(inputObject.transform, "Placeholder", Vector2.zero, new Vector2(size.x - 24f, size.y), placeholderText, 17, TextAnchor.MiddleLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.48f);

            var input = inputObject.GetComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, string value, int fontSize, TextAnchor anchor)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.alignment = anchor;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Camera CreateCamera(string name, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.018f, 0.03f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<PhysicsRaycaster>();
            return camera;
        }

        private static void CreateLight(string name, float intensity)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, 0.94f, 0.82f);
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
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

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = BuildSceneNames
                .Select(sceneName => $"Assets/Scenes/{sceneName}.unity")
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
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
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
