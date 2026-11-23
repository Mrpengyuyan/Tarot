using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Presentation
{
    /// <summary>Card-owned native perimeter ink; the printed sprites and pointer collider stay untouched.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1400)]
    public sealed class RitualPaperFinish : MonoBehaviour
    {
        private static readonly int[] CornerProperties = { Shader.PropertyToID("_Corner0"), Shader.PropertyToID("_Corner1"), Shader.PropertyToID("_Corner2"), Shader.PropertyToID("_Corner3") };
        private static readonly int ViewportProperty = Shader.PropertyToID("_Viewport");
        private static readonly int StyleProperty = Shader.PropertyToID("_Style");
        private static readonly int StrokeProperty = Shader.PropertyToID("_Stroke");
        private static readonly int[] QuadTriangles = { 0, 1, 2, 0, 2, 3 };
        private static Material cachedFinishMaterial;
        private static Material cachedShadowMaterial;

        private readonly Vector2[] points = new Vector2[4];
        private readonly Vector3[] vertices = new Vector3[4];
        private CardView card;
        private CardFlipController flip;
        private RitualPaperSurface surface;
        private Camera view;
        private ReadingFlowController flow;
        private RitualPickPresentation pick;
        private bool picking;
        private MeshRenderer frame;
        private Mesh mesh;
        private MaterialPropertyBlock ink;

        private bool geometryValid;
        private Matrix4x4 lastSourceMatrix;
        private Matrix4x4 lastFrameWorldToLocal;
        private Matrix4x4 lastCameraMatrix;
        private Matrix4x4 lastCameraProjection;
        private Rect lastViewport;
        private Sprite lastSprite;
        private Color lastSourceColor;
        private bool lastFaceUp;
        private bool lastPicking;
        private RitualPickPresentation.FinishStage lastStage;
        private int lastSortingLayerId;
        private int lastSortingOrder;

        public Renderer FrameRenderer => frame;
        public bool IsReady => isActiveAndEnabled && frame != null;
        internal int RebuildCount { get; private set; }

        public static RitualPaperFinish Attach(CardView card, Camera camera, ReadingFlowController flow)
            => AttachTo(card, camera, flow, null, false);

        internal static RitualPaperFinish AttachPick(CardView card, Camera camera, ReadingFlowController flow, RitualPickPresentation pick)
            => pick != null ? AttachTo(card, camera, flow, pick, true) : null;

        private static RitualPaperFinish AttachTo(CardView card, Camera camera, ReadingFlowController flow, RitualPickPresentation pick, bool picking)
        {
            if (card == null || camera == null || flow == null) return null;
            var owner = card.GetComponent<RitualPaperFinish>();
            if (owner == null) owner = card.gameObject.AddComponent<RitualPaperFinish>();
            owner.card = card;
            owner.flip = card.GetComponent<CardFlipController>();
            owner.surface = card.GetComponent<RitualPaperSurface>();
            owner.view = camera;
            owner.flow = flow;
            owner.pick = pick;
            owner.picking = picking;
            owner.geometryValid = false;
            owner.Build();
            owner.Refresh();
            return owner;
        }

        private void OnEnable() => Build();

        private void Build()
        {
            if (!isActiveAndEnabled || card == null || view == null || frame != null) return;
            if (cachedFinishMaterial == null) cachedFinishMaterial = Resources.Load<Material>("Presentation/RitualPaperFinish");
            if (cachedShadowMaterial == null) cachedShadowMaterial = Resources.Load<Material>("Presentation/RitualPaperShadow");
            if (cachedFinishMaterial == null || cachedShadowMaterial == null)
            {
                Debug.LogError("Ritual paper finish material is missing.");
                return;
            }
            ink ??= new MaterialPropertyBlock();
            var node = new GameObject("RitualPaperFinishMesh", typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(card.transform, false);
            mesh = new Mesh { name = "RitualPaperPerimeter", hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            // Separate queues keep every shadow below every print, with perimeter ink above them.
            mesh.subMeshCount = 2;
            mesh.SetTriangles(QuadTriangles, 0);
            mesh.SetTriangles(QuadTriangles, 1);
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            frame = node.GetComponent<MeshRenderer>();
            frame.sharedMaterials = new[] { cachedShadowMaterial, cachedFinishMaterial };
            frame.shadowCastingMode = ShadowCastingMode.Off;
            frame.receiveShadows = false;
            frame.lightProbeUsage = LightProbeUsage.Off;
            frame.reflectionProbeUsage = ReflectionProbeUsage.Off;
            frame.enabled = false;
            geometryValid = false;
            RitualPaperRendering.Register(frame);
            Refresh();
        }

        private void SetFrameEnabled(bool enabled)
        {
            if (frame != null && frame.enabled != enabled) frame.enabled = enabled;
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (!IsReady) return;
            if (card == null || view == null || flow == null || view.pixelWidth <= 2 || view.pixelHeight <= 2
                || !RitualPaperRendering.TryGetInspection(gameObject.scene.handle, view, out var inspection)
                || (inspection != null && inspection.IsOpen))
            {
                SetFrameEnabled(false);
                return;
            }

            var stage = RitualPickPresentation.FinishStage.Fan;
            if (picking)
            {
                if (pick == null || !pick.TryGetFinish(card, out stage))
                {
                    SetFrameEnabled(false);
                    return;
                }
            }
            else if (flow.State != ReadingFlowState.WaitingForFlip && flow.State != ReadingFlowState.ResultReady)
            {
                SetFrameEnabled(false);
                return;
            }

            if (flip != null && flip.IsFlipping)
            {
                SetFrameEnabled(false);
                return;
            }

            var faceUp = card.IsFaceUp;
            if (!faceUp && surface == null) surface = card.GetComponent<RitualPaperSurface>();
            var source = faceUp ? card.FaceArtwork : surface != null ? surface.Back : null;
            if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null)
            {
                SetFrameEnabled(false);
                return;
            }

            var sourceMatrix = source.transform.localToWorldMatrix;
            var frameWorldToLocal = frame.transform.worldToLocalMatrix;
            var cameraMatrix = view.worldToCameraMatrix;
            var cameraProjection = view.projectionMatrix;
            var viewport = view.pixelRect;
            var sprite = source.sprite;
            var sourceColor = source.color;
            var sortingLayerId = source.sortingLayerID;
            var sortingOrder = source.sortingOrder;

            if (geometryValid
                && lastFaceUp == faceUp
                && lastPicking == picking
                && (!picking || lastStage == stage)
                && lastSprite == sprite
                && lastSourceColor == sourceColor
                && lastSortingLayerId == sortingLayerId
                && lastSortingOrder == sortingOrder
                && lastViewport == viewport
                && lastSourceMatrix == sourceMatrix
                && lastFrameWorldToLocal == frameWorldToLocal
                && lastCameraMatrix == cameraMatrix
                && lastCameraProjection == cameraProjection)
            {
                SetFrameEnabled(true);
                return;
            }

            var bounds = sprite.bounds;
            var min = Vector2.one * float.PositiveInfinity;
            var max = Vector2.one * float.NegativeInfinity;
            var depth = 0f;
            for (var i = 0; i < 4; i++)
            {
                var local = new Vector3(i == 0 || i == 3 ? bounds.min.x : bounds.max.x,
                    i < 2 ? bounds.max.y : bounds.min.y, 0f);
                var p = view.WorldToScreenPoint(sourceMatrix.MultiplyPoint3x4(local));
                if (p.z <= view.nearClipPlane || p.z >= view.farClipPlane)
                {
                    geometryValid = false;
                    SetFrameEnabled(false);
                    return;
                }
                points[i] = p;
                depth += p.z * 0.25f;
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            var area = 0f;
            for (var i = 0; i < 4; i++) area += points[i].x * points[(i + 1) % 4].y - points[(i + 1) % 4].x * points[i].y;
            if (Mathf.Abs(area) < 1f)
            {
                geometryValid = false;
                SetFrameEnabled(false);
                return;
            }

            var density = RitualDeckLayout.Density(view.pixelWidth, view.pixelHeight);
            var style = new Vector4(1f, 12f, 8f, .6f);
            var stroke = faceUp ? new Color(223f / 255f, 199f / 255f, 141f / 255f) : new Color(181f / 255f, 139f / 255f, 74f / 255f);
            var opacity = 1f;
            if (picking)
            {
                style = stage == RitualPickPresentation.FinishStage.Fan ? new Vector4(1f, 5f, 6f, .6f)
                    : stage == RitualPickPresentation.FinishStage.Flying ? new Vector4(1f, 16f, 12.5f, 10f / 15f)
                    : new Vector4(1f, 8f, 7f, 8f / 15f);
                stroke = stage == RitualPickPresentation.FinishStage.Fan ? new Color(186f / 255f, 137f / 255f, 67f / 255f)
                    : stage == RitualPickPresentation.FinishStage.Flying ? new Color(208f / 255f, 178f / 255f, 120f / 255f)
                    : new Color(192f / 255f, 161f / 255f, 104f / 255f);
                opacity = Mathf.Clamp01(sourceColor.a);
            }

            style.x *= density; style.y *= density; style.z *= density; style.w *= opacity;
            var padding = Mathf.Abs(style.y) + 3f * style.z + 2f * density;
            min -= Vector2.one * padding; max += Vector2.one * padding;
            for (var i = 0; i < 4; i++)
                vertices[i] = frameWorldToLocal.MultiplyPoint3x4(view.ScreenToWorldPoint(new Vector3(
                    i == 0 || i == 3 ? min.x : max.x, i < 2 ? max.y : min.y, depth + 0.001f)));
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            for (var i = 0; i < 4; i++) ink.SetVector(CornerProperties[i], new Vector4(points[i].x, points[i].y, 0f, 0f));
            ink.SetVector(ViewportProperty, new Vector4(viewport.x, viewport.y, viewport.width, viewport.height));
            ink.SetVector(StyleProperty, style);
            stroke = stroke.linear;
            if (picking) { stroke.r *= sourceColor.r; stroke.g *= sourceColor.g; stroke.b *= sourceColor.b; }
            ink.SetVector(StrokeProperty, new Vector4(stroke.r, stroke.g, stroke.b, opacity));
            frame.SetPropertyBlock(ink);
            frame.sortingLayerID = sortingLayerId;
            frame.sortingOrder = sortingOrder;
            SetFrameEnabled(true);

            lastSourceMatrix = sourceMatrix;
            lastFrameWorldToLocal = frameWorldToLocal;
            lastCameraMatrix = cameraMatrix;
            lastCameraProjection = cameraProjection;
            lastViewport = viewport;
            lastSprite = sprite;
            lastSourceColor = sourceColor;
            lastFaceUp = faceUp;
            lastPicking = picking;
            lastStage = stage;
            lastSortingLayerId = sortingLayerId;
            lastSortingOrder = sortingOrder;
            geometryValid = true;
            RebuildCount++;
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();

        private void Release()
        {
            geometryValid = false;
            if (frame != null)
            {
                RitualPaperRendering.Unregister(frame);
                frame.gameObject.SetActive(false);
                Destroy(frame.gameObject);
            }
            if (mesh != null) Destroy(mesh);
            frame = null; mesh = null;
        }
    }
}
