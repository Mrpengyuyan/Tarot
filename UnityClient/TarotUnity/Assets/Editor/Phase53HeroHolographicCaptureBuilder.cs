using System.Collections.Generic;
using System.IO;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only capture for Phase 53: the Result hero card is empty at rest (its
    /// sprite is filled at runtime), so this loads a card into it, drives the
    /// holographic material's _Sheen to two positions, and renders each - proving
    /// the foil renders on the hero card and that the band moves. It does not save
    /// the scene.
    /// </summary>
    public static class Phase53HeroHolographicCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase53";
        private const string SpritePath = "Assets/Art/Tarot/RWS1909_HD/major_01_magician.jpg";
        private const int Width = 2560;
        private const int Height = 1440;

        [MenuItem("Tools/Tarot Unity/Run Phase 53 Capture (Hero Holographic)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity", OpenSceneMode.Single);
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();

            var hero = Object.FindObjectsByType<HolographicHeroCard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (hero.Length == 0)
            {
                Debug.LogError("Phase 53 capture: no HolographicHeroCard in the scene; run the bootstrap first.");
                return;
            }

            var image = hero[0].GetComponent<Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"Phase 53 capture: sprite missing at {SpritePath}");
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = true;

            // Instance the material so the capture never dirties the shared asset.
            var instanced = new Material(image.material);
            image.material = instanced;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            Directory.CreateDirectory(ReviewFolder);
            // _Sheen.x sweeps the band; these land it low and high on the card.
            CaptureAt(camera, instanced, new Vector4(0.55f, 0.3f, 0f, 0f), "Result_hero_holo_A.png");
            CaptureAt(camera, instanced, new Vector4(-0.55f, -0.3f, 0f, 0f), "Result_hero_holo_B.png");

            Debug.Log($"Phase 53 capture written to {ReviewFolder}/");
        }

        private static void CaptureAt(Camera camera, Material material, Vector4 sheen, string fileName)
        {
            material.SetVector("_Sheen", sheen);

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var prevAspect = camera.aspect;
            var prevTarget = camera.targetTexture;
            try
            {
                camera.aspect = (float)Width / Height;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                tex.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{ReviewFolder}/{fileName}", tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = null;
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(rt);
            }
        }
    }
}
