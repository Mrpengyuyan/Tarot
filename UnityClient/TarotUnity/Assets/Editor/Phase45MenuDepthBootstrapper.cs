using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 45 fills the menu out. Two candles and a deck left the mid-ground
    /// empty and the frame read thin, and the type sat as three isolated lines
    /// with voids between them.
    ///
    /// Everything added here goes in the mid-ground band (z 1.5-2.6) that the
    /// camera sees between the subtitle and the button - the emptiest part of the
    /// composition - and stays dimmer than the cards.
    /// </summary>
    public static class Phase45MenuDepthBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        [MenuItem("Tools/Tarot Unity/Run Phase 45 Menu Depth Bootstrap")]
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
            StageScryingOrb();
            StageCenser();
            StageBackCandles();
            StageTypography();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 45 menu depth complete.");
        }

        /// <summary>
        /// The scrying orb: the one prop a divination table is incomplete without.
        /// Glass is faked - a dark, very smooth sphere reads as a crystal ball
        /// because it catches the candles as two hard speculars, which is all the
        /// eye needs at this size.
        /// </summary>
        private static void StageScryingOrb()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                Debug.LogError("Phase 45: MP_MenuStage missing.");
                return;
            }

            // A crystal ball is lit from inside, which is also what makes it
            // legible here: it sits between the candle pools, and a smooth dark
            // sphere with no light of its own rendered as a black lump. The cool
            // cast is the moon answering the candles - the palette's only cold note.
            var glass = EnsureLit("MP_OrbGlass", new Color(0.16f, 0.13f, 0.22f), 0.96f, 0.22f);
            glass.EnableKeyword("_EMISSION");
            glass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            glass.SetColor("_EmissionColor", new Color(0.14f, 0.12f, 0.26f, 1f));
            EditorUtility.SetDirty(glass);
            var brass = LoadMaterial("MP_Brass");

            var root = stage.transform.Find("MP_ScryingOrb")?.gameObject
                       ?? CreateChild(stage.transform, "MP_ScryingOrb");
            root.transform.localPosition = new Vector3(1.18f, 0f, 1.78f);

            EnsurePrimitive(root.transform, "Stand", PrimitiveType.Cylinder,
                new Vector3(0f, 0.022f, 0f), new Vector3(0.20f, 0.022f, 0.20f), Vector3.zero, brass);
            EnsurePrimitive(root.transform, "Collar", PrimitiveType.Cylinder,
                new Vector3(0f, 0.056f, 0f), new Vector3(0.13f, 0.016f, 0.13f), Vector3.zero, brass);
            EnsurePrimitive(root.transform, "Orb", PrimitiveType.Sphere,
                new Vector3(0f, 0.29f, 0f), new Vector3(0.46f, 0.46f, 0.46f), Vector3.zero, glass);
            EnsurePrimitive(root.transform, "Halo", PrimitiveType.Quad,
                new Vector3(0f, 0.29f, -0.03f), new Vector3(0.62f, 0.62f, 1f),
                Vector3.zero, EnsureOrbHalo());

            // A whisper of moonlight so the orb throws its own colour onto the cloth.
            EnsureChildLight(root.transform, "OrbLight", new Vector3(0f, 0.29f, 0f),
                new Color(0.60f, 0.58f, 1f, 1f), 0.75f, 2.2f);

            EditorUtility.SetDirty(root);
        }

        /// <summary>A censer with smoke: the vertical element the mid-ground had none of.</summary>
        private static void StageCenser()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            var brass = LoadMaterial("MP_Brass");
            var root = stage.transform.Find("MP_Censer")?.gameObject
                       ?? CreateChild(stage.transform, "MP_Censer");
            root.transform.localPosition = new Vector3(-1.32f, 0f, 1.92f);

            EnsurePrimitive(root.transform, "Foot", PrimitiveType.Cylinder,
                new Vector3(0f, 0.014f, 0f), new Vector3(0.17f, 0.014f, 0.17f), Vector3.zero, brass);
            EnsurePrimitive(root.transform, "Bowl", PrimitiveType.Sphere,
                new Vector3(0f, 0.085f, 0f), new Vector3(0.23f, 0.14f, 0.23f), Vector3.zero, brass);
            EnsurePrimitive(root.transform, "Rim", PrimitiveType.Cylinder,
                new Vector3(0f, 0.145f, 0f), new Vector3(0.20f, 0.010f, 0.20f), Vector3.zero, brass);
            // Coals: what the smoke is coming off, and what makes the censer read
            // as burning rather than as a brass lump in the dark.
            EnsurePrimitive(root.transform, "Coals", PrimitiveType.Cylinder,
                new Vector3(0f, 0.150f, 0f), new Vector3(0.155f, 0.006f, 0.155f),
                Vector3.zero, EnsureCoals());

            EnsureChildLight(root.transform, "EmberLight", new Vector3(0f, 0.17f, 0f),
                new Color(1f, 0.42f, 0.14f, 1f), 0.8f, 1.9f);

            StageSmoke(root.transform);
            EditorUtility.SetDirty(root);
        }

        private static void StageSmoke(Transform censer)
        {
            var existing = censer.Find("Smoke");
            var go = existing != null ? existing.gameObject : CreateChild(censer, "Smoke");
            go.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.AddComponent<ParticleSystem>();
            }

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = 6.5f;
            main.startSpeed = 0.16f;
            main.startSize = 0.10f;
            // Smoke is lit by the candles it drifts past, not by itself; keep it
            // dim or it reads as steam.
            main.startColor = new Color(0.72f, 0.60f, 0.52f, 0.17f);
            main.maxParticles = 40;
            main.gravityModifier = -0.014f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 2.6f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.10f;
            noise.frequency = 0.35f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var glow = LoadMaterial("MP_WarmGlow");
            if (renderer != null && glow != null)
            {
                renderer.sharedMaterial = glow;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// Two more candles set further back and burnt lower. Repeating a light at
        /// a second depth is what turns a lit strip into a room.
        /// </summary>
        private static void StageBackCandles()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            StageBackCandle(stage.transform, "MP_BackCandle_L", new Vector3(-2.95f, 0f, 2.55f), 0.175f);
            StageBackCandle(stage.transform, "MP_BackCandle_R", new Vector3(3.05f, 0f, 2.25f), 0.145f);
        }

        private static void StageBackCandle(Transform parent, string name, Vector3 basePosition, float waxHeight)
        {
            var wax = LoadMaterial("MP_CandleWax");
            var wick = LoadMaterial("MP_CandleWick");
            var flameMat = LoadMaterial("MP_CandleFlame");
            var glow = LoadMaterial("MP_WarmGlow");

            var flameY = waxHeight * 2f + 0.07f;
            var existing = parent.Find(name);
            var root = existing != null ? existing.gameObject : CreateChild(parent, name);
            root.transform.localPosition = new Vector3(basePosition.x, basePosition.y + flameY, basePosition.z);
            root.transform.localRotation = Quaternion.identity;

            // Same contract as the front candles: the Light rides the root, so the
            // root sits at flame height and the wax hangs below on negative offsets.
            EnsurePrimitive(root.transform, "Body", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight - flameY, 0f), new Vector3(0.085f, waxHeight, 0.085f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Wick", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight * 2f + 0.018f - flameY, 0f),
                new Vector3(0.008f, 0.018f, 0.008f), Vector3.zero, wick);
            EnsurePrimitive(root.transform, "Flame", PrimitiveType.Quad,
                new Vector3(0f, 0f, -0.004f), new Vector3(0.115f, 0.19f, 1f), Vector3.zero, flameMat);
            EnsurePrimitive(root.transform, "Halo", PrimitiveType.Quad,
                new Vector3(0f, 0f, 0.01f), new Vector3(0.46f, 0.46f, 1f), Vector3.zero, glow);

            var light = root.GetComponent<Light>();
            if (light == null)
            {
                light = root.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.64f, 0.28f, 1f);
            light.intensity = 1.5f;
            light.range = 4.2f;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(root);
        }

        /// <summary>
        /// The type read as three isolated lines with voids between them. This
        /// binds the title block: a gold rule joins the title to its subtitle,
        /// tracking opens the display cut so it reads as inscribed rather than
        /// typed, and the status line recedes to the weight of a footnote.
        /// </summary>
        private static void StageTypography()
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                return;
            }

            var root = canvas.transform;

            var title = root.Find("TitleText")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.characterSpacing = 14f;
                (title.transform as RectTransform).anchoredPosition = new Vector2(0f, 168f);
                EditorUtility.SetDirty(title);
            }

            var subtitle = root.Find("SubtitleText")?.GetComponent<TMP_Text>();
            if (subtitle != null)
            {
                subtitle.fontSize = 20f;
                subtitle.characterSpacing = 6f;
                (subtitle.transform as RectTransform).anchoredPosition = new Vector2(0f, 88f);
                EditorUtility.SetDirty(subtitle);
            }

            var status = root.Find("StatusText")?.GetComponent<TMP_Text>();
            if (status != null)
            {
                status.fontSize = 15f;
                status.characterSpacing = 4f;
                EditorUtility.SetDirty(status);
            }

            var startLabel = root.Find("StartReadingButton/Label")?.GetComponent<TMP_Text>();
            if (startLabel != null)
            {
                startLabel.characterSpacing = 8f;
                EditorUtility.SetDirty(startLabel);
            }

            var quitLabel = root.Find("QuitButton/Label")?.GetComponent<TMP_Text>();
            if (quitLabel != null)
            {
                quitLabel.characterSpacing = 5f;
                EditorUtility.SetDirty(quitLabel);
            }

            EnsureTitleRule(root);
        }

        private static void EnsureTitleRule(Transform root)
        {
            var existing = root.Find("MP_TitleRule");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("MP_TitleRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root, false);
                // Behind the text, in front of the washes.
                go.transform.SetSiblingIndex(Mathf.Max(0, root.childCount - 1));
            }
            else
            {
                go = existing.gameObject;
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 16f);
            rect.anchoredPosition = new Vector2(0f, 126f);

            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/TarotDivider.png");
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.55f);
            image.raycastTarget = false;
            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// Puts a point light on a CHILD of the prop, never on the prop root.
        /// A Light added to the root shares that root's Transform, so writing
        /// light.transform.localPosition silently drags the whole prop to the
        /// origin - which is exactly what stacked the orb and the censer in the
        /// centre of the frame here, and what stacked both candles at the origin
        /// back in Phase 42. Same bug, twice.
        /// </summary>
        private static void EnsureChildLight(Transform parent, string name, Vector3 localPosition,
            Color color, float intensity, float range)
        {
            // An earlier run of this bootstrap put the Light on the prop root; leave
            // it there and the prop carries two lights and stays draggable by the
            // same mistake.
            var strayRootLight = parent.GetComponent<Light>();
            if (strayRootLight != null)
            {
                Object.DestroyImmediate(strayRootLight, true);
            }

            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : CreateChild(parent, name);
            go.transform.localPosition = localPosition;

            var light = go.GetComponent<Light>();
            if (light == null)
            {
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(go);
        }

        private static Material EnsureCoals()
        {
            var mat = EnsureLit("MP_Coals", new Color(0.22f, 0.05f, 0.02f), 0.3f, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", new Color(1.5f, 0.42f, 0.10f, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material EnsureOrbHalo()
        {
            var path = $"{MaterialFolder}/MP_OrbHalo.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/TarotGlow.png"));
            mat.SetColor("_BaseColor", new Color(0.42f, 0.40f, 0.78f, 0.28f));
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            foreach (var (prop, val) in new (string, float)[]
            {
                ("_Surface", 1f), ("_Blend", 1f),
                ("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha),
                ("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One),
                ("_ZWrite", 0f),
            })
            {
                if (mat.HasProperty(prop))
                {
                    mat.SetFloat(prop, val);
                }
            }

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material EnsureLit(string name, Color color, float smoothness, float metallic)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
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
