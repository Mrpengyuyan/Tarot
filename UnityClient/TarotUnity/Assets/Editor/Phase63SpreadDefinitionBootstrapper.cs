using System.Collections.Generic;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 63: turn spreads into data (SpreadDefinition/SpreadCatalog assets) and
    /// add the ten-card Celtic Cross. This generates the catalog assets, builds the
    /// Celtic Cross table slots + sockets + glows + camera pose, adds a spread button,
    /// and wires every spread-aware system (layout, socket glows, camera, reading
    /// room) off the catalog. The one- and three-card spreads keep their existing
    /// scene objects; only their slot/glow references are re-registered by count.
    /// Coordinates here are a first pass to be tuned from the review capture.
    /// </summary>
    public static class Phase63SpreadDefinitionBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string SpreadFolder = "Assets/Resources/Spreads";
        private const string SocketMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_CardSocket.mat";
        private const string WarmGlowMaterialPath = "Assets/Art/MidnightParlor/Materials/MP_WarmGlow.mat";

        private const int CelticId = 3;
        private const int CelticCount = 10;

        // Celtic Cross, in reading order (0..9). Cross on the left (clear of the deck
        // at x≈-2.9), staff column on the right. Z (depth) is exaggerated so the
        // cross's vertical arm still reads under the angled table camera.
        private static readonly Vector3[] CelticSlots =
        {
            new Vector3(-0.6f, 0.12f, 0.3f),   // 0 现状 (center)
            new Vector3(0.55f, 0.12f, 0.3f),   // 1 挑战 (right of center)
            new Vector3(-0.6f, 0.12f, -1.2f),  // 2 根基 (below/near)
            new Vector3(-1.75f, 0.12f, 0.3f),  // 3 过去 (left)
            new Vector3(-0.6f, 0.12f, 1.7f),   // 4 顶冠 (above/far)
            new Vector3(1.7f, 0.12f, 0.3f),    // 5 未来 (far right of arm)
            new Vector3(3.2f, 0.12f, -1.0f),   // 6 自我 (staff bottom)
            new Vector3(3.2f, 0.12f, 0.1f),    // 7 环境
            new Vector3(3.2f, 0.12f, 1.2f),    // 8 希望与恐惧
            new Vector3(3.2f, 0.12f, 2.3f),    // 9 结果 (staff top)
        };

        // The spread's centre, used to aim the camera.
        private static readonly Vector3 SpreadCenter = new Vector3(0.75f, 0.12f, 0.45f);

        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        private static readonly string[] CelticMeanings =
        {
            "此刻问题的核心。", "正在阻碍或考验你的。", "事情的根源与基础。", "刚刚过去、正在离开的。",
            "你的目标或可能的最好结果。", "接下来会到来的。", "你在其中的姿态。", "周围的人与外部影响。",
            "你内心的期待与担忧。", "综合之下的最终走向。",
        };

        // Framing for the whole cross+staff: aim at the spread centre so only the
        // position and FOV need tuning.
        private static readonly Vector3 CelticCameraPos = new Vector3(0.75f, 5.7f, -4.7f);
        private const float CelticCameraFov = 50f;
        private static Quaternion CelticCameraRotation =>
            Quaternion.LookRotation(SpreadCenter - CelticCameraPos, Vector3.up);

        private static readonly Vector3 SocketScale = new Vector3(1.06f, 1.52f, 1f);
        private static readonly Vector3 GlowScale = new Vector3(2.1f, 2.65f, 1f);
        private static readonly Vector3 FlatEuler = new Vector3(90f, 0f, 0f);

        [MenuItem("Tools/Tarot Unity/Run Phase 63 Spread Definition + Celtic Cross Bootstrap")]
        public static void Run()
        {
            var catalog = BuildCatalogAssets();

            var scene = EditorSceneManager.OpenScene(ReadingRoomScenePath, OpenSceneMode.Single);

            var layoutGo = Object.FindObjectOfType<SpreadLayoutController>();
            var indicator = Object.FindObjectOfType<RitualStepIndicator>();
            var camera = Object.FindObjectOfType<CameraChoreographyController>();
            var room = Object.FindObjectOfType<ReadingRoomController>();
            if (layoutGo == null || indicator == null || camera == null || room == null)
            {
                Debug.LogError("Phase 63: a required controller is missing from the reading room.");
                return;
            }

            var socketMat = AssetDatabase.LoadAssetAtPath<Material>(SocketMaterialPath);
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(WarmGlowMaterialPath);

            // --- Celtic scene objects (idempotent: rebuild each run) ---
            var slotParent = layoutGo.transform;
            var slotGroup = ReplaceGroup(slotParent, "Celtic_Slots");
            var stage = GameObject.Find("MP_TableStage")?.transform;
            var socketGroup = ReplaceGroup(stage, "MP_CelticSockets");
            var glowGroup = ReplaceGroup(stage, "MP_CelticSocketGlows");

            var celticSlots = new Transform[CelticCount];
            var celticGlows = new GameObject[CelticCount];
            for (var i = 0; i < CelticCount; i++)
            {
                var pos = CelticSlots[i];

                var slot = new GameObject($"Celtic_Slot_{i:D2}");
                slot.transform.SetParent(slotGroup.transform, false);
                slot.transform.position = pos;
                celticSlots[i] = slot.transform;

                BuildQuad(socketGroup.transform, $"MP_Socket_Celtic_{i:D2}",
                    new Vector3(pos.x, 0.145f, pos.z), SocketScale, socketMat, active: true);
                celticGlows[i] = BuildQuad(glowGroup.transform, $"MP_SocketGlow_Celtic_{i:D2}",
                    new Vector3(pos.x, 0.155f, pos.z), GlowScale, glowMat, active: false);
            }

            // --- Celtic camera pose ---
            var pose = ReplaceGroup(null, "MP_CelticCrossPose");
            pose.transform.SetPositionAndRotation(CelticCameraPos, CelticCameraRotation);

            // --- Existing one/three-card scene objects, found by name ---
            // The Phase 61 glow quads are inactive, so reach them through their
            // (active) group rather than GameObject.Find, which skips inactive.
            var oneSlot = GameObject.Find("OneCardSlot")?.transform;
            var threeSlots = FindAll("PastSlot", "PresentSlot", "AdviceSlot");
            var glow61 = GameObject.Find("MP_SocketGlows")?.transform;
            var oneGlow = FindChildGo(glow61, "MP_SocketGlow_OneCardSlot");
            var threeGlows = new[]
            {
                FindChildGo(glow61, "MP_SocketGlow_PastSlot"),
                FindChildGo(glow61, "MP_SocketGlow_PresentSlot"),
                FindChildGo(glow61, "MP_SocketGlow_AdviceSlot"),
            };

            // --- Wire the spread-aware systems by card count ---
            WireLayout(layoutGo, oneSlot, threeSlots, celticSlots);
            WireIndicatorGlows(indicator, oneGlow, threeGlows, celticGlows);
            WireCameraPose(camera, pose.transform);
            WireRoom(room, catalog);
            EnsureCelticButton(room);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 63: spread catalog built and Celtic Cross wired into the reading room.");
        }

        // ---------- Catalog assets ----------

        private static SpreadCatalog BuildCatalogAssets()
        {
            Directory.CreateDirectory(SpreadFolder);

            var one = UpsertDefinition("SpreadDefinition_OneCard", 1, 1, "One Card Focus",
                new[] { new Vector3(0f, 0.12f, 0.15f) }, new[] { "核心" },
                new[] { "针对这个问题最清晰的信号。" });

            var three = UpsertDefinition("SpreadDefinition_ThreeCard", 2, 3, "Past / Present / Advice",
                new[] { new Vector3(-1.45f, 0.12f, 0.15f), new Vector3(0f, 0.12f, 0.15f), new Vector3(1.45f, 0.12f, 0.15f) },
                new[] { "过去", "现在", "建议" },
                new[] { "是什么把这个问题带到了这里。", "此刻正在起作用的是什么。", "接下来更有用的姿态。" });

            var celtic = UpsertDefinition("SpreadDefinition_CelticCross", CelticId, CelticCount, "凯尔特十字",
                CelticSlots, CelticNames, CelticMeanings, CelticCameraPos, CelticCameraRotation.eulerAngles, CelticCameraFov);

            var catalogPath = $"{SpreadFolder}/SpreadCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<SpreadCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpreadCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.spreads = new[] { one, three, celtic };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static SpreadDefinition UpsertDefinition(
            string assetName, int id, int count, string display,
            Vector3[] slotPositions, string[] names, string[] meanings)
        {
            return UpsertDefinition(assetName, id, count, display, slotPositions, names, meanings,
                Vector3.zero, Vector3.zero, 60f);
        }

        private static SpreadDefinition UpsertDefinition(
            string assetName, int id, int count, string display,
            Vector3[] slotPositions, string[] names, string[] meanings,
            Vector3 camPos, Vector3 camEuler, float camFov)
        {
            var path = $"{SpreadFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<SpreadDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<SpreadDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.spreadId = id;
            def.cardCount = count;
            def.displayName = display;
            var slots = new SlotPlacement[slotPositions.Length];
            for (var i = 0; i < slotPositions.Length; i++)
            {
                slots[i] = new SlotPlacement { localPosition = slotPositions[i], yawDegrees = 0f };
            }
            def.slots = slots;
            def.positionNames = names;
            def.positionMeanings = meanings;
            def.cameraPosition = camPos;
            def.cameraEuler = camEuler;
            def.cameraFov = camFov;
            EditorUtility.SetDirty(def);
            return def;
        }

        // ---------- Scene helpers ----------

        private static GameObject ReplaceGroup(Transform parent, string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static GameObject BuildQuad(Transform parent, string name, Vector3 worldPos, Vector3 scale, Material material, bool active)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.position = worldPos;
            quad.transform.eulerAngles = FlatEuler;
            quad.transform.localScale = scale;

            var collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = quad.GetComponent<MeshRenderer>();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            quad.SetActive(active);
            return quad;
        }

        private static Transform[] FindAll(params string[] names)
        {
            var result = new Transform[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                result[i] = GameObject.Find(names[i])?.transform;
            }
            return result;
        }

        // Finds a child by name, including inactive ones (unlike GameObject.Find).
        private static GameObject FindChildGo(Transform parent, string name)
        {
            var child = parent != null ? parent.Find(name) : null;
            return child != null ? child.gameObject : null;
        }

        // ---------- Wiring ----------

        private static void WireLayout(SpreadLayoutController layout, Transform one, Transform[] three, Transform[] celtic)
        {
            layout.ConfigureSpread(1, one != null ? new[] { one } : System.Array.Empty<Transform>());
            layout.ConfigureSpread(3, three);
            layout.ConfigureSpread(CelticCount, celtic);
            EditorUtility.SetDirty(layout);
        }

        private static void WireIndicatorGlows(RitualStepIndicator indicator, GameObject one, GameObject[] three, GameObject[] celtic)
        {
            var so = new SerializedObject(indicator);
            var sets = so.FindProperty("socketGlowSets");
            sets.arraySize = 3;
            Phase61ReadingRoomSlotStepBootstrapper.WriteGlowSet(sets.GetArrayElementAtIndex(0), 1, new[] { one });
            Phase61ReadingRoomSlotStepBootstrapper.WriteGlowSet(sets.GetArrayElementAtIndex(1), 3, three);
            Phase61ReadingRoomSlotStepBootstrapper.WriteGlowSet(sets.GetArrayElementAtIndex(2), CelticCount, celtic);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireCameraPose(CameraChoreographyController camera, Transform pose)
        {
            var so = new SerializedObject(camera);
            var poses = so.FindProperty("spreadPoses");
            poses.arraySize = 1;
            var el = poses.GetArrayElementAtIndex(0);
            el.FindPropertyRelative("cardCount").intValue = CelticCount;
            el.FindPropertyRelative("pose").objectReferenceValue = pose;
            el.FindPropertyRelative("fov").floatValue = CelticCameraFov;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRoom(ReadingRoomController room, SpreadCatalog catalog)
        {
            var so = new SerializedObject(room);
            so.FindProperty("spreadCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Celtic spread button (duplicate of the three-card button) ----------

        private static void EnsureCelticButton(ReadingRoomController room)
        {
            var canvas = GameObject.Find("ReadingRoomCanvas");
            var three = canvas != null ? canvas.transform.Find("ThreeCardButton") : null;
            if (three == null)
            {
                Debug.LogWarning("Phase 63: ThreeCardButton not found; Celtic button skipped.");
                return;
            }

            var existing = canvas.transform.Find("CelticCrossButton");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var copy = Object.Instantiate(three.gameObject, three.parent);
            copy.name = "CelticCrossButton";
            var label = copy.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = "凯尔特十字";
            }

            // Re-lay the bottom action tray (y = -232) so all five fit without the
            // guarded row (one/three/draw/reveal) overlapping.
            SetPos(canvas.transform.Find("OneCardButton"), new Vector2(-400f, -232f), new Vector2(150f, 42f));
            SetPos(three, new Vector2(-225f, -232f), new Vector2(150f, 42f));
            SetPos(copy.transform, new Vector2(-30f, -232f), new Vector2(190f, 42f));
            SetPos(canvas.transform.Find("DrawButton"), new Vector2(180f, -232f), new Vector2(180f, 46f));
            SetPos(canvas.transform.Find("RevealResultButton"), new Vector2(385f, -232f), new Vector2(180f, 46f));

            var so = new SerializedObject(room);
            so.FindProperty("celticCrossButton").objectReferenceValue = copy.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPos(Transform t, Vector2 pos, Vector2 size)
        {
            if (t == null)
            {
                return;
            }

            var rt = t as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
