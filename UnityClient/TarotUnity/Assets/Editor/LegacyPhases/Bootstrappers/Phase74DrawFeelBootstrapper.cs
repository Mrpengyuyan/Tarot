using System.IO;
using System.Linq;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 74, the user's third pass on the draw. Runs after the Phase 73 bootstrap. Idempotent.
    /// - The shuffle is stretched from two seconds to three: every beat slows by about half.
    /// - The shuffle showed blank tan cards: the deck is eight card-sized blocks in the tan
    ///   paper-edge material with one card back laid on top, so a cut exposed a bare tan top on
    ///   each pile. Every block now carries a card back on its top face.
    /// - The flip turns the card over (see CardFlipController): this sets its beats and gives the
    ///   card prefab the light that sweeps the face - a quad on the artwork, off until the sweep
    ///   (which sizes it to the sprite), drawing a generated soft band (MP_FlipSheen.png) through
    ///   an additive material.
    /// </summary>
    public static class Phase74DrawFeelBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string SheenTexturePath = "Assets/Art/MidnightParlor/Textures/MP_FlipSheen.png";
        public const string SheenMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_FlipSheen.mat";
        public const string SheenName = "Phase74_RevealSheen";
        public const string DeckBackName = "Phase74_CardBack";
        private const string CardBackMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_CardBack.mat";

        // A child of the face artwork, facing the same way, a hair in front of it (the art's -z is
        // the card's up; the art's z runs through Front's 0.035 thickness, so -0.1 is 0.0035 m).
        // The flip sizes it to the sprite on the card when it sweeps.
        public const string ArtworkPath = "Front/Phase12_FaceArtworkPlaceholder";
        private static readonly Vector3 SheenPosition = new Vector3(0f, 0f, -0.1f);

        [MenuItem("Tools/Tarot Unity/Run Phase 74 Draw Feel Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var material = EnsureSheenMaterial(EnsureSheenTexture());
            SetUpCardFlip(material);
            SlowTheShuffle();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 74 draw feel bootstrap complete.");
        }

        /// <summary>
        /// A diagonal band of warm white light, soft-edged, on a clear ground; the edge columns are
        /// fully clear so the clamped texture shows nothing once the band is offset off the face.
        /// </summary>
        private static Texture2D EnsureSheenTexture()
        {
            const int w = 128;
            const int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var v = (y + 0.5f) / h;
                var centre = 0.5f + 0.25f * (v - 0.5f);
                for (var x = 0; x < w; x++)
                {
                    var u = (x + 0.5f) / w;
                    var d = u - centre;
                    var core = Mathf.Exp(-d * d / (2f * 0.045f * 0.045f));
                    var halo = Mathf.Exp(-d * d / (2f * 0.13f * 0.13f));
                    var a = x < 2 || x >= w - 2 ? 0f : Mathf.Clamp01(0.85f * core + 0.25f * halo);
                    pixels[y * w + x] = new Color(1f, 0.96f, 0.88f, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            var full = Path.GetFullPath(SheenTexturePath);
            if (!File.Exists(full) || !File.ReadAllBytes(full).SequenceEqual(png))
            {
                File.WriteAllBytes(full, png);
            }

            AssetDatabase.ImportAsset(SheenTexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheenTexturePath);
            var dirty = importer.textureType != TextureImporterType.Default
                        || importer.wrapMode != TextureWrapMode.Clamp
                        || importer.mipmapEnabled
                        || !importer.alphaIsTransparency
                        || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(SheenTexturePath);
        }

        /// <summary>URP Unlit, transparent, additive: the band adds light to the face and never darkens it.</summary>
        private static Material EnsureSheenMaterial(Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SheenMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, SheenMaterialPath);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 2f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_QueueOffset", 50f);
            material.renderQueue = 3050;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetUpCardFlip(Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var art = root.transform.Find(ArtworkPath);
                var earlier = root.transform.Find(SheenName);
                if (earlier != null)
                {
                    earlier.SetParent(art, false);   // a first draft hung it off the card root
                }

                var sheen = art.Find(SheenName);
                if (sheen == null)
                {
                    sheen = new GameObject(SheenName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    sheen.SetParent(art, false);
                }

                sheen.localPosition = SheenPosition;
                sheen.localRotation = Quaternion.identity;
                sheen.localScale = Vector3.one;
                sheen.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var renderer = sheen.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.enabled = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var flip = new SerializedObject(root.GetComponent<CardFlipController>());
                flip.FindProperty("revealSheen").objectReferenceValue = renderer;
                flip.FindProperty("anticipationPause").floatValue = 0.22f;
                flip.FindProperty("flipDuration").floatValue = 0.62f;
                flip.FindProperty("faceRevealPause").floatValue = 0.16f;
                flip.FindProperty("settleSeconds").floatValue = 0.18f;
                flip.FindProperty("sheenIntensity").floatValue = 0.85f;
                flip.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>About three seconds: the Phase 73 two-second timing with every beat half as long again.</summary>
        /// <summary>
        /// A card back on the top face of each deck block (the blocks are 0.8 x 0.02 x 1.18; the
        /// back is 0.78 x 1.16 like the stack's top back, a hair above the face).
        /// </summary>
        private static void BackEveryDeckCard()
        {
            var back = AssetDatabase.LoadAssetAtPath<Material>(CardBackMaterialPath);
            var stack = GameObject.Find("DeckStack").transform.Find("MP_DeckStack");
            foreach (Transform block in stack)
            {
                if (!block.name.StartsWith("MP_DeckCard_"))
                {
                    continue;
                }

                var face = block.Find(DeckBackName);
                if (face == null)
                {
                    face = new GameObject(DeckBackName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    face.SetParent(block, false);
                }

                var size = block.localScale;
                face.localPosition = new Vector3(0f, 0.55f, 0f);
                face.localRotation = Quaternion.Euler(90f, 0f, 0f);
                face.localScale = new Vector3(0.78f / size.x, 1.16f / size.z, 1f);
                face.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var renderer = face.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = back;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorUtility.SetDirty(face.gameObject);
            }
        }

        private static void SlowTheShuffle()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var shuffle = new SerializedObject(Object.FindFirstObjectByType<DeckShuffleChoreographer>());
            shuffle.FindProperty("anticipationSeconds").floatValue = 0.2f;
            shuffle.FindProperty("cutSeconds").floatValue = 0.55f;
            shuffle.FindProperty("recutSeconds").floatValue = 0.4f;
            shuffle.FindProperty("riffleCardSeconds").floatValue = 0.2f;
            shuffle.FindProperty("riffleStagger").floatValue = 0.065f;
            shuffle.FindProperty("settleSeconds").floatValue = 0.18f;
            shuffle.FindProperty("squareHoldSeconds").floatValue = 0.35f;
            shuffle.ApplyModifiedPropertiesWithoutUndo();
            BackEveryDeckCard();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
