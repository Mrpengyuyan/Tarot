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
    /// Phase 32 verification: stages three face-up HD cards at the spread slots exactly
    /// like DeckController.DealCards (instantiate under the card parent, move to slot,
    /// SetFaceUp + SetFaceArtwork) and renders the seated view, so the card-face sizing
    /// fix can be judged in the context the player sees. Read-only; spawned cards are
    /// destroyed and no scene is saved.
    /// </summary>
    public static class Phase32CardFaceCaptureBuilder
    {
        private const string ReviewFolder = "Docs/VisualReview/Phase32";
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const int W = 2560, H = 1440;

        private static readonly (string slot, string key)[] Placements =
        {
            ("PastSlot", "major_00"),
            ("PresentSlot", "major_01"),
            ("AdviceSlot", "major_17"),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 32 Card Face Capture")]
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
            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            Transform cardParent = deck != null ? deck.transform : null;
            if (deck != null)
            {
                var p = new SerializedObject(deck).FindProperty("cardParent");
                if (p != null && p.objectReferenceValue is Transform t)
                {
                    cardParent = t;
                }
            }

            var spawned = new List<GameObject>();
            foreach (var (slotName, key) in Placements)
            {
                var slot = GameObject.Find(slotName);
                if (slot == null || prefab == null)
                {
                    continue;
                }

                var card = UnityEngine.Object.Instantiate(prefab, cardParent);
                card.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
                card.name = $"Phase32_FaceUp_{slotName}";
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
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(ReviewFolder, "ReadingRoom_cards_faceup.png"), tex.EncodeToPNG());
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
            Debug.Log("Tarot Unity Phase 32 card face capture complete.");
        }
    }
}
