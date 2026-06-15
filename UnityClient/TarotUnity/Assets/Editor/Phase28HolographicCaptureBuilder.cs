using System;
using System.Collections.Generic;
using System.IO;
using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 28 verification: places face-up HD cards at the spread slots (the
    /// real "after a flip" state) and renders the seated camera view, so the
    /// holographic sheen can be judged in the context the player actually sees.
    /// </summary>
    public static class Phase28HolographicCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase28";
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const int W = 1280, H = 720;

        private static readonly (string slot, string key)[] Placements =
        {
            ("PastSlot", "major_00"),
            ("PresentSlot", "major_01"),
            ("AdviceSlot", "major_17"),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 28 Holographic Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(ReviewFolder);
            EditorSceneManager.OpenScene(ScenePath);

            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in ReadingRoom.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var spawned = new List<GameObject>();

            foreach (var (slotName, key) in Placements)
            {
                var slot = GameObject.Find(slotName);
                if (slot == null)
                {
                    continue;
                }

                var card = UnityEngine.Object.Instantiate(prefab, slot.transform.position, slot.transform.rotation);
                card.name = $"Phase28_FaceUp_{slotName}";
                var view = card.GetComponentInChildren<CardView>(true);
                if (view != null)
                {
                    view.SetFaceUp(true);
                    view.SetFaceArtwork(catalog != null ? catalog.FindSprite(key) : null);
                }

                spawned.Add(card);
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;

            try
            {
                camera.aspect = (float)W / H;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(ReviewFolder, "ReadingRoom_faceup.png"), tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
                foreach (var c in spawned)
                {
                    if (c != null)
                    {
                        UnityEngine.Object.DestroyImmediate(c);
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 28 holographic capture complete.");
        }
    }
}
