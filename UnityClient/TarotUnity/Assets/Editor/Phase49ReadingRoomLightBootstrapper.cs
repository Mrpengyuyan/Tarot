using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 49 ports the menu's lighting to the ReadingRoom, which still carried
    /// the exact defect Phase 42 diagnosed: Unity's stock daylight skybox ambient
    /// at full strength over a candlelit midnight room. That is why this screen
    /// reads flat - no pools, no falloff, the far wooden rim barred across the top
    /// - no matter how its point lights are tuned.
    ///
    /// It also had a second, worse problem the menu never had: **nothing in the
    /// room emits light**. Seven point lights lit the table from nowhere. The menu
    /// spent six phases establishing that candles light this world; the room the
    /// player actually sits in has to honour that or the product reads as two
    /// different games.
    /// </summary>
    public static class Phase49ReadingRoomLightBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        /// <summary>
        /// Legacy point lights from the Phase 12-15 era. They were tuned to fight a
        /// daylight skybox; with ambient gone they stack into a flat wash and undo
        /// the candles. Dimmed to a floor rather than deleted - the reveal/flip
        /// phases reference them by name.
        /// </summary>
        private static readonly (string Name, float Intensity)[] LegacyLights =
        {
            ("Ritual Ember Light", 0.55f),
            ("Moon Fill Light", 0.30f),
            ("Phase12_FocusedCardLight", 0.30f),
            ("Phase14_RevealLightWarm", 0.22f),
            ("Phase15_WarmKeyLight", 0.38f),
            ("Phase15_CoolRimLight", 0.26f),
        };

        // Framing the card row without standing in it: the slots run x -1.45..1.45
        // at z=0.15, the deck sits at x=-2.35.
        // Measured against this camera (pos 0,2.85,-4.15 / pitch 32.4 / FOV 40):
        // at the card row's depth the frame edge falls at x=+-3.34, so the first
        // pass at +-3.35/3.45 stood the front candles exactly on the cut. The back
        // pair are pushed to the outer corners because the step tracker owns the
        // top of this frame and candles behind it grow out of the UI.
        private static readonly (string Name, Vector3 Base, float Wax)[] Candles =
        {
            ("MP_RoomCandle_L", new Vector3(-2.90f, 0f, 0.45f), 0.30f),
            ("MP_RoomCandle_R", new Vector3(3.00f, 0f, 0.30f), 0.235f),
            ("MP_RoomCandle_BackL", new Vector3(-4.15f, 0f, 3.40f), 0.175f),
            ("MP_RoomCandle_BackR", new Vector3(4.25f, 0f, 3.15f), 0.145f),
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 49 ReadingRoom Light Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StageAmbient();
            StageCandles();
            StageTableFill();
            DimLegacyLights();
            StageBackdrop();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 49 ReadingRoom lighting complete.");
        }

        /// <summary>Same contract as the menu (Phase 42): ambient is a shadow floor, not a source.</summary>
        private static void StageAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.085f, 0.068f, 0.072f, 1f);
            RenderSettings.reflectionIntensity = 0.06f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            // The directional reaches the far rim 8 units out and bars it across
            // the frame; the candles are the only real light in this room too.
            var key = GameObject.Find("Table Key Light")?.GetComponent<Light>();
            if (key != null)
            {
                key.intensity = 0.035f;
                key.color = new Color(0.60f, 0.66f, 1f, 1f);
                EditorUtility.SetDirty(key);
            }
        }

        private static void StageCandles()
        {
            foreach (var (name, basePos, wax) in Candles)
            {
                var isBack = name.Contains("Back");
                StageCandle(name, basePos, wax, isBack ? 1.5f : 4.2f, isBack);
            }
        }

        /// <summary>
        /// Same rig as the menu's candles, including the rule that cost two phases
        /// to learn: the Light rides the root, so the root sits at flame height and
        /// the wax hangs below it on negative offsets. Never write
        /// light.transform.localPosition on a prop root.
        /// </summary>
        private static void StageCandle(string name, Vector3 basePosition, float waxHeight,
            float intensity, bool isBack)
        {
            var stage = GameObject.Find("MP_TableStage");
            if (stage == null)
            {
                Debug.LogError("Phase 49: MP_TableStage missing; run the Phase 38 bootstrap first.");
                return;
            }

            var wax = LoadMaterial("MP_CandleWax");
            var wick = LoadMaterial("MP_CandleWick");
            var flameMat = LoadMaterial("MP_CandleFlame");
            var glow = LoadMaterial("MP_WarmGlow");

            var flameY = waxHeight * 2f + 0.085f;
            var existing = stage.transform.Find(name);
            var root = existing != null ? existing.gameObject : CreateChild(stage.transform, name);
            root.transform.localPosition = new Vector3(basePosition.x, basePosition.y + flameY, basePosition.z);
            root.transform.localRotation = Quaternion.identity;

            EnsurePrimitive(root.transform, "WaxPool", PrimitiveType.Cylinder,
                new Vector3(0f, 0.012f - flameY, 0f), new Vector3(0.155f, 0.012f, 0.155f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Body", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight - flameY, 0f), new Vector3(0.105f, waxHeight, 0.105f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Lip", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight * 2f - flameY, 0f), new Vector3(0.118f, 0.014f, 0.118f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Wick", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight * 2f + 0.022f - flameY, 0f),
                new Vector3(0.009f, 0.022f, 0.009f), Vector3.zero, wick);
            EnsurePrimitive(root.transform, "Flame", PrimitiveType.Quad,
                new Vector3(0f, 0f, -0.005f), new Vector3(0.15f, 0.25f, 1f), Vector3.zero, flameMat);
            EnsurePrimitive(root.transform, "Halo", PrimitiveType.Quad,
                new Vector3(0f, 0f, 0.012f), new Vector3(0.62f, 0.62f, 1f), Vector3.zero, glow);

            var light = root.GetComponent<Light>();
            if (light == null)
            {
                light = root.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.66f, 0.30f, 1f);
            light.intensity = intensity;
            light.range = isBack ? 4.2f : 7.0f;
            light.shadows = isBack ? LightShadows.None : LightShadows.Soft;
            EditorUtility.SetDirty(light);

            var flicker = root.GetComponent<CandleFlickerController>();
            if (flicker == null)
            {
                flicker = root.AddComponent<CandleFlickerController>();
            }

            var so = new SerializedObject(flicker);
            so.FindProperty("flameLight").objectReferenceValue = light;
            so.FindProperty("flameBillboard").objectReferenceValue = root.transform.Find("Flame");
            so.FindProperty("baseIntensity").floatValue = intensity;
            so.FindProperty("intensityFlicker").floatValue = isBack ? 0.20f : 0.15f;
            so.FindProperty("flameScaleFlicker").floatValue = isBack ? 0.08f : 0.12f;
            so.FindProperty("flickerSpeed").floatValue = isBack ? 5.2f : 4.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flicker);
        }

        /// <summary>The unseen fill that lets gold read as gold (Phase 42's lesson).</summary>
        private static void StageTableFill()
        {
            var stage = GameObject.Find("MP_TableStage");
            if (stage == null)
            {
                return;
            }

            var existing = stage.transform.Find("MP_RoomFill");
            var go = existing != null ? existing.gameObject : CreateChild(stage.transform, "MP_RoomFill");
            go.transform.localPosition = new Vector3(0f, 2.3f, 0.4f);

            var fill = go.GetComponent<Light>();
            if (fill == null)
            {
                fill = go.AddComponent<Light>();
            }

            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.84f, 0.66f, 1f);
            fill.intensity = 1.5f;
            fill.range = 9.5f;
            fill.shadows = LightShadows.None;
            EditorUtility.SetDirty(go);
        }

        private static void DimLegacyLights()
        {
            foreach (var (name, intensity) in LegacyLights)
            {
                var light = GameObject.Find(name)?.GetComponent<Light>();
                if (light != null)
                {
                    light.intensity = intensity;
                    EditorUtility.SetDirty(light);
                }
            }
        }

        /// <summary>The room's backdrop gets the same drapery the menu has.</summary>
        private static void StageBackdrop()
        {
            var stage = GameObject.Find("MP_TableStage");
            var backdrop = stage != null ? stage.transform.Find("MP_ParlorBackdrop") : null;
            if (backdrop == null)
            {
                return;
            }

            var haze = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/MP_ParlorHaze.mat");
            if (haze != null)
            {
                backdrop.GetComponent<MeshRenderer>().sharedMaterial = haze;
                EditorUtility.SetDirty(backdrop.gameObject);
            }
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsurePrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
            }

            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            EditorUtility.SetDirty(go);
        }
    }
}
