using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 40 re-composes the MainMenu and Result screens on the Midnight Parlor
    /// language: both get the velvet-and-walnut stage and the gold plaque chrome.
    /// The menu's brown disc "table" and green slab are deleted and replaced with a
    /// real tabletop vignette (deck stack, scattered cards, candle halos); the
    /// result screen frames its hero card and reading scroll in gold. Idempotent.
    /// </summary>
    public static class Phase40MenuResultBootstrapper
    {
        public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;

        /// <summary>Menu world objects deleted by the recomposition.</summary>
        public static readonly string[] LegacyMenuObjects =
        {
            "Phase7_ImmersiveMenuRoot",
            "Phase8_MenuTableSurface",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 40 Menu+Result Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RebuildMenu();
            RebuildResult();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 40 menu+result recomposition complete.");
        }

        private static void RebuildMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            foreach (var name in LegacyMenuObjects)
            {
                var target = GameObject.Find(name);
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                    Debug.Log($"Phase 40: deleted legacy menu object '{name}'.");
                }
            }

            var cloth = LoadMaterial("MP_TableCloth");
            var wood = LoadMaterial("MP_TableWood");
            var backdrop = LoadMaterial("MP_ParlorBackdrop");
            var glow = LoadMaterial("MP_WarmGlow");

            var stage = GameObject.Find("MP_MenuStage") ?? new GameObject("MP_MenuStage");
            stage.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            EnsurePrimitive(stage.transform, "MP_MenuCloth", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 1f), new Vector3(30f, 0.1f, 22f), Vector3.zero, cloth);
            EnsurePrimitive(stage.transform, "MP_MenuRimFar", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 5.5f), new Vector3(32f, 0.4f, 1.4f), Vector3.zero, wood);
            EnsurePrimitive(stage.transform, "MP_ParlorBackdrop", PrimitiveType.Quad,
                new Vector3(0f, 4f, 11f), new Vector3(50f, 16f, 1f), Vector3.zero, backdrop);

            BuildCardStack(stage.transform, "MP_MenuDeck", new Vector3(-1.95f, 0f, -1.2f), 10f, 10);
            BuildScatterCard(stage.transform, "MP_ScatterCard_A", new Vector3(1.5f, 0f, -1.4f), -14f);
            BuildScatterCard(stage.transform, "MP_ScatterCard_B", new Vector3(1.05f, 0f, -0.68f), 6f);

            EnsurePrimitive(stage.transform, "MP_CandleGlow_L", PrimitiveType.Quad,
                new Vector3(-2.25f, 0.62f, -0.88f), new Vector3(1.5f, 1.5f, 1f), Vector3.zero, glow);
            EnsurePrimitive(stage.transform, "MP_CandleGlow_R", PrimitiveType.Quad,
                new Vector3(2.25f, 0.62f, -0.88f), new Vector3(1.5f, 1.5f, 1f), Vector3.zero, glow);

            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas != null)
            {
                var root = canvas.transform;
                Phase39UiReskinBootstrapper.SkinButton(root, "StartReadingButton", 1.9f);
                SkinQuietButton(root, "QuitButton", 3.5f);

                SetActive(root, "StartReadingButton/Phase8_StartButtonGoldTrim", false);
                SetActive(root, "Phase11_ActionRail", false);

                // The dark washes were tuned against a black void; on velvet they
                // bury the material read. The two Phase 11 "depth" fakes go dark
                // entirely, the remaining backdrops drop to a light grade.
                SetActive(root, "Phase11_MenuDepthFrame", false);
                SetActive(root, "Phase11_TableDepthShadow", false);
                SetImageAlpha(root, "Phase7_MenuBackdrop", 0.22f);
                SetImageAlpha(root, "MenuBackdrop", 0.15f);
                SetImageAlpha(root, "Phase7_MenuVignette", 0.25f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RebuildResult()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            var cloth = LoadMaterial("MP_TableCloth");
            var backdrop = LoadMaterial("MP_ParlorBackdrop");

            var stage = GameObject.Find("MP_ResultStage") ?? new GameObject("MP_ResultStage");
            stage.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            EnsurePrimitive(stage.transform, "MP_ResultCloth", PrimitiveType.Cube,
                new Vector3(0f, -0.5f, 1f), new Vector3(30f, 0.1f, 22f), Vector3.zero, cloth);
            EnsurePrimitive(stage.transform, "MP_ParlorBackdrop", PrimitiveType.Quad,
                new Vector3(0f, 3.5f, 11f), new Vector3(50f, 16f, 1f), Vector3.zero, backdrop);

            // The rim beam reads as a gray stripe behind the reading UI at this
            // shallow camera pitch; the Result stays cloth + backdrop only.
            var staleRim = stage.transform.Find("MP_ResultRimFar");
            if (staleRim != null)
            {
                Object.DestroyImmediate(staleRim.gameObject);
            }

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas != null)
            {
                var root = canvas.transform;
                Phase39UiReskinBootstrapper.Skin(root, "ResultReadingScroll", "TarotPanel", 2f);
                Phase39UiReskinBootstrapper.Skin(root, "Phase12_ResultCardShowcase", "TarotPanel", 2.4f);
                Phase39UiReskinBootstrapper.SkinButton(root, "BackToMenuButton", 2.2f);

                SkinDivider(root, "Phase8_ResultGoldDividerTop");
                SkinDivider(root, "Phase8_ResultGoldDividerBottom");

                // The flat amber halo rectangle becomes a real radial candle glow
                // behind the hero card.
                var halo = root.Find("Phase14_ResultCardHalo")?.GetComponent<Image>();
                if (halo != null)
                {
                    halo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"{Phase37AssetFoundationBootstrapper.SpriteFolder}/TarotGlow.png");
                    halo.type = Image.Type.Simple;
                    halo.color = new Color(1f, 0.82f, 0.5f, 0.55f);
                    halo.rectTransform.sizeDelta = new Vector2(470f, 640f);
                    EditorUtility.SetDirty(halo);
                }

                SetImageAlpha(root, "Phase7_ResultBackdrop", 0.40f);
                SetImageAlpha(root, "ResultBackdrop", 0.24f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildCardStack(Transform parent, string name, Vector3 basePos, float yaw, int layers)
        {
            var body = LoadMaterial("MP_DeckBody");
            var back = LoadMaterial("MP_CardBack");
            var stack = parent.Find(name)?.gameObject ?? CreateChild(parent, name);
            stack.transform.localPosition = basePos;
            stack.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var rng = new System.Random(777);
            for (var i = 0; i < layers; i++)
            {
                var jitterX = (float)(rng.NextDouble() - 0.5) * 0.024f;
                var jitterZ = (float)(rng.NextDouble() - 0.5) * 0.024f;
                var twist = (float)(rng.NextDouble() - 0.5) * 2.4f;
                EnsurePrimitive(stack.transform, $"Card_{i:D2}", PrimitiveType.Cube,
                    new Vector3(jitterX, 0.011f + 0.023f * i, jitterZ),
                    new Vector3(0.8f, 0.02f, 1.18f), new Vector3(0f, twist, 0f), body);
            }

            EnsurePrimitive(stack.transform, "TopBack", PrimitiveType.Quad,
                new Vector3(0f, 0.011f + 0.023f * (layers - 1) + 0.012f, 0f),
                new Vector3(0.78f, 1.16f, 1f), new Vector3(90f, 0f, 0f), back);
        }

        private static void BuildScatterCard(Transform parent, string name, Vector3 basePos, float yaw)
        {
            var body = LoadMaterial("MP_DeckBody");
            var back = LoadMaterial("MP_CardBack");
            var card = parent.Find(name)?.gameObject ?? CreateChild(parent, name);
            card.transform.localPosition = basePos;
            card.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            EnsurePrimitive(card.transform, "Body", PrimitiveType.Cube,
                new Vector3(0f, 0.011f, 0f), new Vector3(0.8f, 0.02f, 1.18f), Vector3.zero, body);
            EnsurePrimitive(card.transform, "TopBack", PrimitiveType.Quad,
                new Vector3(0f, 0.023f, 0f), new Vector3(0.78f, 1.16f, 1f),
                new Vector3(90f, 0f, 0f), back);
        }

        private static void SkinQuietButton(Transform root, string path, float pixelsPerUnitMultiplier)
        {
            Phase39UiReskinBootstrapper.Skin(root, path, "TarotPanelSubtle", pixelsPerUnitMultiplier);
            var button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.66f);
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.66f);
            colors.highlightedColor = new Color(1f, 0.97f, 0.84f, 0.9f);
            colors.pressedColor = new Color(0.72f, 0.68f, 0.62f, 0.9f);
            colors.selectedColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            EditorUtility.SetDirty(button);
        }

        private static void SkinDivider(Transform root, string path)
        {
            var image = root.Find(path)?.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{Phase37AssetFoundationBootstrapper.SpriteFolder}/TarotDivider.png");
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 14f);
            EditorUtility.SetDirty(image);
        }

        private static void SetActive(Transform root, string path, bool active)
        {
            var target = root.Find(path);
            if (target != null && target.gameObject.activeSelf != active)
            {
                target.gameObject.SetActive(active);
                EditorUtility.SetDirty(target.gameObject);
            }
        }

        private static void SetImageAlpha(Transform root, string path, float alpha)
        {
            var image = root.Find(path)?.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsurePrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
            }

            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            EditorUtility.SetDirty(go);
        }
    }
}
