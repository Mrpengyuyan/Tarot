using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 66: an online reading can reach the Result screen while its AI text is
    /// still generating, after it failed, or after the player switched to offline
    /// text. This lays the status line (inside the reading scroll), the mode label (on
    /// the spread-name line) and the retry / offline buttons (copied from the back
    /// button), then wires them to the presenter and the scene controller. Re-running
    /// reuses the objects it created, so the layout stays reproducible.
    /// </summary>
    public static class Phase66ResultInterpretationStateBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        public const string StatusName = "Phase66_InterpretationStatus";
        public const string ModeLabelName = "Phase66_ModeLabel";
        public const string RetryButtonName = "Phase66_RetryInterpretationButton";
        public const string OfflineButtonName = "Phase66_OfflineInterpretationButton";

        private static readonly Color StatusInk = new Color(0.96f, 0.91f, 0.80f, 1f);
        private static readonly Color ModeInk = new Color(0.86f, 0.71f, 0.42f, 1f);

        [MenuItem("Tools/Tarot Unity/Run Phase 66 Result Interpretation States")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 66: ResultCanvas not found.");
                return;
            }

            var scroll = canvas.transform.Find("ResultReadingScroll");
            var viewport = scroll != null ? scroll.Find("Viewport") : null;
            var spreadNameTransform = canvas.transform.Find("SpreadNameText");
            var backButton = canvas.transform.Find("BackToMenuButton");
            var presenter = canvas.GetComponent<ResultPanelPresenter>();
            var controller = canvas.GetComponent<ResultSceneController>();
            if (scroll == null || viewport == null || spreadNameTransform == null || backButton == null
                || presenter == null || controller == null)
            {
                Debug.LogError("Phase 66: ResultReadingScroll/Viewport, SpreadNameText, BackToMenuButton, " +
                    "ResultPanelPresenter or ResultSceneController not found.");
                return;
            }

            var spreadName = spreadNameTransform.GetComponent<TMP_Text>();
            if (spreadName == null)
            {
                Debug.LogError("Phase 66: SpreadNameText has no TMP_Text to borrow the body font from.");
                return;
            }

            var status = EnsureText(scroll, StatusName, spreadName);
            var statusRect = status.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = new Vector2(40f, 40f);
            statusRect.offsetMax = new Vector2(-40f, -40f);
            status.fontSize = 22f;
            status.color = StatusInk;
            status.alignment = TextAlignmentOptions.Center;
            status.enableWordWrapping = true;
            status.text = string.Empty;
            status.transform.SetAsLastSibling();
            status.gameObject.SetActive(false);

            var mode = EnsureText(canvas.transform, ModeLabelName, spreadName);
            var modeRect = mode.rectTransform;
            var spreadRect = spreadName.rectTransform;
            modeRect.anchorMin = spreadRect.anchorMin;
            modeRect.anchorMax = spreadRect.anchorMax;
            modeRect.pivot = spreadRect.pivot;
            modeRect.anchoredPosition = spreadRect.anchoredPosition;
            modeRect.sizeDelta = spreadRect.sizeDelta;
            mode.fontSize = 15f;
            mode.color = ModeInk;
            mode.alignment = TextAlignmentOptions.MidlineRight;
            mode.enableWordWrapping = false;
            mode.text = string.Empty;
            mode.transform.SetSiblingIndex(spreadNameTransform.GetSiblingIndex() + 1);

            var retry = EnsureButton(canvas.transform, backButton.gameObject, RetryButtonName,
                ReleaseUxCopy.RetryButtonLabel, new Vector2(-280f, -300f));
            var offline = EnsureButton(canvas.transform, backButton.gameObject, OfflineButtonName,
                ReleaseUxCopy.OfflineButtonLabel, new Vector2(280f, -300f));

            var group = viewport.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = viewport.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("interpretationStatusText").objectReferenceValue = status;
            presenterSo.FindProperty("modeLabelText").objectReferenceValue = mode;
            presenterSo.FindProperty("retryInterpretationButton").objectReferenceValue = retry;
            presenterSo.FindProperty("offlineInterpretationButton").objectReferenceValue = offline;
            presenterSo.FindProperty("readingContentGroup").objectReferenceValue = group;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("retryInterpretationButton").objectReferenceValue = retry;
            controllerSo.FindProperty("offlineInterpretationButton").objectReferenceValue = offline;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Phase 66: result interpretation-state UI laid and wired.");
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, TMP_Text fontSource)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
            }

            // Borrow the body SDF font and material so the Phase 24 role check holds.
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureButton(Transform canvas, GameObject template, string name, string label, Vector2 position)
        {
            var existing = canvas.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                // A copy of 回到牌桌 keeps the kit sprite, label font and size. Its click
                // handler is added in code; the template has no persistent calls.
                go = Object.Instantiate(template, canvas);
                go.name = name;
            }

            go.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;

            var labelTransform = go.transform.Find("Label");
            var labelText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            if (labelText != null)
            {
                labelText.text = label;
            }

            go.SetActive(false);
            return go.GetComponent<Button>();
        }
    }
}
