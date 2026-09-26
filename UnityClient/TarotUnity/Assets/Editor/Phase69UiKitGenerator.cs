using System.IO;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 69: draws the glass-and-card-stock kit in C#, so it rebuilds without Python/PIL.
    /// Both panels are 96 px with an 8 px outer ring - clear on the glass, a soft shadow on the
    /// card stock - so a rect shows the same visible size in either state. Output is
    /// deterministic; files are only rewritten when their bytes change.
    /// </summary>
    public static class Phase69UiKitGenerator
    {
        public const string GlassPath = "Assets/Art/MidnightParlor/Sprites/GlassPanel.png";
        public const string CardStockPath = "Assets/Art/MidnightParlor/Sprites/CardStock.png";
        public const string SparklePath = "Assets/Art/MidnightParlor/Sprites/Sparkle.png";

        private const int PanelSize = 96;
        private const float Margin = 8f;
        private const float Radius = 4f;
        private const int PanelBorder = 32;

        [MenuItem("Tools/Tarot Unity/Generate Phase 69 UI Kit")]
        public static void Run()
        {
            Write(GlassPath, DrawGlass());
            Write(CardStockPath, DrawCardStock());
            Write(SparklePath, DrawSparkle());
            AssetDatabase.Refresh();
            Configure(GlassPath, PanelBorder);
            Configure(CardStockPath, PanelBorder);
            Configure(SparklePath, 0);
            Debug.Log("Phase 69 UI kit generated.");
        }

        private static Color[] DrawGlass()
        {
            var fill = new Color(14f / 255f, 6f / 255f, 14f / 255f, 0.52f);
            var gold = new Color(219f / 255f, 161f / 255f, 61f / 255f, 1f);
            var px = new Color[PanelSize * PanelSize];
            for (var y = 0; y < PanelSize; y++)
            {
                for (var x = 0; x < PanelSize; x++)
                {
                    var d = PanelSdf(x + 0.5f, y + 0.5f);
                    var c = fill;
                    c.a *= Coverage(d);
                    c = Over(WithAlpha(gold, 0.55f * Band(d, 0.5f)), c);
                    c = Over(WithAlpha(gold, 0.22f * Band(d, 5.5f)), c);
                    px[y * PanelSize + x] = c;
                }
            }

            return px;
        }

        private static Color[] DrawCardStock()
        {
            var top = new Color(242f / 255f, 231f / 255f, 203f / 255f, 1f);
            var bottom = new Color(230f / 255f, 214f / 255f, 176f / 255f, 1f);
            var keyline = new Color(58f / 255f, 42f / 255f, 32f / 255f, 1f);
            var px = new Color[PanelSize * PanelSize];
            for (var y = 0; y < PanelSize; y++)
            {
                for (var x = 0; x < PanelSize; x++)
                {
                    var d = PanelSdf(x + 0.5f, y + 0.5f);
                    // Shadow: the same shape nudged 2 px down, fading over the 8 px ring.
                    var ds = PanelSdf(x + 0.5f, y + 0.5f + 2f);
                    var fade = 1f - Mathf.Clamp01(ds / Margin);
                    var c = new Color(0f, 0f, 0f, 0.55f * fade * fade);
                    var t = Mathf.InverseLerp(Margin, PanelSize - Margin, y + 0.5f);
                    var stock = Color.Lerp(bottom, top, t);
                    stock.a = Coverage(d);
                    c = Over(stock, c);
                    c = Over(WithAlpha(keyline, 0.85f * Band(d, 3.5f)), c);
                    px[y * PanelSize + x] = c;
                }
            }

            return px;
        }

        private static Color[] DrawSparkle()
        {
            const int size = 64;
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs((x + 0.5f - size / 2f) / (size / 2f));
                    var dy = Mathf.Abs((y + 0.5f - size / 2f) / (size / 2f));
                    var s = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);          // |x|^0.5 + |y|^0.5 <= 1 is a four-point star
                    var star = Mathf.Clamp01((1f - s) * 6f);
                    var r2 = dx * dx + dy * dy;
                    var glow = 0.35f * Mathf.Exp(-r2 * 12f);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Max(star, glow));
                }
            }

            return px;
        }

        /// <summary>Signed distance to the panel's rounded rect (negative inside).</summary>
        private static float PanelSdf(float x, float y)
        {
            var c = PanelSize / 2f;
            var half = c - Margin - Radius;
            var qx = Mathf.Abs(x - c) - half;
            var qy = Mathf.Abs(y - c) - half;
            var ox = Mathf.Max(qx, 0f);
            var oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - Radius;
        }

        private static float Coverage(float d) => Mathf.Clamp01(0.5f - d);

        /// <summary>A 1 px line whose centre is <paramref name="inset"/> px inside the edge.</summary>
        private static float Band(float d, float inset) => Mathf.Clamp01(1f - Mathf.Abs(-d - inset));

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Over(Color top, Color under)
        {
            var a = top.a + under.a * (1f - top.a);
            if (a <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            var r = (top.r * top.a + under.r * under.a * (1f - top.a)) / a;
            var g = (top.g * top.a + under.g * under.a * (1f - top.a)) / a;
            var b = (top.b * top.a + under.b * under.a * (1f - top.a)) / a;
            return new Color(r, g, b, a);
        }

        private static void Write(string path, Color[] pixels)
        {
            var size = (int)Mathf.Sqrt(pixels.Length);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            if (File.Exists(path) && ByteEqual(File.ReadAllBytes(path), bytes))
            {
                return;
            }

            File.WriteAllBytes(path, bytes);
        }

        private static bool ByteEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Configure(string path, int border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;   // the project default is Multiple
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
