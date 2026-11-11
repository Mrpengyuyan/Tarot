using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualTableRenderingTests
    {
        private ApiClient previous;
        private RenderTexture target;
        private Camera cameraView;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += KeepOffline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            cameraView = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene == SceneManager.GetActiveScene() && c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth).First();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= KeepOffline;
            if (cameraView != null) cameraView.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("TableRenderingCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator StableStagesShareTheOriginalTableShotWithoutRemovingTheDeckApproach()
        {
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            yield return new WaitForSeconds(1f);
            AssertTableShot(choreography);
            foreach (var aspect in new[] { 16f / 9f, 4f / 3f })
            {
                cameraView.aspect = aspect;
                foreach (var count in new[] { 1, 3, 10 })
                {
                    choreography.FocusDeck();
                    yield return new WaitForSeconds(0.9f);
                    Assert.That(Vector3.Distance(cameraView.transform.position, new Vector3(0f, 2.85f, -4.15f)),
                        Is.GreaterThan(0.5f), "The camera must still travel toward the actual deck.");
                    choreography.FocusDraw(count);
                    yield return new WaitForSeconds(0.9f);
                    AssertTableShot(choreography);
                    choreography.FocusSpread(count);
                    yield return new WaitForSeconds(0.9f);
                    AssertTableShot(choreography);
                    foreach (var name in new[] { "MP_RoomCandle_L", "MP_RoomCandle_R", "MP_RoomCandle_BackL", "MP_RoomCandle_BackR" })
                    {
                        var point = cameraView.WorldToViewportPoint(GameObject.Find(name).transform.position);
                        // The approved 4:3 frame is a center-crop of the 16:9 tableau.
                        var x = (point.x - 0.5f) * aspect / (16f / 9f) + 0.5f;
                        Assert.That(x, Is.InRange(0.02f, 0.98f), name + " no longer frames the table");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator ReapplyingTableFramingDoesNotCaptureTheDeckShotOrLeaveAnOrphanPose()
        {
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            Assert.That(choreography.TryGetDrawPose(3, out var table, out var fov), Is.True);
            var position = table.position;
            choreography.FocusDeck();
            yield return new WaitForSeconds(1f);
            choreography.ApplyRitualTableFraming();
            Assert.That(choreography.TryGetSpreadPose(10, out var same, out var sameFov), Is.True);
            Assert.That(same, Is.SameAs(table));
            Assert.That(same.position, Is.EqualTo(position));
            Assert.That(sameFov, Is.EqualTo(fov));
            var authored = (Transform)typeof(CameraChoreographyController)
                .GetField("defaultPose", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(choreography);
            Assert.That(authored.position, Is.EqualTo(new Vector3(0f, 2.45f, -3.55f)), "The authored legacy pose remains unchanged.");
            Object.Destroy(choreography);
            yield return null;
            Assert.That(table == null, Is.True, "Removing the adapter must release its generated pose.");
        }

        [UnityTest]
        public IEnumerator CaptureRestoresAnAlreadyBoundCallerTarget()
        {
            var canvas = GameObject.Find("ReadingRoomCanvas").GetComponent<Canvas>();
            var previousTarget = RenderTexture.active;
            var callerTarget = new RenderTexture(16, 16, 0);
            try
            {
                using (var capture = new RitualFrameCapture(cameraView, canvas, 128, 72))
                {
                    yield return null;
                    RenderTexture.active = callerTarget;
                    var pixels = capture.Render();
                    Object.Destroy(pixels);
                    Assert.That(RenderTexture.active, Is.SameAs(callerTarget), "Capture must restore the target bound before rendering or blitting.");
                }
                Assert.That(RenderTexture.active, Is.SameAs(callerTarget));
            }
            finally
            {
                RenderTexture.active = previousTarget;
                callerTarget.Release();
                Object.Destroy(callerTarget);
            }
        }

        [UnityTest]
        public IEnumerator CapturedOverlayKeepsItsInkOutsideTheWorldColorGrade()
        {
#if UNITY_EDITOR
            var canvas = GameObject.Find("ReadingRoomCanvas").GetComponent<Canvas>();
            var node = new GameObject("OverlayInkProbe", typeof(RectTransform), typeof(RawImage));
            node.transform.SetParent(canvas.transform, false);
            var image = node.GetComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MidnightParlor/Sprites/TarotCardBack.png");
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled = false;
            foreach (var flicker in Object.FindObjectsByType<CandleFlickerController>(FindObjectsSortMode.None)) flicker.enabled = false;
            target = new RenderTexture(128, 128, 24);
            cameraView.targetTexture = target;
            cameraView.aspect = 1f;
            canvas.enabled = false;
            RenderWorld();
            var world = Read(target);
            canvas.enabled = true;
            var type = typeof(RitualUiCaptureTests).Assembly.GetType("TarotUnity.Tests.PlayMode.RitualFrameCapture");
            Assert.That(type, Is.Not.Null, "Capture overlay UI after, not inside, the world's cinematic grade.");
            using (var capture = (IDisposable)Activator.CreateInstance(type, cameraView, canvas, 128, 128))
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var pixels = (Texture2D)type.GetMethod("Render").Invoke(capture, null);
                var sourceTarget = new RenderTexture(128, 128, 0);
                var reference = ReadSource(image.texture, sourceTarget);
                try
                {
                    var ink = pixels.GetPixel(16, 83);
                    var expected = reference.GetPixel(32, 83);
                    Assert.That(ink.r, Is.EqualTo(expected.r).Within(0.015f));
                    Assert.That(ink.g, Is.EqualTo(expected.g).Within(0.015f));
                    Assert.That(ink.b, Is.EqualTo(expected.b).Within(0.015f));
                    var backdrop = pixels.GetPixel(96, 64);
                    var originalBackdrop = world.GetPixel(96, 64);
                    Assert.That(backdrop.r, Is.EqualTo(originalBackdrop.r).Within(0.015f));
                    Assert.That(backdrop.g, Is.EqualTo(originalBackdrop.g).Within(0.015f));
                    Assert.That(backdrop.b, Is.EqualTo(originalBackdrop.b).Within(0.015f));
                    var translucent = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    translucent.SetPixel(0, 0, new Color(0.2f, 0.4f, 0.6f, 0.5f));
                    translucent.Apply();
                    image.texture = translucent;
                    var blended = (Texture2D)type.GetMethod("Render").Invoke(capture, null);
                    try
                    {
                        var actualBlend = blended.GetPixel(16, 83);
                        var expectedBlend = Color.Lerp(world.GetPixel(16, 83).linear,
                            new Color(0.2f, 0.4f, 0.6f).linear, 128f / 255f).gamma;
                        Assert.That(actualBlend.r, Is.EqualTo(expectedBlend.r).Within(0.015f));
                        Assert.That(actualBlend.g, Is.EqualTo(expectedBlend.g).Within(0.015f));
                        Assert.That(actualBlend.b, Is.EqualTo(expectedBlend.b).Within(0.015f));
                    }
                    finally { Object.Destroy(blended); Object.Destroy(translucent); }
                }
                finally
                {
                    Object.Destroy(pixels);
                    Object.Destroy(reference);
                    Object.Destroy(world);
                    sourceTarget.Release();
                    Object.Destroy(sourceTarget);
                }
            }
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(node.layer, Is.EqualTo(0));
#else
            Assert.Ignore("Requires the reference texture in the editor.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator ReadablePaperDoesNotReplaceTheWorldCinematicGrade()
        {
            var volume = Object.FindFirstObjectByType<Volume>();
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
            Assert.That(profile.TryGet<Tonemapping>(out var tone), Is.True);
            Assert.That(tone.mode.value, Is.EqualTo(TonemappingMode.ACES), "Keep the approved amber table and candle shadows.");
            Assert.That(profile.TryGet<ColorAdjustments>(out var color), Is.True);
            Assert.That(color.postExposure.value, Is.EqualTo(0.45f).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DiscardedCardsDoNotStayInTheRoomLayerRegistry()
        {
#if UNITY_EDITOR
            var rendering = GameObject.Find("ReadingRoomCanvas").GetComponent<RitualPaperRendering>();
            var registry = (System.Collections.IDictionary)typeof(RitualPaperRendering)
                .GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rendering);
            for (var i = 0; i < 3; i++)
            {
                var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab"));
                var card = root.GetComponent<CardView>();
                card.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]);
                RitualPaperSurface.Attach(card);
                Assert.That(registry.Count, Is.EqualTo(2));
                Object.Destroy(root);
                yield return null;
                yield return null;
                Assert.That(registry.Count, Is.Zero, "Repeated abandoned fans must not retain destroyed face/back objects.");
            }
#else
            Assert.Ignore("Requires the editor fixture prefab.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator PaperPassReusesItsCameraAndRestoresOnlyItsOwnLayersAndMask()
        {
#if UNITY_EDITOR
            var host = GameObject.Find("ReadingRoomCanvas");
            var rendering = host.GetComponent<RitualPaperRendering>();
            var volume = Object.FindFirstObjectByType<Volume>();
            var shared = volume.sharedProfile;
            var stack = cameraView.GetUniversalAdditionalCameraData().cameraStack;
            Assert.That(stack.Count(c => c != null && c.name == "RitualPaperCamera"), Is.EqualTo(1));
            Assert.That(RitualPaperRendering.Attach(host, cameraView), Is.SameAs(rendering));
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab"));
            var card = root.GetComponent<CardView>();
            card.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]);
            var faceLayer = card.FaceArtwork.gameObject.layer;
            var surface = RitualPaperSurface.Attach(card);
            var paperLayer = LayerMask.NameToLayer("RitualPaper");
            Assert.That(paperLayer, Is.GreaterThanOrEqualTo(8));
            Assert.That(surface.Back.gameObject.layer, Is.EqualTo(paperLayer));
            Assert.That(card.FaceArtwork.gameObject.layer, Is.EqualTo(paperLayer));
            Assert.That(root.layer, Is.Zero, "The real pointer collider stays on its original physics layer.");
            rendering.enabled = false;
            yield return null;
            Assert.That(stack.Any(c => c != null && c.name == "RitualPaperCamera"), Is.False);
            Assert.That(surface.Back.gameObject.layer, Is.Zero);
            Assert.That(card.FaceArtwork.gameObject.layer, Is.EqualTo(faceLayer));
            Assert.That(volume.HasInstantiatedProfile(), Is.False);
            rendering.enabled = true;
            yield return null;
            Assert.That(stack.Count(c => c != null && c.name == "RitualPaperCamera"), Is.EqualTo(1));
            Assert.That(surface.Back.gameObject.layer, Is.EqualTo(paperLayer));
            Object.Destroy(surface);
            yield return null;
            Assert.That(card.FaceArtwork.gameObject.layer, Is.EqualTo(faceLayer));
            cameraView.cullingMask ^= 1 << 27;
            var unrelatedBit = cameraView.cullingMask & (1 << 27);
            Object.Destroy(rendering);
            yield return null;
            Assert.That(cameraView.cullingMask & (1 << 27), Is.EqualTo(unrelatedBit));
            Assert.That(cameraView.cullingMask & (1 << paperLayer), Is.Not.Zero);
            Assert.That(stack.Any(c => c != null && c.name == "RitualPaperCamera"), Is.False);
            Assert.That(volume.sharedProfile, Is.SameAs(shared));
            Assert.That(volume.HasInstantiatedProfile(), Is.False);
#else
            Assert.Ignore("Requires the editor fixture prefab.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator PrintedBackKeepsTheReferencePurpleAndGoldThroughTheActualRoomRenderer()
        {
#if UNITY_EDITOR
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            cameraView.transform.SetPositionAndRotation(new Vector3(1000f, 1000f, 1000f), Quaternion.identity);
            cameraView.orthographic = true;
            cameraView.orthographicSize = 0.6f;
            cameraView.aspect = 0.64f;
            target = new RenderTexture(640, 1000, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cameraView.targetTexture = target;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab"));
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
            root.transform.SetPositionAndRotation(cameraView.transform.position + Vector3.forward * 2f, Quaternion.Euler(-90f, 0f, 0f));
            root.transform.localScale = Vector3.one;
            var card = root.GetComponent<CardView>();
            card.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]);
            var surface = RitualPaperSurface.Attach(card);
            yield return null;
            RenderWorld();
            var rendered = Read(target);
            var sourceTarget = new RenderTexture(640, 1000, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var source = surface.Back.sprite.texture;
            var reference = ReadSource(source, sourceTarget);
            var directory = Environment.GetEnvironmentVariable("TAROT_RENDER_REPORT_DIR");
            if (!string.IsNullOrEmpty(directory)) File.WriteAllBytes(Path.Combine(directory, "paper-probe.png"), rendered.EncodeToPNG());
            try
            {
                foreach (var uv in new[] { new Vector2(0.25f, 0.65f), new Vector2(0.47f, 0.5f) })
                {
                    var point = cameraView.WorldToScreenPoint(root.transform.position
                        + cameraView.transform.right * ((uv.x - 0.5f) * RitualPaperSurface.PaperWidth)
                        + cameraView.transform.up * (uv.y - 0.5f));
                    var actual = rendered.GetPixel(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
                    var expected = reference.GetPixel((int)(uv.x * reference.width), (int)(uv.y * reference.height));
                    Debug.Log($"Printed ink UV{uv}: reference{expected}, actual{actual}");
                    Assert.That(Mathf.Abs(actual.r - expected.r), Is.LessThan(0.045f), "Printed red channel was regraded.");
                    Assert.That(Mathf.Abs(actual.g - expected.g), Is.LessThan(0.045f), "Printed green channel was regraded.");
                    Assert.That(Mathf.Abs(actual.b - expected.b), Is.LessThan(0.045f), "Purple paper must not be crushed to black.");
                }
            }
            finally
            {
                Object.Destroy(rendered);
                Object.Destroy(reference);
                sourceTarget.Release();
                Object.Destroy(sourceTarget);
            }
#else
            Assert.Ignore("Requires the editor fixture prefab, not a production asset lookup.");
            yield break;
#endif
        }

        private void AssertTableShot(CameraChoreographyController choreography)
        {
            Assert.That(choreography.CurrentBaseFov, Is.EqualTo(40f).Within(0.01f));
            Assert.That(Vector3.Distance(cameraView.transform.position, new Vector3(0f, 2.85f, -4.15f)),
                Is.LessThan(0.04f), "The candles must not change size or leave the frame between stable stages.");
            Assert.That(Quaternion.Angle(cameraView.transform.rotation, Quaternion.Euler(32.4f, 0f, 0f)), Is.LessThan(0.7f));
        }

        private static Texture2D Read(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            return image;
        }

        private void RenderWorld()
        {
            var previousTarget = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                for (var i = 0; i < 4; i++) RenderPipeline.SubmitRenderRequest(cameraView, request);
            }
            finally { RenderTexture.active = previousTarget; }
        }

        private static Texture2D ReadSource(Texture source, RenderTexture target)
        {
            var previousTarget = RenderTexture.active;
            try { Graphics.Blit(source, target); return Read(target); }
            finally { RenderTexture.active = previousTarget; }
        }

        private static void KeepOffline(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ReadingRoom") return;
            typeof(ReadingRoomController).GetField("backendMode", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(), BackendIntegrationMode.LocalSimulation);
        }
    }
}
