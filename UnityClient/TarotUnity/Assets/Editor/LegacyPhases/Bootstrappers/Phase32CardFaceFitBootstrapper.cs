using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 32 sets the card prefab's face artwork target footprint explicitly so the
    /// serialized value is clean (the runtime code also falls back to the same default if
    /// the field is left at zero). This is the data half of the oversized-card-face fix;
    /// the behaviour lives in CardView.FitFaceArtwork.
    /// </summary>
    public static class Phase32CardFaceFitBootstrapper
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private static readonly Vector2 FaceArtworkWorldSize = new(0.64f, 1.0f);

        // The face artwork sat only ~0.003 above the cream front planes, so the opaque
        // card front occluded the (now correctly sized) art at grazing angles. Lift it
        // clear of those planes along the card's thickness axis. The face renderer's
        // parent (Front) has a 0.035 thickness scale, so this local Y maps to ~0.027 world
        // units of clearance - enough to clear the planes, small enough not to look detached.
        private const float FaceArtworkLocalLift = 0.85f;

        [MenuItem("Tools/Tarot Unity/Run Phase 32 Card Face Fit Bootstrap")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Phase 32: card prefab not found at {CardPrefabPath}");
                return;
            }

            var view = prefab.GetComponentInChildren<CardView>(true);
            if (view == null)
            {
                Debug.LogError("Phase 32: CardView not found on card prefab.");
                return;
            }

            var so = new SerializedObject(view);
            var prop = so.FindProperty("faceArtworkWorldSize");
            if (prop == null)
            {
                Debug.LogError("Phase 32: faceArtworkWorldSize property not found.");
                return;
            }

            prop.vector2Value = FaceArtworkWorldSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            var faceRenderer = new SerializedObject(view).FindProperty("faceArtworkRenderer").objectReferenceValue as SpriteRenderer;
            if (faceRenderer == null)
            {
                Debug.LogError("Phase 32: faceArtworkRenderer not wired on the prefab.");
                return;
            }

            var faceTransform = faceRenderer.transform;
            var pos = faceTransform.localPosition;
            pos.y = FaceArtworkLocalLift;
            faceTransform.localPosition = pos;
            EditorUtility.SetDirty(faceTransform);

            AssetDatabase.SaveAssets();
            Debug.Log($"Tarot Unity Phase 32 card face fit bootstrap complete (faceArtworkWorldSize={FaceArtworkWorldSize}, faceLocalY={FaceArtworkLocalLift}).");
        }
    }
}
