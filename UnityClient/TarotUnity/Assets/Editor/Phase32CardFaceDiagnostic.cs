using TarotUnity.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// TEMPORARY read-only investigation for the oversized face-up card-face bug. Opens
    /// ReadingRoom, deals one card exactly like DeckController.DealCards, flips it face up
    /// with HD art, then logs the authoritative state: the actual CardView.faceArtworkRenderer
    /// field (via SerializedObject), the resolved sprite/PPU, and every renderer under the
    /// card unfiltered. Spawned card is destroyed; no scene is saved.
    /// </summary>
    public static class Phase32CardFaceDiagnostic
    {
        [MenuItem("Tools/Tarot Unity/Run Phase 32 Card Face Diagnostic")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>("Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Debug.Log($"P32 catalog={(catalog != null ? "loaded" : "NULL")} prefab={(prefab != null ? "loaded" : "NULL")}");
            if (catalog == null || prefab == null)
            {
                return;
            }

            var deck = Object.FindFirstObjectByType<DeckController>();
            Transform cardParent = deck != null ? deck.transform : null;
            if (deck != null)
            {
                var p = new SerializedObject(deck).FindProperty("cardParent");
                if (p != null && p.objectReferenceValue is Transform t)
                {
                    cardParent = t;
                }
            }

            Debug.Log($"P32 deck={(deck != null ? deck.name : "NULL")} cardParent={(cardParent != null ? cardParent.name : "NULL")} parentLossy={(cardParent != null ? cardParent.lossyScale.ToString() : "n/a")}");

            const string key = "major_01";
            var slot = GameObject.Find("PresentSlot");
            var card = Object.Instantiate(prefab, cardParent);
            if (slot != null)
            {
                card.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
            }

            var view = card.GetComponentInChildren<CardView>(true);
            Debug.Log($"P32 view={(view != null ? "found" : "NULL")}");

            view.SetFaceUp(true);
            var sprite = catalog.FindSprite(key);
            Debug.Log($"P32 FindSprite({key}) -> {(sprite != null ? sprite.name : "NULL")} ppu={(sprite != null ? sprite.pixelsPerUnit : 0)} rectPx={(sprite != null ? sprite.rect.size.ToString() : "n/a")} boundsWorldAtScale1={(sprite != null ? sprite.bounds.size.ToString() : "n/a")}");
            view.SetFaceArtwork(sprite);

            var so = new SerializedObject(view);
            var far = so.FindProperty("faceArtworkRenderer").objectReferenceValue as SpriteRenderer;
            Debug.Log($"P32 faceArtworkRenderer FIELD -> {(far != null ? far.name : "NULL")}");
            if (far != null)
            {
                Debug.Log($"P32   far.sprite={(far.sprite != null ? far.sprite.name : "null")} enabled={far.enabled} lossy={far.transform.lossyScale} localScale={far.transform.localScale} worldBounds={far.bounds.size} mat={(far.sharedMaterial != null ? far.sharedMaterial.name : "null")}");
            }

            Debug.Log("P32 === FACE-LAYER STACKING (centerY = height above table; higher occludes lower for a top-down view) ===");
            foreach (var r in card.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled)
                {
                    continue;
                }

                var n = r.name;
                var isFaceLayer = n.Contains("Face") || n.Contains("Front") || n.Contains("Glass")
                    || n.Contains("CardFacePlane") || n.Contains("Artwork") || n == "Phase15_CardBody";
                if (!isFaceLayer)
                {
                    continue;
                }

                var sr = r as SpriteRenderer;
                var sortOrder = sr != null ? sr.sortingOrder.ToString() : "(mesh)";
                var queue = r.sharedMaterial != null ? r.sharedMaterial.renderQueue.ToString() : "-";
                var mat = r.sharedMaterial != null ? r.sharedMaterial.name : "-";
                Debug.Log($"P32   '{r.name,-30}' centerY={r.bounds.center.y:0.0000} sizeXZ=({r.bounds.size.x:0.00},{r.bounds.size.z:0.00}) sort={sortOrder} queue={queue} mat={mat}");
            }

            Object.DestroyImmediate(card);
            Debug.Log("P32 diagnostic complete.");
        }
    }
}
