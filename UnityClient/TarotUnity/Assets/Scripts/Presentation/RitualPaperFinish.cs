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
        private readonly Vector2[] points = new Vector2[4];
        private readonly Vector3[] vertices = new Vector3[4];
        private CardView card;
        private Camera view;
        private ReadingFlowController flow;
        private MeshRenderer frame;
        private Mesh mesh;
        private MaterialPropertyBlock ink;

        public Renderer FrameRenderer => frame;
        public bool IsReady => isActiveAndEnabled && frame != null;

        public static RitualPaperFinish Attach(CardView card, Camera camera, ReadingFlowController flow)
        {
            if (card == null || camera == null || flow == null) return null;
            var owner = card.GetComponent<RitualPaperFinish>();
            if (owner != null) return owner;
            owner = card.gameObject.AddComponent<RitualPaperFinish>();
            owner.card = card; owner.view = camera; owner.flow = flow;
            owner.Build();
            return owner;
        }

        private void OnEnable() => Build();
        private void Build()
        {
            if (!isActiveAndEnabled || card == null || view == null || frame != null) return;
            var material = Resources.Load<Material>("Presentation/RitualPaperFinish");
            var shadow = Resources.Load<Material>("Presentation/RitualPaperShadow");
            if (material == null || shadow == null) { Debug.LogError("Ritual paper finish material is missing."); return; }
            ink ??= new MaterialPropertyBlock();
            var node = new GameObject("RitualPaperFinishMesh", typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(card.transform, false);
            mesh = new Mesh { name = "RitualPaperPerimeter", hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            // Separate queues keep every shadow below every print, with perimeter ink above them.
            mesh.subMeshCount = 2;
            var triangles = new[] { 0,1,2,0,2,3 };
            mesh.SetTriangles(triangles,0);
            mesh.SetTriangles(triangles,1);
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            frame = node.GetComponent<MeshRenderer>();
            frame.sharedMaterials = new[] { shadow,material };
            frame.shadowCastingMode = ShadowCastingMode.Off;
            frame.receiveShadows = false;
            frame.lightProbeUsage = LightProbeUsage.Off;
            frame.reflectionProbeUsage = ReflectionProbeUsage.Off;
            RitualPaperRendering.Register(frame);
            Refresh();
        }

        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (!IsReady) return;
            frame.enabled = false;
            if (card == null || view == null || flow == null || view.pixelWidth <= 2 || view.pixelHeight <= 2
                || (flow.State != ReadingFlowState.WaitingForFlip && flow.State != ReadingFlowState.ResultReady)
                || !RitualPaperRendering.TryGetInspection(gameObject.scene.handle, view, out var inspection)
                || inspection != null && inspection.IsOpen) return;
            var flip = card.GetComponent<CardFlipController>();
            if (flip != null && flip.IsFlipping) return;
            var source = card.IsFaceUp ? card.FaceArtwork : card.GetComponent<RitualPaperSurface>()?.Back;
            if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null) return;
            var bounds = source.sprite.bounds;
            var min = Vector2.one * float.PositiveInfinity;
            var max = Vector2.one * float.NegativeInfinity;
            var depth = 0f;
            for (var i = 0; i < 4; i++)
            {
                var local = new Vector3(i == 0 || i == 3 ? bounds.min.x : bounds.max.x,
                    i < 2 ? bounds.max.y : bounds.min.y, 0f);
                var p = view.WorldToScreenPoint(source.transform.TransformPoint(local));
                if (p.z <= view.nearClipPlane || p.z >= view.farClipPlane) return;
                points[i] = p; depth += p.z * 0.25f;
                min = Vector2.Min(min,p); max = Vector2.Max(max,p);
            }
            var area = 0f;
            for (var i = 0; i < 4; i++) area += points[i].x*points[(i+1)%4].y - points[(i+1)%4].x*points[i].y;
            if (Mathf.Abs(area) < 1f) return;
            var density = RitualDeckLayout.Density(view.pixelWidth,view.pixelHeight);
            var padding = 38f*density;
            min -= Vector2.one*padding; max += Vector2.one*padding;
            var worldToLocal = frame.transform.worldToLocalMatrix;
            for (var i = 0; i < 4; i++)
                vertices[i] = worldToLocal.MultiplyPoint3x4(view.ScreenToWorldPoint(new Vector3(
                    i == 0 || i == 3 ? min.x : max.x, i < 2 ? max.y : min.y, depth+0.001f)));
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            for (var i = 0; i < 4; i++) ink.SetVector(CornerProperties[i],new Vector4(points[i].x,points[i].y,0f,0f));
            var viewport = view.pixelRect;
            ink.SetVector("_Viewport",new Vector4(viewport.x,viewport.y,viewport.width,viewport.height));
            ink.SetVector("_Style",new Vector4(density,12f*density,8f*density,0.6f));
            var stroke = (card.IsFaceUp ? new Color(223f/255f,199f/255f,141f/255f) : new Color(181f/255f,139f/255f,74f/255f)).linear;
            ink.SetVector("_Stroke",new Vector4(stroke.r,stroke.g,stroke.b,1f));
            frame.SetPropertyBlock(ink);
            frame.sortingLayerID = source.sortingLayerID;
            frame.sortingOrder = source.sortingOrder;
            frame.enabled = true;
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private void Release()
        {
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
