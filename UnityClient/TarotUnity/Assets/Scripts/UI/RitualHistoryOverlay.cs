using System;
using System.Collections;
using System.Collections.Generic;
using TarotUnity.Core;
using TarotUnity.Data;
using TarotUnity.Network;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>Main-menu history drawer for revisiting saved offline and online readings.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1060)]
    public sealed class RitualHistoryOverlay : MonoBehaviour
    {
        private static readonly Color Gold = new(240f / 255f, 214f / 255f, 165f / 255f, 1f);
        private static readonly Color QuietGold = new(194f / 255f, 173f / 255f, 138f / 255f, 1f);
        private static readonly Color MutedInk = new(160f / 255f, 146f / 255f, 122f / 255f, 0.9f);
        private static readonly Color Hairline = new(208f / 255f, 180f / 255f, 124f / 255f, 0.45f);
        private static readonly Color BackdropColor = new(0.03f, 0.025f, 0.04f, 0.76f);
        private static readonly Color PanelColor = new(0.06f, 0.05f, 0.08f, 0.96f);
        private static readonly Color RowColor = new(0.10f, 0.085f, 0.13f, 0.72f);

        private RectTransform canvas;
        private Button triggerButton;
        private GameObject modalRoot;
        private RectTransform listContent;
        private TMP_Text statusLabel;
        private Button clearButton;
        private bool escapeHeld;
        private bool loadingRemote;
        private readonly List<PredictionResponse> remoteRecords = new();

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
        public Button TriggerButton => triggerButton;

        public static RitualHistoryOverlay Attach(GameObject host)
        {
            if (host == null || host.transform is not RectTransform) return null;
            var existing = host.GetComponent<RitualHistoryOverlay>();
            if (existing != null) return existing;
            var owner = host.AddComponent<RitualHistoryOverlay>();
            owner.Build();
            return owner;
        }

        public void Open()
        {
            if (modalRoot == null) return;
            modalRoot.transform.SetAsLastSibling();
            modalRoot.SetActive(true);
            RebuildList();
            TryFetchRemoteRecords();
        }

        public void Close()
        {
            if (modalRoot != null && modalRoot.activeSelf)
            {
                modalRoot.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Build()
        {
            canvas = (RectTransform)transform;
            triggerButton = CreateTrigger();
            modalRoot = CreateModal();
            modalRoot.SetActive(false);
            RitualTypography.ApplyHierarchy(triggerButton.transform);
            RitualTypography.ApplyHierarchy(modalRoot.transform);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                escapeHeld = false;
                return;
            }

            var pressed = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            if (pressed && !escapeHeld)
            {
                Close();
            }

            escapeHeld = pressed;
        }

        private void OnDestroy()
        {
            if (triggerButton != null) Destroy(triggerButton.gameObject);
            if (modalRoot != null) Destroy(modalRoot);
        }

        private Button CreateTrigger()
        {
            var go = new GameObject("RitualHistoryButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-132f, -24f);
            rect.sizeDelta = new Vector2(88f, 38f);

            var plate = go.GetComponent<Image>();
            plate.color = Color.clear;
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Toggle);

            var label = CreateText(go.transform, "Label", "\u8bb0\u5f55", 16f, QuietGold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.characterSpacing = 12f;

            if (Application.isPlaying)
            {
                RitualControlFeedback.Attach(button, RitualControlFeedback.Kind.Quiet);
            }

            return button;
        }

        private GameObject CreateModal()
        {
            var root = new GameObject("RitualHistoryModal", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas, false);
            Stretch((RectTransform)root.transform);

            var backdrop = root.GetComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            var backdropButton = root.GetComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };
            backdropButton.onClick.AddListener(Close);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(680f, 520f);
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = PanelColor;
            panelImg.raycastTarget = true;

            CreateRule(panel.transform, "TopBorder", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 1f));
            CreateRule(panel.transform, "BottomBorder", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f));

            var heading = CreateText(panel.transform, "Heading", "\u5360\u535c\u8bb0\u5f55", 24f, Gold, TextAlignmentOptions.Center);
            PlaceTop(heading.rectTransform, 20f, 520f, 34f);
            heading.characterSpacing = 10f;

            statusLabel = CreateText(panel.transform, "StatusText", "", 13f, MutedInk, TextAlignmentOptions.Center);
            PlaceTop(statusLabel.rectTransform, 56f, 600f, 22f);

            CreateRule(panel.transform, "Divider", new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -84f), new Vector2(0f, 1f));

            var scrollGo = new GameObject("HistoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = (RectTransform)scrollGo.transform;
            scrollRectTransform.anchorMin = new Vector2(0.05f, 0.14f);
            scrollRectTransform.anchorMax = new Vector2(0.95f, 0.82f);
            scrollRectTransform.offsetMin = scrollRectTransform.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = Color.clear;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            Stretch((RectTransform)viewportGo.transform);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            listContent = (RectTransform)contentGo.transform;
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewportGo.transform;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            clearButton = CreateFooterButton(panel.transform, "ClearHistoryButton", "\u6e05\u7a7a\u672c\u5730", new Vector2(-120f, 22f), QuietGold, false, () =>
            {
                ReadingHistoryStore.Clear();
                RebuildList();
            });
            CreateFooterButton(panel.transform, "CloseHistoryButton", "\u5173\u95ed", new Vector2(100f, 22f), Gold, true, Close);

            return root;
        }

        private void TryFetchRemoteRecords()
        {
            var client = ApiClient.Shared;
            if (loadingRemote || client == null || !client.HasSession)
            {
                return;
            }

            StartCoroutine(FetchRemoteRoutine(client));
        }

        private IEnumerator FetchRemoteRoutine(ApiClient client)
        {
            loadingRemote = true;
            yield return client.GetRecords(
                20,
                records =>
                {
                    remoteRecords.Clear();
                    if (records != null)
                    {
                        remoteRecords.AddRange(records);
                    }
                },
                _ => { });
            loadingRemote = false;
            if (IsOpen)
            {
                RebuildList();
            }
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            var localEntries = ReadingHistoryStore.LoadAll();
            var seenPredictionIds = new HashSet<int>();
            var renderedCount = 0;

            for (var i = 0; i < localEntries.Count; i++)
            {
                var entry = localEntries[i];
                if (entry?.snapshot == null) continue;
                if (entry.snapshot.predictionId > 0) seenPredictionIds.Add(entry.snapshot.predictionId);
                AddLocalRow(entry);
                renderedCount++;
            }

            for (var i = 0; i < remoteRecords.Count; i++)
            {
                var record = remoteRecords[i];
                if (record == null || seenPredictionIds.Contains(record.id)) continue;
                AddRemoteRow(record);
                renderedCount++;
            }

            if (clearButton != null) clearButton.gameObject.SetActive(localEntries.Count > 0);
            if (statusLabel != null)
            {
                statusLabel.text = renderedCount > 0
                    ? $"\u5171 {renderedCount} \u6761\u8bb0\u5f55"
                    : "\u6682\u65e0\u5360\u535c\u8bb0\u5f55";
            }

            if (renderedCount == 0)
            {
                var emptyGo = new GameObject("EmptyNotice", typeof(RectTransform), typeof(LayoutElement));
                emptyGo.transform.SetParent(listContent, false);
                emptyGo.GetComponent<LayoutElement>().preferredHeight = 140f;
                var empty = CreateText(emptyGo.transform, "Text",
                    "\u6682\u65e0\u5360\u535c\u8bb0\u5f55\u3002\n\u5b8c\u6210\u4e00\u6b21\u5360\u535c\u540e\uff0c\u724c\u9762\u4e0e\u89e3\u8bfb\u4f1a\u81ea\u52a8\u4fdd\u5b58\u5728\u8fd9\u91cc\u3002",
                    15f, MutedInk, TextAlignmentOptions.Center);
                empty.textWrappingMode = TextWrappingModes.Normal;
                Stretch(empty.rectTransform);
            }

            RitualTypography.ApplyHierarchy(listContent);
        }

        private void AddLocalRow(ReadingHistoryEntry entry)
        {
            var snap = entry.snapshot;
            var spread = string.IsNullOrWhiteSpace(snap.spreadName) ? $"{snap.cardCount} \u5f20\u724c" : snap.spreadName;
            var badge = snap.source == ReadingSource.Online ? "\u5728\u7ebf" : "\u79bb\u7ebf";
            var subtitle = $"{spread} \u00b7 {badge} \u00b7 {FormatTimestamp(entry.savedAtUtc)}";
            var question = string.IsNullOrWhiteSpace(snap.question) ? "\uff08\u672a\u8bb0\u5f55\u95ee\u9898\uff09" : snap.question.Trim();

            CreateRow(
                $"HistoryRow_{entry.historyId}",
                question,
                subtitle,
                () => OpenSnapshot(snap),
                () =>
                {
                    ReadingHistoryStore.Remove(entry.historyId);
                    RebuildList();
                });
        }

        private void AddRemoteRow(PredictionResponse record)
        {
            var subtitle = $"\u5728\u7ebf\u4e91\u7aef \u00b7 {FormatTimestamp(record.created_at)}";
            var question = string.IsNullOrWhiteSpace(record.question) ? "\uff08\u672a\u8bb0\u5f55\u95ee\u9898\uff09" : record.question.Trim();
            CreateRow(
                $"RemoteRow_{record.id}",
                question,
                subtitle,
                () => StartCoroutine(OpenRemoteRecordRoutine(record.id)),
                null);
        }

        private void CreateRow(string name, string question, string subtitle, Action onView, Action onDelete)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(listContent, false);
            row.GetComponent<Image>().color = RowColor;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 76f;

            var qText = CreateText(row.transform, "Question", question, 16f, Gold, TextAlignmentOptions.Left);
            qText.overflowMode = TextOverflowModes.Ellipsis;
            var qRect = qText.rectTransform;
            qRect.anchorMin = new Vector2(0.03f, 0.46f);
            qRect.anchorMax = new Vector2(onDelete != null ? 0.72f : 0.82f, 0.92f);
            qRect.offsetMin = qRect.offsetMax = Vector2.zero;

            var subText = CreateText(row.transform, "Meta", subtitle, 13f, MutedInk, TextAlignmentOptions.Left);
            var subRect = subText.rectTransform;
            subRect.anchorMin = new Vector2(0.03f, 0.10f);
            subRect.anchorMax = new Vector2(onDelete != null ? 0.72f : 0.82f, 0.46f);
            subRect.offsetMin = subRect.offsetMax = Vector2.zero;

            var viewBtn = CreateRowAction(row.transform, "ViewButton", "\u67e5\u770b",
                onDelete != null ? new Vector2(0.74f, 0.22f) : new Vector2(0.83f, 0.22f),
                onDelete != null ? new Vector2(0.86f, 0.78f) : new Vector2(0.97f, 0.78f),
                Gold, onView);
            if (Application.isPlaying) RitualControlFeedback.Attach(viewBtn, RitualControlFeedback.Kind.Quiet);

            if (onDelete != null)
            {
                var delBtn = CreateRowAction(row.transform, "DeleteButton", "\u5220\u9664",
                    new Vector2(0.87f, 0.22f), new Vector2(0.98f, 0.78f), MutedInk, onDelete);
                if (Application.isPlaying) RitualControlFeedback.Attach(delBtn, RitualControlFeedback.Kind.Quiet);
            }
        }

        private Button CreateRowAction(Transform parent, string name, string labelText, Vector2 anchorMin, Vector2 anchorMax, Color color, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var plate = go.GetComponent<Image>();
            plate.color = new Color(Hairline.r, Hairline.g, Hairline.b, 0.10f);
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onClick?.Invoke());

            var label = CreateText(go.transform, "Label", labelText, 14f, color, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return button;
        }

        private void OpenSnapshot(ReadingSessionSnapshot snapshot)
        {
            if (snapshot == null) return;
            ReadingSessionStore.Save(snapshot);
            Close();
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(GameSceneId.Result);
                return;
            }

            SceneManager.LoadScene(GameSceneId.Result.ToString());
        }

        private IEnumerator OpenRemoteRecordRoutine(int predictionId)
        {
            var client = ApiClient.Shared;
            if (client == null) yield break;
            if (statusLabel != null) statusLabel.text = "\u6b63\u5728\u52a0\u8f7d\u8bb0\u5f55\u2026\u2026";
            PredictionDetailResponse detail = null;
            string error = null;
            yield return client.GetRecord(predictionId, d => detail = d, err => error = err);
            if (detail != null)
            {
                var snapshot = ReadingSessionMapper.FromBackendDetail(detail);
                ReadingHistoryStore.Record(snapshot);
                OpenSnapshot(snapshot);
            }
            else if (statusLabel != null)
            {
                statusLabel.text = string.IsNullOrEmpty(error) ? "\u52a0\u8f7d\u8bb0\u5f55\u5931\u8d25" : $"\u52a0\u8f7d\u5931\u8d25\uff1a{error}";
            }
        }

        private Button CreateFooterButton(Transform parent, string name, string text, Vector2 offset, Color ink, bool rules, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(140f, 36f);

            var plate = go.GetComponent<Image>();
            plate.color = Color.clear;
            plate.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onClick?.Invoke());

            var label = CreateText(go.transform, "Label", text, 16f, ink, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.characterSpacing = rules ? 12f : 6f;

            if (rules)
            {
                CreateRule(go.transform, "RitualRuleTop", new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), Vector2.zero, new Vector2(0f, 1f));
                CreateRule(go.transform, "RitualRuleBottom", new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), Vector2.zero, new Vector2(0f, 1f));
            }

            if (Application.isPlaying)
            {
                RitualControlFeedback.Attach(button, rules ? RitualControlFeedback.Kind.Primary : RitualControlFeedback.Kind.Quiet);
            }

            return button;
        }

        private static string FormatTimestamp(string iso)
        {
            if (!string.IsNullOrWhiteSpace(iso) && DateTime.TryParse(iso, out var parsed))
            {
                return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }

            return "\u521a\u521a";
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            go.AddComponent<TarotUiPreserveColor>();
            return text;
        }

        private static void CreateRule(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = Hairline;
            image.raycastTarget = false;
        }

        private static void PlaceTop(RectTransform rect, float topOffset, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
