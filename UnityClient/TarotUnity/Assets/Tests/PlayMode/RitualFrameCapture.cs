using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>Capture world grading first and overlay UI last, as the normal player does.</summary>
    internal sealed class RitualFrameCapture : IDisposable
    {
        private const int CaptureLayer = 30;
        private readonly Camera camera;
        private readonly Canvas canvas;
        private readonly Camera overlay;
        private readonly RenderTexture texture;
        private readonly RenderTexture uiTexture;
        private readonly Material composite;
        private readonly RenderTexture originalTarget;
        private readonly float originalAspect;
        private readonly int originalMask;
        private readonly RenderMode originalMode;
        private readonly Camera originalWorldCamera;
        private readonly float originalDistance;
        private readonly Dictionary<GameObject, int> layers = new();

        public RitualFrameCapture(Camera camera, Canvas canvas, int width, int height)
        {
            this.camera = camera;
            this.canvas = canvas;
            originalTarget = camera.targetTexture;
            originalAspect = camera.aspect;
            originalMask = camera.cullingMask;
            originalMode = canvas.renderMode;
            originalWorldCamera = canvas.worldCamera;
            originalDistance = canvas.planeDistance;
            texture = new RenderTexture(width, height, 24);
            uiTexture = new RenderTexture(width, height, 24);
            composite = new Material(Shader.Find("Hidden/Tarot/CaptureComposite"));
            camera.targetTexture = texture;
            camera.aspect = (float)width / height;
            camera.cullingMask &= ~(1 << CaptureLayer);
            overlay = new GameObject("RitualCaptureOverlay", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            overlay.CopyFrom(camera);
            overlay.targetTexture = uiTexture;
            overlay.cullingMask = 1 << CaptureLayer;
            overlay.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            var overlayData = overlay.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Base;
            overlayData.renderPostProcessing = false;
            overlayData.volumeLayerMask = 0;
            overlay.clearFlags = CameraClearFlags.SolidColor;
            overlay.backgroundColor = Color.clear;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = overlay;
            canvas.planeDistance = overlay.nearClipPlane + 0.05f;
            CaptureLayers();
        }

        private void CaptureLayers()
        {
            foreach (var node in canvas.GetComponentsInChildren<Transform>(true))
            {
                var value = node.gameObject;
                if (!layers.ContainsKey(value)) layers.Add(value, value.layer);
                value.layer = CaptureLayer;
            }
        }

        public Texture2D Render()
        {
            var previous = RenderTexture.active;
            try
            {
                CaptureLayers();
                overlay.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
                overlay.fieldOfView = camera.fieldOfView;
                overlay.aspect = camera.aspect;
                Canvas.ForceUpdateCanvases();
                var request = new RenderPipeline.StandardRequest { destination = texture };
                for (var i = 0; i < 4; i++) RenderPipeline.SubmitRenderRequest(camera, request);
                overlay.Render();
                Graphics.Blit(uiTexture, texture, composite);
                RenderTexture.active = texture;
                var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                return pixels;
            }
            finally { RenderTexture.active = previous; }
        }

        public void Dispose()
        {
            if (canvas != null)
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalWorldCamera;
                canvas.planeDistance = originalDistance;
            }
            foreach (var entry in layers) if (entry.Key != null) entry.Key.layer = entry.Value;
            if (camera != null)
            {
                camera.targetTexture = originalTarget;
                camera.aspect = originalAspect;
                camera.cullingMask = originalMask;
            }
            if (overlay != null)
            {
                overlay.targetTexture = null;
                overlay.gameObject.SetActive(false);
                Object.Destroy(overlay.gameObject);
            }
            texture.Release();
            uiTexture.Release();
            Object.Destroy(texture);
            Object.Destroy(uiTexture);
            Object.Destroy(composite);
        }
    }
}
