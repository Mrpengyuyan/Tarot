using System;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 71 review shots, written to $PHASE71_CAPTURE_DIR (nothing goes into the repo):
    /// a dealt card on the one-card slot seen from the one-card pose - plain, waiting to be
    /// flipped, and hovered - and the menu with its censer simulated a few seconds in, twice.
    /// </summary>
    public static class Phase71CaptureBuilder
    {
        private const int W = 2560, H = 1440;

        public static void Run()
        {
            var dir = Environment.GetEnvironmentVariable("PHASE71_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Set PHASE71_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(dir);
            CaptureCard(dir);
            CaptureMenu(dir);
            Debug.Log($"Phase 71 capture -> {dir}");
        }

        private static void CaptureCard(string dir)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            var layout = UnityEngine.Object.FindFirstObjectByType<SpreadLayoutController>();
            var slot = layout.GetSlots(1)[0];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Phase71AnimationFixBootstrapper.CardPrefabPath);
            var card = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<CardView>();
            card.transform.SetPositionAndRotation(slot.position, slot.rotation);
            card.SetFaceUp(false);   // dealt cards lie face down

            var body = FindDeep(card.transform, "Phase15_CardBody").GetComponent<Renderer>().bounds;
            var halo = FindDeep(card.transform, "Highlight");
            var cloth = GameObject.Find("MP_TableCloth");
            Debug.Log($"Phase 71 capture heights: slot {slot.position.y:0.0000} cardBodyMin {body.min.y:0.0000} " +
                      $"halo {halo.position.y:0.0000} clothTop {(cloth != null ? cloth.GetComponent<Renderer>().bounds.max.y : float.NaN):0.0000}");

            var choreography = UnityEngine.Object.FindFirstObjectByType<CameraChoreographyController>();
            var so = new SerializedObject(choreography);
            var pose = (Transform)so.FindProperty("oneCardPose").objectReferenceValue;
            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            camera.fieldOfView = so.FindProperty("oneCardFov").floatValue;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            card.SetHighlighted(false);
            Render(camera, Path.Combine(dir, "Card_plain.png"));
            card.SetHighlighted(true);
            Render(camera, Path.Combine(dir, "Card_waiting.png"));
            card.SetHovered(true);
            Render(camera, Path.Combine(dir, "Card_hovered.png"));
        }

        private static void CaptureMenu(string dir)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var censer = GameObject.Find("MP_MenuStage").transform.Find("MP_Censer");
            var systems = censer.GetComponentsInChildren<ParticleSystem>(true);
            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            foreach (var (seconds, file) in new[] { (6f, "Menu_censer_a.png"), (7.3f, "Menu_censer_b.png") })
            {
                foreach (var ps in systems)
                {
                    ps.Simulate(seconds, false, true, true);
                }

                Render(camera, Path.Combine(dir, file));
            }
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

        private static void Render(Camera camera, string path)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            camera.aspect = (float)W / H;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            Canvas.ForceUpdateCanvases();
            CaptureRig.RenderConverged(camera);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
