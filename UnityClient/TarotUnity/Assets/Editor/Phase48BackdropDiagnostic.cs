using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Read-only. Answers one question before anything is built in the menu's
    /// upper third: what world height is actually visible at each depth?
    ///
    /// The camera is pitched 27 degrees down, so the top of the frame is only 9
    /// degrees below horizontal. That puts a hard ceiling on what can be staged
    /// far away - a lamp, a window or a picture hung at wall height may simply
    /// not be in shot. Projects the backdrop's corners to prove which band of it
    /// the camera can see.
    /// </summary>
    public static class Phase48BackdropDiagnostic
    {
        [MenuItem("Tools/Tarot Unity/Run Phase 48 Backdrop Diagnostic")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            camera.aspect = 16f / 9f;

            var sb = new StringBuilder();
            sb.AppendLine($"camera pos={camera.transform.position} euler={camera.transform.eulerAngles} fov={camera.fieldOfView}");
            sb.AppendLine();

            // Highest visible world Y at a range of depths: walk Y down until the
            // point lands inside the viewport.
            sb.AppendLine("depth(z) -> highest visible world Y");
            for (var z = 2f; z <= 12f; z += 1f)
            {
                var highest = float.NaN;
                for (var y = 8f; y >= -2f; y -= 0.02f)
                {
                    var vp = camera.WorldToViewportPoint(new Vector3(0f, y, z));
                    if (vp.z > 0f && vp.y <= 1f)
                    {
                        highest = y;
                        break;
                    }
                }

                sb.AppendLine($"  z={z,5:F1}  yMax={highest,6:F2}");
            }

            sb.AppendLine();
            var backdrop = GameObject.Find("MP_MenuStage")?.transform.Find("MP_ParlorBackdrop");
            if (backdrop != null)
            {
                var pos = backdrop.position;
                var scale = backdrop.lossyScale;
                var bottom = pos.y - scale.y * 0.5f;
                var top = pos.y + scale.y * 0.5f;
                sb.AppendLine($"backdrop: z={pos.z} spans world Y {bottom:F2} .. {top:F2}");

                for (var i = 0; i <= 10; i++)
                {
                    var y = Mathf.Lerp(bottom, top, i / 10f);
                    var vp = camera.WorldToViewportPoint(new Vector3(0f, y, pos.z));
                    // v = 0 at the texture's bottom edge, 1 at its top.
                    var v = Mathf.InverseLerp(bottom, top, y);
                    sb.AppendLine($"  worldY={y,6:F2}  texV={v:F2}  viewportY={vp.y:F3}" +
                                  (vp.y >= 0f && vp.y <= 1f ? "   <-- VISIBLE" : ""));
                }
            }

            System.IO.File.WriteAllText("Logs/phase48_backdrop_diagnostic.txt", sb.ToString());
            Debug.Log("Phase 48 diagnostic written to Logs/phase48_backdrop_diagnostic.txt");
        }
    }
}
