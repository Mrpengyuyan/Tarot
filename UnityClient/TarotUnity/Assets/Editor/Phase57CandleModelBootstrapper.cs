using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 57 remodels the candles. Phase 46 gave them wax *surfaces* - grain,
    /// drips, translucency - but they were still four Unity primitives stacked up,
    /// and the close-up diagnosis showed what that costs: a 20-segment silhouette,
    /// a dead-parallel tube, a flat top, and a wider disc "Lip" that reads as a
    /// machined collar on a pill bottle. This replaces the WaxPool/Body/Lip stack
    /// with one lathed mesh carrying a real candle profile, and re-cuts the wick.
    ///
    /// What is deliberately NOT touched: the candle root's world position, its
    /// Light, the Flame/Halo billboards, and the flame height. Six phases fought
    /// for this lighting and framing; this pass is geometry only.
    /// </summary>
    public static class Phase57CandleModelBootstrapper
    {
        public const string MeshFolder = "Assets/Models/Candles";

        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string RoomScene = "Assets/Scenes/ReadingRoom.unity";

        /// <summary>Radius, full wax height, and how finely to lathe each candle.</summary>
        private static readonly (string Name, float Radius, float Height, int Segments)[] MenuCandles =
        {
            ("Phase8_LeftCandle", 0.105f, 0.60f, 64),
            ("Phase8_RightCandle", 0.105f, 0.47f, 64),
            ("MP_BackCandle_L", 0.085f, 0.35f, 40),
            ("MP_BackCandle_R", 0.085f, 0.29f, 40),
        };

        private static readonly (string Name, float Radius, float Height, int Segments)[] RoomCandles =
        {
            ("MP_RoomCandle_L", 0.105f, 0.60f, 64),
            ("MP_RoomCandle_R", 0.105f, 0.47f, 64),
            ("MP_RoomCandle_BackL", 0.105f, 0.35f, 40),
            ("MP_RoomCandle_BackR", 0.105f, 0.29f, 40),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 57 - Candle Modelling")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            Directory.CreateDirectory(MeshFolder);

            Remodel(MenuScene, MenuCandles);
            Remodel(RoomScene, RoomCandles);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 57: candles remodelled.");
        }

        private static void Remodel(string scenePath, (string Name, float Radius, float Height, int Segments)[] candles)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var materials = Phase37AssetFoundationBootstrapper.MaterialFolder;
            var wax = AssetDatabase.LoadAssetAtPath<Material>($"{materials}/MP_CandleWax.mat");
            var wick = AssetDatabase.LoadAssetAtPath<Material>($"{materials}/MP_CandleWick.mat");
            if (wax == null || wick == null)
            {
                Debug.LogError("Phase 57: candle materials missing; run the Phase 42/46 bootstraps first.");
                return;
            }

            foreach (var (name, radius, height, segments) in candles)
            {
                var root = GameObject.Find(name);
                if (root == null)
                {
                    Debug.LogWarning($"Phase 57: {name} missing.");
                    continue;
                }

                // The root sits at flame height with the wax hanging below it on
                // negative local offsets (Phase 42's rule, and the Light lives here -
                // never move it). The lathe is built base-at-zero, so it hangs from
                // the same base the old primitives stood on.
                var flameY = height + 0.085f;

                ReplaceWithLathe(root, name, radius, height, segments, flameY, wax, wick);
                EditorUtility.SetDirty(root);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReplaceWithLathe(GameObject root, string candleName, float radius, float height,
            int segments, float flameY, Material wax, Material wick)
        {
            // The three primitives the lathe supersedes. Their job is now done by one
            // continuous surface, so leaving them would double the silhouette.
            foreach (var superseded in new[] { "WaxPool", "Body", "Lip" })
            {
                var child = root.transform.Find(superseded);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var waxMesh = SaveMesh(CandleMeshBuilder.BuildWax($"{candleName}_Wax", radius, height, segments),
                $"{candleName}_Wax");
            var waxObject = EnsureChild(root.transform, "Wax");
            waxObject.transform.localPosition = new Vector3(0f, -flameY, 0f);
            waxObject.transform.localRotation = Quaternion.identity;
            waxObject.transform.localScale = Vector3.one;
            EnsureRenderer(waxObject, waxMesh, wax);

            // The wick: tapered and curled, seated in the crater rather than on a
            // flat lid. The crater floor is the melt depth below the rim.
            var wickMesh = SaveMesh(CandleMeshBuilder.BuildWick($"{candleName}_Wick", radius * 0.085f, 0.044f),
                $"{candleName}_Wick");
            var wickObject = EnsureChild(root.transform, "Wick");
            var craterDepth = Mathf.Lerp(0.024f, 0.040f, Mathf.InverseLerp(0.62f, 0.26f, height));
            wickObject.transform.localPosition = new Vector3(0f, height - craterDepth - flameY, 0f);
            wickObject.transform.localRotation = Quaternion.identity;
            wickObject.transform.localScale = Vector3.one;
            EnsureRenderer(wickObject, wickMesh, wick);
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static void EnsureRenderer(GameObject target, Mesh mesh, Material material)
        {
            // Never use ?? here: GetComponent returns a fake-null UnityEngine.Object
            // that ?? treats as non-null, so AddComponent would never run.
            var filter = target.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = target.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = mesh;

            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = target.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // A primitive collider would now be the wrong shape, and nothing in this
            // game raycasts a candle.
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Mesh SaveMesh(Mesh mesh, string assetName)
        {
            var path = $"{MeshFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                // Keep the asset identity (and every scene reference to it) and swap
                // the geometry in place.
                existing.Clear();
                existing.SetVertices(new System.Collections.Generic.List<Vector3>(mesh.vertices));
                existing.SetUVs(0, new System.Collections.Generic.List<Vector2>(mesh.uv));
                existing.SetTriangles(mesh.triangles, 0);
                existing.RecalculateNormals();
                existing.RecalculateTangents();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
