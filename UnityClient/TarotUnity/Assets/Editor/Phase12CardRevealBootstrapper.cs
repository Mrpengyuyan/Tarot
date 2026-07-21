using System.IO;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    public static class Phase12CardRevealBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string Phase12DocPath = "Docs/PHASE12_CARD_FIRST_REVEAL.md";
        private const string MaterialFolder = "Assets/Materials";

        private static readonly Color ParchmentGold = new(0.91f, 0.76f, 0.42f, 1f);
        private static readonly Color SoftParchment = new(0.94f, 0.86f, 0.66f, 0.90f);

        [MenuItem("Tools/Tarot Unity/Run Phase 12 Card Reveal Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 12 Card Reveal Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 12 card reveal bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            UpgradeCardPrefab();
            UpgradeReadingRoomScene();
            UpgradeResultScene();
            // Phase 12's narrative now lives in Docs/PROJECT_CHRONICLE.md (the doc
            // consolidation). WritePhase12Doc() is left dormant so re-running this
            // bootstrapper cannot resurrect the deleted per-phase doc.
            // WritePhase12Doc();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 12 card-first reveal bootstrap complete.");
        }

        private static void UpgradeCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var front = root.transform.Find("Front");
                if (front == null)
                {
                    Debug.LogWarning($"Phase 12 could not find Front in {CardPrefabPath}.");
                    return;
                }

                EnsureQuad(
                    front,
                    "Phase12_FaceArtworkFrame",
                    new Vector3(0f, 0.081f, -0.035f),
                    new Vector3(0.58f, 0.74f, 1f),
                    new Color(ParchmentGold.r, ParchmentGold.g, ParchmentGold.b, 0.55f),
                    front.childCount);

                var placeholderObject = EnsureChild(front, "Phase12_FaceArtworkPlaceholder");
                placeholderObject.transform.localPosition = new Vector3(0f, 0.083f, -0.035f);
                placeholderObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                placeholderObject.transform.localScale = new Vector3(0.46f, 0.60f, 1f);

                var placeholderRenderer = placeholderObject.GetComponent<SpriteRenderer>();
                if (placeholderRenderer == null)
                {
                    placeholderRenderer = placeholderObject.AddComponent<SpriteRenderer>();
                }

                placeholderRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                placeholderRenderer.color = SoftParchment;
                placeholderRenderer.sortingOrder = 4;

                var label = EnsureTextMesh(
                    placeholderObject.transform,
                    "Phase12_FaceArtworkLabel",
                    "Tarot Art\nPending Licensed Deck",
                    new Vector3(0f, 0f, -0.014f),
                    24,
                    new Color(0.18f, 0.11f, 0.06f, 1f));
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;

                var cardView = root.GetComponent<CardView>();
                if (cardView != null)
                {
                    SetSerializedReference(cardView, "faceArtworkRenderer", placeholderRenderer);
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

            var stage = EnsureQuad(
                null,
                "Phase12_CardRevealStage",
                new Vector3(0f, -0.46f, 0.20f),
                new Vector3(4.65f, 1.42f, 1f),
                new Color(0.16f, 0.10f, 0.20f, 0.38f),
                0);
            stage.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var backdrop = EnsureQuad(
                null,
                "Phase12_RevealBackdrop",
                new Vector3(0f, -0.50f, 0.08f),
                new Vector3(5.35f, 1.82f, 1f),
                new Color(ParchmentGold.r, ParchmentGold.g, ParchmentGold.b, 0.12f),
                0);
            backdrop.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var lightObject = GameObject.Find("Phase12_FocusedCardLight") ?? new GameObject("Phase12_FocusedCardLight");
            lightObject.transform.position = new Vector3(0f, 1.65f, -1.15f);
            lightObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.77f, 0.45f, 1f);
            light.intensity = 0.85f;
            light.range = 4.4f;
            EditorUtility.SetDirty(lightObject);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas != null)
            {
                EnsurePanel(canvas.transform, "Phase12_CardFocusVignette", new Vector2(0f, -48f), new Vector2(930f, 240f), new Color(0.010f, 0.008f, 0.018f, 0.34f), 0);
                EnsureText(canvas.transform, "Phase12_RevealInstruction", "点击牌面，揭开此刻的讯息", new Vector2(0f, -146f), new Vector2(520f, 38f), 20, FontStyle.Bold);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
        }

        private static void UpgradeResultScene()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                EnsurePanel(canvas.transform, "Phase12_ResultCardShowcase", new Vector2(-330f, 20f), new Vector2(178f, 268f), new Color(0.10f, 0.070f, 0.040f, 0.72f), 4);
                EnsurePanel(canvas.transform, "Phase12_ResultCardPlaceholder", new Vector2(-330f, 20f), new Vector2(136f, 214f), new Color(ParchmentGold.r, ParchmentGold.g, ParchmentGold.b, 0.24f), 5);
                EnsurePanel(canvas.transform, "Phase12_ResultCardArtworkSlot", new Vector2(-330f, 20f), new Vector2(108f, 168f), new Color(SoftParchment.r, SoftParchment.g, SoftParchment.b, 0.32f), 6);

                var overallRect = canvas.transform.Find("OverallText")?.GetComponent<RectTransform>();
                if (overallRect != null && overallRect.sizeDelta.x < 660f)
                {
                    overallRect.sizeDelta = new Vector2(660f, overallRect.sizeDelta.y);
                    EditorUtility.SetDirty(overallRect);
                }
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ResultScenePath);
        }

        private static void WritePhase12Doc()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Phase12DocPath));
            File.WriteAllText(
                Phase12DocPath,
                "# Phase 12 Card-First Reveal\n" +
                "\n" +
                "Date: 2026-05-28\n" +
                "\n" +
                "## Scope\n" +
                "\n" +
                "Phase 12 implements the approved card-first reveal direction for the current Unity tarot vertical slice. It prepares durable scene and prefab anchors for card face artwork, reveal staging, and result-card presence while keeping the existing gameplay and text interpretation flow intact.\n" +
                "\n" +
                "Phase 12 adds a durable face-art slot and card-first reveal staging, but it does not import the final tarot deck artwork. The real deck task still requires source discovery, license review, texture import settings, and card-name mapping.\n" +
                "\n" +
                "## Approved Direction B\n" +
                "\n" +
                "Direction B puts the card face first: the ReadingRoom should stage the reveal around the selected card, and the Result scene should preserve a visible card showcase beside the interpretation text. Text remains important, but it now supports the card moment instead of replacing it.\n" +
                "\n" +
                "## Bootstrap Changes\n" +
                "\n" +
                "- Card prefab: adds `Front/Phase12_FaceArtworkFrame`, `Front/Phase12_FaceArtworkPlaceholder`, and `Front/Phase12_FaceArtworkPlaceholder/Phase12_FaceArtworkLabel`; assigns `CardView.faceArtworkRenderer` to the placeholder `SpriteRenderer`; leaves `TitleLabel` and `PositionLabel` as fallback labels.\n" +
                "- ReadingRoom: adds `Phase12_CardRevealStage`, `Phase12_RevealBackdrop`, `Phase12_FocusedCardLight`, `Phase12_CardFocusVignette`, and `Phase12_RevealInstruction` with the copy `点击牌面，揭开此刻的讯息`.\n" +
                "- Result: adds `Phase12_ResultCardShowcase`, `Phase12_ResultCardPlaceholder`, and `Phase12_ResultCardArtworkSlot`; preserves result text fields and keeps `OverallText` wide enough for reading copy.\n" +
                "\n" +
                "## How To Run The Bootstrapper\n" +
                "\n" +
                "In the Unity Editor, run `Tools/Tarot Unity/Run Phase 12 Card Reveal Bootstrap`.\n" +
                "\n" +
                "Batchmode example, only when no Unity Editor lock is held:\n" +
                "\n" +
                "```bash\n" +
                "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -executeMethod TarotUnity.Editor.Phase12CardRevealBootstrapper.Run -quit\n" +
                "```\n" +
                "\n" +
                "## How To Run Tests\n" +
                "\n" +
                "```bash\n" +
                "/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/maochuandou/BUPT/Game/UnityTarot/UnityClient/TarotUnity -runTests -testPlatform EditMode -testFilter TarotUnity.Tests.EditMode.Phase12CardFirstRevealTests -testResults TestResults/phase12-editmode.xml\n" +
                "```\n" +
                "\n" +
                "Do not use `-nographics` for visual screenshot capture workflows; Phase 12 tests themselves are EditMode asset and scene checks.\n");
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

        private static TextMesh EnsureTextMesh(Transform parent, string name, string text, Vector3 localPosition, int fontSize, Color color)
        {
            var labelObject = EnsureChild(parent, name);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);

            var textMesh = labelObject.GetComponent<TextMesh>();
            if (textMesh == null)
            {
                textMesh = labelObject.AddComponent<TextMesh>();
            }

            textMesh.text = text;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = fontSize;
            textMesh.characterSize = 0.10f;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            EditorUtility.SetDirty(labelObject);
            EditorUtility.SetDirty(textMesh);
            return textMesh;
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

        private static Text EnsureText(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, FontStyle fontStyle)
        {
            var existing = parent.Find(name);
            var labelObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Text));

            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.98f, 0.91f, 0.72f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;

            EditorUtility.SetDirty(labelObject);
            EditorUtility.SetDirty(label);
            return label;
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
                Debug.LogWarning($"Phase 12 could not find serialized property {propertyName} on {target.name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
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

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
