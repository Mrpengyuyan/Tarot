using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 64: the Result screen rendered on pure black while the menu and reading
    /// room are warm candlelit parlors, so a reading looked like it floated in a void.
    /// This lays a dark parlor backdrop (TarotBackdrop) behind the Result canvas to
    /// tie the three screens together, and lifts the menu's near-invisible 离席 exit
    /// link to a restrained-but-legible ivory.
    /// </summary>
    public static class Phase64ResultBackdropBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BackdropPath = "Assets/Art/MidnightParlor/Sprites/TarotBackdrop.png";
        private const string BackdropName = "MP_ResultBackdrop";

        private static readonly Color QuitInk = new Color(0.82f, 0.75f, 0.60f, 0.88f);

        [MenuItem("Tools/Tarot Unity/Run Phase 64 Result Backdrop + Quit Link")]
        public static void Run()
        {
            BuildResultBackdrop();
            RestyleQuitLink();
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 64: result backdrop laid and quit link restyled.");
        }

        private static void BuildResultBackdrop()
        {
            // Make sure a freshly-dropped PNG is in the AssetDatabase and imported
            // before we touch its importer (batch mode won't do it lazily).
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(BackdropPath, ImportAssetOptions.ForceSynchronousImport);

            // The PNG imports as a default texture in a 3D project; an Image needs a
            // single Sprite (the project's default sprite mode is Multiple, which
            // yields no sprite without slices - force Single).
            var importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
            if (importer != null &&
                (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            AssetDatabase.ImportAsset(BackdropPath, ImportAssetOptions.ForceSynchronousImport);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
            if (sprite == null)
            {
                Debug.LogError($"Phase 64: backdrop sprite missing at {BackdropPath}.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 64: ResultCanvas not found.");
                return;
            }

            var existing = canvas.transform.Find(BackdropName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject(BackdropName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling(); // behind every reading element

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RestyleQuitLink()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            var label = GameObject.Find("QuitButton")?.transform.Find("Label");
            if (label == null)
            {
                Debug.LogWarning("Phase 64: QuitButton/Label not found; quit link unchanged.");
                return;
            }

            // Legible via ink, not size: keep a quiet 18pt (below the primary label's
            // 20, guarded by Phase 35) so the exit link never competes with 入席问牌.
            var tmp = label.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.color = QuitInk;
                tmp.fontSize = 18f;
            }
            else
            {
                var legacy = label.GetComponent<Text>();
                if (legacy != null)
                {
                    legacy.color = QuitInk;
                }
            }

            EditorUtility.SetDirty(label.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
