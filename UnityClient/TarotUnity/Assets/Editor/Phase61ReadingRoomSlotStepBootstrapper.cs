using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 61: refine the reading room's card slots and ritual step bar.
    /// <para>
    /// The sockets keep their <c>MP_CardSocket</c> decal (now a recessed velvet
    /// pool from the regenerated <c>TarotSocket.png</c>) and gain a warm additive
    /// glow quad each. A new <see cref="RitualStepIndicator"/> lights the glow for
    /// the selected spread once the draw begins, and drives the five step chips
    /// (choose spread → write question → draw → flip → reveal) so the current step
    /// reads gold and the finished steps read as completed.
    /// </para>
    /// This wires the runtime; it does not itself regenerate the socket texture
    /// (that is a one-off asset regen via Tools/UiKitGenerator/gen_uikit.py).
    /// </summary>
    public static class Phase61ReadingRoomSlotStepBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string WarmGlowMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_WarmGlow.mat";
        private const string GlowGroupName = "MP_SocketGlows";

        // Socket slot keys, in the order the sockets were built (Phase 38).
        private const string OneSlot = "OneCardSlot";
        private static readonly string[] ThreeSlots = { "PastSlot", "PresentSlot", "AdviceSlot" };

        // Glow quad sits just above the socket decal and blooms past the card edges.
        private const float GlowLift = 0.010f;
        private static readonly Vector3 GlowScale = new Vector3(2.1f, 2.65f, 1f);
        private static readonly Vector3 GlowEuler = new Vector3(90f, 0f, 0f);

        // The five ritual chips, left to right.
        private static readonly string[] ChipNames =
        {
            "Phase7_Progress_ChooseSpread",
            "Phase7_Progress_AskQuestion",
            "Phase7_Progress_DrawCards",
            "Phase7_Progress_FlipCards",
            "Phase7_Progress_RevealResult",
        };

        [MenuItem("Tools/Tarot Unity/Run Phase 61 Reading Room Slot + Step Bootstrap")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ReadingRoomScenePath, OpenSceneMode.Single);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 61: ReadingRoomCanvas not found.");
                return;
            }

            var flow = Object.FindObjectOfType<ReadingFlowController>();
            if (flow == null)
            {
                Debug.LogError("Phase 61: ReadingFlowController not found.");
                return;
            }

            var warmGlow = AssetDatabase.LoadAssetAtPath<Material>(WarmGlowMaterialPath);
            if (warmGlow == null)
            {
                Debug.LogError($"Phase 61: {WarmGlowMaterialPath} not found.");
                return;
            }

            // --- Socket glow quads (idempotent: rebuild the group each run) ---
            // The group sits under the table stage as a SIBLING of MP_CardSockets, so
            // the socket group keeps exactly its four sockets (guarded by Phase 38).
            var stage = GameObject.Find(Phase38TableRebuildBootstrapper.StageRootName);
            var parent = stage != null ? stage.transform : null;

            var existingGroup = GameObject.Find(GlowGroupName);
            if (existingGroup != null)
            {
                Object.DestroyImmediate(existingGroup);
            }

            var group = new GameObject(GlowGroupName);
            group.transform.SetParent(parent, worldPositionStays: false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            var oneGlow = BuildSocketGlow(group.transform, OneSlot, warmGlow);
            var threeGlows = new GameObject[ThreeSlots.Length];
            for (var i = 0; i < ThreeSlots.Length; i++)
            {
                threeGlows[i] = BuildSocketGlow(group.transform, ThreeSlots[i], warmGlow);
            }

            // --- Ritual step indicator (idempotent: reset so palette tuning in the
            //     component defaults re-applies on each run) ---
            var existingIndicator = canvas.GetComponent<RitualStepIndicator>();
            if (existingIndicator != null)
            {
                Object.DestroyImmediate(existingIndicator);
            }

            var indicator = canvas.AddComponent<RitualStepIndicator>();

            WireIndicator(indicator, flow, oneGlow, threeGlows);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 61: socket glows built and ritual step indicator wired.");
        }

        private static GameObject BuildSocketGlow(Transform parent, string slotKey, Material warmGlow)
        {
            var socket = GameObject.Find($"MP_Socket_{slotKey}");
            var socketPos = socket != null ? socket.transform.position : Vector3.zero;

            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = $"MP_SocketGlow_{slotKey}";
            glow.transform.SetParent(parent, worldPositionStays: false);
            glow.transform.position = new Vector3(socketPos.x, socketPos.y + GlowLift, socketPos.z);
            glow.transform.eulerAngles = GlowEuler;
            glow.transform.localScale = GlowScale;

            // The quad is a decal, not a clickable target — drop its collider so it
            // never intercepts the card flip raycasts.
            var collider = glow.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = glow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = warmGlow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            glow.SetActive(false); // the indicator lights it during the draw.
            return glow;
        }

        private static void WireIndicator(
            RitualStepIndicator indicator, ReadingFlowController flow, GameObject oneGlow, GameObject[] threeGlows)
        {
            var so = new SerializedObject(indicator);
            so.FindProperty("flowController").objectReferenceValue = flow;

            var chipsProp = so.FindProperty("chips");
            chipsProp.arraySize = ChipNames.Length;
            for (var i = 0; i < ChipNames.Length; i++)
            {
                var marker = GameObject.Find(ChipNames[i]);
                var el = chipsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue =
                    marker != null ? marker.GetComponent<RectTransform>() : null;
                el.FindPropertyRelative("plate").objectReferenceValue =
                    marker != null ? FindGraphic(marker.transform, "Plate") : null;
                el.FindPropertyRelative("label").objectReferenceValue =
                    marker != null ? FindGraphic(marker.transform, "Label") : null;
            }

            so.FindProperty("oneCardSocketGlow").objectReferenceValue = oneGlow;
            var threeProp = so.FindProperty("threeCardSocketGlows");
            threeProp.arraySize = threeGlows.Length;
            for (var i = 0; i < threeGlows.Length; i++)
            {
                threeProp.GetArrayElementAtIndex(i).objectReferenceValue = threeGlows[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Graphic FindGraphic(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<Graphic>() : null;
        }
    }
}
