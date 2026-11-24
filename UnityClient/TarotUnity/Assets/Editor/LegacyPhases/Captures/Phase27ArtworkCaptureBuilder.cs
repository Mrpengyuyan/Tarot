using System;
using System.IO;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 27 verification: renders the Result scene with a real HD card placed
    /// in the result hero slot (as the presenter does at runtime), so the HD card
    /// face can be eyeballed against the old low-res art.
    /// </summary>
    public static class Phase27ArtworkCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase27";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const int W = 1280, H = 720;

        [MenuItem("Tools/Tarot Unity/Run Phase 27 Artwork Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            EditorSceneManager.OpenScene(ResultScenePath);

            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            var slot = GameObject.Find("ResultCanvas")?.transform.Find("Phase12_ResultCardArtworkSlot")?.GetComponent<Image>();
            if (catalog != null && slot != null)
            {
                slot.sprite = catalog.FindSprite("major_01"); // The Magician
                slot.color = Color.white;
                slot.preserveAspect = true;
                slot.enabled = true;
            }

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in Result scene.");
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var canvasStates = PrepareCanvases(camera);

            try
            {
                camera.aspect = (float)W / H;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(ReviewFolder, "Result_with_HD_card.png"), tex.EncodeToPNG());
            }
            finally
            {
                RestoreCanvases(canvasStates);
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 27 artwork capture complete.");
        }

        private static (Canvas c, RenderMode m, Camera cam, float d, bool p)[] PrepareCanvases(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new (Canvas, RenderMode, Camera, float, bool)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                states[i] = (c, c.renderMode, c.worldCamera, c.planeDistance, c.pixelPerfect);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            return states;
        }

        private static void RestoreCanvases((Canvas c, RenderMode m, Camera cam, float d, bool p)[] states)
        {
            foreach (var s in states)
            {
                if (s.c == null)
                {
                    continue;
                }

                s.c.renderMode = s.m;
                s.c.worldCamera = s.cam;
                s.c.planeDistance = s.d;
                s.c.pixelPerfect = s.p;
            }
        }
    }
}
