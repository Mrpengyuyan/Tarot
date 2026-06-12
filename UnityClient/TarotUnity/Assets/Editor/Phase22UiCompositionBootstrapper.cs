using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 22 re-composes the ReadingRoom UI for the Phase 21 seated camera:
    /// the card stage stays clear, every clickable control moves into a bottom
    /// action tray (Hearthstone rule: information far, actions near the player),
    /// and flat-era fake-table overlays are deactivated so the real 3D table
    /// shows through. Also nudges the deck stack into the default framing.
    /// </summary>
    public static class Phase22UiCompositionBootstrapper
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";

        // Canvas reference resolution is 1280x720, center-anchored coordinates.
        // The projected card band spans roughly y = +86 down to y = -108, so the
        // action tray starts at -130 and the top info zone stays above +100.
        private static readonly (string Name, Vector2 Position)[] UiMoves =
        {
            ("QuestionInput", new Vector2(0f, -164f)),
            ("Phase8_QuestionPanelFrame", new Vector2(0f, -164f)),
            ("Phase11_QuestionGlow", new Vector2(0f, -164f)),
            ("OneCardButton", new Vector2(-330f, -232f)),
            ("ThreeCardButton", new Vector2(-126f, -232f)),
            ("DrawButton", new Vector2(126f, -232f)),
            ("RevealResultButton", new Vector2(390f, -232f)),
            ("Phase8_SpreadChoiceFrame", new Vector2(-228f, -232f)),
            ("Phase12_RevealInstruction", new Vector2(0f, 118f)),
            ("FlowStatusText", new Vector2(0f, -290f)),
            ("Phase10_ReleaseStatusText", new Vector2(0f, -332f)),
        };

        // Flat-era translucent overlays that used to fake a table on the old
        // top-down camera. The real 3D table and the Phase 20 post-processing
        // vignette replace them; they are deactivated, not deleted.
        private static readonly string[] OverlaysToDeactivate =
        {
            "Phase11_TableFocusFrame",
            "Phase8_CardSlotsGlow",
            "Phase12_CardFocusVignette",
        };

        private static readonly Vector2 ActionDockPosition = new(0f, -204f);
        private static readonly Vector2 ActionDockSize = new(1120f, 148f);

        // Deck moves from x=-2.9 (clipped by the seated framing) to -2.35 so the
        // player always sees their deck. DeckPose pitch/yaw re-aimed to match.
        private static readonly Vector3 DeckStackPosition = new(-2.35f, 0.12f, 0.1f);
        private static readonly Vector3 DeckPoseEuler = new(37.9f, -9.9f, 0f);

        [MenuItem("Tools/Tarot Unity/Run Phase 22 UI Composition Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Exited Play Mode. Run Phase 22 UI Composition Bootstrap again after Unity returns to Edit Mode.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Phase 22 bootstrap cancelled because current scene changes were not saved.");
                return;
            }

            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("Phase 22 bootstrap skipped because ReadingRoomCanvas is missing.");
                return;
            }

            foreach (var move in UiMoves)
            {
                var rect = canvas.transform.Find(move.Name)?.GetComponent<RectTransform>();
                if (rect == null)
                {
                    Debug.LogWarning($"Phase 22 bootstrap missing UI element {move.Name}.");
                    continue;
                }

                rect.anchoredPosition = move.Position;
                EditorUtility.SetDirty(rect);
            }

            var dock = canvas.transform.Find("Phase11_ActionDock")?.GetComponent<RectTransform>();
            if (dock != null)
            {
                dock.anchoredPosition = ActionDockPosition;
                dock.sizeDelta = ActionDockSize;
                EditorUtility.SetDirty(dock);
            }

            foreach (var overlayName in OverlaysToDeactivate)
            {
                var overlay = canvas.transform.Find(overlayName);
                if (overlay != null && overlay.gameObject.activeSelf)
                {
                    overlay.gameObject.SetActive(false);
                    EditorUtility.SetDirty(overlay.gameObject);
                }
            }

            var deck = GameObject.Find("DeckStack");
            if (deck != null)
            {
                deck.transform.position = DeckStackPosition;
                EditorUtility.SetDirty(deck.transform);
            }

            var choreographyRoot = GameObject.Find("ReadingRoomCameraChoreography");
            var deckPose = choreographyRoot != null ? choreographyRoot.transform.Find("DeckPose") : null;
            if (deckPose != null)
            {
                deckPose.localEulerAngles = DeckPoseEuler;
                EditorUtility.SetDirty(deckPose);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ReadingRoomScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 22 UI composition bootstrap complete.");
        }
    }
}
