using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 38 read-only scene audit: dumps the full hierarchy of a scene
    /// (transforms, renderers/materials, UI graphics) to Temp/ so the table
    /// rebuild works from scene-file ground truth instead of bootstrapper
    /// archaeology. Never modifies the scene.
    /// </summary>
    public static class Phase38SceneAudit
    {
        [MenuItem("Tools/Tarot Unity/Run Phase 38 Scene Audit (ReadingRoom)")]
        public static void RunReadingRoom() => Dump("Assets/Scenes/ReadingRoom.unity", "Logs/phase38_readingroom_audit.txt");

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Scene Audit (MainMenu)")]
        public static void RunMainMenu() => Dump("Assets/Scenes/MainMenu.unity", "Logs/phase38_mainmenu_audit.txt");

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Scene Audit (Result)")]
        public static void RunResult() => Dump("Assets/Scenes/Result.unity", "Logs/phase38_result_audit.txt");

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Scene Audit (Card Prefab)")]
        public static void RunCardPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            var sb = new StringBuilder();
            sb.AppendLine("AUDIT PF_TarotCard.prefab");
            Walk(prefab.transform, 0, sb);
            File.WriteAllText("Logs/phase38_cardprefab_audit.txt", sb.ToString());
            Debug.Log("Phase 38 card prefab audit written.");
        }

        private static void Dump(string scenePath, string outPath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine($"AUDIT {scenePath}");
            foreach (var root in scene.GetRootGameObjects())
            {
                Walk(root.transform, 0, sb);
            }

            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Phase 38 audit written: {outPath} ({sb.Length} chars)");
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            var indent = new string(' ', depth * 2);
            var go = t.gameObject;
            sb.Append(indent).Append(go.activeSelf ? "+" : "-").Append(' ').Append(go.name);

            if (t is RectTransform rt)
            {
                sb.Append($" [RT pos={rt.anchoredPosition} size={rt.sizeDelta}]");
                var g = go.GetComponent<Graphic>();
                if (g != null)
                {
                    sb.Append($" [{g.GetType().Name} color={g.color}");
                    if (g is Image img && img.sprite != null) sb.Append($" sprite={img.sprite.name} type={img.type}");
                    if (g is Text txt) sb.Append($" text=\"{Trim(txt.text)}\" size={txt.fontSize}");
                    sb.Append(']');
                }
            }
            else
            {
                sb.Append($" [pos={t.localPosition} scl={t.localScale} rot={t.localEulerAngles}]");
                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = r.sharedMaterial;
                    sb.Append($" [{r.GetType().Name} enabled={r.enabled} mat={(mat ? mat.name : "null")} shader={(mat && mat.shader ? mat.shader.name : "?")}]");
                }

                var light = go.GetComponent<Light>();
                if (light != null)
                {
                    sb.Append($" [Light {light.type} color={light.color} intensity={light.intensity} range={light.range}]");
                }
            }

            foreach (var comp in go.GetComponents<MonoBehaviour>())
            {
                if (comp != null && !(comp is Graphic))
                {
                    sb.Append($" <{comp.GetType().Name}>");
                }
            }

            sb.AppendLine();
            foreach (Transform child in t)
            {
                Walk(child, depth + 1, sb);
            }
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", "\\n");
            return s.Length > 40 ? s.Substring(0, 40) + "..." : s;
        }
    }
}
