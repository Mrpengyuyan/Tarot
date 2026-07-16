using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 37 lays the asset foundation for the Midnight Parlor visual redesign
    /// (Docs/PHASE37_VISUAL_REDESIGN_BLUEPRINT.md): it configures the importers for
    /// the CC0 PBR surfaces (ambientCG Fabric034 felt, Wood051 espresso wood) and the
    /// locally composed gold UI kit (card back, nine-slice panels/buttons, medallion,
    /// parchment, divider, socket decal, glow), then bakes the URP materials the
    /// scene rebuild phases consume. Idempotent.
    /// </summary>
    public static class Phase37AssetFoundationBootstrapper
    {
        public const string TextureFolder = "Assets/Art/MidnightParlor/Textures";
        public const string SpriteFolder = "Assets/Art/MidnightParlor/Sprites";
        public const string MaterialFolder = "Assets/Art/MidnightParlor/Materials";

        [MenuItem("Tools/Tarot Unity/Run Phase 37 Asset Foundation Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            foreach (var folder in new[] { TextureFolder, SpriteFolder })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogError($"Phase 37 bootstrap: missing asset folder {folder}.");
                    return;
                }
            }

            ConfigureSurfaceTextures();
            ConfigureUiSprites();
            AssetDatabase.Refresh();
            CreateMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 37 asset foundation bootstrap complete.");
        }

        private static void ConfigureSurfaceTextures()
        {
            foreach (var path in Directory.GetFiles(TextureFolder, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                var isNormal = assetPath.Contains("Normal");
                var isRoughness = assetPath.Contains("Roughness");
                var changed = false;
                changed |= Set(importer.textureType,
                    isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default,
                    v => importer.textureType = v);
                changed |= Set(importer.sRGBTexture, !isNormal && !isRoughness, v => importer.sRGBTexture = v);
                changed |= Set(importer.wrapMode, TextureWrapMode.Repeat, v => importer.wrapMode = v);
                changed |= Set(importer.mipmapEnabled, true, v => importer.mipmapEnabled = v);
                changed |= Set(importer.maxTextureSize, 1024, v => importer.maxTextureSize = v);
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureUiSprites()
        {
            foreach (var path in Directory.GetFiles(SpriteFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                var assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(assetPath);
                // The card back is consumed by 3D card materials, not UI images.
                var isWorldTexture = name is "TarotCardBack" or "TarotSocket" or "TarotGlow";

                var changed = false;
                changed |= Set(importer.textureType,
                    name == "TarotCardBack" ? TextureImporterType.Default : TextureImporterType.Sprite,
                    v => importer.textureType = v);
                changed |= Set(importer.sRGBTexture, true, v => importer.sRGBTexture = v);
                changed |= Set(importer.alphaIsTransparency, name != "TarotCardBack", v => importer.alphaIsTransparency = v);
                changed |= Set(importer.mipmapEnabled, isWorldTexture, v => importer.mipmapEnabled = v);
                changed |= Set(importer.maxTextureSize, 2048, v => importer.maxTextureSize = v);
                changed |= Set(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);

                if (importer.textureType == TextureImporterType.Sprite)
                {
                    changed |= Set(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);
                    var border = NineSliceBorder(name);
                    if (importer.spriteBorder != border)
                    {
                        importer.spriteBorder = border;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>Nine-slice borders in pixels (left, bottom, right, top); zero = no slicing.</summary>
        public static Vector4 NineSliceBorder(string spriteName)
        {
            switch (spriteName)
            {
                case "TarotPanel": return new Vector4(96f, 96f, 96f, 96f);
                case "TarotPanelSubtle": return new Vector4(56f, 56f, 56f, 56f);
                case "TarotButton": return new Vector4(72f, 56f, 72f, 56f);
                case "TarotParchment": return new Vector4(120f, 120f, 120f, 120f);
                default: return Vector4.zero;
            }
        }

        private static void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");

            var cloth = EnsureMaterial("MP_TableCloth", lit);
            cloth.SetTexture("_BaseMap", LoadTexture("Fabric034_1K-JPG_Color.jpg"));
            // Deep oxblood, not scarlet. The felt albedo is near-white, so under the
            // candle key + ACES + bloom a mid-red tint blew out into pool-table red;
            // the velvet has to sit dark enough that the gold and the cards stay the
            // brightest things on the table.
            cloth.SetColor("_BaseColor", new Color(0.27f, 0.095f, 0.125f, 1f));
            cloth.SetTexture("_BumpMap", LoadTexture("Fabric034_1K-JPG_NormalGL.jpg"));
            cloth.EnableKeyword("_NORMALMAP");
            cloth.SetFloat("_Smoothness", 0.12f);
            cloth.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
            EditorUtility.SetDirty(cloth);

            var wood = EnsureMaterial("MP_TableWood", lit);
            wood.SetTexture("_BaseMap", LoadTexture("Wood051_1K-JPG_Color.jpg"));
            // Dark walnut. At near-white (0.85) this multiplier let the already-brown
            // wood texture pick up every stray bounce, which is how the far table rim
            // ended up reading as a lit bar across the menu.
            wood.SetColor("_BaseColor", new Color(0.38f, 0.31f, 0.26f, 1f));
            wood.SetTexture("_BumpMap", LoadTexture("Wood051_1K-JPG_NormalGL.jpg"));
            wood.EnableKeyword("_NORMALMAP");
            wood.SetFloat("_Smoothness", 0.34f);
            wood.SetTextureScale("_BaseMap", new Vector2(2f, 1f));
            EditorUtility.SetDirty(wood);

            var back = EnsureMaterial("MP_CardBack", lit);
            back.SetTexture("_BaseMap", LoadSpriteTexture("TarotCardBack.png"));
            back.SetColor("_BaseColor", Color.white);
            back.SetFloat("_Smoothness", 0.34f);
            // Point-light specular put a hard cool glare across the back texture;
            // the foil sheen belongs to the face's holographic shader, not the back.
            back.SetFloat("_SpecularHighlights", 0f);
            back.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(back);

            var socket = EnsureTransparent(EnsureMaterial("MP_CardSocket", unlit));
            socket.SetTexture("_BaseMap", LoadSpriteTexture("TarotSocket.png"));
            socket.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(socket);

            var glow = EnsureTransparent(EnsureMaterial("MP_WarmGlow", unlit));
            glow.SetTexture("_BaseMap", LoadSpriteTexture("TarotGlow.png"));
            glow.SetColor("_BaseColor", new Color(1f, 0.86f, 0.55f, 0.55f));
            EditorUtility.SetDirty(glow);
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{fileName}");
        }

        private static Texture2D LoadSpriteTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/{fileName}");
        }

        private static Material EnsureMaterial(string materialName, Shader shader)
        {
            Directory.CreateDirectory(MaterialFolder);
            var materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static Material EnsureTransparent(Material material)
        {
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static bool Set<T>(T current, T target, Action<T> apply)
        {
            if (Equals(current, target))
            {
                return false;
            }

            apply(target);
            return true;
        }
    }
}
