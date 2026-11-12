using System.Collections.Generic;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TarotUnity.Presentation
{
    /// <summary>Render real printed surfaces after the world grade, without flattening the candlelit room.</summary>
    [DisallowMultipleComponent]
    public sealed class RitualPaperRendering : MonoBehaviour
    {
        private static readonly Dictionary<int, RitualPaperRendering> Rooms = new();
        private readonly Dictionary<GameObject, int> layers = new();
        private Camera worldCamera;
        private Camera paperCamera;
        private UniversalAdditionalCameraData data;
        private int layer;
        private int originalMaskBit;
        private RitualTableAtmosphere atmosphere;

        public static RitualPaperRendering Attach(GameObject host, Camera camera)
        {
            if (host == null || camera == null) return null;
            if (Rooms.TryGetValue(host.scene.handle, out var existing) && existing != null) return existing;
            var owner = host.GetComponent<RitualPaperRendering>() ?? host.AddComponent<RitualPaperRendering>();
            owner.worldCamera = camera;
            owner.Apply();
            return owner;
        }

        internal static void Register(RitualPaperSurface surface)
        {
            if (Rooms.TryGetValue(surface.gameObject.scene.handle, out var owner) && owner != null && owner.isActiveAndEnabled)
                owner.Track(surface);
        }

        internal static void Unregister(RitualPaperSurface surface)
        {
            if (!Rooms.TryGetValue(surface.gameObject.scene.handle, out var owner) || owner == null) return;
            owner.Restore(surface.Back != null ? surface.Back.gameObject : null);
            var face = surface.GetComponent<CardView>()?.FaceArtwork;
            owner.Restore(face != null ? face.gameObject : null);
        }

        internal static void Register(Renderer renderer)
        {
            if (renderer != null && Rooms.TryGetValue(renderer.gameObject.scene.handle, out var owner) && owner != null && owner.isActiveAndEnabled)
                owner.Track(renderer);
        }

        internal static void Unregister(Renderer renderer)
        {
            if (renderer != null && Rooms.TryGetValue(renderer.gameObject.scene.handle, out var owner) && owner != null)
                owner.Restore(renderer.gameObject);
        }

        internal static bool TryGetInspection(int scene, Camera camera, out CardInspectionController inspection)
        {
            inspection = null;
            if (!Rooms.TryGetValue(scene,out var owner) || owner == null || !owner.isActiveAndEnabled || owner.worldCamera != camera) return false;
            inspection = owner.GetComponent<CardInspectionController>();
            return true;
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            if (!isActiveAndEnabled || worldCamera == null || paperCamera != null) return;
            layer = LayerMask.NameToLayer("RitualPaper");
            if (layer < 0) return;
            var mask = 1 << layer;
            originalMaskBit = worldCamera.cullingMask & mask;
            worldCamera.cullingMask &= ~mask;
            paperCamera = new GameObject("RitualPaperCamera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            SceneManager.MoveGameObjectToScene(paperCamera.gameObject, gameObject.scene);
            paperCamera.CopyFrom(worldCamera);
            paperCamera.tag = "Untagged";
            paperCamera.depth = worldCamera.depth - 1f;
            paperCamera.cullingMask = mask;
            var paperData = paperCamera.GetUniversalAdditionalCameraData();
            paperData.renderType = CameraRenderType.Overlay;
            paperData.renderPostProcessing = false;
            paperData.volumeLayerMask = 0;
            data = worldCamera.GetUniversalAdditionalCameraData();
            data.cameraStack.Add(paperCamera);
            atmosphere = RitualTableAtmosphere.Attach(paperCamera,layer);
            Rooms[gameObject.scene.handle] = this;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            SyncCamera();
            foreach (var surface in FindObjectsByType<RitualPaperSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (surface.gameObject.scene == gameObject.scene) Track(surface);
            foreach (var deck in FindObjectsByType<RitualDeckPresentation>(FindObjectsSortMode.None))
                if (deck.gameObject.scene == gameObject.scene && deck.IsReady)
                    foreach (var renderer in deck.LayerRenderers) Track(renderer);
            foreach (var finish in FindObjectsByType<RitualPaperFinish>(FindObjectsSortMode.None))
                if (finish.gameObject.scene == gameObject.scene && finish.IsReady) Track(finish.FrameRenderer);
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == worldCamera) SyncCamera();
        }

        private void SyncCamera()
        {
            if (worldCamera == null || paperCamera == null) return;
            paperCamera.transform.SetPositionAndRotation(worldCamera.transform.position, worldCamera.transform.rotation);
            paperCamera.fieldOfView = worldCamera.fieldOfView;
            paperCamera.orthographic = worldCamera.orthographic;
            paperCamera.orthographicSize = worldCamera.orthographicSize;
            paperCamera.nearClipPlane = worldCamera.nearClipPlane;
            paperCamera.farClipPlane = worldCamera.farClipPlane;
            // URP inherits the base target for the stack; retaining it here would
            // keep a released resize/capture texture referenced by this camera.
            paperCamera.targetTexture = null;
            paperCamera.aspect = worldCamera.aspect;
            paperCamera.rect = worldCamera.rect;
            atmosphere?.Fit(paperCamera);
        }

        private void Track(RitualPaperSurface surface)
        {
            Track(surface.Back);
            Track(surface.GetComponent<CardView>()?.FaceArtwork);
        }

        private void Track(Renderer renderer)
        {
            if (renderer == null) return;
            var node = renderer.gameObject;
            if (!layers.ContainsKey(node)) layers.Add(node, node.layer);
            node.layer = layer;
        }

        private void Restore(GameObject node)
        {
            if (node == null || !layers.TryGetValue(node, out var original)) return;
            if (node.layer == layer) node.layer = original;
            layers.Remove(node);
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();

        private void Release()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            atmosphere?.Dispose();
            atmosphere = null;
            if (Rooms.TryGetValue(gameObject.scene.handle, out var owner) && owner == this) Rooms.Remove(gameObject.scene.handle);
            foreach (var entry in layers)
                if (entry.Key != null && entry.Key.layer == layer) entry.Key.layer = entry.Value;
            layers.Clear();
            if (paperCamera == null) return;
            if (data != null) data.cameraStack.Remove(paperCamera);
            if (worldCamera != null) worldCamera.cullingMask = (worldCamera.cullingMask & ~(1 << layer)) | originalMaskBit;
            paperCamera.gameObject.SetActive(false);
            Destroy(paperCamera.gameObject);
            paperCamera = null;
        }
    }
}
