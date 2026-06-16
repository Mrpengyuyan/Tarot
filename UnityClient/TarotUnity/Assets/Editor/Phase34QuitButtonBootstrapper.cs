using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 34 adds a "quit" affordance to the main menu (desktop builds had no in-game
    /// way to exit). It clones the existing StartReadingButton so styling stays identical
    /// to the menu's primary button (Phase 30 consistency), relabels it, places it as a
    /// stacked secondary action below the start plate and its status line, and wires it to
    /// MainMenuController.quitButton. Idempotent.
    /// </summary>
    public static class Phase34QuitButtonBootstrapper
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string QuitLabel = "退出占卜";
        private const float GapBelowBlock = 30f;

        [MenuItem("Tools/Tarot Unity/Run Phase 34 Quit Button Bootstrap")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath);
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 34: MainMenuCanvas not found.");
                return;
            }

            var start = canvas.transform.Find("StartReadingButton") as RectTransform;
            if (start == null)
            {
                Debug.LogError("Phase 34: StartReadingButton not found.");
                return;
            }

            var existing = canvas.transform.Find("QuitButton") as RectTransform;
            RectTransform quit;
            if (existing != null)
            {
                quit = existing;
            }
            else
            {
                var clone = Object.Instantiate(start.gameObject, start.parent);
                clone.name = "QuitButton";
                quit = clone.GetComponent<RectTransform>();
            }

            // Same footprint/anchoring as the primary button (consistent stacked menu).
            quit.anchorMin = start.anchorMin;
            quit.anchorMax = start.anchorMax;
            quit.pivot = start.pivot;
            quit.sizeDelta = start.sizeDelta;
            quit.localScale = Vector3.one;

            // Place below the lowest edge of the start plate and its status line.
            var lowest = start.anchoredPosition.y - start.sizeDelta.y * 0.5f;
            var status = canvas.transform.Find("StatusText") as RectTransform;
            if (status != null)
            {
                lowest = Mathf.Min(lowest, status.anchoredPosition.y - status.sizeDelta.y * 0.5f);
            }

            quit.anchoredPosition = new Vector2(start.anchoredPosition.x, lowest - GapBelowBlock - quit.sizeDelta.y * 0.5f);

            // Relabel and clear any inherited click wiring (Start is wired at runtime, but be safe).
            var label = quit.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = QuitLabel;
                EditorUtility.SetDirty(label);
            }

            var button = quit.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
            }

            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("Phase 34: MainMenuController not found.");
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("quitButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(quit.gameObject);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log($"Tarot Unity Phase 34 quit button bootstrap complete (y={quit.anchoredPosition.y:0.0}).");
        }
    }
}
