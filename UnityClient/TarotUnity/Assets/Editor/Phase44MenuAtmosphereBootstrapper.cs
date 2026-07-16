using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 44 answers the second menu review: the deck on the right clipped
    /// through itself, the copy stated the obvious, the centre column read as a
    /// form, and the table was too empty to feel like anything was at stake.
    /// </summary>
    public static class Phase44MenuAtmosphereBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        // A diviner speaks in ritual, not instructions. 入席 / 离席 (take your
        // seat / leave the table) also gives the two actions one vocabulary.
        public const string SubtitleCopy = "你尚未开口，牌已听见。";
        public const string StartCopy = "入席问牌";
        public const string QuitCopy = "离席";
        public const string StatusCopy = "烛火已燃，牌已洗过。";

        [MenuItem("Tools/Tarot Unity/Run Phase 44 Menu Atmosphere Bootstrap")]
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

            ConfigureArcaneTexture();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixScatterClipping();
            StageArcaneCircle();
            StageTableTrinkets();
            StageDustMotes();
            RestageCopyAndLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 44 menu atmosphere complete.");
        }

        private static void ConfigureArcaneTexture()
        {
            var path = $"{SpriteFolder}/ArcaneCircle.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogError($"Phase 44: missing {path}; run gen_arcane.py first.");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Both loose cards sat at the same local Y (0.01) with overlapping
        /// footprints, so two coplanar meshes intersected - the clipping. Real
        /// scattered cards lie on top of each other; the second is lifted by one
        /// card thickness and they are spread so only a corner overlaps.
        /// </summary>
        private static void FixScatterClipping()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                Debug.LogError("Phase 44: MP_MenuStage missing.");
                return;
            }

            var a = stage.transform.Find("MP_ScatterCard_A");
            if (a != null)
            {
                a.localPosition = new Vector3(1.44f, 0f, 0.10f);
                a.localRotation = Quaternion.Euler(0f, -13f, 0f);
                EditorUtility.SetDirty(a.gameObject);
            }

            var b = stage.transform.Find("MP_ScatterCard_B");
            if (b != null)
            {
                // One card thickness up, so it rests on A instead of through it.
                b.localPosition = new Vector3(1.86f, 0.023f, -0.62f);
                b.localRotation = Quaternion.Euler(0f, 29f, 0f);
                EditorUtility.SetDirty(b.gameObject);
            }
        }

        /// <summary>The ritual circle: what fills an empty diviner's table.</summary>
        private static void StageArcaneCircle()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            // Faint. At half alpha this out-shouted the cards, which breaks the
            // one rule the whole art direction rests on: the brightest, sharpest
            // thing on the table is always a card. It is ground, not subject.
            var mat = EnsureTransparentUnlit("MP_ArcaneCircle", "ArcaneCircle",
                new Color(0.92f, 0.72f, 0.42f, 0.17f));
            EnsurePrimitive(stage.transform, "MP_ArcaneCircle", PrimitiveType.Quad,
                new Vector3(0f, 0.004f, 0.55f), new Vector3(3.5f, 3.5f, 1f),
                new Vector3(90f, 0f, 0f), mat);
        }

        /// <summary>
        /// A few coins and a ring: the small change of a diviner's table. Cheap
        /// geometry, but it is the difference between "two candles on felt" and
        /// somewhere a person actually works.
        /// </summary>
        private static void StageTableTrinkets()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            var brass = EnsureLit("MP_Brass", new Color(0.62f, 0.44f, 0.16f), 0.72f, 0.85f);
            var root = stage.transform.Find("MP_Trinkets")?.gameObject
                       ?? CreateChild(stage.transform, "MP_Trinkets");
            root.transform.localPosition = Vector3.zero;

            EnsurePrimitive(root.transform, "Coin_A", PrimitiveType.Cylinder,
                new Vector3(-2.18f, 0.008f, -0.72f), new Vector3(0.075f, 0.008f, 0.075f),
                Vector3.zero, brass);
            EnsurePrimitive(root.transform, "Coin_B", PrimitiveType.Cylinder,
                new Vector3(-2.04f, 0.008f, -0.90f), new Vector3(0.075f, 0.008f, 0.075f),
                new Vector3(0f, 22f, 0f), brass);
            EnsurePrimitive(root.transform, "Coin_C", PrimitiveType.Cylinder,
                new Vector3(-2.12f, 0.024f, -0.81f), new Vector3(0.075f, 0.008f, 0.075f),
                new Vector3(0f, 47f, 0f), brass);
            EnsurePrimitive(root.transform, "Ring", PrimitiveType.Cylinder,
                new Vector3(2.30f, 0.006f, -0.86f), new Vector3(0.10f, 0.006f, 0.10f),
                Vector3.zero, brass);
        }

        /// <summary>Dust hanging in the candlelight - the cheapest real atmosphere there is.</summary>
        private static void StageDustMotes()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            var existing = stage.transform.Find("MP_DustMotes");
            var go = existing != null ? existing.gameObject : CreateChild(stage.transform, "MP_DustMotes");
            go.transform.localPosition = new Vector3(0f, 0.9f, -0.4f);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.AddComponent<ParticleSystem>();
            }

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 14f;
            main.startSpeed = 0.045f;
            main.startSize = 0.014f;
            main.startColor = new Color(1f, 0.86f, 0.62f, 0.5f);
            main.maxParticles = 90;
            main.gravityModifier = -0.006f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(7.5f, 1.6f, 4.5f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.06f;
            noise.frequency = 0.22f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var dust = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/MP_WarmGlow.mat");
            if (renderer != null && dust != null)
            {
                renderer.sharedMaterial = dust;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// Copy and layout. The centre stacked button / status / quit read as a
        /// form; a menu should offer one way in. The status line keeps its job
        /// (it reports backend and offline state) but moves to the foot of the
        /// frame where a system line belongs, and quit becomes a corner
        /// affordance - desktop players still need a way out (Phase 34), it just
        /// does not belong in the same column as the invitation.
        /// </summary>
        private static void RestageCopyAndLayout()
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                return;
            }

            var root = canvas.transform;
            SetCopy(root, "SubtitleText", SubtitleCopy);
            // The controller writes this at runtime, but the serialized copy is
            // what the editor, the capture pipeline and the first frame all show.
            SetCopy(root, "StatusText", StatusCopy);
            SetCopy(root, "StartReadingButton/Label", StartCopy);
            SetCopy(root, "QuitButton/Label", QuitCopy);

            var start = root.Find("StartReadingButton") as RectTransform;
            if (start != null)
            {
                start.anchoredPosition = new Vector2(0f, -74f);
                EditorUtility.SetDirty(start.gameObject);
            }

            var status = root.Find("StatusText") as RectTransform;
            if (status != null)
            {
                status.anchoredPosition = new Vector2(0f, -300f);
                EditorUtility.SetDirty(status.gameObject);
            }

            var quit = root.Find("QuitButton") as RectTransform;
            if (quit != null)
            {
                quit.anchorMin = new Vector2(1f, 0f);
                quit.anchorMax = new Vector2(1f, 0f);
                quit.pivot = new Vector2(1f, 0f);
                quit.sizeDelta = new Vector2(112f, 34f);
                quit.anchoredPosition = new Vector2(-34f, 26f);
                EditorUtility.SetDirty(quit.gameObject);

                // A plaque in the corner still reads as a button competing with the
                // invitation; the affordance here should be a quiet link.
                var quitImage = quit.GetComponent<Image>();
                if (quitImage != null)
                {
                    quitImage.sprite = null;
                    quitImage.color = new Color(1f, 1f, 1f, 0f);
                    var quitButton = quit.GetComponent<Button>();
                    if (quitButton != null)
                    {
                        var colors = quitButton.colors;
                        colors.normalColor = new Color(1f, 1f, 1f, 0f);
                        colors.highlightedColor = new Color(1f, 0.9f, 0.6f, 0.14f);
                        colors.pressedColor = new Color(1f, 0.9f, 0.6f, 0.22f);
                        colors.selectedColor = colors.normalColor;
                        quitButton.colors = colors;
                        EditorUtility.SetDirty(quitButton);
                    }

                    EditorUtility.SetDirty(quitImage);
                }

                var quitLabel = quit.Find("Label") as RectTransform;
                if (quitLabel != null)
                {
                    quitLabel.anchorMin = Vector2.zero;
                    quitLabel.anchorMax = Vector2.one;
                    quitLabel.offsetMin = Vector2.zero;
                    quitLabel.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(quitLabel.gameObject);
                }
            }
        }

        private static void SetCopy(Transform root, string path, string copy)
        {
            var target = root.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"Phase 44: no text at {path}.");
                return;
            }

            var tmp = target.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = copy;
                EditorUtility.SetDirty(tmp);
                return;
            }

            var legacy = target.GetComponent<Text>();
            if (legacy != null)
            {
                legacy.text = copy;
                EditorUtility.SetDirty(legacy);
            }
        }

        private static Material EnsureLit(string name, Color color, float smoothness, float metallic)
        {
            var mat = EnsureMaterial(name, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material EnsureTransparentUnlit(string name, string textureName, Color color)
        {
            var mat = EnsureMaterial(name, Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture("_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/{textureName}.png"));
            mat.SetColor("_BaseColor", color);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(mat, "_Surface", 1f);
            SetFloatIfPresent(mat, "_Blend", 0f);
            SetFloatIfPresent(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(mat, "_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static Material EnsureMaterial(string name, Shader shader)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
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
