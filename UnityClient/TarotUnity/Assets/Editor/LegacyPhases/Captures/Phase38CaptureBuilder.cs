using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 38 iteration capture: renders the ReadingRoom at HD with framing
    /// card proxies on the three-card slots so the rebuilt table, sockets, deck
    /// stack, and card backs can be reviewed per bootstrap iteration. Output goes
    /// to Docs/VisualReview/Phase38/.
    /// </summary>
    public static class Phase38CaptureBuilder
    {
        public const string ReviewFolder = "Docs/VisualReview/Phase38";
        private const int Width = 2560;
        private const int Height = 1440;
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private static readonly string[] FramingSlotNames = { "PastSlot", "PresentSlot", "AdviceSlot" };

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Capture (ReadingRoom)")]
        public static void Run() => Capture("Assets/Scenes/ReadingRoom.unity", "ReadingRoom.png", true);

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Capture (MainMenu)")]
        public static void RunMainMenu() => Capture("Assets/Scenes/MainMenu.unity", "MainMenu.png", false);

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Capture (Result)")]
        public static void RunResult() => Capture("Assets/Scenes/Result.unity", "Result.png", false);

        private static void Capture(string scenePath, string fileName, bool placeCards)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();

            var proxies = new List<GameObject>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            foreach (var slotName in placeCards ? FramingSlotNames : System.Array.Empty<string>())
            {
                var slot = GameObject.Find(slotName);
                if (slot != null && prefab != null)
                {
                    var proxy = Object.Instantiate(prefab, slot.transform.position + Vector3.up * 0.02f,
                        slot.transform.rotation);
                    // Show the deal-state look: face down, no reveal dressing.
                    var view = proxy.GetComponent<TarotUnity.Gameplay.CardView>();
                    if (view != null)
                    {
                        view.SetFaceUp(false);
                    }

                    proxies.Add(proxy);
                }
            }

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var prevAspect = camera.aspect;
            try
            {
                camera.aspect = (float)Width / Height;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                tex.Apply();
                Directory.CreateDirectory(ReviewFolder);
                File.WriteAllBytes($"{ReviewFolder}/{fileName}", tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                camera.aspect = prevAspect;
                RenderTexture.active = null;
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(rt);
                foreach (var proxy in proxies)
                {
                    if (proxy != null)
                    {
                        Object.DestroyImmediate(proxy);
                    }
                }
            }

            Debug.Log($"Phase 38 capture written to {ReviewFolder}/{fileName}");
        }
    }
}
