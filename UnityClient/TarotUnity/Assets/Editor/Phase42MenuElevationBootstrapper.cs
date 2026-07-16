using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 42 does two things the played build exposed.
    ///
    /// 1. Sharpness: the player shipped as a fixed 1280x720 non-resizable window,
    ///    so going fullscreen stretched 720p across a 1440p display and softened
    ///    every glyph. The player now opens as a borderless fullscreen window at
    ///    the display's native resolution (no upscale), and is resizable.
    ///
    /// 2. Menu elevation: the table concept was right but the staging was not.
    ///    The camera sat at a 10.8 degree pitch with a 60 degree (Unity default,
    ///    wide-angle) FOV, so the velvet compressed into a thin band under a black
    ///    void, and the props read as primitives. This restages the menu as one
    ///    lit room: a seated look-down framing on a 36mm-equivalent lens, a parlor
    ///    backdrop with real haze instead of a flat black quad, candles built as
    ///    actual candles (tapered wax, pooled base, wick, additive flame) that own
    ///    the lighting, and the deck promoted to hero with a lifted top card.
    /// </summary>
    public static class Phase42MenuElevationBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;

        // Seated at the table, leaning in. Pitch and FOV are the whole difference
        // between "a thin band of cloth" and "a tabletop you are sitting at".
        public static readonly Vector3 CameraPosition = new(0f, 2.7f, -3.8f);
        public static readonly Vector3 CameraEuler = new(27f, 0f, 0f);
        public const float CameraFov = 36f;

        /// <summary>
        /// Full-screen UI washes retired in favour of scene lighting + the Phase 20
        /// vignette, plus the orphaned crest. "月光牌桌" restated what the title and
        /// subtitle already say and floated between them and the button with nothing
        /// to anchor it - the one accessory this composition is better without.
        /// </summary>
        public static readonly string[] RetiredWashes =
        {
            "Phase7_MenuVignette",
            "Phase7_MenuBackdrop",
            "MenuBackdrop",
            "Phase8_MenuCrest",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 42 Menu Elevation Bootstrap")]
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

            ApplyDisplaySettings();
            ConfigureNewTextures();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StageAmbient();
            StageCamera();
            StageBackdrop();
            StageCandles();
            StageDeckHero();
            StageLighting();
            RetireWashes();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 42 menu elevation complete.");
        }

        /// <summary>Native-resolution presentation: the fix for blurry built text.</summary>
        private static void ApplyDisplaySettings()
        {
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.macRetinaSupport = true;
        }

        private static void ConfigureNewTextures()
        {
            foreach (var name in new[] { "ParlorBackdrop", "CandleFlame" })
            {
                var path = $"{SpriteFolder}/{name}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    Debug.LogError($"Phase 42: missing texture {path}; run gen_backdrop.py first.");
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = name == "CandleFlame";
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// The scene shipped on Unity's stock skybox ambient at full intensity: a
        /// blue daylight sky lighting a candlelit midnight room from every angle.
        /// That is what made the wood rim read as a fluorescent bar, the candles
        /// read as grey PVC, and the whole frame read flat no matter how the
        /// practical lights were tuned. Ambient drops to a near-black tint so the
        /// candles are the only thing lighting this room.
        /// </summary>
        private static void StageAmbient()
        {
            // Low and slightly warm rather than zero: with no ambient at all the wax
            // went black (a point light sitting on the flame grazes the candle's own
            // vertical sides at N·L≈0) and the gold on the card backs stopped reading
            // as gold. This is a floor for shadow detail, not a light source.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.085f, 0.068f, 0.072f, 1f);
            RenderSettings.reflectionIntensity = 0.06f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        private static void StageCamera()
        {
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("Phase 42: no camera in MainMenu.");
                return;
            }

            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraEuler));
            camera.fieldOfView = CameraFov;
            EditorUtility.SetDirty(camera);
        }

        private static void StageBackdrop()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                Debug.LogError("Phase 42: MP_MenuStage missing; run the Phase 40 bootstrap first.");
                return;
            }

            // End the cloth exactly at the far rim. Both sit ~9 units out, far
            // beyond candle range, so the table falls off into the hazy dark on its
            // own - a lit strip of wood floating over black is what read as cheap.
            var cloth = stage.transform.Find("MP_MenuCloth");
            if (cloth != null)
            {
                cloth.localPosition = new Vector3(0f, -0.05f, -0.5f);
                cloth.localScale = new Vector3(30f, 0.1f, 12f);
                EditorUtility.SetDirty(cloth.gameObject);
            }

            var rim = stage.transform.Find("MP_MenuRimFar");
            if (rim != null)
            {
                rim.localPosition = new Vector3(0f, 0.09f, 5.5f);
                rim.localScale = new Vector3(30f, 0.3f, 1.1f);
                EditorUtility.SetDirty(rim.gameObject);
            }

            var haze = EnsureUnlitMaterial("MP_ParlorHaze", "ParlorBackdrop", Color.white, additive: false);
            var backdrop = stage.transform.Find("MP_ParlorBackdrop");
            if (backdrop != null)
            {
                backdrop.localPosition = new Vector3(0f, 3.2f, 11f);
                backdrop.localRotation = Quaternion.identity;
                backdrop.localScale = new Vector3(30f, 14f, 1f);
                backdrop.GetComponent<MeshRenderer>().sharedMaterial = haze;
                EditorUtility.SetDirty(backdrop.gameObject);
            }
        }

        private static void StageCandles()
        {
            // Asymmetric on purpose: two candles burnt to the same height read as
            // instanced props, not as objects someone lit.
            StageCandle("Phase8_LeftCandle", new Vector3(-2.35f, 0f, 0.7f), 0.30f);
            StageCandle("Phase8_RightCandle", new Vector3(2.45f, 0f, 0.5f), 0.235f);

            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            // Keep the Phase 40 glow quads (tests pin them) but tighten them into a
            // halo hugging each flame. At the old 1.7 scale they hung against the
            // backdrop and read as lens flares rather than candlelight.
            var glow = LoadMaterial("MP_WarmGlow");
            foreach (var (name, pos) in new[]
            {
                ("MP_CandleGlow_L", new Vector3(-2.35f, 0.685f, 0.7f)),
                ("MP_CandleGlow_R", new Vector3(2.45f, 0.555f, 0.5f)),
            })
            {
                EnsurePrimitive(stage.transform, name, PrimitiveType.Quad, pos,
                    new Vector3(0.62f, 0.62f, 1f), Vector3.zero, glow);
            }
        }

        /// <summary>
        /// Builds one candle. <paramref name="basePosition"/> is where the wax meets
        /// the cloth. The Light component lives on the root, and a candle must cast
        /// from its flame rather than its base, so the root is seated at flame height
        /// and the wax hangs below it on negative local offsets.
        /// </summary>
        private static void StageCandle(string rootName, Vector3 basePosition, float waxHeight)
        {
            var root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.LogWarning($"Phase 42: {rootName} missing.");
                return;
            }

            var flameY = waxHeight * 2f + 0.085f;
            root.transform.position = new Vector3(basePosition.x, basePosition.y + flameY, basePosition.z);
            root.transform.rotation = Quaternion.identity;

            // Clear the Phase 8 primitive blobs; rebuild as a real candle.
            foreach (var legacy in new[] { "Body", "Flame" })
            {
                var child = root.transform.Find(legacy);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Wax is translucent and lit from inside by its own flame. A point light
            // sitting on the wick can never do that (it grazes the body at N·L≈0), so
            // the glow is baked into the material as emission - the same cheat every
            // candle render uses, and bloom picks it up.
            var wax = EnsureLitMaterial("MP_CandleWax", new Color(0.94f, 0.89f, 0.78f), 0.24f);
            wax.EnableKeyword("_EMISSION");
            wax.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            wax.SetColor("_EmissionColor", new Color(0.62f, 0.34f, 0.13f, 1f));
            EditorUtility.SetDirty(wax);
            var wick = EnsureLitMaterial("MP_CandleWick", new Color(0.08f, 0.06f, 0.05f), 0.1f);
            var flameMat = EnsureUnlitMaterial("MP_CandleFlame", "CandleFlame",
                new Color(1.5f, 0.95f, 0.5f, 1f), additive: true);

            // Unity's cylinder is 2 units tall, so localScale.y is the half-height.
            // Local Y is measured down from the flame at the root.
            EnsurePrimitive(root.transform, "WaxPool", PrimitiveType.Cylinder,
                new Vector3(0f, 0.012f - flameY, 0f), new Vector3(0.155f, 0.012f, 0.155f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Body", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight - flameY, 0f), new Vector3(0.105f, waxHeight, 0.105f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Lip", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight * 2f - flameY, 0f), new Vector3(0.118f, 0.014f, 0.118f), Vector3.zero, wax);
            EnsurePrimitive(root.transform, "Wick", PrimitiveType.Cylinder,
                new Vector3(0f, waxHeight * 2f + 0.022f - flameY, 0f), new Vector3(0.009f, 0.022f, 0.009f), Vector3.zero, wick);
            EnsurePrimitive(root.transform, "Flame", PrimitiveType.Quad,
                new Vector3(0f, 0f, -0.005f), new Vector3(0.15f, 0.25f, 1f), Vector3.zero, flameMat);

            // The candles are the menu's motivated light; they must own the room.
            // NOTE: the Light lives on the candle root, so light.transform IS
            // root.transform - never reposition via light.transform here or it
            // wipes the staging above.
            var light = root.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.66f, 0.30f, 1f);
                light.intensity = 4.2f;
                light.range = 7.0f;
                light.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(light);
            }

            EditorUtility.SetDirty(root);
        }

        private static void StageDeckHero()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage == null)
            {
                return;
            }

            // The deck is the fantasy of the game in one object: give it the left
            // third and lift a card off the top so it catches the candlelight.
            var deck = stage.transform.Find("MP_MenuDeck");
            if (deck != null)
            {
                deck.localPosition = new Vector3(-1.5f, 0f, -0.15f);
                deck.localRotation = Quaternion.Euler(0f, 8f, 0f);
                EditorUtility.SetDirty(deck.gameObject);

                var body = LoadMaterial("MP_DeckBody");
                var back = LoadMaterial("MP_CardBack");
                // The top card pushed askew off the stack, still flat on it. A
                // card tilted up into the air showed the camera its unlit underside
                // and read as a black wedge; flat, it catches the candlelight like
                // the loose cards do.
                var lifted = deck.Find("MP_LiftedCard")?.gameObject ?? CreateChild(deck, "MP_LiftedCard");
                lifted.transform.localPosition = new Vector3(0.44f, 0.243f, 0.19f);
                lifted.transform.localRotation = Quaternion.Euler(0f, 17f, 0f);
                lifted.transform.localScale = Vector3.one;
                EnsurePrimitive(lifted.transform, "Body", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.8f, 0.02f, 1.18f), Vector3.zero, body);
                EnsurePrimitive(lifted.transform, "TopBack", PrimitiveType.Quad,
                    new Vector3(0f, 0.012f, 0f), new Vector3(0.78f, 1.16f, 1f),
                    new Vector3(90f, 0f, 0f), back);
                EditorUtility.SetDirty(lifted);
            }

            // Push the loose cards to the right third so the centre column stays
            // clear for the title/button stack.
            var scatterA = stage.transform.Find("MP_ScatterCard_A");
            if (scatterA != null)
            {
                scatterA.localPosition = new Vector3(1.36f, 0f, 0.06f);
                scatterA.localRotation = Quaternion.Euler(0f, -17f, 0f);
                EditorUtility.SetDirty(scatterA.gameObject);
            }

            var scatterB = stage.transform.Find("MP_ScatterCard_B");
            if (scatterB != null)
            {
                scatterB.localPosition = new Vector3(1.74f, 0f, -0.74f);
                scatterB.localRotation = Quaternion.Euler(0f, 26f, 0f);
                EditorUtility.SetDirty(scatterB.gameObject);
            }
        }

        private static void StageLighting()
        {
            var stage = GameObject.Find("MP_MenuStage");
            if (stage != null)
            {
                // The unseen fill every candlelit set uses: it reads as the candles'
                // collective glow and is what lets the gold on the card backs and the
                // wax on the candle sides hold their colour. Kept dim enough that the
                // two flames remain the visible motivation.
                var existingFill = stage.transform.Find("MP_TableFill");
                var fillGo = existingFill != null
                    ? existingFill.gameObject
                    : CreateChild(stage.transform, "MP_TableFill");
                fillGo.transform.localPosition = new Vector3(0f, 2.15f, 0.15f);

                // Explicit null check, not ??: the null-coalescing operator bypasses
                // Unity's overloaded == and hands back a fake-null Component.
                var fill = fillGo.GetComponent<Light>();
                if (fill == null)
                {
                    fill = fillGo.AddComponent<Light>();
                }

                fill.type = LightType.Point;
                fill.color = new Color(1f, 0.84f, 0.66f, 1f);
                fill.intensity = 1.15f;
                fill.range = 9.5f;
                fill.shadows = LightShadows.None;
                EditorUtility.SetDirty(fillGo);
            }

            // The directional key was flattening the room; drop it to a floor light
            // so the candle pools actually read as pools.
            var key = GameObject.Find("Menu Key Light");
            var light = key != null ? key.GetComponent<Light>() : null;
            if (light != null)
            {
                // A directional light reaches everything equally, including the far
                // rim 9 units out that should be lost in the dark - at 0.11 it still
                // lit that rim's top face into a fluorescent bar across the frame.
                // The candles are the only real light in this room.
                light.intensity = 0.035f;
                light.color = new Color(0.60f, 0.66f, 1f, 1f);
                EditorUtility.SetDirty(light);
            }
        }

        private static void RetireWashes()
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                return;
            }

            foreach (var name in RetiredWashes)
            {
                var wash = canvas.transform.Find(name);
                if (wash != null && wash.gameObject.activeSelf)
                {
                    wash.gameObject.SetActive(false);
                    EditorUtility.SetDirty(wash.gameObject);
                }
            }
        }

        private static Material EnsureLitMaterial(string name, Color color, float smoothness)
        {
            var material = EnsureMaterial(name, Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureUnlitMaterial(string name, string textureName, Color color, bool additive)
        {
            var material = EnsureMaterial(name, Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteFolder}/{textureName}.png"));
            material.SetColor("_BaseColor", color);

            if (additive)
            {
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
                SetFloatIfPresent(material, "_Surface", 1f);
                SetFloatIfPresent(material, "_Blend", 1f);
                SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.One);
                SetFloatIfPresent(material, "_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            EditorUtility.SetDirty(material);
            return material;
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
