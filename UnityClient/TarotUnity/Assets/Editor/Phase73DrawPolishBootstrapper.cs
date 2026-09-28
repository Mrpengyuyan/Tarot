using System.Linq;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 73, the user's second pass on the draw. Runs after the Phase 72 bootstrap. Idempotent.
    /// - The face-up card glared: layers from Phases 7-15 were stacked on the face (an ivory slab,
    ///   gold bars, glow panels, a title band) and blew out under the table pool. They stop
    ///   drawing, and the components that switched two of them back on lose those references.
    ///   The face is now the card body, the artwork and its gold mat.
    /// - Yellow sparks drifted through the whole reading (the Phase 18 ambient and deck-focus
    ///   loops) and burst on shuffle, deal, flip and result (Phase 8 and 18). All go quiet; the
    ///   objects stay for the older tests that look them up.
    /// - The deck sat over the Past slot and the Celtic Cross's left slot, and half out of the
    ///   default view. It moves back and left, behind the Past slot and clear of the Celtic Cross,
    //   where the default view, the shuffle camera (which follows it) and the draw camera all see it.
    /// - The shuffle is stretched to two seconds.
    /// - The fan's surroundings read as black: the cloth ended at the player's edge (z -6.3) and
    ///   the side rims stood at x ±5.6 with unlit cloth beyond. A near-side cloth piece (its own
    ///   material, same texel density as the shared cloth material) runs the table on toward the
    ///   player, and the side rims move out to the cloth's edges.
    /// </summary>
    public static class Phase73DrawPolishBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        public static readonly string[] LegacyFaceLayers =
        {
            "Front", "FrontFrame", "InnerGlow", "Phase7_TitleBand", "Top", "Bottom", "Left", "Right",
            "Phase12_FaceArtworkLabel", "Phase14_RevealGlow", "Phase15_CardFacePlane",
        };

        private const string ClothMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_TableCloth.mat";
        private const string NearClothMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_TableClothNear.mat";
        // As long as the shared cloth (15), so its tiling is a whole number and the pattern runs on
        // across the seam in phase.
        public const float NearClothLength = 15f;
        public const float RimX = 11.45f;
        public const float RimFarZ = 4.8f;

        // The reading room's ritual sparks (Phase 8 cue bursts and Phase 18 loops). Named, so a
        // particle system added to the room later is not silenced by a re-run.
        public static readonly string[] SilencedParticles =
        {
            "ShuffleParticles", "DealParticles", "FlipParticles", "ResultReadyParticles",
            "Phase18_AmbientDustParticles", "Phase18_DeckFocusParticles", "Phase18_FlipSparkParticles",
        };

        public static readonly Vector3 DeckPosition = new Vector3(-1.9f, 0.12f, 2.3f);

        // Where the shuffle camera sits relative to the deck (Phase 21's framing, kept).
        private static readonly Vector3 DeckPoseOffset = new Vector3(0.35f, 1.58f, -2.0f);

        [MenuItem("Tools/Tarot Unity/Run Phase 73 Draw Polish Bootstrap")]
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

            CleanCardFace();
            RestageRoom();
            AssetDatabase.SaveAssets();
            Debug.Log("Tarot Unity Phase 73 draw polish bootstrap complete.");
        }

        private static void CleanCardFace()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => LegacyFaceLayers.Contains(r.name)))
                {
                    renderer.enabled = false;
                }

                var threeD = new SerializedObject(root.GetComponent<ThreeDCardPresentationController>());
                threeD.FindProperty("cardFaceRenderer").objectReferenceValue = null;
                threeD.ApplyModifiedPropertiesWithoutUndo();
                var reveal = new SerializedObject(root.GetComponent<DimensionalCardRevealController>());
                reveal.FindProperty("revealGlowRenderer").objectReferenceValue = null;
                reveal.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RestageRoom()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => SilencedParticles.Contains(p.name)))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;
                var emission = ps.emission;
                emission.enabled = false;
                ps.GetComponent<ParticleSystemRenderer>().enabled = false;
                EditorUtility.SetDirty(ps.gameObject);
            }

            var deck = GameObject.Find("DeckStack");
            deck.transform.position = DeckPosition;
            EditorUtility.SetDirty(deck.transform);

            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            var deckPose = (Transform)new SerializedObject(choreography).FindProperty("deckPose").objectReferenceValue;
            deckPose.position = DeckPosition + DeckPoseOffset;
            EditorUtility.SetDirty(deckPose);

            var shuffle = new SerializedObject(Object.FindFirstObjectByType<DeckShuffleChoreographer>());
            shuffle.FindProperty("riffleStagger").floatValue = 0.04f;
            shuffle.FindProperty("cutSeconds").floatValue = 0.38f;
            shuffle.FindProperty("recutSeconds").floatValue = 0.24f;
            shuffle.FindProperty("squareHoldSeconds").floatValue = 0.25f;
            shuffle.ApplyModifiedPropertiesWithoutUndo();

            ExtendTable();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ExtendTable()
        {
            var cloth = GameObject.Find("MP_TableCloth");
            var clothBounds = cloth.GetComponent<Renderer>().bounds;
            var clothSize = cloth.transform.localScale;
            var nearLength = NearClothLength;
            var nearEdge = clothBounds.min.z - nearLength;

            // The shared cloth material tiles 3 x 3 over the 24 x 15 cloth; the near piece keeps that density.
            if (AssetDatabase.LoadAssetAtPath<Material>(NearClothMaterialPath) == null)
            {
                AssetDatabase.CopyAsset(ClothMaterialPath, NearClothMaterialPath);
            }

            var shared = AssetDatabase.LoadAssetAtPath<Material>(ClothMaterialPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(NearClothMaterialPath);
            foreach (var property in new[] { "_BaseMap", "_MainTex", "_BumpMap" })
            {
                if (material.HasProperty(property))
                {
                    var scale = shared.GetTextureScale(property);
                    material.SetTextureScale(property, new Vector2(scale.x, scale.y * nearLength / clothSize.z));
                }
            }

            EditorUtility.SetDirty(material);

            var existing = cloth.transform.parent.Find("MP_TableClothNear");
            var near = existing != null ? existing.gameObject : Object.Instantiate(cloth, cloth.transform.parent);
            near.name = "MP_TableClothNear";
            near.transform.localRotation = cloth.transform.localRotation;
            near.transform.localScale = new Vector3(clothSize.x, clothSize.y, nearLength);
            near.transform.position = new Vector3(cloth.transform.position.x, cloth.transform.position.y, nearEdge + nearLength / 2f);
            near.GetComponent<Renderer>().sharedMaterial = material;
            EditorUtility.SetDirty(near);

            var rimLength = RimFarZ - nearEdge;
            foreach (var (name, side) in new[] { ("MP_TableRimLeft", -1f), ("MP_TableRimRight", 1f) })
            {
                var rim = GameObject.Find(name).transform;
                rim.position = new Vector3(side * RimX, rim.position.y, nearEdge + rimLength / 2f);
                rim.localScale = new Vector3(rim.localScale.x, rim.localScale.y, rimLength);
                EditorUtility.SetDirty(rim);
            }
        }
    }
}
