using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Drives the holographic foil on the Result hero card. The flipped 3D card
    /// face reads its sheen from a real view angle (Phase 28); this is a UI Image
    /// on a Screen-Space Overlay canvas, which has none - so the sheen direction is
    /// fed to the material instead:
    ///
    /// - <b>idle</b>: a slow figure-of-eight drift, so the card is always subtly
    ///   alive the way a foil card catches ambient light;
    /// - <b>on hover</b>: the band snaps to follow the pointer and the card tilts a
    ///   few degrees toward it, the way a physical foil turns to the light in hand.
    ///
    /// The material is instanced at runtime so the per-frame sheen never dirties the
    /// shared asset.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class HolographicHeroCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private Image heroImage;
        [SerializeField] private Material holographicMaterial;

        [Header("Idle drift")]
        [SerializeField] private float idleDriftSpeed = 0.35f;
        [SerializeField] private float idleDriftAmount = 0.55f;

        [Header("Pointer response")]
        [SerializeField] private float pointerSheen = 1f;
        [SerializeField] private float maxTiltDegrees = 6f;
        [SerializeField] private float followLerp = 12f;
        [SerializeField] private float returnLerp = 5f;

        private static readonly int SheenId = Shader.PropertyToID("_Sheen");

        private Material instance;
        private RectTransform rect;
        private bool hovered;
        private Vector2 sheen;
        private Vector2 targetSheen;
        private float driftPhase;

        private void Awake()
        {
            if (heroImage == null)
            {
                heroImage = GetComponent<Image>();
            }

            rect = (RectTransform)transform;

            if (holographicMaterial != null)
            {
                instance = new Material(holographicMaterial);
                heroImage.material = instance;
            }

            driftPhase = Random.value * 10f;
        }

        private void OnDisable()
        {
            hovered = false;
            targetSheen = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private void Update()
        {
            if (!hovered)
            {
                // A slow, uneven figure-of-eight so the foil never sits perfectly still.
                driftPhase += Time.deltaTime * idleDriftSpeed;
                targetSheen = new Vector2(
                    Mathf.Sin(driftPhase) * idleDriftAmount,
                    Mathf.Sin(driftPhase * 0.6f + 1.2f) * idleDriftAmount * 0.7f);
            }

            var step = Mathf.Clamp01((hovered ? followLerp : returnLerp) * Time.deltaTime);
            sheen = Vector2.Lerp(sheen, targetSheen, step);

            if (instance != null)
            {
                instance.SetVector(SheenId, new Vector4(sheen.x, sheen.y, 0f, 0f));
            }

            // Parallax lean toward the sheen. On an Overlay canvas an X/Y RectTransform
            // rotation foreshortens the quad, so the card reads as tilting in hand.
            // Idle keeps this near flat (the tilt only engages while hovered).
            var lean = hovered ? sheen : Vector2.zero;
            var targetRotation = Quaternion.Euler(lean.y * maxTiltDegrees, -lean.x * maxTiltDegrees, 0f);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRotation, step);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!hovered || rect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out var local))
            {
                var r = rect.rect;
                var nx = Mathf.Clamp((local.x - r.x) / Mathf.Max(1e-3f, r.width) * 2f - 1f, -1f, 1f);
                var ny = Mathf.Clamp((local.y - r.y) / Mathf.Max(1e-3f, r.height) * 2f - 1f, -1f, 1f);
                targetSheen = new Vector2(nx, ny) * pointerSheen;
            }
        }
    }
}
