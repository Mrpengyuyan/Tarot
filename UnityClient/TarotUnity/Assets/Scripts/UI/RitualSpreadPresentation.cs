using System.Collections.Generic;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1200)]
    public sealed class RitualSpreadPresentation : MonoBehaviour
    {
        private ReadingFlowController flow;
        private DeckController deck;
        private CameraChoreographyController choreography;
        private Camera cameraView;
        private SpreadLayoutController layouts;
        private RectTransform canvas;
        private RectTransform action;
        private TMP_Text actionText;
        private TMP_Text progress;
        private Transform world;
        private Transform projectionAxes;
        private readonly List<Transform> slots = new();
        private readonly List<TMP_Text> labels = new();
        private int count;
        private Transform[] originalSlots;
        private RitualSpreadFrame frame;
        private Vector2Int frameSize;
        private CardDrawData[] labelDraws;
        private bool[] labelReversed;
        private RitualSpreadCardLease cardLease;

        public static RitualSpreadPresentation Attach(GameObject host, ReadingFlowController flow,
            DeckController deck, CameraChoreographyController choreography, Camera camera)
        {
            if (host == null || host.transform is not RectTransform) return null;
            var owner = host.GetComponent<RitualSpreadPresentation>();
            if (owner != null) return owner;
            owner = host.AddComponent<RitualSpreadPresentation>();
            owner.flow = flow;
            owner.deck = deck;
            owner.choreography = choreography;
            owner.cameraView = camera;
            owner.layouts = FindFirstObjectByType<SpreadLayoutController>();
            owner.canvas = (RectTransform)host.transform;
            owner.action = host.transform.Find("RevealResultButton") as RectTransform;
            owner.actionText = owner.action != null ? owner.action.GetComponentInChildren<TMP_Text>(true) : null;
            owner.progress = host.transform.Find("RitualProgress")?.GetComponent<TMP_Text>();
            return owner;
        }

        public void Prepare(int cardCount)
        {
            if (layouts == null || cameraView == null || choreography == null
                || !choreography.TryGetSpreadPose(cardCount, out _, out _)) return;
            if (cardCount != 1 && cardCount != 3 && cardCount != 10) return;
            RestoreSlots();
            if (world != null)
            {
                world.gameObject.SetActive(false);
                Destroy(world.gameObject);
            }
            foreach (var label in labels)
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                    Destroy(label.gameObject);
                }
            slots.Clear();
            labels.Clear();
            count = cardCount;
            originalSlots = layouts.GetSlots(count).ToArray();
            frame = null;
            labelDraws = new CardDrawData[count];
            labelReversed = new bool[count];
            world = new GameObject("RitualSpreadWorld", typeof(RitualSpreadCardLease)).transform;
            cardLease = world.GetComponent<RitualSpreadCardLease>();
            cardLease.enabled = false;
            SceneManager.MoveGameObjectToScene(world.gameObject, gameObject.scene);
            projectionAxes = new GameObject("ProjectionAxes").transform;
            projectionAxes.SetParent(world, false);
            for (var i = 0; i < count; i++)
            {
                var slot = new GameObject("RitualSpreadSlot_" + i, typeof(RitualSpreadSlot)).transform;
                slot.SetParent(projectionAxes, false);
                slots.Add(slot);
                var node = new GameObject("RitualPositionLabel_" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
                node.transform.SetParent(canvas, false);
                var text = node.GetComponent<TMP_Text>();
                text.font = actionText != null ? actionText.font : null;
                text.color = new Color(226f / 255f, 203f / 255f, 163f / 255f);
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.raycastTarget = false;
                text.margin = Vector4.zero;
                node.AddComponent<TarotUiPreserveColor>();
                node.SetActive(false);
                labels.Add(text);
            }
            layouts.ConfigureSpread(count, slots.ToArray());
            RitualTypography.ApplyHierarchy(canvas);
            RefreshLayout();
        }

        private void LateUpdate() => RefreshLayout();

        public void RefreshLayout()
        {
            if (world == null || cameraView == null || flow == null) return;
            var width = cameraView.pixelWidth;
            var height = cameraView.pixelHeight;
            if (width <= 2 || height <= 2) return;
            var viewportSize = new Vector2Int(width, height);
            if (frame == null || frameSize != viewportSize)
            {
                frame = RitualSpreadLayout.Compute(count, width, height);
                frameSize = viewportSize;
            }
            var final = flow.State == ReadingFlowState.WaitingForFlip || flow.State == ReadingFlowState.ResultReady;
            var pose = cameraView.transform;
            var fov = cameraView.fieldOfView;
            if (!final) choreography.TryGetSpreadPose(count, out pose, out fov);
            if (pose == null) return;
            const float depth = 2.5f;
            var pixelScale = depth * 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) / height;
            // Keep the table camera. Calibrate the real card plane to the preview's longer perspective.
            var verticalOffset = (height * 0.5f - frame.PerspectiveOrigin.y) * pixelScale;
            var depthScale = depth / frame.Perspective;
            // Factor the off-centre perspective's Y/Z shear into two rotations and a scale.
            // A translation alone incorrectly scales the origin offset with each corner's depth.
            var shear = verticalOffset / frame.Perspective;
            var theta = 0.5f * Mathf.Atan2(2f * pixelScale * shear,
                pixelScale * pixelScale - shear * shear - depthScale * depthScale);
            var trace = pixelScale * pixelScale + shear * shear + depthScale * depthScale;
            var discriminant = Mathf.Sqrt(Mathf.Max(0f, trace * trace
                - 4f * pixelScale * pixelScale * depthScale * depthScale));
            var scaleY = Mathf.Sqrt((trace + discriminant) * 0.5f);
            var scaleZ = Mathf.Sqrt((trace - discriminant) * 0.5f);
            var phi = Mathf.Atan2(depthScale * Mathf.Sin(theta),
                pixelScale * Mathf.Cos(theta) + shear * Mathf.Sin(theta));
            world.SetPositionAndRotation(pose.position + pose.rotation * new Vector3(
                (frame.PerspectiveOrigin.x - width * 0.5f) * pixelScale, verticalOffset, depth),
                pose.rotation * Quaternion.AngleAxis(phi * Mathf.Rad2Deg, Vector3.right));
            world.localScale = new Vector3(pixelScale, scaleY, scaleZ);
            projectionAxes.localRotation = Quaternion.AngleAxis(-theta * Mathf.Rad2Deg, Vector3.right);
            for (var i = 0; i < count; i++)
            {
                var placement = frame.Cards[i];
                slots[i].localPosition = new Vector3(placement.Center.x - frame.PerspectiveOrigin.x,
                    frame.PerspectiveOrigin.y - placement.Center.y, 0f);
                slots[i].localRotation = Quaternion.Euler(placement.Tilt, 0f, -placement.Roll) * Quaternion.Euler(-90f, 0f, 0f);
                slots[i].localScale = new Vector3(placement.Size.x / RitualSpreadSlot.PaperWidth,
                    placement.Size.x / RitualSpreadSlot.PaperWidth, placement.Size.y);
                var card = deck != null && i < deck.ActiveCards.Count ? deck.ActiveCards[i] : null;
                var flip = card != null ? card.GetComponent<CardFlipController>() : null;
                var visible = final && card != null && card.IsFaceUp && (flip == null || !flip.IsFlipping);
                labels[i].gameObject.SetActive(visible);
                if (!visible) continue;
                if (labelDraws[i] != card.DrawData || labelReversed[i] != card.ShowsReversed || string.IsNullOrEmpty(labels[i].text))
                {
                    var name = count == 1 ? "\u6b64\u523b" : count == 3
                        ? (i == 0 ? "\u8fc7\u53bb" : i == 1 ? "\u73b0\u5728" : "\u672a\u6765") : (i + 1).ToString();
                    labels[i].text = name + (card.ShowsReversed ? " \u00b7 \u9006\u4f4d" : "");
                    labelDraws[i] = card.DrawData;
                    labelReversed[i] = card.ShowsReversed;
                }
                Place(labels[i].rectTransform, new Rect(placement.LabelCenter - new Vector2(placement.LabelWidth, placement.LabelFontSize * 1.5f) * 0.5f,
                    new Vector2(placement.LabelWidth, placement.LabelFontSize * 1.5f)), width, height);
                labels[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -placement.Roll);
                labels[i].fontSize = placement.LabelFontSize * canvas.rect.height / height;
                labels[i].characterSpacing = 12f;
            }
            if (!final) return;
            if (action != null) Place(action, frame.ActionRect, width, height);
            if (actionText != null)
            {
                actionText.fontSize = frame.ActionFontSize * canvas.rect.height / height;
                actionText.characterSpacing = 20f;
            }
            if (progress != null)
            {
                var size = new Vector2(width * 0.6f, frame.ProgressFontSize * 1.5f);
                Place(progress.rectTransform, new Rect(frame.ProgressCenter - size * 0.5f, size), width, height);
                progress.fontSize = frame.ProgressFontSize * canvas.rect.height / height;
                progress.characterSpacing = 15f;
            }
        }

        private void Place(RectTransform rect, Rect pixel, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2((pixel.center.x / width - 0.5f) * canvas.rect.width,
                (0.5f - pixel.center.y / height) * canvas.rect.height);
            rect.sizeDelta = new Vector2(pixel.width / width * canvas.rect.width, pixel.height / height * canvas.rect.height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            RestoreSlots();
            if (world != null)
            {
                if (world.GetComponentInChildren<CardView>(true) != null) cardLease.enabled = true;
                else Destroy(world.gameObject);
            }
            foreach (var label in labels) if (label != null) Destroy(label.gameObject);
        }

        private void OnDisable()
        {
            foreach (var label in labels) if (label != null) label.gameObject.SetActive(false);
        }

        private void RestoreSlots()
        {
            if (layouts == null || originalSlots == null || slots.Count == 0) return;
            var current = layouts.GetSlots(count);
            if (current.Count == slots.Count && current[0] == slots[0]) layouts.ConfigureSpread(count, originalSlots);
            originalSlots = null;
        }
    }
}
