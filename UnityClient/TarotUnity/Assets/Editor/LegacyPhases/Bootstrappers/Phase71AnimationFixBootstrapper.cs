using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 71 fixes two effects the user flagged after playing the Phase 69/70 build:
    /// - The card's hover/"flip me" highlight was the Phase 2 graybox slab: an opaque amber box
    ///   bigger than the card, laid over it. Under the Phase 68 table pool it blew out into a
    ///   glaring yellow board. It becomes a flat quad of the socket-glow material under the card,
    ///   so only a warm rim shows past the card's edges (CardView widens it on hover).
    /// - The menu censer's "smoke" was a bead-string of glowing blobs rising in a straight line.
    ///   It goes quiet and the censer throws embers instead - flickering sparks that swirl up and
    ///   burn out, with an occasional burst.
    /// Idempotent.
    /// </summary>
    public static class Phase71AnimationFixBootstrapper
    {
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string WarmGlowPath = "Assets/Art/MidnightParlor/Materials/MP_WarmGlow.mat";

        // Under the card body (Phase15_CardMeshRoot sits at -0.018, so the body's bottom face is
        // at -0.0355) and above its drop shadow (-0.054 through the same root).
        private static readonly Vector3 HaloPosition = new Vector3(0f, -0.045f, 0f);
        private static readonly Vector3 HaloEuler = new Vector3(90f, 0f, 0f);
        private static readonly Vector3 HaloScale = new Vector3(1.5f, 1.95f, 1f);

        [MenuItem("Tools/Tarot Unity/Run Phase 71 Animation Fix Bootstrap")]
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

            var glow = AssetDatabase.LoadAssetAtPath<Material>(WarmGlowPath);
            if (glow == null)
            {
                Debug.LogError("Phase 71: MP_WarmGlow missing; run the Phase 37 bootstrap first.");
                return;
            }

            RebuildCardHalo(glow);
            RestageCenser(glow);
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 71 animation fix complete.");
        }

        private static void RebuildCardHalo(Material glow)
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var halo = FindDeep(root.transform, "Highlight");
                if (halo == null)
                {
                    Debug.LogError("Phase 71: PF_TarotCard has no Highlight child.");
                    return;
                }

                foreach (var collider in halo.GetComponents<Collider>())
                {
                    Object.DestroyImmediate(collider);
                }

                halo.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var renderer = halo.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = glow;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                halo.localPosition = HaloPosition;
                halo.localEulerAngles = HaloEuler;
                halo.localScale = HaloScale;
                halo.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RestageCenser(Material glow)
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var censer = GameObject.Find("MP_MenuStage")?.transform.Find("MP_Censer");
            if (censer == null)
            {
                Debug.LogError("Phase 71: MP_MenuStage/MP_Censer not found; run the Phase 45 bootstrap first.");
                return;
            }

            SilenceSmoke(censer.Find("Smoke").GetComponent<ParticleSystem>());
            StageEmbers(censer, glow);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// The Phase 45 smoke goes quiet. With the warm glow sprite it could only be either a bead
        /// string of blobs (the original), a glowing gold column (dense), or two bright smudges at
        /// the bowl (sparse) - never smoke. The embers carry the censer now. The object stays
        /// because Phase45MenuDepthTests checks for it.
        /// </summary>
        private static void SilenceSmoke(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.enabled = false;
            ps.GetComponent<ParticleSystemRenderer>().enabled = false;
            EditorUtility.SetDirty(ps.gameObject);
        }

        /// <summary>Small sparks off the coals: they flicker, swirl up faster as they rise, and burn out.</summary>
        private static void StageEmbers(Transform censer, Material glow)
        {
            var existing = censer.Find("Embers");
            var go = existing != null ? existing.gameObject : new GameObject("Embers");
            if (existing == null)
            {
                go.transform.SetParent(censer, false);
            }

            go.transform.localPosition = new Vector3(0f, 0.155f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // GetComponent returns Unity's fake null, which `??` does not see.
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.AddComponent<ParticleSystem>();
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.duration = 3.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.46f, 0.14f, 1f));
            main.maxParticles = 80;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.4f, (short)3, (short)6, 1, 0.01f) { probability = 0.7f },
                new ParticleSystem.Burst(1.9f, (short)2, (short)4, 1, 0.01f) { probability = 0.5f },
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.06f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var burnOut = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, burnOut);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.25f), 0.5f),
                    new GradientColorKey(new Color(0.85f, 0.2f, 0.08f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.85f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 1.2f;
            noise.scrollSpeed = 0.4f;
            noise.octaveCount = 2;
            noise.sizeAmount = 0.6f;   // flicker

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = glow;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 1.2f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            EditorUtility.SetDirty(go);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
