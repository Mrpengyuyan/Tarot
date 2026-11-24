using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 47 answers three specific notes on the menu: the scrying orb read as
    /// a plastic marble, the primary button was a flat purple plaque, and the
    /// deck's stacked edges were dead black.
    /// </summary>
    public static class Phase47OrbAndCraftBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;
        public const string OrbShaderName = "TarotUnity/ScryingOrb";

        [MenuItem("Tools/Tarot Unity/Run Phase 47 Orb And Craft Bootstrap")]
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

            ConfigureOrbTexture();
            var orbMaterial = BuildOrbMaterial();
            DeepenDeckEdges();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyOrbMaterial(orbMaterial);
            RemoveOverAdditions();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 47 orb and craft complete.");
        }

        private static void ConfigureOrbTexture()
        {
            var path = $"{SpriteFolder}/OrbInterior.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogError($"Phase 47: missing {path}; run gen_orb.py first.");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            // Equirectangular: U wraps around, V must not bleed across the poles.
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static Material BuildOrbMaterial()
        {
            var shader = Shader.Find(OrbShaderName);
            if (shader == null)
            {
                Debug.LogError($"Phase 47: shader {OrbShaderName} not found.");
                return null;
            }

            var path = $"{MaterialFolder}/MP_OrbGlass.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetTexture("_InteriorTex",
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/OrbInterior.png"));
            // Deep and smoky, not royal blue. A bright even interior reads as a
            // painted marble; the nebula should be something half-seen in the dark.
            mat.SetColor("_InteriorColor", new Color(0.30f, 0.26f, 0.56f, 1f));
            mat.SetFloat("_InteriorDepth", 0.32f);
            mat.SetColor("_GlassColor", new Color(0.13f, 0.11f, 0.26f, 1f));
            mat.SetColor("_RimColor", new Color(0.66f, 0.62f, 0.98f, 1f));
            mat.SetFloat("_RimPower", 3.0f);
            mat.SetFloat("_RimIntensity", 1.15f);
            mat.SetFloat("_SpecPower", 110f);
            mat.SetFloat("_SpecIntensity", 2.4f);
            mat.SetFloat("_Opacity", 0.93f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ApplyOrbMaterial(Material orbMaterial)
        {
            if (orbMaterial == null)
            {
                return;
            }

            var orb = GameObject.Find("MP_ScryingOrb")?.transform.Find("Orb");
            if (orb == null)
            {
                Debug.LogWarning("Phase 47: MP_ScryingOrb/Orb missing.");
                return;
            }

            orb.GetComponent<MeshRenderer>().sharedMaterial = orbMaterial;
            EditorUtility.SetDirty(orb.gameObject);

            // The halo quad was propping up a marble that could not hold an edge.
            // The shader's Fresnel rim does that job now, and better; left on, the
            // halo just fogs the rim it is meant to sell.
            var halo = GameObject.Find("MP_ScryingOrb")?.transform.Find("Halo");
            if (halo != null && halo.gameObject.activeSelf)
            {
                halo.gameObject.SetActive(false);
                EditorUtility.SetDirty(halo.gameObject);
            }
        }

        /// <summary>
        /// Undoes two additions from this phase's first pass that did not earn
        /// their place. Kept as code rather than reverted by hand so a re-run of
        /// an already-staged scene lands in the same state.
        ///
        ///  - A card fan in the mid-left sat at x=-2.35, which is exactly where
        ///    the front-left candle stands, so the candle grew out of the cards.
        ///    Even placed clear it was wrong: that side already carries the deck,
        ///    two candles, the censer and the coins, and a second pile of cards
        ///    says nothing the deck does not already say.
        ///  - Wax spills duplicated the WaxPool each candle has carried since
        ///    Phase 42, so every candle stood in two offset discs and read as
        ///    being served on a saucer.
        ///
        /// The table is full. Further additions belong in the backdrop, not on
        /// the cloth.
        /// </summary>
        private static void RemoveOverAdditions()
        {
            var fan = GameObject.Find("MP_MenuStage")?.transform.Find("MP_CardFan");
            if (fan != null)
            {
                Object.DestroyImmediate(fan.gameObject);
            }

            foreach (var candleName in new[]
            {
                "Phase8_LeftCandle", "Phase8_RightCandle", "MP_BackCandle_L", "MP_BackCandle_R",
            })
            {
                var spill = GameObject.Find(candleName)?.transform.Find("Spill");
                if (spill != null)
                {
                    Object.DestroyImmediate(spill.gameObject);
                }
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsurePrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
            }

            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// The deck's stacked edges crushed to black: MP_DeckBody was a near-black
        /// tint, so the layered card sides had no value separation and the stack
        /// read as one solid lump. A deck's charm is that you can count it.
        /// </summary>
        private static void DeepenDeckEdges()
        {
            var body = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/MP_DeckBody.mat");
            if (body == null)
            {
                Debug.LogError("Phase 47: MP_DeckBody missing.");
                return;
            }

            // Aged paper, not ink. Each card's edge can now catch the candles and
            // throw its own shadow onto the one below.
            body.SetColor("_BaseColor", new Color(0.60f, 0.50f, 0.38f, 1f));
            body.SetFloat("_Smoothness", 0.16f);
            EditorUtility.SetDirty(body);
        }
    }
}
