using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only. Two runs of the colour probe disagreed about the *same* baseline,
    /// which means something accumulates between renders rather than the variants
    /// mattering. The wax is RealtimeEmissive, and realtime GI converges over frames.
    /// This renders one loaded scene repeatedly and measures after each render, to
    /// establish whether "the candles are saturated red" is the shipped look or an
    /// artefact of measuring a single batch-mode frame. Nothing is saved.
    /// </summary>
    public static class Phase58GiConvergenceProbe
    {
        private const string OutputFolder = "Docs/VisualReview/Phase58";
        private const int W = 2560, H = 1440;

        [MenuItem("Tools/Tarot Unity/Probe Phase 58 GI Convergence")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);
            var report = new StringBuilder();
            report.AppendLine("Front-left menu candle measured after each successive render of one loaded scene.");
            report.AppendLine("If the colour walks and then settles, a single-shot batch capture was never the shipped look.");
            report.AppendLine();
            report.AppendLine($"{"render #",-12}{"median wax RGB",-22}{"sat",-8}{"B/R"}");

            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };

            for (var i = 1; i <= 24; i++)
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.targetTexture = null;

                if (i is 1 or 2 or 3 or 4 or 6 or 8 or 12 or 16 or 20 or 24)
                {
                    var (rgb, sat, br) = Measure(rt);
                    report.AppendLine($"{i,-12}({rgb.r,3}, {rgb.g,3}, {rgb.b,3})       {sat,-8:F2}{br:F2}");
                }
            }

            rt.Release();
            Object.DestroyImmediate(rt);

            File.WriteAllText($"{OutputFolder}/gi_convergence.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static ((int r, int g, int b) rgb, float sat, float br) Measure(RenderTexture rt)
        {
            RenderTexture.active = rt;
            var shot = new Texture2D(W, H, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            shot.Apply();
            RenderTexture.active = null;

            var lit = new List<Color32>();
            for (var y = 640; y < 880; y++)
            {
                for (var x = 190; x < 300; x++)
                {
                    var p = (Color32)shot.GetPixel(x, H - 1 - y);
                    if (p.r + p.g + p.b > 260)
                    {
                        lit.Add(p);
                    }
                }
            }

            Object.DestroyImmediate(shot);
            if (lit.Count == 0)
            {
                return ((0, 0, 0), 0f, 0f);
            }

            lit.Sort((a, b) => (b.r + b.g + b.b).CompareTo(a.r + a.g + a.b));
            var mid = lit[lit.Count / 2];
            var max = Mathf.Max(mid.r, Mathf.Max(mid.g, mid.b));
            var min = Mathf.Min(mid.r, Mathf.Min(mid.g, mid.b));
            return ((mid.r, mid.g, mid.b), max == 0 ? 0f : (max - min) / (float)max, mid.b / Mathf.Max(1f, mid.r));
        }
    }
}
