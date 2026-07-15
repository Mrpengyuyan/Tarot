using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 38 rebuilds the ReadingRoom tabletop in the Midnight Parlor language
    /// (Docs/PHASE37_VISUAL_REDESIGN_BLUEPRINT.md): one velvet cloth with a walnut
    /// rim replaces eight generations of overlapping flat-color planes, the card
    /// slots become gold socket decals on the cloth, the deck becomes a staggered
    /// stack wearing the composed celestial card back, and the card prefab's
    /// primitive-block back ornaments give way to the real card-back texture.
    /// The superseded visual objects are deleted (user-authorized teardown), while
    /// every gameplay anchor (slots, deck root, Phase 15 anchors, aura/particle
    /// controllers) stays. Idempotent.
    /// </summary>
    public static class Phase38TableRebuildBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        public const string StageRootName = "MP_TableStage";

        private const string MaterialFolder = Phase37AssetFoundationBootstrapper.MaterialFolder;

        /// <summary>Scene root objects deleted by the rebuild.</summary>
        public static readonly string[] LegacySceneObjects =
        {
            "Graybox Tarot Table",
            "Phase8_ReadingVisualRoot",
            "Phase12_CardRevealStage",
            "Phase12_RevealBackdrop",
            "Phase14_TableDepthPlane",
            "Phase14_CardRevealPool",
        };

        /// <summary>Children of Phase15_ThreeDTableRoot deleted by the rebuild.</summary>
        public static readonly string[] LegacyTableRootChildren =
        {
            "Phase15_RitualTableSurface",
            "Phase15_TableDepthRing",
        };

        /// <summary>Card prefab Back ornaments replaced by the composed back texture.</summary>
        public static readonly string[] LegacyBackOrnaments =
        {
            "BackSigil",
            "BackConstellation",
            "Phase7_MoonSigil",
            "Phase7_BackVeil",
            "Phase8_BackPatternTop",
            "Phase8_BackPatternBottom",
            "Phase8_CenterGem",
        };

        /// <summary>
        /// Always-on Phase 14 "2.5D" dressing quads replaced by the real card back
        /// (the reveal glow and the Phase 15 drop shadow stay - they are referenced
        /// and animated by the presentation controllers).
        /// </summary>
        public static readonly string[] LegacyDimensionalDressing =
        {
            "Phase14_CardEdge",
            "Phase14_FaceRimLight",
            "Phase14_ArtworkGlass",
            "Phase14_CastShadow",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 38 Table Rebuild Bootstrap")]
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

            RebuildCardPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeleteLegacySceneObjects();
            HideSlotSlabs();
            BuildTableStage();
            RestyleDeck();
            RebalanceLights();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 38 table rebuild complete.");
        }

        private static void DeleteLegacySceneObjects()
        {
            foreach (var name in LegacySceneObjects)
            {
                var target = GameObject.Find(name);
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                    Debug.Log($"Phase 38: deleted legacy scene object '{name}'.");
                }
            }

            // The stray empty world-space Phase7_TableVignette root (the UI one lives
            // under ReadingRoomCanvas and keeps its name).
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "Phase7_TableVignette" && root.transform.childCount == 0
                    && root.GetComponents<Component>().Length == 1)
                {
                    Object.DestroyImmediate(root);
                    Debug.Log("Phase 38: deleted stray empty world Phase7_TableVignette root.");
                    break;
                }
            }

            var tableRoot = GameObject.Find("Phase15_ThreeDTableRoot");
            if (tableRoot != null)
            {
                foreach (var childName in LegacyTableRootChildren)
                {
                    var child = tableRoot.transform.Find(childName);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                        Debug.Log($"Phase 38: deleted legacy table child '{childName}'.");
                    }
                }
            }
        }

        private static void HideSlotSlabs()
        {
            foreach (var slotName in new[] { "OneCardSlot", "PastSlot", "PresentSlot", "AdviceSlot" })
            {
                var slot = GameObject.Find(slotName);
                var renderer = slot != null ? slot.GetComponent<MeshRenderer>() : null;
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void BuildTableStage()
        {
            var cloth = LoadMaterial("MP_TableCloth");
            cloth.SetTextureScale("_BaseMap", new Vector2(5f, 3.5f));
            var wood = LoadMaterial("MP_TableWood");
            var socket = LoadMaterial("MP_CardSocket");

            var stage = GameObject.Find(StageRootName) ?? new GameObject(StageRootName);
            stage.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            EnsurePrimitive(stage.transform, "MP_TableCloth", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 1.2f), new Vector3(24f, 0.1f, 15f), Vector3.zero, cloth);
            EnsurePrimitive(stage.transform, "MP_TableRimFar", PrimitiveType.Cube,
                new Vector3(0f, 0.10f, 4.2f), new Vector3(24f, 0.34f, 1.2f), Vector3.zero, wood);
            EnsurePrimitive(stage.transform, "MP_TableRimLeft", PrimitiveType.Cube,
                new Vector3(-5.6f, 0.10f, 1.0f), new Vector3(1.1f, 0.34f, 7.6f), Vector3.zero, wood);
            EnsurePrimitive(stage.transform, "MP_TableRimRight", PrimitiveType.Cube,
                new Vector3(5.6f, 0.10f, 1.0f), new Vector3(1.1f, 0.34f, 7.6f), Vector3.zero, wood);

            // The dark parlor beyond the table edge: catches the frame corners so no
            // camera pose ever sees raw void.
            var backdrop = EnsureBackdropMaterial();
            EnsurePrimitive(stage.transform, "MP_ParlorBackdrop", PrimitiveType.Quad,
                new Vector3(0f, 2.5f, 9.5f), new Vector3(40f, 12f, 1f),
                Vector3.zero, backdrop);

            var sockets = stage.transform.Find("MP_CardSockets")?.gameObject
                          ?? CreateChild(stage.transform, "MP_CardSockets");
            foreach (var slotName in new[] { "OneCardSlot", "PastSlot", "PresentSlot", "AdviceSlot" })
            {
                var slot = GameObject.Find(slotName);
                if (slot == null)
                {
                    continue;
                }

                var pos = slot.transform.position;
                EnsurePrimitive(sockets.transform, $"MP_Socket_{slotName}", PrimitiveType.Quad,
                    new Vector3(pos.x, 0.145f, pos.z), new Vector3(1.06f, 1.52f, 1f),
                    new Vector3(90f, 0f, 0f), socket);
            }
        }

        private static void RestyleDeck()
        {
            var deckStack = GameObject.Find("DeckStack");
            if (deckStack == null)
            {
                Debug.LogWarning("Phase 38: DeckStack not found; deck restyle skipped.");
                return;
            }

            var graybox = deckStack.transform.Find("Graybox Deck");
            var grayboxRenderer = graybox != null ? graybox.GetComponent<MeshRenderer>() : null;
            if (grayboxRenderer != null && grayboxRenderer.enabled)
            {
                grayboxRenderer.enabled = false;
                EditorUtility.SetDirty(grayboxRenderer);
            }

            var body = EnsureDeckBodyMaterial();
            var back = LoadMaterial("MP_CardBack");

            var stack = deckStack.transform.Find("MP_DeckStack")?.gameObject
                        ?? CreateChild(deckStack.transform, "MP_DeckStack");
            stack.transform.localPosition = Vector3.zero;

            const int layers = 8;
            var rng = new System.Random(1909);
            for (var i = 0; i < layers; i++)
            {
                var jitterX = (float)(rng.NextDouble() - 0.5) * 0.024f;
                var jitterZ = (float)(rng.NextDouble() - 0.5) * 0.024f;
                var twist = (float)(rng.NextDouble() - 0.5) * 2.4f;
                EnsurePrimitive(stack.transform, $"MP_DeckCard_{i:D2}", PrimitiveType.Cube,
                    new Vector3(jitterX, -0.13f + 0.023f * i, jitterZ),
                    new Vector3(0.8f, 0.02f, 1.18f), new Vector3(0f, twist, 0f), body);
            }

            var topY = -0.13f + 0.023f * (layers - 1) + 0.012f;
            EnsurePrimitive(stack.transform, "MP_DeckTopBack", PrimitiveType.Quad,
                new Vector3(0f, topY, 0f), new Vector3(0.78f, 1.16f, 1f),
                new Vector3(90f, 0f, 0f), back);
        }

        private static void RebalanceLights()
        {
            var key = GameObject.Find("Table Key Light");
            var light = key != null ? key.GetComponent<Light>() : null;
            if (light != null)
            {
                light.intensity = 0.85f;
                EditorUtility.SetDirty(light);
            }

            // The cool fill was tuned against flat gray planes; on the aubergine card
            // backs it reads as a blue cast, so pull it down to a hint.
            var moon = GameObject.Find("Moon Fill Light");
            var moonLight = moon != null ? moon.GetComponent<Light>() : null;
            if (moonLight != null)
            {
                moonLight.intensity = 0.4f;
                moonLight.color = new Color(0.52f, 0.58f, 0.95f, 1f);
                EditorUtility.SetDirty(moonLight);
            }
        }

        private static void RebuildCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var back = root.transform.Find("Back");
                if (back == null)
                {
                    Debug.LogWarning("Phase 38: card prefab has no Back child; skipped.");
                    return;
                }

                foreach (var name in LegacyBackOrnaments)
                {
                    var child = back.Find(name);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                        Debug.Log($"Phase 38: deleted card back ornament '{name}'.");
                    }
                }

                var dimensionalRoot = root.transform.Find("Phase14_DimensionalRoot");
                if (dimensionalRoot != null)
                {
                    foreach (var name in LegacyDimensionalDressing)
                    {
                        var child = dimensionalRoot.Find(name);
                        if (child != null)
                        {
                            Object.DestroyImmediate(child.gameObject);
                            Debug.Log($"Phase 38: deleted dimensional dressing '{name}'.");
                        }
                    }
                }

                // BackSigil is gone; clear the stale serialized reference so the
                // prefab carries no missing-object fields.
                var polish = root.GetComponent<TarotUnity.Presentation.CardPresentationPolish>();
                if (polish != null)
                {
                    var so = new SerializedObject(polish);
                    var sigilProp = so.FindProperty("backSigil");
                    var sigilRendererProp = so.FindProperty("sigilRenderer");
                    if (sigilProp != null && sigilProp.objectReferenceValue == null)
                    {
                        sigilProp.objectReferenceValue = null;
                    }

                    if (sigilRendererProp != null && sigilRendererProp.objectReferenceValue == null)
                    {
                        sigilRendererProp.objectReferenceValue = null;
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var backMaterial = LoadMaterial("MP_CardBack");
                var face = back.Find("MP_CardBackFace")?.gameObject;
                if (face == null)
                {
                    face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    face.name = "MP_CardBackFace";
                    Object.DestroyImmediate(face.GetComponent<Collider>());
                    face.transform.SetParent(back, false);
                }

                face.transform.localPosition = new Vector3(0f, 0.62f, 0f);
                face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                face.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                face.GetComponent<MeshRenderer>().sharedMaterial = backMaterial;

                // The Phase 15 shell's underside plane shows during the flip - give it
                // the same composed back (the design is 180-degree symmetric on purpose).
                var shellBack = root.transform.Find("Phase15_CardMeshRoot/Phase15_CardBackPlane");
                if (shellBack != null)
                {
                    shellBack.GetComponent<MeshRenderer>().sharedMaterial = backMaterial;
                }

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material EnsureBackdropMaterial()
        {
            var path = $"{MaterialFolder}/MP_ParlorBackdrop.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", new Color(0.055f, 0.03f, 0.05f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureDeckBodyMaterial()
        {
            var path = $"{MaterialFolder}/MP_DeckBody.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", new Color(0.16f, 0.09f, 0.14f, 1f));
            material.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(material);
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
