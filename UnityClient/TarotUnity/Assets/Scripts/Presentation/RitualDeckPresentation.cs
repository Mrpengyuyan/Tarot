using System.Collections;
using System.Collections.Generic;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Presentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1300)]
    public sealed class RitualDeckPresentation : MonoBehaviour
    {
        private enum PoseMode { Parked, Pickup, Raised, Returning }
        private struct OriginalRenderer { public Renderer Value; public bool Enabled; }
        private readonly List<Renderer> layers = new();
        private readonly List<Mesh> meshes = new();
        private readonly Vector3[] corners = new Vector3[4];
        private MaterialPropertyBlock ink;
        private OriginalRenderer[] originals;
        private DeckShuffleChoreographer deck;
        private Camera viewCamera;
        private CameraChoreographyController choreography;
        private RitualReadingView view;
        private SpreadFanController fan;
        private Transform visualRoot;
        private Material material;
        private Vector3 authoredPosition, authoredScale;
        private Quaternion authoredRotation;
        private PoseMode mode;
        private float pickupProgress, shuffleProgress, returnSince;
        private int generation;
        private bool releasing;
        private Vector3 pickupViewPosition, shuffleViewPosition;
        private Quaternion pickupViewRotation, shuffleViewRotation;
        private float pickupViewFov, shuffleViewFov;
        private bool hasShuffleFrame, shuffleCompleted;

        public IReadOnlyList<Renderer> LayerRenderers => layers;
        public bool IsReady => isActiveAndEnabled && !releasing && visualRoot != null;
        public bool IsShuffling { get; private set; }
        public float ShuffleProgress => shuffleProgress;
        public bool IsReturning => mode == PoseMode.Returning;
        public RitualDeckPose CurrentPose { get; private set; }

        internal void GetAuthoredPose(out Vector3 position, out Quaternion rotation, out Vector3 scale)
        { position = authoredPosition; rotation = authoredRotation; scale = authoredScale; }

        public static RitualDeckPresentation Attach(DeckShuffleChoreographer deck, Camera camera,
            RitualReadingView view, SpreadFanController fan, CameraChoreographyController choreography = null)
        {
            if (deck == null || camera == null) return null;
            var owner = deck.GetComponent<RitualDeckPresentation>();
            if (owner != null) return owner;
            owner = deck.gameObject.AddComponent<RitualDeckPresentation>();
            owner.deck = deck; owner.viewCamera = camera; owner.view = view; owner.fan = fan;
            owner.choreography = choreography;
            owner.authoredPosition = deck.transform.localPosition;
            owner.authoredRotation = deck.transform.localRotation;
            owner.authoredScale = deck.transform.localScale;
            deck.Presentation = owner;
            owner.Build();
            return owner;
        }

        private void OnEnable() => Build();

        private void Build()
        {
            if (!isActiveAndEnabled || deck == null || visualRoot != null || deck.transform.childCount == 0) return;
            var source = Resources.Load<Material>("Presentation/RitualDeckInk");
            if (source == null) { Debug.LogError("Ritual deck ink material is missing."); return; }
            ink ??= new MaterialPropertyBlock();
            mode = PoseMode.Parked;
            pickupProgress = shuffleProgress = 0f;
            hasShuffleFrame = shuffleCompleted = false;
            var fraction = fan != null ? fan.DeckShowing : 1f;
            if (originals == null)
            {
                var renderers = deck.GetComponentsInChildren<Renderer>(true);
                originals = new OriginalRenderer[renderers.Length];
                for (var i = 0; i < renderers.Length; i++) originals[i] = new OriginalRenderer { Value = renderers[i], Enabled = renderers[i].enabled };
            }
            foreach (var original in originals) if (original.Value != null) original.Value.enabled = false;
            material = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
            visualRoot = new GameObject("RitualDeckPaper").transform;
            // Nest below one original child: legacy shuffle still sees precisely nine authored children.
            visualRoot.SetParent(deck.transform.GetChild(0), false);
            for (var i = 0; i < RitualDeckLayout.LayerCount; i++)
            {
                var node = new GameObject("DeckPaper_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                node.transform.SetParent(visualRoot, false);
                var mesh = new Mesh { name = "RitualDeckPlane_" + i, hideFlags = HideFlags.HideAndDontSave };
                mesh.MarkDynamic();
                mesh.vertices = new Vector3[4];
                mesh.uv = new[] { new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(1f,0f), new Vector2(0f,0f) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                node.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                // Front first plus depth-writing ink implements one translucent opacity group.
                renderer.sortingOrder = -100 - i;
                layers.Add(renderer); meshes.Add(mesh);
                RitualPaperRendering.Register(renderer);
            }
            releasing = false;
            fan?.RefreshDeckVisuals(deck.transform, fraction);
            RefreshGeometry();
        }

        public void Park()
        {
            if (mode == PoseMode.Parked || mode == PoseMode.Returning) return;
            generation++;
            IsShuffling = false;
            mode = PoseMode.Returning;
            returnSince = Time.time;
        }

        public IEnumerator Pickup()
        {
            var token = ++generation;
            CaptureShuffleFrame();
            mode = PoseMode.Pickup;
            shuffleProgress = 0f;
            for (var elapsed = 0f; elapsed < RitualDeckLayout.PickupSeconds; elapsed += Time.deltaTime)
            {
                if (!Current(token)) yield break;
                pickupProgress = elapsed / RitualDeckLayout.PickupSeconds;
                RefreshGeometry();
                yield return null;
            }
            if (Current(token)) { pickupProgress = 1f; mode = PoseMode.Raised; RefreshGeometry(); }
        }

        public IEnumerator Shuffle()
        {
            var token = ++generation;
            if (!hasShuffleFrame) CaptureShuffleFrame();
            shuffleCompleted = false;
            mode = PoseMode.Raised;
            IsShuffling = true;
            try
            {
                for (var elapsed = 0f; elapsed < RitualDeckLayout.ShuffleSeconds + RitualDeckLayout.SettleSeconds; elapsed += Time.deltaTime)
                {
                    if (!Current(token)) yield break;
                    shuffleProgress = Mathf.Clamp01(elapsed / RitualDeckLayout.ShuffleSeconds);
                    RefreshGeometry();
                    yield return null;
                }
                if (Current(token)) { shuffleProgress = 1f; RefreshGeometry(); }
            }
            finally
            {
                if (this != null && generation == token) { IsShuffling = false; shuffleCompleted = true; }
            }
        }

        private void CaptureShuffleFrame()
        {
            pickupViewPosition = viewCamera.transform.position;
            pickupViewRotation = viewCamera.transform.rotation;
            pickupViewFov = viewCamera.fieldOfView;
            hasShuffleFrame = false;
            if (choreography == null || !choreography.TryGetDeckPose(out var target, out shuffleViewFov)) return;
            hasShuffleFrame = true;
            shuffleViewPosition = target.position;
            shuffleViewRotation = target.rotation;
            shuffleCompleted = false;
        }

        private bool Current(int token) => this != null && IsReady && generation == token;
        private void LateUpdate() => RefreshGeometry();

        public void RefreshGeometry()
        {
            if (!IsReady || viewCamera == null || viewCamera.pixelWidth <= 2 || viewCamera.pixelHeight <= 2) return;
            var width = viewCamera.pixelWidth;
            var height = viewCamera.pixelHeight;
            var t = (Time.time - returnSince) / RitualDeckLayout.ReturnSeconds;
            if (mode == PoseMode.Returning && t >= 1f) mode = PoseMode.Parked;
            CurrentPose = mode == PoseMode.Pickup ? RitualDeckLayout.Pickup(width, height, pickupProgress)
                : mode == PoseMode.Returning ? RitualDeckLayout.Returning(width, height, t)
                : mode == PoseMode.Raised ? RitualDeckLayout.Raised(width, height)
                : RitualDeckLayout.Parked(width, height, view != null && view.Stage == RitualReadingView.ViewStage.Question);
            var framePosition = viewCamera.transform.position;
            var frameRotation = viewCamera.transform.rotation;
            var frameFov = viewCamera.fieldOfView;
            // Lift toward the authored deck shot, then hold that world frame while the real camera approaches.
            if (hasShuffleFrame && (mode == PoseMode.Pickup || mode == PoseMode.Raised && !shuffleCompleted))
            {
                var blend = mode == PoseMode.Pickup ? pickupProgress : 1f;
                framePosition = Vector3.Lerp(pickupViewPosition, shuffleViewPosition, blend);
                frameRotation = Quaternion.Slerp(pickupViewRotation, shuffleViewRotation, blend);
                frameFov = Mathf.Lerp(pickupViewFov, shuffleViewFov, blend);
            }
            var pixelUnit = 2f * Mathf.Tan(frameFov * Mathf.Deg2Rad * 0.5f) / height;
            var rootCenter = CurrentPose.Origin + CurrentPose.Size * 0.5f;
            deck.transform.position = framePosition + frameRotation * new Vector3(
                (rootCenter.x - width * 0.5f) * pixelUnit * 2.5f, (height * 0.5f - rootCenter.y) * pixelUnit * 2.5f, 2.5f);
            deck.transform.rotation = frameRotation * Quaternion.Euler(-CurrentPose.Pitch, 0f, -CurrentPose.Roll);
            deck.transform.localScale = authoredScale;
            ink.SetFloat("_Opacity", CurrentPose.Opacity);
            ink.SetVector("_BorderSize", new Vector4(CurrentPose.Density / CurrentPose.Size.x, CurrentPose.Density / CurrentPose.Size.y, 0f, 0f));
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = RitualDeckLayout.ShuffleLayer(i, mode == PoseMode.Raised ? shuffleProgress : 0f, width, height);
                for (var j = 0; j < 4; j++)
                {
                    var p = RitualDeckLayout.ProjectCorner(CurrentPose, layer, j);
                    var depth = 2.5f * (1f - p.z / (900f * CurrentPose.Density)) - i * 0.00001f;
                    var world = framePosition + frameRotation * new Vector3((p.x - width * 0.5f) * pixelUnit * depth,
                        (height * 0.5f - p.y) * pixelUnit * depth, depth);
                    corners[j] = layers[i].transform.InverseTransformPoint(world);
                }
                meshes[i].vertices = corners;
                meshes[i].RecalculateBounds();
                layers[i].SetPropertyBlock(ink);
            }
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private void Release()
        {
            generation++;
            IsShuffling = false;
            if (visualRoot == null) return;
            releasing = true;
            var fraction = fan != null ? fan.DeckShowing : 1f;
            foreach (var renderer in layers)
                if (renderer != null) { renderer.enabled = false; RitualPaperRendering.Unregister(renderer); }
            foreach (var original in originals) if (original.Value != null) original.Value.enabled = original.Enabled;
            if (deck != null)
            {
                deck.transform.localPosition = authoredPosition;
                deck.transform.localRotation = authoredRotation;
                deck.transform.localScale = authoredScale;
                fan?.RefreshDeckVisuals(deck.transform, fraction);
            }
            visualRoot.gameObject.SetActive(false);
            Destroy(visualRoot.gameObject);
            foreach (var mesh in meshes) Destroy(mesh);
            Destroy(material);
            visualRoot = null; material = null;
            layers.Clear(); meshes.Clear();
        }
    }
}
