using System.IO;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 53 puts the holographic foil on the Result hero card - the drawn card
    /// the player sits and admires. Phase 28 gave the flipped 3D face its sheen from
    /// a real view angle; the hero card is a UI Image on a Screen-Space Overlay
    /// canvas with no view angle, so it needs the UI shader
    /// (TarotUnity/HolographicCardUI) driven by HolographicHeroCard - a slow idle
    /// drift plus a pointer-following sheen and a parallax lean on hover.
    ///
    /// The two cards share the same glare/iridescence maths so the foil reads as one
    /// material across the product.
    /// </summary>
    public static class Phase53HeroHolographicBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/Result.unity";
        public const string ShaderName = "TarotUnity/HolographicCardUI";
        public const string MaterialPath = "Assets/Materials/MAT_HolographicHeroCardUI.mat";
        private const string HeroName = "Phase12_ResultCardArtworkSlot";

        [MenuItem("Tools/Tarot Unity/Run Phase 53 - Hero Card Holographic Foil")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            var material = CreateOrUpdateMaterial();
            if (material == null)
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 53: ResultCanvas missing.");
                return;
            }

            var hero = FindDeep(canvas.transform, HeroName);
            if (hero == null)
            {
                Debug.LogError($"Phase 53: {HeroName} not found.");
                return;
            }

            var image = hero.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"Phase 53: {HeroName} has no Image.");
                return;
            }

            image.material = material;
            image.raycastTarget = true;   // so the pointer-reactive foil can hear hovers
            EditorUtility.SetDirty(image);

            var holo = hero.GetComponent<HolographicHeroCard>();
            if (holo == null)
            {
                holo = hero.AddComponent<HolographicHeroCard>();
            }

            var so = new SerializedObject(holo);
            so.FindProperty("heroImage").objectReferenceValue = image;
            so.FindProperty("holographicMaterial").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(holo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 53: hero card holographic foil applied.");
        }

        private static Material CreateOrUpdateMaterial()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Phase 53: shader '{ShaderName}' not found.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            // Warm, gold-leaning glare so the foil sits inside the parlor's palette,
            // with the iridescence carrying the rainbow. Tuned a touch calmer than
            // the 3D face default because the hero card is large and viewed flat.
            material.SetColor("_Color", Color.white);
            material.SetColor("_GlareColor", new Color(1f, 0.95f, 0.82f, 1f));
            material.SetFloat("_GlareIntensity", 0.62f);
            material.SetFloat("_GlareWidth", 0.22f);
            // With the decoupled band, _Sheen.x (-1..1) sweeps the band and this
            // shift keeps a full pointer sweep travelling right across the card face.
            material.SetFloat("_GlareShift", 0.85f);
            material.SetFloat("_Iridescence", 0.30f);
            material.SetVector("_Sheen", Vector4.zero);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root.gameObject;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
