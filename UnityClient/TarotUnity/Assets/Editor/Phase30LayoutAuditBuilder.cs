using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 30 step 1: a READ-ONLY layout audit. It opens each core scene and logs the
    /// canvas reference size plus the resolved screen-edge margins of the comparable
    /// "secondary" UI elements (top heading, sub-label, primary button / footer) so the
    /// unification pass can target genuine cross-screen inconsistencies with real numbers.
    /// It never modifies or saves any scene.
    /// </summary>
    public static class Phase30LayoutAuditBuilder
    {
        private static readonly (string scene, string canvas)[] Screens =
        {
            ("Assets/Scenes/MainMenu.unity", "MainMenuCanvas"),
            ("Assets/Scenes/ReadingRoom.unity", "ReadingRoomCanvas"),
            ("Assets/Scenes/Result.unity", "ResultCanvas"),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 30 Layout Audit")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PHASE 30 LAYOUT AUDIT (read-only) ===");

            foreach (var (scenePath, canvasName) in Screens)
            {
                EditorSceneManager.OpenScene(scenePath);
                var canvasGo = GameObject.Find(canvasName);
                if (canvasGo == null)
                {
                    sb.AppendLine($"\n## {canvasName}: NOT FOUND");
                    continue;
                }

                var canvasRect = canvasGo.GetComponent<RectTransform>();
                var size = canvasRect.rect.size;
                sb.AppendLine($"\n## {canvasName}  refSize={size.x}x{size.y}");

                // Rebuild any layout-driven content so resolved rects are accurate.
                foreach (var lg in canvasGo.GetComponentsInChildren<LayoutGroup>(true))
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(lg.GetComponent<RectTransform>());
                }

                var report = new List<string>();
                foreach (var t in canvasGo.GetComponentsInChildren<Transform>(true))
                {
                    var rt = t as RectTransform;
                    if (rt == null || rt == canvasRect)
                    {
                        continue;
                    }

                    if (!IsAuditTarget(t.name))
                    {
                        continue;
                    }

                    rt.GetWorldCorners(Corners);
                    // World corners are in canvas pixel space for an overlay/camera canvas.
                    var minX = Corners[0].x;
                    var maxX = Corners[2].x;
                    var minY = Corners[0].y;
                    var maxY = Corners[2].y;
                    // Convert to margins from each screen edge using the canvas rect.
                    var canvasMinX = canvasRect.position.x - canvasRect.pivot.x * size.x;
                    var canvasMinY = canvasRect.position.y - canvasRect.pivot.y * size.y;
                    var left = minX - canvasMinX;
                    var right = canvasMinX + size.x - maxX;
                    var bottom = minY - canvasMinY;
                    var top = canvasMinY + size.y - maxY;
                    report.Add(
                        $"  {t.name,-34} active={t.gameObject.activeInHierarchy,-5} " +
                        $"w={maxX - minX,6:0} h={maxY - minY,5:0} | topMargin={top,6:0} bottomMargin={bottom,6:0} leftMargin={left,6:0} rightMargin={right,6:0}");
                }

                report.Sort();
                foreach (var line in report)
                {
                    sb.AppendLine(line);
                }
            }

            Debug.Log(sb.ToString());
        }

        private static readonly Vector3[] Corners = new Vector3[4];

        private static bool IsAuditTarget(string name)
        {
            // Comparable "secondary" elements across the three screens.
            return name is "TitleText" or "SubtitleText" or "StatusText" or "StartReadingButton"
                or "QuestionText" or "SpreadNameText" or "BackToMenuButton"
                or "RevealResultButton" or "DrawButton"
                or "ResultReadingScroll";
        }
    }
}
