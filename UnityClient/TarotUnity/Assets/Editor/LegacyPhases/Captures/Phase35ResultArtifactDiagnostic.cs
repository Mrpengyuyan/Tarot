using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 35 diagnostic: the Result screen shows a stray 3D plate poking out from
    /// behind the reading panel (visible left of the 建议 body copy in the Phase 31
    /// archive). This read-only tool opens Result.unity and projects every active
    /// world-space renderer into the capture camera's viewport so the offending
    /// object can be identified by name instead of guessed at. It also dumps the
    /// hero-slot UI rects (to locate the misaligned backing seam) and the MainMenu
    /// start/status/quit rects (for the button polish pass). It never saves.
    /// </summary>
    public static class Phase35ResultArtifactDiagnostic
    {
        // Artifact zone measured on the 2560x1440 archive shot (viewport, y up).
        private const float ZoneXMin = 0.30f, ZoneXMax = 0.48f;
        private const float ZoneYMin = 0.38f, ZoneYMax = 0.48f;

        [MenuItem("Tools/Tarot Unity/Run Phase 35 Result Artifact Diagnostic")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("Phase 35 diagnostic: no camera in Result.unity");
                return;
            }

            camera.aspect = 2560f / 1440f;

            var report = new StringBuilder();
            report.AppendLine("=== Phase 35 Result renderer projection report ===");
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var b = r.bounds;
                var vp = camera.WorldToViewportPoint(b.center);
                var inZone = vp.z > 0f &&
                             vp.x >= ZoneXMin && vp.x <= ZoneXMax &&
                             vp.y >= ZoneYMin && vp.y <= ZoneYMax;
                report.AppendLine(
                    $"{(inZone ? ">>> IN ZONE " : "            ")}" +
                    $"{Path(r.transform)} | type={r.GetType().Name} | " +
                    $"viewport=({vp.x:F3},{vp.y:F3},z{vp.z:F2}) | " +
                    $"worldCenter={b.center} | worldSize={b.size}");
            }

            Debug.Log(report.ToString());

            DumpResultHeroUi();
            DumpMainMenu();
        }

        private static void DumpResultHeroUi()
        {
            var report = new StringBuilder();
            report.AppendLine("=== Phase 35 Result hero-region UI rects (left third) ===");
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                var rt = g.rectTransform;
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                // Canvas is ScreenSpaceOverlay in-scene: world corners are pixel-ish coords.
                var minX = Mathf.Min(corners[0].x, corners[2].x);
                var maxX = Mathf.Max(corners[0].x, corners[2].x);
                if (minX > 700f)
                {
                    continue; // right of the hero third at 1280 reference width
                }

                var color = g.color;
                report.AppendLine(
                    $"{Path(rt)} | type={g.GetType().Name} | active={g.gameObject.activeInHierarchy} | " +
                    $"xy=({corners[0].x:F1},{corners[0].y:F1})-({corners[2].x:F1},{corners[2].y:F1}) " +
                    $"w={maxX - minX:F1} h={corners[2].y - corners[0].y:F1} | " +
                    $"color=({color.r:F2},{color.g:F2},{color.b:F2},a{color.a:F2})");
            }

            Debug.Log(report.ToString());
        }

        private static void DumpMainMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var report = new StringBuilder();
            report.AppendLine("=== Phase 35 MainMenu button rects ===");
            var canvas = GameObject.Find("MainMenuCanvas");
            foreach (var name in new[] { "StartReadingButton", "StatusText", "QuitButton" })
            {
                var t = canvas != null ? canvas.transform.Find(name) as RectTransform : null;
                if (t == null)
                {
                    report.AppendLine($"{name}: NOT FOUND");
                    continue;
                }

                report.AppendLine(
                    $"{name}: anchoredPos={t.anchoredPosition} sizeDelta={t.sizeDelta} " +
                    $"anchorMin={t.anchorMin} anchorMax={t.anchorMax} pivot={t.pivot}");
                foreach (Transform child in t)
                {
                    var img = child.GetComponent<UnityEngine.UI.Image>();
                    var txt = child.GetComponent<UnityEngine.UI.Text>();
                    var crt = child as RectTransform;
                    report.AppendLine(
                        $"  child {child.name}: active={child.gameObject.activeSelf} " +
                        $"anchoredPos={crt.anchoredPosition} sizeDelta={crt.sizeDelta}" +
                        (img != null ? $" imageColor=({img.color.r:F2},{img.color.g:F2},{img.color.b:F2},a{img.color.a:F2})" : string.Empty) +
                        (txt != null ? $" text='{txt.text}' fontSize={txt.fontSize} fontStyle={txt.fontStyle}" : string.Empty));
                }

                var selfImg = t.GetComponent<UnityEngine.UI.Image>();
                if (selfImg != null)
                {
                    report.AppendLine($"  selfImage color=({selfImg.color.r:F2},{selfImg.color.g:F2},{selfImg.color.b:F2},a{selfImg.color.a:F2})");
                }

                var selfTxt = t.GetComponent<UnityEngine.UI.Text>();
                if (selfTxt != null)
                {
                    report.AppendLine($"  selfText '{selfTxt.text}' fontSize={selfTxt.fontSize}");
                }
            }

            Debug.Log(report.ToString());
        }

        private static string Path(Transform t)
        {
            var s = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                s = t.name + "/" + s;
            }

            return s;
        }
    }
}
