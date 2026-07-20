using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only diagnosis for the candle modelling pass. Reports what the candles
    /// are actually made of (mesh resolution, the primitive stack, the silhouette
    /// each piece presents) and renders close-ups, so the modelling work is aimed
    /// at a measured defect rather than a guess. Does not modify or save the scene.
    /// </summary>
    public static class Phase57CandleDiagnosticCapture
    {
        private const string OutputFolder = "Docs/VisualReview/Phase57";

        [MenuItem("Tools/Tarot Unity/Diagnose Phase 57 Candle Modelling")]
        public static void Run()
        {
            Directory.CreateDirectory(OutputFolder);
            var report = new StringBuilder();

            ReportPrimitiveResolution(report);

            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            ReportCandle(report, "Phase8_LeftCandle");
            ReportCandle(report, "MP_BackCandle_L");
            CaptureCandle("Phase8_LeftCandle", "menu_left_candle_after");

            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity", OpenSceneMode.Single);
            ReportCandle(report, "MP_RoomCandle_L");
            CaptureCandle("MP_RoomCandle_L", "room_left_candle_after");

            File.WriteAllText($"{OutputFolder}/candle_diagnosis.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void ReportPrimitiveResolution(StringBuilder report)
        {
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var mesh = probe.GetComponent<MeshFilter>().sharedMesh;

            // Count the distinct angular steps around the silhouette - that, not the
            // raw vertex count (caps and the UV seam inflate it), is what decides
            // whether the edge reads as a curve or as facets.
            var angles = new System.Collections.Generic.HashSet<int>();
            foreach (var v in mesh.vertices)
            {
                if (Mathf.Abs(v.x) > 0.001f || Mathf.Abs(v.z) > 0.001f)
                {
                    angles.Add(Mathf.RoundToInt(Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg));
                }
            }

            report.AppendLine("== Unity built-in Cylinder (what every candle piece is made of) ==");
            report.AppendLine($"vertices: {mesh.vertexCount}, triangles: {mesh.triangles.Length / 3}");
            report.AppendLine($"radial segments: {angles.Count}  ->  {360f / Mathf.Max(1, angles.Count):F1} degrees per facet");
            report.AppendLine();
            Object.DestroyImmediate(probe);
        }

        private static void ReportCandle(StringBuilder report, string candleName)
        {
            var candle = GameObject.Find(candleName);
            if (candle == null)
            {
                report.AppendLine($"== {candleName}: MISSING ==");
                return;
            }

            report.AppendLine($"== {candleName} (world y {candle.transform.position.y:F3}) ==");
            foreach (Transform child in candle.transform)
            {
                var filter = child.GetComponent<MeshFilter>();
                var meshName = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "(no mesh)";
                var s = child.localScale;
                report.AppendLine(
                    $"  {child.name,-9} mesh={meshName,-10} localY={child.localPosition.y,7:F3} " +
                    $"scale=({s.x:F3}, {s.y:F3}, {s.z:F3})  worldRadius={s.x:F3}");
            }

            report.AppendLine();
        }

        /// <summary>
        /// A tight close-up of one candle, framed from the scene camera's own
        /// direction so the capture shows the silhouette the player actually sees.
        /// </summary>
        private static void CaptureCandle(string candleName, string outputName)
        {
            var candle = GameObject.Find(candleName);
            if (candle == null)
            {
                return;
            }

            var rigObject = new GameObject("Phase57_CaptureCamera");
            try
            {
                var camera = rigObject.AddComponent<Camera>();
                var sceneCamera = Camera.main;
                if (sceneCamera != null)
                {
                    camera.clearFlags = sceneCamera.clearFlags;
                    camera.backgroundColor = sceneCamera.backgroundColor;
                }

                // Sit close to the flame, on the same side the player views from.
                var target = candle.transform.position;
                var viewDirection = sceneCamera != null
                    ? Vector3.ProjectOnPlane(sceneCamera.transform.forward, Vector3.up).normalized
                    : Vector3.forward;
                camera.transform.position = target - viewDirection * 0.85f + Vector3.up * 0.05f;
                camera.transform.LookAt(target - Vector3.up * 0.12f);
                camera.fieldOfView = 32f;

                var rt = new RenderTexture(1400, 1400, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                camera.targetTexture = null;

                File.WriteAllBytes($"{OutputFolder}/{outputName}.png", shot.EncodeToPNG());
                Object.DestroyImmediate(shot);
                rt.Release();
                Object.DestroyImmediate(rt);
                Debug.Log($"Phase 57: captured {outputName}.png");
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
            }
        }
    }
}
