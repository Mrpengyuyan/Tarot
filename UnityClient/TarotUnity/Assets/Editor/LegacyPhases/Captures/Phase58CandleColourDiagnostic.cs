using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only. The front menu candles render as saturated red while the room's
    /// read as cream wax, on the same material - so the cause is in the scenes, not
    /// the material. This lists every light in both, plus the wax material's
    /// emission, so the fix is aimed at a measured difference. Nothing is saved.
    /// </summary>
    public static class Phase58CandleColourDiagnostic
    {
        private const string OutputFolder = "Docs/VisualReview/Phase58";

        [MenuItem("Tools/Tarot Unity/Diagnose Phase 58 Candle Colour")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);
            var report = new StringBuilder();

            ReportMaterial(report);
            ReportScene(report, "Assets/Scenes/MainMenu.unity");
            ReportScene(report, "Assets/Scenes/ReadingRoom.unity");

            File.WriteAllText($"{OutputFolder}/colour_diagnosis.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void ReportMaterial(StringBuilder report)
        {
            var path = $"{Phase37AssetFoundationBootstrapper.MaterialFolder}/MP_CandleWax.mat";
            var wax = AssetDatabase.LoadAssetAtPath<Material>(path);
            report.AppendLine("== MP_CandleWax (shared by both scenes) ==");
            if (wax == null)
            {
                report.AppendLine("  MISSING");
                return;
            }

            var emission = wax.GetColor("_EmissionColor");
            var baseColour = wax.HasProperty("_BaseColor") ? wax.GetColor("_BaseColor") : Color.magenta;
            report.AppendLine($"  baseColor    = {Fmt(baseColour)}");
            report.AppendLine($"  emissionColor= {Fmt(emission)}   (max channel {Mathf.Max(emission.r, emission.g, emission.b):F3})");
            report.AppendLine($"  emission keyword: {wax.IsKeywordEnabled("_EMISSION")}");
            report.AppendLine($"  emission map: {(wax.GetTexture("_EmissionMap") != null ? wax.GetTexture("_EmissionMap").name : "none")}");
            report.AppendLine();
        }

        private static void ReportScene(StringBuilder report, string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            report.AppendLine($"== {Path.GetFileNameWithoutExtension(scenePath)} lights ==");

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            System.Array.Sort(lights, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var light in lights)
            {
                var active = light.gameObject.activeInHierarchy ? "" : "  [INACTIVE]";
                report.AppendLine(
                    $"  {light.name,-28} {light.type,-11} i={light.intensity,5:F2} range={light.range,5:F2} " +
                    $"colour={Fmt(light.color)} pos={light.transform.position}{active}");
            }

            report.AppendLine();
        }

        private static string Fmt(Color c) => $"({c.r:F3}, {c.g:F3}, {c.b:F3})";
    }
}
