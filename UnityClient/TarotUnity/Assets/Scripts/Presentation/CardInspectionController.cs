using System.Collections;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TarotUnity.Presentation
{
    // Follow the camera's breathing after its LateUpdate, without changing the camera itself.
    [DefaultExecutionOrder(100)]
    public sealed class CardInspectionController : MonoBehaviour
    {
        private const float TravelSeconds = 0.3f;
        private const float HoldSeconds = 1.5f;
        private Camera viewCamera;
        private CanvasGroup roomGroup;
        private GameObject overlay;
        private TMP_Text title;
        private TMP_Text caption;
        private Transform cardTransform;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Quaternion artRelativeRotation;
        private Vector3 artCenter;
        private float artHeight;
        private float artWidth;
        private Vector3 restArtScale;
        private float artAspectCorrection;
        private Renderer[] cardRenderers;
        private bool[] rendererStates;
        private Color restColor;
        private MaterialPropertyBlock restMaterial;
        private MaterialPropertyBlock closeupMaterial;
        private CardHoverTiltController hover;
        private bool hoverEnabled;
        private float roomAlpha;
        private bool roomInteractable;
        private bool roomBlocksRaycasts;
        private bool automatic;
        private bool closing;
        private float progress;
        private float heldSeconds;
        private bool hasCapturedState;
        private bool escapeHeld;

        public bool IsOpen => hasCapturedState;
        public CardView CurrentCard { get; private set; }

        public void Initialize(Camera camera, Canvas roomCanvas)
        {
            restMaterial = new MaterialPropertyBlock();
            closeupMaterial = new MaterialPropertyBlock();
            viewCamera = camera != null ? camera : FindFirstObjectByType<Camera>();
            if (roomCanvas == null || overlay != null)
            {
                return;
            }

            roomGroup = roomCanvas.GetComponent<CanvasGroup>();
            if (roomGroup == null)
            {
                roomGroup = roomCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            var theme = roomCanvas.GetComponent<TarotUiTheme>();
            BuildOverlay(theme != null ? theme.TmpBodyFont : TMP_Settings.defaultFontAsset);
        }

        public bool Show(CardView card, bool automatically)
        {
            if (!isActiveAndEnabled || IsOpen || card == null || !card.IsFaceUp
                || card.FaceArtwork == null || card.FaceArtwork.sprite == null || viewCamera == null || overlay == null)
            {
                return false;
            }

            var flip = card.GetComponent<CardFlipController>();
            if (!automatically && flip != null && flip.IsFlipping)
            {
                return false;
            }

            CurrentCard = card;
            automatic = automatically;
            closing = false;
            progress = 0f;
            heldSeconds = 0f;
            escapeHeld = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            cardTransform = card.transform;
            restPosition = cardTransform.localPosition;
            restRotation = cardTransform.localRotation;
            restScale = cardTransform.localScale;
            var artwork = card.FaceArtwork;
            artRelativeRotation = Quaternion.Inverse(cardTransform.rotation) * artwork.transform.rotation;
            artCenter = cardTransform.InverseTransformPoint(artwork.transform.TransformPoint(artwork.sprite.bounds.center));
            artHeight = Mathf.Abs(artwork.sprite.bounds.size.y * artwork.transform.lossyScale.y);
            artWidth = Mathf.Abs(artwork.sprite.bounds.size.x * artwork.transform.lossyScale.x);
            restArtScale = artwork.transform.localScale;
            var sourceAspect = artwork.sprite.bounds.size.x / artwork.sprite.bounds.size.y;
            artAspectCorrection = artHeight * sourceAspect / Mathf.Max(artWidth, 0.001f);
            artWidth = artHeight * sourceAspect;
            // The legacy tabletop slab, glow and fake shadow are not the lifted face.
            cardRenderers = card.GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[cardRenderers.Length];
            for (var i = 0; i < cardRenderers.Length; i++)
            {
                rendererStates[i] = cardRenderers[i].enabled;
                if (cardRenderers[i] != artwork)
                {
                    cardRenderers[i].enabled = false;
                }
            }
            restColor = artwork.color;
            artwork.GetPropertyBlock(restMaterial);
            artwork.GetPropertyBlock(closeupMaterial);
            closeupMaterial.SetFloat("_GlareIntensity", 0.06f);
            closeupMaterial.SetFloat("_Iridescence", 0.02f);
            artwork.SetPropertyBlock(closeupMaterial);
            artwork.color = Color.white;
            hover = card.GetComponent<CardHoverTiltController>();
            hoverEnabled = hover != null && hover.enabled;
            if (hover != null)
            {
                hover.enabled = false;
            }

            roomAlpha = roomGroup.alpha;
            roomInteractable = roomGroup.interactable;
            roomBlocksRaycasts = roomGroup.blocksRaycasts;
            hasCapturedState = true;
            roomGroup.alpha = 0f;
            roomGroup.interactable = false;
            roomGroup.blocksRaycasts = false;
            title.text = card.DrawData?.tarot_card?.name_zh ?? "牌面";
            caption.text = $"{card.DrawData?.position_name} · {(card.ShowsReversed ? "逆位" : "正位")}";
            overlay.SetActive(true);
            return true;
        }

        public IEnumerator ShowAutomatic(CardView card)
        {
            if (!Show(card, true))
            {
                yield break;
            }

            while (IsOpen)
            {
                yield return null;
            }
        }

        public void Close()
        {
            closing = true;
        }

        public void CloseImmediate()
        {
            if (cardTransform != null)
            {
                cardTransform.localPosition = restPosition;
                cardTransform.localRotation = restRotation;
                cardTransform.localScale = restScale;
            }

            if (CurrentCard != null && CurrentCard.FaceArtwork != null)
            {
                CurrentCard.FaceArtwork.color = restColor;
                CurrentCard.FaceArtwork.SetPropertyBlock(restMaterial);
                CurrentCard.FaceArtwork.transform.localScale = restArtScale;
            }

            if (cardRenderers != null)
            {
                for (var i = 0; i < cardRenderers.Length; i++)
                {
                    if (cardRenderers[i] != null)
                    {
                        cardRenderers[i].enabled = rendererStates[i];
                    }
                }
            }

            if (hover != null)
            {
                hover.enabled = hoverEnabled;
            }

            if (roomGroup != null && hasCapturedState)
            {
                roomGroup.alpha = roomAlpha;
                roomGroup.interactable = roomInteractable;
                roomGroup.blocksRaycasts = roomBlocksRaycasts;
            }

            CurrentCard = null;
            hasCapturedState = false;
            cardTransform = null;
            hover = null;
            cardRenderers = null;
            rendererStates = null;
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (cardTransform == null || viewCamera == null)
            {
                if (overlay != null && overlay.activeSelf)
                {
                    CloseImmediate();
                }

                return;
            }

            var escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            if (escapePressed && !escapeHeld)
            {
                Close();
            }

            escapeHeld = escapePressed;

            progress = Mathf.MoveTowards(progress, closing ? 0f : 1f, Time.unscaledDeltaTime / TravelSeconds);
            if (closing && progress <= 0f)
            {
                CloseImmediate();
                return;
            }

            if (!closing && progress >= 1f && automatic)
            {
                heldSeconds += Time.unscaledDeltaTime;
                if (heldSeconds >= HoldSeconds)
                {
                    Close();
                }
            }

            var distance = Mathf.Max(1.5f, viewCamera.nearClipPlane + 1f);
            var height = viewCamera.orthographic ? viewCamera.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var zoom = Mathf.Min(height * 0.6f / Mathf.Max(artHeight, 0.001f),
                height * viewCamera.aspect * 0.8f / Mathf.Max(artWidth, 0.001f));
            var targetArtRotation = viewCamera.transform.rotation
                * Quaternion.Euler(0f, 0f, CurrentCard.ShowsReversed ? 180f : 0f);
            var targetRotation = targetArtRotation * Quaternion.Inverse(artRelativeRotation);
            var blend = Mathf.SmoothStep(0f, 1f, progress);
            CurrentCard.FaceArtwork.transform.localScale = new Vector3(
                restArtScale.x * Mathf.Lerp(1f, artAspectCorrection, blend), restArtScale.y, restArtScale.z);
            cardTransform.localScale = Vector3.Lerp(restScale, restScale * zoom, blend);
            var restWorldRotation = cardTransform.parent != null ? cardTransform.parent.rotation * restRotation : restRotation;
            cardTransform.rotation = Quaternion.Slerp(restWorldRotation, targetRotation, blend);
            var center = viewCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
            var offset = cardTransform.TransformPoint(artCenter) - cardTransform.position;
            var restWorldPosition = cardTransform.parent != null ? cardTransform.parent.TransformPoint(restPosition) : restPosition;
            cardTransform.position = Vector3.Lerp(restWorldPosition, center - offset, blend);
        }

        private void OnDisable() => CloseImmediate();

        private void OnDestroy()
        {
            CloseImmediate();
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }

        private void BuildOverlay(TMP_FontAsset font)
        {
            overlay = new GameObject("CardInspectionOverlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            var background = CreateRect("CloseBackground", overlay.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = Color.clear;
            background.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            title = CreateLabel("CardName", new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f), 32f, font);
            caption = CreateLabel("CardPosition", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.88f), 23f, font);
            var close = CreateRect("CloseButton", overlay.transform, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.15f));
            close.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.075f, 0.035f, 0.96f);
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var label = CreateLabel("CloseLabel", new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.15f), 23f, font);
            label.text = "收起牌面 · Esc";
            overlay.SetActive(false);
        }

        private TMP_Text CreateLabel(string name, Vector2 min, Vector2 max, float size, TMP_FontAsset font)
        {
            var rect = CreateRect(name, overlay.transform, min, max);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = size;
            label.color = new Color(0.96f, 0.9f, 0.75f);
            label.raycastTarget = false;
            label.richText = false;
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
