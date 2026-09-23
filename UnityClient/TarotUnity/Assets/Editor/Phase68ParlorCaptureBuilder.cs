using System;
using System.Collections.Generic;
using System.IO;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 68 review shots of the reading room from the poses the player actually sits
    /// at. Earlier reading-room captures rendered the Main Camera where the scene saves it
    /// (0, 2.85, -4.15, FOV 40), but ReadingRoomController.Start calls PlayOpening, which
    /// moves the camera to CameraChoreographyController.defaultPose within 0.75 s - so those
    /// shots showed a framing the player only sees for the first moment.
    /// This renders every pose the controller can move to (default, deck, one card, three
    /// cards, result, and each registered spread pose) with its own field of view, plus the
    /// saved scene camera for comparison, and the default and three-card poses without UI.
    /// Each pose shows the sockets of the spread it is seen with in play (SpreadSocketVisibility).
    /// READ-ONLY: the scene is not saved. Set PHASE68_CAPTURE_DIR to render elsewhere.
    /// </summary>
    public static class Phase68ParlorCaptureBuilder
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string DefaultOutFolder = "Docs/VisualReview/Phase68";
        private const int W = 2560, H = 1440;

        private static string outFolder = DefaultOutFolder;

        [MenuItem("Tools/Tarot Unity/Run Phase 68 Parlor Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            var overrideDir = Environment.GetEnvironmentVariable("PHASE68_CAPTURE_DIR");
            outFolder = string.IsNullOrEmpty(overrideDir) ? DefaultOutFolder : overrideDir;
            Directory.CreateDirectory(outFolder);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("No camera in the reading room.");
            }

            var choreography = UnityEngine.Object.FindFirstObjectByType<CameraChoreographyController>();
            if (choreography == null)
            {
                throw new InvalidOperationException("No CameraChoreographyController in the reading room.");
            }

            var savedPosition = camera.transform.position;
            var savedRotation = camera.transform.rotation;
            var savedFov = camera.fieldOfView;

            try
            {
                var sockets = UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>();
                sockets?.Apply(1);
                RenderToFile(camera, "ReadingRoom_scenecamera.png", true);

                foreach (var (label, pose, fov) in ReadPoses(choreography))
                {
                    camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                    camera.fieldOfView = fov;
                    sockets?.Apply(CardCountFor(label));
                    RenderToFile(camera, $"ReadingRoom_{label}.png", true);
                    Debug.Log($"Phase 68 capture {label}: position {pose.position} pitch {pose.eulerAngles.x:0.0} fov {fov:0.#}");
                    if (label == "default" || label == "threeCard")
                    {
                        RenderToFile(camera, $"ReadingRoom_{label}_noui.png", false);
                    }
                }
            }
            finally
            {
                camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
                camera.fieldOfView = savedFov;
                UnityEngine.Object.FindFirstObjectByType<SpreadSocketVisibility>()?.Apply(1);
            }

            Debug.Log($"Phase 68 parlor capture complete -> {outFolder}");
        }

        // The spread each pose is seen with in play: the room opens on one card, the three-card
        // and result poses follow a three-card draw, and a spread pose shows its own sockets.
        private static int CardCountFor(string label)
        {
            if (label.StartsWith("spread", StringComparison.Ordinal)
                && int.TryParse(label.Substring("spread".Length), out var count))
            {
                return count;
            }

            return label == "threeCard" || label == "result" ? 3 : 1;
        }

        private static List<(string Label, Transform Pose, float Fov)> ReadPoses(CameraChoreographyController choreography)
        {
            var so = new SerializedObject(choreography);
            var poses = new List<(string, Transform, float)>();
            foreach (var (label, poseField, fovField) in new[]
                     {
                         ("default", "defaultPose", "defaultFov"),
                         ("deck", "deckPose", "deckFov"),
                         ("oneCard", "oneCardPose", "oneCardFov"),
                         ("threeCard", "threeCardPose", "threeCardFov"),
                         ("result", "resultPose", "resultFov"),
                     })
            {
                var pose = so.FindProperty(poseField)?.objectReferenceValue as Transform;
                if (pose != null)
                {
                    poses.Add((label, pose, so.FindProperty(fovField).floatValue));
                }
            }

            var spreadPoses = so.FindProperty("spreadPoses");
            for (var i = 0; spreadPoses != null && i < spreadPoses.arraySize; i++)
            {
                var entry = spreadPoses.GetArrayElementAtIndex(i);
                var pose = entry.FindPropertyRelative("pose").objectReferenceValue as Transform;
                if (pose != null)
                {
                    var count = entry.FindPropertyRelative("cardCount").intValue;
                    poses.Add(($"spread{count}", pose, entry.FindPropertyRelative("fov").floatValue));
                }
            }

            return poses;
        }

        private static void RenderToFile(Camera camera, string file, bool withUi)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var states = PrepareCanvases(camera, withUi);

            try
            {
                camera.aspect = (float)W / H;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, file), tex.EncodeToPNG());
            }
            finally
            {
                RestoreCanvases(states);
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        private static (Canvas c, RenderMode m, Camera cam, float d, bool p, bool e)[] PrepareCanvases(Camera camera, bool withUi)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new (Canvas, RenderMode, Camera, float, bool, bool)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                states[i] = (c, c.renderMode, c.worldCamera, c.planeDistance, c.pixelPerfect, c.enabled);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
                c.enabled = withUi && c.enabled;
            }

            return states;
        }

        private static void RestoreCanvases((Canvas c, RenderMode m, Camera cam, float d, bool p, bool e)[] states)
        {
            foreach (var s in states)
            {
                if (s.c != null)
                {
                    s.c.renderMode = s.m;
                    s.c.worldCamera = s.cam;
                    s.c.planeDistance = s.d;
                    s.c.pixelPerfect = s.p;
                    s.c.enabled = s.e;
                }
            }
        }
    }
}
