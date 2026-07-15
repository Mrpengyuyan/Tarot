using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 39 reskins the ReadingRoom UI chrome in the Midnight Parlor language:
    /// the flat translucent boxes (HUD plate, progress plates, action dock, input,
    /// buttons) become gold-framed plaques built from the Phase 37 nine-slice kit,
    /// and the redundant flat-era frames behind them are deactivated (established
    /// Phase 22/25 convention: deactivate, keep for archaeology). Idempotent.
    /// </summary>
    public static class Phase39UiReskinBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        /// <summary>Redundant flat-era chrome deactivated by the reskin.</summary>
        public static readonly string[] DeactivatedChrome =
        {
            "Phase8_SpreadChoiceFrame",
            "Phase8_QuestionPanelFrame",
            "Phase11_QuestionGlow",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 39 UI Reskin Bootstrap")]
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

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 39: ReadingRoomCanvas not found.");
                return;
            }

            var root = canvas.transform;

            Skin(root, "Phase7_RitualHudRoot/Phase7_HudPlate", "TarotPanelSubtle", 2f);
            foreach (var step in new[]
            {
                "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
                "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
            })
            {
                Skin(root, $"Phase7_RitualHudRoot/{step}/Plate", "TarotPanelSubtle", 3.2f);
            }

            Skin(root, "Phase11_ActionDock", "TarotPanel", 2f);
            Skin(root, "QuestionInput", "TarotPanelSubtle", 3f);

            SkinButton(root, "OneCardButton", 2.4f);
            SkinButton(root, "ThreeCardButton", 2.4f);
            SkinButton(root, "DrawButton", 2.2f);
            SkinButton(root, "RevealResultButton", 2f);

            foreach (var name in DeactivatedChrome)
            {
                var chrome = root.Find(name);
                if (chrome != null && chrome.gameObject.activeSelf)
                {
                    chrome.gameObject.SetActive(false);
                    EditorUtility.SetDirty(chrome.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 39 UI reskin complete.");
        }

        /// <summary>
        /// Skins a Button with the plaque sprite and resets its ColorBlock: the old
        /// flat-era normal colors (dark red/gray fills) would overwrite the sprite
        /// tint at runtime. Hover brightens toward gold, press sinks darker.
        /// </summary>
        public static void SkinButton(Transform root, string path, float pixelsPerUnitMultiplier)
        {
            Skin(root, path, "TarotButton", pixelsPerUnitMultiplier);

            var button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.84f, 1f);
            colors.pressedColor = new Color(0.72f, 0.68f, 0.62f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.52f, 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            EditorUtility.SetDirty(button);
        }

        /// <summary>
        /// Puts a Phase 37 nine-slice sprite on an existing Image, preserving the
        /// element's rect. Shared by the Phase 40 menu/result reskin.
        /// </summary>
        public static void Skin(Transform root, string path, string spriteName, float pixelsPerUnitMultiplier)
        {
            var target = root.Find(path);
            var image = target != null ? target.GetComponent<Image>() : null;
            if (image == null)
            {
                Debug.LogWarning($"Phase 39: no Image at '{path}'; skipped.");
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
            if (sprite == null)
            {
                Debug.LogError($"Phase 39: sprite '{spriteName}' missing; run the Phase 37 bootstrap first.");
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }
    }
}
