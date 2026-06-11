using System.IO;
using System.Linq;
using TarotUnity.Core;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase1AssetBootstrapper
    {
        private static readonly string[] SceneNames =
        {
            "Boot",
            "MainMenu",
            "ReadingRoom",
            "Result",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 1 Bootstrap")]
        public static void Run()
        {
            EnsureFolders();
            CreateScenes();
            CreatePrefabs();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 1 bootstrap complete.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets", "Audio");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Cards");
            EnsureFolder("Assets/Prefabs", "Gameplay");
            EnsureFolder("Assets/Prefabs", "UI");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Scripts");
            EnsureFolder("Assets/Scripts", "Core");
            EnsureFolder("Assets/Scripts", "Data");
            EnsureFolder("Assets/Scripts", "Gameplay");
            EnsureFolder("Assets/Scripts", "Network");
            EnsureFolder("Assets/Scripts", "UI");
            EnsureFolder("Assets", "ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects", "Spreads");
            EnsureFolder("Assets", "UI");
            EnsureFolder("Assets", "VFX");
        }

        private static void CreateScenes()
        {
            CreateScene("Boot", SetupBootScene);
            CreateScene("MainMenu", SetupMenuScene);
            CreateScene("ReadingRoom", SetupReadingRoomScene);
            CreateScene("Result", SetupResultScene);
        }

        private static void CreateScene(string sceneName, System.Action setup)
        {
            var scenePath = $"Assets/Scenes/{sceneName}.unity";
            if (File.Exists(scenePath))
            {
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            setup();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        }

        private static void SetupBootScene()
        {
            var systems = new GameObject("Bootstrapper");
            systems.AddComponent<GameBootstrap>();
            systems.AddComponent<SceneFlowManager>();
            systems.AddComponent<AudioManager>();
            CreateCamera("Boot Camera", new Vector3(0f, 1.2f, -8f), Quaternion.identity);
        }

        private static void SetupMenuScene()
        {
            CreateCamera("Main Camera", new Vector3(0f, 1.6f, -8f), Quaternion.identity);
            CreateDirectionalLight("Key Light");
            CreateCanvasWithLabel("MainMenuCanvas", "Tarot Unity", "Start reading");
        }

        private static void SetupReadingRoomScene()
        {
            CreateCamera("Main Camera", new Vector3(0f, 6f, -7f), Quaternion.Euler(55f, 0f, 0f));
            CreateDirectionalLight("Moon Table Light");

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Graybox Tarot Table";
            table.transform.localScale = new Vector3(7f, 0.2f, 4f);

            var flow = new GameObject("ReadingFlow");
            flow.AddComponent<ReadingFlowController>();

            var spreadRoot = new GameObject("SpreadSlots");
            spreadRoot.AddComponent<SpreadLayoutController>();

            var deck = new GameObject("DeckStack");
            deck.AddComponent<DeckController>();
            deck.transform.position = new Vector3(-2.8f, 0.25f, 0f);
        }

        private static void SetupResultScene()
        {
            CreateCamera("Main Camera", new Vector3(0f, 1.6f, -8f), Quaternion.identity);
            CreateDirectionalLight("Result Light");
            CreateResultPanel("ResultCanvas");
        }

        private static void CreatePrefabs()
        {
            CreateTarotCardPrefab();
            CreateDeckStackPrefab();
            CreateSpreadSlotPrefab();
            CreateResultPanelPrefab();
        }

        private static void CreateTarotCardPrefab()
        {
            var path = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject("PF_TarotCard");
            root.AddComponent<CardView>();
            root.AddComponent<CardFlipController>();

            var back = new GameObject("Back");
            back.transform.SetParent(root.transform);
            back.AddComponent<SpriteRenderer>();

            var front = new GameObject("Front");
            front.transform.SetParent(root.transform);
            front.AddComponent<SpriteRenderer>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateDeckStackPrefab()
        {
            var path = "Assets/Prefabs/Gameplay/PF_DeckStack.prefab";
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject("PF_DeckStack");
            root.AddComponent<DeckController>();

            var parent = new GameObject("CardParent");
            parent.transform.SetParent(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateSpreadSlotPrefab()
        {
            var path = "Assets/Prefabs/Gameplay/PF_SpreadSlot.prefab";
            if (File.Exists(path))
            {
                return;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PF_SpreadSlot";
            root.transform.localScale = new Vector3(0.8f, 0.03f, 1.25f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateResultPanelPrefab()
        {
            var path = "Assets/Prefabs/UI/PF_ResultPanel.prefab";
            if (File.Exists(path))
            {
                return;
            }

            var root = CreateResultPanel("PF_ResultPanel");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateResultPanel(string name)
        {
            var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<ResultPanelPresenter>();

            CreateText(canvas.transform, "QuestionText", new Vector2(0f, 170f), "Question");
            CreateText(canvas.transform, "SpreadNameText", new Vector2(0f, 125f), "Spread");
            CreateText(canvas.transform, "SummaryText", new Vector2(0f, 70f), "Summary");
            CreateText(canvas.transform, "OverallText", new Vector2(0f, 10f), "Overall interpretation");
            CreateText(canvas.transform, "AdviceText", new Vector2(0f, -70f), "Advice");

            return canvas;
        }

        private static void CreateCanvasWithLabel(string canvasName, string title, string action)
        {
            var canvas = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CreateText(canvas.transform, "TitleText", new Vector2(0f, 80f), title);
            CreateText(canvas.transform, "ActionText", new Vector2(0f, 20f), action);
        }

        private static void CreateText(Transform parent, string name, Vector2 anchoredPosition, string label)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 44f);
            rect.anchoredPosition = anchoredPosition;

            var text = textObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = Color.white;
        }

        private static Camera CreateCamera(string name, Vector3 position, Quaternion rotation)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.SetPositionAndRotation(position, rotation);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
            return camera;
        }

        private static void CreateDirectionalLight(string name)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = SceneNames
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
    }
}

