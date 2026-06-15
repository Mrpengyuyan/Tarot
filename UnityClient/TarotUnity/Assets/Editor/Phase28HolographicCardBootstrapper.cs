using TarotUnity.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 28 gives the flipped card face a holographic foil sheen. It builds a
    /// material from the TarotUnity/HolographicCard shader and assigns it to the
    /// card prefab's face SpriteRenderer (the one CardView already drives with the
    /// real RWS art). The shader adds a view-angle glare band, so the sheen sweeps
    /// as the card tilts toward the pointer (Phase 23) - the card reads as glossy
    /// and dimensional. It is a single, reversible material swap; the shader falls
    /// back to Sprites/Default so the art never breaks.
    /// </summary>
    public static class Phase28HolographicCardBootstrapper
    {
        private const string ShaderName = "TarotUnity/HolographicCard";
        private const string MaterialPath = "Assets/Materials/MAT_HolographicCardFace.mat";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        private static readonly Color GlareColor = new(1.0f, 0.96f, 0.86f, 1f);
        private const float GlareIntensity = 0.5f;
        private const float GlareWidth = 0.16f;
        private const float GlareShift = 2.4f;
        private const float Iridescence = 0.26f;

        [MenuItem("Tools/Tarot Unity/Run Phase 28 Holographic Card Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 28 Holographic Card Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Phase 28 bootstrap: shader '{ShaderName}' not found. Ensure the .shader compiled.");
                return;
            }

            var material = CreateOrUpdateMaterial(shader);
            AssignToCardPrefab(material);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 28 holographic card bootstrap complete.");
        }

        private static Material CreateOrUpdateMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_Color", Color.white);
            material.SetColor("_GlareColor", GlareColor);
            material.SetFloat("_GlareIntensity", GlareIntensity);
            material.SetFloat("_GlareWidth", GlareWidth);
            material.SetFloat("_GlareShift", GlareShift);
            material.SetFloat("_Iridescence", Iridescence);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignToCardPrefab(Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            if (root == null)
            {
                Debug.LogError($"Phase 28 bootstrap: could not open card prefab at {CardPrefabPath}.");
                return;
            }

            try
            {
                var cardView = root.GetComponentInChildren<CardView>(true);
                if (cardView == null)
                {
                    Debug.LogError("Phase 28 bootstrap: no CardView on the card prefab.");
                    return;
                }

                var serialized = new SerializedObject(cardView);
                var faceRenderer = serialized.FindProperty("faceArtworkRenderer")?.objectReferenceValue as SpriteRenderer;
                if (faceRenderer == null)
                {
                    Debug.LogError("Phase 28 bootstrap: CardView has no faceArtworkRenderer assigned.");
                    return;
                }

                faceRenderer.sharedMaterial = material;
                EditorUtility.SetDirty(faceRenderer);
                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
