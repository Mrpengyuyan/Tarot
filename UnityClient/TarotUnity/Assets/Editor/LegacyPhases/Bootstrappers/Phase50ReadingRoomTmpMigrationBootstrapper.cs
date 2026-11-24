using System.Collections.Generic;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 50 migrates the ReadingRoom's text from legacy UI.Text to TextMeshPro
    /// SDF, finishing the job Phase 43 started on the menu. Legacy Text rasterises
    /// each glyph into a bitmap atlas at one size, so it softens the instant the
    /// canvas scales to the built resolution and its outline is four offset copies
    /// of the mesh; the SDF cuts stay sharp at any resolution.
    ///
    /// The menu was the easy screen: it holds no input, so it converted cleanly.
    /// The ReadingRoom carries the piece the menu never had - a question
    /// <see cref="InputField"/> that has to become a <see cref="TMP_InputField"/>,
    /// which is a different component with a different child hierarchy (a masked
    /// Text Area holding TMP text and placeholder). That field is rebuilt from
    /// TMP's own factory and the old one deleted, so the structure is correct
    /// rather than surgically patched.
    /// </summary>
    public static class Phase50ReadingRoomTmpMigrationBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CanvasName = "ReadingRoomCanvas";

        [MenuItem("Tools/Tarot Unity/Run Phase 50 - Migrate ReadingRoom Text to TMP")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                Phase43TmpFoundationBootstrapper.BodyFontAssetPath);
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                Phase43TmpFoundationBootstrapper.DisplayFontAssetPath);
            if (body == null || display == null)
            {
                Debug.LogError("Phase 50: SDF font assets missing; run Phase 43 steps 1 and 2 first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find(CanvasName);
            if (canvas == null)
            {
                Debug.LogError($"Phase 50: {CanvasName} missing.");
                return;
            }

            var theme = canvas.GetComponent<TarotUiTheme>();
            AssignThemeFonts(theme, body, display);

            // Rebuild the input first: it owns two of the legacy Text components
            // (its typed text and placeholder), so converting it before the Text
            // sweep keeps those from being double-handled.
            ConvertQuestionInput(canvas, body);

            var legacy = canvas.GetComponentsInChildren<Text>(true);
            var converted = 0;
            foreach (var text in legacy)
            {
                if (Convert(text, theme, body, display))
                {
                    converted++;
                }
            }

            ApplyTypography(canvas, theme, body, display);
            RewireReadingRoomController(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Phase 50: migrated {converted} ReadingRoom Text components + the question field to TMP SDF.");
        }

        private static void AssignThemeFonts(TarotUiTheme theme, TMP_FontAsset body, TMP_FontAsset display)
        {
            if (theme == null)
            {
                Debug.LogWarning("Phase 50: no TarotUiTheme on the ReadingRoom canvas.");
                return;
            }

            var so = new SerializedObject(theme);
            so.FindProperty("tmpBodyFont").objectReferenceValue = body;
            so.FindProperty("tmpDisplayFont").objectReferenceValue = display;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        /// <summary>Same in-place Text -> TMP conversion the menu used (Phase 43).</summary>
        private static bool Convert(Text text, TarotUiTheme theme, TMP_FontAsset body, TMP_FontAsset display)
        {
            if (text == null)
            {
                return false;
            }

            var go = text.gameObject;
            var content = text.text;
            var fontSize = text.fontSize;
            var color = text.color;
            var alignment = MapAlignment(text.alignment);
            var wasActive = go.activeSelf;

            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;
            var isDisplay = fontSize >= threshold;
            var isButtonLabel = text.GetComponentInParent<Button>(true) != null;

            var outline = go.GetComponent<Outline>();
            if (outline != null)
            {
                Object.DestroyImmediate(outline, true);
            }

            var shadow = go.GetComponent<Shadow>();
            if (shadow != null)
            {
                Object.DestroyImmediate(shadow, true);
            }

            Object.DestroyImmediate(text, true);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.font = isDisplay || isButtonLabel ? display : body;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            go.SetActive(wasActive);
            EditorUtility.SetDirty(go);
            return true;
        }

        /// <summary>
        /// Rebuilds the question field as a TMP_InputField. The legacy InputField
        /// and the TMP one are different components with different child layouts,
        /// so the field is built fresh from <see cref="TMP_DefaultControls"/> -
        /// which produces the correct masked Text Area / placeholder structure -
        /// and reseated into the old field's RectTransform and background. The old
        /// GameObject is then removed.
        /// </summary>
        private static void ConvertQuestionInput(GameObject canvas, TMP_FontAsset body)
        {
            var oldGo = FindDeep(canvas.transform, "QuestionInput");
            if (oldGo == null)
            {
                Debug.LogWarning("Phase 50: QuestionInput not found.");
                return;
            }

            var old = oldGo.GetComponent<InputField>();
            if (old == null)
            {
                // Already a TMP_InputField (re-run) - nothing to rebuild.
                return;
            }

            var rect = oldGo.GetComponent<RectTransform>();
            var parent = rect.parent;
            var siblingIndex = rect.GetSiblingIndex();
            var anchorMin = rect.anchorMin;
            var anchorMax = rect.anchorMax;
            var anchoredPos = rect.anchoredPosition;
            var sizeDelta = rect.sizeDelta;
            var pivot = rect.pivot;

            var oldBg = old.targetGraphic as Image;
            var bgSprite = oldBg != null ? oldBg.sprite : null;
            var bgColor = oldBg != null ? oldBg.color : Color.white;
            var bgType = oldBg != null ? oldBg.type : Image.Type.Sliced;
            var bgPpu = oldBg != null ? oldBg.pixelsPerUnitMultiplier : 1f;

            var placeholderText = (old.placeholder as Text) != null
                ? ((Text)old.placeholder).text
                : "写下你想问的事…";
            var currentText = old.text;
            var textSize = old.textComponent != null ? old.textComponent.fontSize : 20;
            var lineType = (TMP_InputField.LineType)(int)old.lineType;
            var contentType = (TMP_InputField.ContentType)(int)old.contentType;
            var characterLimit = old.characterLimit;

            var resources = new TMP_DefaultControls.Resources { inputField = bgSprite };
            var newGo = TMP_DefaultControls.CreateInputField(resources);
            newGo.name = "QuestionInput";

            var newRect = newGo.GetComponent<RectTransform>();
            newRect.SetParent(parent, false);
            newRect.anchorMin = anchorMin;
            newRect.anchorMax = anchorMax;
            newRect.pivot = pivot;
            newRect.sizeDelta = sizeDelta;
            newRect.anchoredPosition = anchoredPos;
            newRect.SetSiblingIndex(siblingIndex);

            var newBg = newGo.GetComponent<Image>();
            if (newBg != null)
            {
                newBg.sprite = bgSprite;
                newBg.color = bgColor;
                newBg.type = bgType;
                newBg.pixelsPerUnitMultiplier = bgPpu;
            }

            var newInput = newGo.GetComponent<TMP_InputField>();
            newInput.text = currentText;
            newInput.characterLimit = characterLimit;
            newInput.lineType = lineType;
            newInput.contentType = contentType;

            // The legacy caret defaulted to near-black, invisible on the dark input
            // ground; give it the ivory the typed text uses so it can be seen.
            newInput.customCaretColor = true;
            newInput.caretColor = new Color(0.96f, 0.91f, 0.80f, 1f);
            newInput.selectionColor = new Color(0.66f, 0.53f, 0.24f, 0.55f);

            if (newInput.textComponent != null)
            {
                newInput.textComponent.font = body;
                newInput.textComponent.fontSize = textSize;
                newInput.textComponent.color = new Color(0.96f, 0.91f, 0.80f, 1f);
            }

            if (newInput.placeholder is TMP_Text ph)
            {
                ph.text = placeholderText;
                ph.font = body;
                ph.fontSize = textSize;
                ph.fontStyle = FontStyles.Italic;
                ph.color = new Color(0.74f, 0.72f, 0.76f, 0.72f);
            }

            Object.DestroyImmediate(oldGo);
            EditorUtility.SetDirty(newGo);
        }

        /// <summary>Assigns role fonts across whatever TMP is now present. Idempotent.</summary>
        private static void ApplyTypography(GameObject canvas, TarotUiTheme theme,
            TMP_FontAsset body, TMP_FontAsset display)
        {
            var threshold = theme != null ? theme.DisplaySizeThreshold : 30;

            foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                // The input's own text/placeholder are styled by ConvertQuestionInput;
                // leave them to keep the placeholder italic and the caret-visible tint.
                if (tmp.GetComponentInParent<TMP_InputField>(true) != null)
                {
                    continue;
                }

                var isDisplay = tmp.fontSize >= threshold;
                var isButtonLabel = tmp.GetComponentInParent<Button>(true) != null;
                tmp.font = isDisplay || isButtonLabel ? display : body;
                EditorUtility.SetDirty(tmp);
            }
        }

        /// <summary>The controller's fields are TMP types now; re-point all four.</summary>
        private static void RewireReadingRoomController(GameObject canvas)
        {
            var controller = canvas.GetComponentInChildren<ReadingRoomController>(true);
            if (controller == null)
            {
                Debug.LogWarning("Phase 50: ReadingRoomController not found.");
                return;
            }

            var so = new SerializedObject(controller);
            WireTmp(so, "spreadStatusText", FindTmpText(canvas, "SpreadStatusText"));
            WireTmp(so, "flowStatusText", FindTmpText(canvas, "FlowStatusText"));
            WireTmp(so, "releaseStatusText", FindTmpText(canvas, "Phase10_ReleaseStatusText"));

            var input = FindDeep(canvas.transform, "QuestionInput")?.GetComponent<TMP_InputField>();
            WireTmp(so, "questionInput", input);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void WireTmp(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"Phase 50: field '{field}' not found on controller.");
                return;
            }

            if (value == null)
            {
                Debug.LogWarning($"Phase 50: no TMP target found for field '{field}'.");
                return;
            }

            prop.objectReferenceValue = value;
        }

        private static TMP_Text FindTmpText(GameObject canvas, string name)
        {
            foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.name == name)
                {
                    return tmp;
                }
            }

            return null;
        }

        private static GameObject FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root.gameObject;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static TextAlignmentOptions MapAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}
