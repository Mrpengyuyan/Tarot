using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only experiment. The menu's front candles render at RGB(246,74,25) -
    /// saturation 0.90 - against the room's (213,149,59). This renders the real menu
    /// camera under several candidate fixes and measures the resulting wax colour,
    /// so the change is picked from data instead of taste. The scene is reloaded
    /// between variants and never saved.
    /// </summary>
    public static class Phase58CandleColourProbe
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string OutputFolder = "Docs/VisualReview/Phase58";
        private const int W = 2560, H = 1440;

        // Where the front-left candle's body lands in the menu framing.
        private static readonly RectInt WaxSample = new(190, 640, 110, 240);

        [MenuItem("Tools/Tarot Unity/Probe Phase 58 Candle Colour")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);
            var report = new StringBuilder();
            report.AppendLine("Front-left menu candle, measured off the real menu camera.");
            report.AppendLine("Target is the room's read: RGB(213,149,59), saturation 0.72, B/R 0.28.");
            report.AppendLine();
            report.AppendLine($"{"variant",-46}{"median wax RGB",-22}{"sat",-8}{"B/R"}");

            var variants = new (string Label, Action Apply)[]
            {
                // Sanity controls first. If killing a light does not move the
                // measurement, the instrument is lying and every row below it is
                // worthless - several candidates converging on one number is exactly
                // the pattern a broken probe produces.
                ("S1 CONTROL candle lights off", () => SetCandleIntensity(0f)),
                ("S2 CONTROL table fill off", () => SetFill(0f)),
                ("0 baseline", () => { }),
                ("1 candle colour -> (1, 0.72, 0.42)", () => SetCandleColour(new Color(1f, 0.72f, 0.42f))),
                ("2 candle colour -> (1, 0.78, 0.52)", () => SetCandleColour(new Color(1f, 0.78f, 0.52f))),
                ("3 table fill 1.15 -> 1.45", () => SetFill(1.45f)),
                ("4 candle intensity 4.2 -> 3.2", () => SetCandleIntensity(3.2f)),
                ("5 wax emission 1.15 -> 0.85", () => SetEmission(0.85f)),
                ("6 = 1 + 3", () => { SetCandleColour(new Color(1f, 0.72f, 0.42f)); SetFill(1.45f); }),
                ("7 = 1 + 3 + 4", () =>
                {
                    SetCandleColour(new Color(1f, 0.72f, 0.42f));
                    SetFill(1.45f);
                    SetCandleIntensity(3.2f);
                }),
                ("8 = 2 + 3 + 4", () =>
                {
                    SetCandleColour(new Color(1f, 0.78f, 0.52f));
                    SetFill(1.45f);
                    SetCandleIntensity(3.2f);
                }),
            };

            foreach (var (label, apply) in variants)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var restoreEmission = ReadEmission();
                apply();

                var (rgb, sat, br) = MeasureWax();
                report.AppendLine($"{label,-46}({rgb.r,3}, {rgb.g,3}, {rgb.b,3})       {sat,-8:F2}{br:F2}");

                // The material is a shared asset, so any emission tweak must be put
                // back or it would leak out of this read-only probe.
                SetEmission(restoreEmission);
            }

            File.WriteAllText($"{OutputFolder}/colour_probe.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void SetCandleColour(Color colour)
        {
            foreach (var name in new[] { "Phase8_LeftCandle", "Phase8_RightCandle" })
            {
                var light = GameObject.Find(name)?.GetComponent<Light>();
                if (light != null)
                {
                    light.color = colour;
                }
            }
        }

        private static void SetCandleIntensity(float intensity)
        {
            foreach (var name in new[] { "Phase8_LeftCandle", "Phase8_RightCandle" })
            {
                var candle = GameObject.Find(name);
                var light = candle?.GetComponent<Light>();
                if (light != null)
                {
                    light.intensity = intensity;
                }

                // The flicker rides around its own stored base, so it has to move too
                // or the running game snaps straight back to the old value.
                var flicker = candle?.GetComponent<CandleFlickerController>();
                if (flicker != null)
                {
                    var so = new SerializedObject(flicker);
                    so.FindProperty("baseIntensity").floatValue = intensity;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SetFill(float intensity)
        {
            var fill = GameObject.Find("MP_MenuStage")?.transform.Find("MP_TableFill")?.GetComponent<Light>();
            if (fill != null)
            {
                fill.intensity = intensity;
            }
        }

        private static float ReadEmission()
        {
            var wax = LoadWax();
            return wax != null ? wax.GetColor("_EmissionColor").maxColorComponent : 1.15f;
        }

        private static void SetEmission(float level)
        {
            var wax = LoadWax();
            if (wax != null)
            {
                wax.SetColor("_EmissionColor", new Color(level, level, level, 1f));
            }
        }

        private static Material LoadWax() => AssetDatabase.LoadAssetAtPath<Material>(
            $"{Phase37AssetFoundationBootstrapper.MaterialFolder}/MP_CandleWax.mat");

        private static ((int r, int g, int b) rgb, float sat, float br) MeasureWax()
        {
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;
            var shot = new Texture2D(W, H, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            shot.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            // ReadPixels is bottom-up; the sample rect was measured top-down.
            var lit = new List<Color32>();
            for (var y = WaxSample.y; y < WaxSample.y + WaxSample.height; y++)
            {
                for (var x = WaxSample.x; x < WaxSample.x + WaxSample.width; x++)
                {
                    var c = shot.GetPixel(x, H - 1 - y);
                    var p = (Color32)c;
                    if (p.r + p.g + p.b > 260)
                    {
                        lit.Add(p);
                    }
                }
            }

            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            if (lit.Count == 0)
            {
                return ((0, 0, 0), 0f, 0f);
            }

            lit.Sort((a, b) => (b.r + b.g + b.b).CompareTo(a.r + a.g + a.b));
            var mid = lit[lit.Count / 2];
            var max = Mathf.Max(mid.r, Mathf.Max(mid.g, mid.b));
            var min = Mathf.Min(mid.r, Mathf.Min(mid.g, mid.b));
            var sat = max == 0 ? 0f : (max - min) / (float)max;
            return ((mid.r, mid.g, mid.b), sat, mid.b / Mathf.Max(1f, mid.r));
        }
    }
}
