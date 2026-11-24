using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// One-shot isolation captures used to track down which scene layer causes a
    /// visual artifact in the Phase 21 seated framing. Never saves the scene.
    /// </summary>
    public static class Phase21DiagnosticCapture
    {
        public const string OutputFolder = "Docs/VisualReview/Phase21/Diagnostics";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private static readonly string[] FramingSlotNames = { "PastSlot", "PresentSlot", "AdviceSlot" };

        [MenuItem("Tools/Tarot Unity/Run Phase 21 Diagnostic Capture")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);

            // Each case reopens the scene so disabled objects never leak between shots.
            Capture("01_baseline_no_proxies", placeProxies: false, disableRoots: null, renderUi: true);
            Capture("02_with_proxies", placeProxies: true, disableRoots: null, renderUi: true);
            Capture("03_no_ui", placeProxies: true, disableRoots: null, renderUi: false);
            Capture("04_aura_off", placeProxies: true, disableRoots: new[] { "Phase16_RitualAuraRoot" }, renderUi: true);
            Capture("05_phase12_14_off", placeProxies: true, disableRoots: new[]
            {
                "Phase12_CardRevealStage", "Phase12_RevealBackdrop", "Phase12_FocusedCardLight",
                "Phase14_CardRevealPool", "Phase14_TableDepthPlane",
            }, renderUi: true);
            Capture("06_phase15_table_off", placeProxies: true, disableRoots: new[] { "Phase15_ThreeDTableRoot" }, renderUi: true);
            Capture("07_post_off", placeProxies: true, disableRoots: new[] { "Phase20_CinematicVolume" }, renderUi: true);
            Capture("08_vignette_off", placeProxies: true, disableRoots: new[] { "Phase7_TableVignette" }, renderUi: true);

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 21 diagnostic capture complete.");
        }

        private static void Capture(string label, bool placeProxies, string[] disableRoots, bool renderUi)
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            if (disableRoots != null)
            {
                foreach (var rootName in disableRoots)
                {
                    var root = GameObject.Find(rootName);
                    if (root != null)
                    {
                        root.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning($"Diagnostic case {label}: object {rootName} not found.");
                    }
                }
            }

            var proxies = new List<GameObject>();
            if (placeProxies)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
                foreach (var slotName in FramingSlotNames)
                {
                    var slot = GameObject.Find(slotName);
                    if (prefab != null && slot != null)
                    {
                        proxies.Add(Object.Instantiate(prefab, slot.transform.position, slot.transform.rotation));
                    }
                }
            }

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning($"Diagnostic case {label}: no camera found.");
                return;
            }

            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            var canvasStates = new List<(Canvas canvas, RenderMode mode, Camera cam, float dist)>();

            if (renderUi)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    canvasStates.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                }
            }
            else
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    canvasStates.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                    canvas.gameObject.SetActive(false);
                }
            }

            try
            {
                camera.aspect = 1280f / 720f;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(OutputFolder, label + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);

                foreach (var proxy in proxies)
                {
                    if (proxy != null)
                    {
                        Object.DestroyImmediate(proxy);
                    }
                }
            }
        }
    }
}
