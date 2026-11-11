using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualDeckPresentationTests
    {
        private ApiClient previous;
        private Camera camera;
        private DeckShuffleChoreographer deck;
        private Component presentation;
        private RenderTexture target;
        private Transform canvas;
        private readonly List<Texture2D> pixels = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return new WaitForSeconds(1f);
            canvas = GameObject.Find("ReadingRoomCanvas").transform;
            camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth).First();
            deck = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Offline;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            foreach (var value in pixels) Object.Destroy(value);
            pixels.Clear();
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("DeckParityCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator TheSameDeckOwnsTwelveNativePaperLayersWithoutAddingDrawsOrAuthoredChildren()
        {
            RequirePresentation();
            Assert.That(deck.transform.childCount, Is.EqualTo(9));
            Assert.That(Layers().Count, Is.EqualTo(12));
            Assert.That(Object.FindObjectsByType<CardView>(FindObjectsSortMode.None), Is.Empty,
                "Decorative deck paper must not create or replace real draw cards.");
            foreach (var renderer in Layers())
            {
                Assert.That(renderer.transform.IsChildOf(deck.transform), Is.True);
                Assert.That(renderer.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("RitualPaper")));
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Tarot/RitualDeckInk"));
            }
            var legacy = deck.GetComponentsInChildren<Renderer>(true).Except(Layers());
            Assert.That(legacy.Any(r => r.enabled), Is.False, "No second lit or stationary slab may remain.");
            Assert.That(deck.PlannedSeconds, Is.EqualTo(5.8f).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualParkedPaperMatchesTheMeasuredShapeThroughResizeAndDensity()
        {
            RequirePresentation();
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1024,768), new Vector2Int(2560,1440), new Vector2Int(2048,1536), new Vector2Int(1280,720) })
            {
                yield return Resize(size);
                var expected = RitualDeckLayout.Parked(size.x, size.y, false);
                var block = new MaterialPropertyBlock();
                for (var i = 0; i < Layers().Count; i++)
                {
                    var renderer = Layers()[i];
                    var vertices = renderer.GetComponent<MeshFilter>().sharedMesh.vertices;
                    Assert.That(vertices.Length, Is.EqualTo(4));
                    for (var j = 0; j < 4; j++)
                    {
                        var p = camera.WorldToScreenPoint(renderer.transform.TransformPoint(vertices[j]));
                        var projected = new Vector2(p.x, size.y - p.y);
                        var reference = RitualDeckLayout.ProjectCorner(expected, RitualDeckLayout.ShuffleLayer(i, 0f, size.x, size.y), j);
                        Assert.That(Vector2.Distance(projected, reference), Is.LessThan(0.35f));
                    }
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.GetFloat("_Opacity"), Is.EqualTo(0.48f).Within(0.001f));
                }
            }
        }

        [UnityTest]
        public IEnumerator StackTransparencyIsAppliedOnceRatherThanTwelveTimes()
        {
            RequirePresentation();
            yield return Resize(new Vector2Int(1280, 720));
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled = false;
            foreach (var light in Object.FindObjectsByType<CandleFlickerController>(FindObjectsSortMode.None)) light.enabled = false;
            foreach (var value in Layers()) value.enabled = false;
            var backdrop = RenderWorld();
            foreach (var value in Layers()) value.enabled = true;
            var result = RenderWorld();
            var top = Layers()[11];
            var v = top.GetComponent<MeshFilter>().sharedMesh.vertices;
            const float u = 0.25f, y = 0.65f;
            var point = camera.WorldToScreenPoint(top.transform.TransformPoint(Vector3.Lerp(Vector3.Lerp(v[0], v[1], u), Vector3.Lerp(v[3], v[2], u), 1f-y)));
            var sourceTarget = new RenderTexture(512, 864, 0);
            var old = RenderTexture.active;
            Texture2D source;
            try { Graphics.Blit(top.sharedMaterial.mainTexture, sourceTarget); source = Read(sourceTarget); }
            finally { RenderTexture.active = old; sourceTarget.Release(); Object.Destroy(sourceTarget); }
            var pose = RitualDeckLayout.Parked(1280, 720, false);
            var texturePoint = new Vector2((u - pose.Density / pose.Size.x) / (1f - 2f * pose.Density / pose.Size.x),
                (y - pose.Density / pose.Size.y) / (1f - 2f * pose.Density / pose.Size.y));
            var paper = source.GetPixelBilinear(texturePoint.x, texturePoint.y);
            var before = backdrop.GetPixel((int)point.x, (int)point.y);
            var after = result.GetPixel((int)point.x, (int)point.y);
            var expected = Color.Lerp(before.linear, paper.linear, 0.48f).gamma;
            Assert.That(after.r, Is.EqualTo(expected.r).Within(0.025f));
            Assert.That(after.g, Is.EqualTo(expected.g).Within(0.025f));
            Assert.That(after.b, Is.EqualTo(expected.b).Within(0.025f));
        }

        [UnityTest]
        public IEnumerator FullPickupShuffleAndFanUseOneTimelineAndTheSameSeventyEightCards()
        {
            RequirePresentation();
            canvas.Find("ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "What should I focus on?";
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var original = deck.transform;
            var began = Time.time;
            var deadline = Time.realtimeSinceStartup + 30f;
            var startedMovingCamera = false;
            var cameraStart = camera.transform.position;
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            while (flow.State == ReadingFlowState.Shuffling)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                startedMovingCamera |= Vector3.Distance(cameraStart, camera.transform.position) > 0.3f;
                Assert.That(deck.transform, Is.SameAs(original));
                yield return null;
            }
            Assert.That(Time.time - began, Is.InRange(6.6f, 7.2f));
            Assert.That(startedMovingCamera, Is.True, "Keep the real camera journey toward the physical deck.");
            while (!fan.AcceptingPicks) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            Assert.That(fan.FanCards.Count, Is.EqualTo(78));
            Assert.That(fan.DeckShowing, Is.Zero);
            Assert.That(Layers().Any(r => r.enabled), Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingAndRebuildingTheDeckDuringSelectionKeepsTheDeckHidden()
        {
            RequirePresentation();
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "A lifecycle test";
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var until = Time.realtimeSinceStartup + 30f;
            while (!fan.AcceptingPicks) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(until)); yield return null; }
            var roots = fan.FanCards.ToArray();
            for (var i = 0; i < 2; i++)
            {
                ((Behaviour)presentation).enabled = false;
                yield return null;
                Assert.That(deck.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy), Is.False);
                Assert.That(fan.DeckShowing, Is.Zero);
                ((Behaviour)presentation).enabled = true;
                yield return null;
                Assert.That(Layers().Count, Is.EqualTo(12));
                Assert.That(Layers().Any(r => r.enabled), Is.False);
                Assert.That(fan.FanCards, Is.EqualTo(roots));
            }
        }

        [UnityTest]
        public IEnumerator ClearingTheAnchorRestoresTheOriginalPoseAndRebuildsOnlyOneParkedDeck()
        {
            RequirePresentation();
            var native = (RitualDeckPresentation)presentation;
            var anchor = deck.GetComponent<RitualDeckAnchor>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var position = (Vector3)native.GetType().GetField("authoredPosition", flags).GetValue(native);
            var rotation = (Quaternion)native.GetType().GetField("authoredRotation", flags).GetValue(native);
            var scale = (Vector3)native.GetType().GetField("authoredScale", flags).GetValue(native);
            var oldMeshes = Layers().Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray();
            var oldMaterial = Layers()[0].sharedMaterial;
            anchor.enabled = false;
            Assert.That(Vector3.Distance(deck.transform.localPosition, position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(deck.transform.localRotation, rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(deck.transform.localScale, scale), Is.LessThan(0.0001f));
            yield return null;
            Assert.That(oldMeshes.All(m => m == null), Is.True);
            Assert.That(oldMaterial == null, Is.True);
            anchor.enabled = true;
            yield return null;
            Assert.That(native.IsReady, Is.True);
            Assert.That(Layers().Count, Is.EqualTo(12));
            Assert.That(deck.GetComponentsInChildren<Transform>(true).Count(t => t.name == "RitualDeckPaper"), Is.EqualTo(1));
            Assert.That(native.CurrentPose.Opacity, Is.EqualTo(0.48f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ACancelledShuffleCannotOverwriteARebuiltPresentationOrItsNextRun()
        {
            RequirePresentation();
            var native = (RitualDeckPresentation)presentation;
            var first = native.Shuffle();
            var second = (IEnumerator)null;
            try
            {
                Assert.That(first.MoveNext(), Is.True);
                native.enabled = false;
                native.enabled = true;
                Assert.That(native.CurrentPose.Opacity, Is.EqualTo(0.48f).Within(0.001f), "Rebuild starts parked, not mid-shuffle.");
                second = native.Shuffle();
                Assert.That(second.MoveNext(), Is.True);
                Assert.That(first.MoveNext(), Is.False);
                Assert.That(native.IsShuffling, Is.True, "The old routine must not clear the new run's flag.");
            }
            finally
            {
                (first as IDisposable)?.Dispose();
                (second as IDisposable)?.Dispose();
            }
            Assert.That(native.IsShuffling, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RebuildingTheRoomPaperPassRetainsExactlyTwelveDeckRegistrations()
        {
            RequirePresentation();
            var paper = canvas.GetComponent<RitualPaperRendering>();
            var registry = (System.Collections.IDictionary)typeof(RitualPaperRendering)
                .GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(paper);
            Assert.That(registry.Count, Is.EqualTo(12));
            for (var i = 0; i < 2; i++)
            {
                paper.enabled = false;
                Assert.That(registry.Count, Is.Zero);
                paper.enabled = true;
                Assert.That(registry.Count, Is.EqualTo(12));
                Assert.That(Layers().All(r => r.gameObject.layer == LayerMask.NameToLayer("RitualPaper")), Is.True);
                yield return null;
            }
            ((Behaviour)presentation).enabled = false;
            Assert.That(registry.Count, Is.Zero, "Removing the deck must unregister every generated plane.");
        }

        [UnityTest]
        public IEnumerator TheCameraReallyApproachesTheRaisedDeckInsteadOfCarryingItAlong()
        {
            RequirePresentation();
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "A camera approach test";
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var native = (RitualDeckPresentation)presentation;
            yield return Until(() => native.IsShuffling);
            var startDistance = Vector3.Distance(camera.transform.position, deck.transform.position);
            var startDeck = deck.transform.position;
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            yield return Until(() => !choreography.IsMoving);
            yield return null;
            Assert.That(startDistance - Vector3.Distance(camera.transform.position, deck.transform.position), Is.GreaterThan(0.5f),
                "Moving the room behind a camera-relative prop is not approaching the deck.");
            Assert.That(Vector3.Distance(startDeck, deck.transform.position), Is.LessThan(0.001f),
                "The raised stack stays in the room during the camera move.");
        }

        [UnityTest]
        public IEnumerator CancellingAnExternallyHostedNativePickupReleasesItsPartialPose()
        {
            RequirePresentation();
            var anchor = deck.GetComponent<RitualDeckAnchor>();
            var native = (RitualDeckPresentation)presentation;
            var outer = anchor.Pickup();
            IEnumerator nested = null;
            try
            {
                Assert.That(outer.MoveNext(), Is.True);
                nested = (IEnumerator)outer.Current;
                Assert.That(nested.MoveNext(), Is.True);
                yield return null;
                Assert.That(nested.MoveNext(), Is.True);
                ((IDisposable)outer).Dispose();
                Assert.That(native.IsReady, Is.False, "Cancelled pickup must release the presentation's pose ownership.");
                Assert.That(anchor.IsPickingUp, Is.False);
                var laterOwner = deck.transform.localPosition + Vector3.right * 0.3f;
                deck.transform.localPosition = laterOwner;
                yield return null;
                Assert.That(nested.MoveNext(), Is.False);
                Assert.That(Vector3.Distance(deck.transform.localPosition, laterOwner), Is.LessThan(0.0001f));
            }
            finally
            {
                (nested as IDisposable)?.Dispose();
                (outer as IDisposable)?.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ReenablingTheAnchorInTheSameShuffleFrameEventuallyRebuildsTheNativeDeck()
        {
            RequirePresentation();
            var anchor = deck.GetComponent<RitualDeckAnchor>();
            var native = (RitualDeckPresentation)presentation;
            deck.Play();
            Assert.That(deck.IsPlaying, Is.True);
            anchor.enabled = false;
            anchor.enabled = true;
            var deadline = Time.realtimeSinceStartup + 1f;
            while ((!native.IsReady || !anchor.IsParked) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(deck.IsPlaying, Is.False);
            Assert.That(native.IsReady, Is.True);
            Assert.That(anchor.IsParked, Is.True);
            Assert.That(Layers().Count, Is.EqualTo(12));
            Assert.That(deck.GetComponentsInChildren<Transform>(true).Count(t => t.name == "RitualDeckPaper"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CaptureTheActualParkedPickupSplitWeaveRecutAndSquaredDeck()
        {
            var directory = Environment.GetEnvironmentVariable("TAROT_RITUAL_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) Assert.Ignore("Opt-in actual deck visual review.");
            RequirePresentation();
            var native = (RitualDeckPresentation)presentation;
            yield return CaptureBoth(directory, "deck-parked");
            canvas.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text = "An offline visual check";
            yield return CaptureBoth(directory, "deck-question");
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var anchor = deck.GetComponent<RitualDeckAnchor>();
            yield return Until(() => anchor.IsPickingUp);
            yield return new WaitForSeconds(0.4f);
            yield return CaptureBoth(directory, "deck-pickup");
            foreach (var sample in new[] { (0.38f, "split"), (0.52f, "weave"), (0.76f, "recut"), (0.86f, "square") })
            {
                yield return Until(() => native.IsShuffling && native.ShuffleProgress >= sample.Item1);
                yield return CaptureBoth(directory, "deck-" + sample.Item2);
            }
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            yield return Until(() => fan.AcceptingPicks);
            Assert.That(Layers().Any(r => r.enabled), Is.False);
            yield return CaptureBoth(directory, "deck-empty");
            fan.RequestPick(fan.FanCards[fan.FanCards.Count / 2]);
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip && !native.IsReturning);
            Assert.That(fan.DeckShowing, Is.EqualTo(1f));
            Assert.That(Layers().All(r => r.enabled), Is.True);
            yield return CaptureBoth(directory, "deck-restored");
        }

        private IEnumerator CaptureBoth(string directory, string name)
        {
            var scale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1024,768) })
                {
                    using var capture = new RitualFrameCapture(camera, canvas.GetComponent<Canvas>(), size.x, size.y);
                    Canvas.ForceUpdateCanvases();
                    canvas.GetComponent<RitualReadingView>().RefreshLayout();
                    yield return null;
                    var pixels = capture.Render();
                    try { File.WriteAllBytes(Path.Combine(directory, $"{name}-{size.x}x{size.y}.png"), pixels.EncodeToPNG()); }
                    finally { Object.Destroy(pixels); }
                    var native = (RitualDeckPresentation)presentation;
                    File.AppendAllText(Path.Combine(directory, "deck-phase-samples.txt"),
                        $"{name}-{size.x}x{size.y}: shuffle={native.ShuffleProgress:F5}, origin={native.CurrentPose.Origin}, opacity={native.CurrentPose.Opacity:F4}\n");
                }
            }
            finally { Time.timeScale = scale; }
        }

        private static IEnumerator Until(Func<bool> ready)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!ready())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "The actual deck flow must complete.");
                yield return null;
            }
        }

        private void RequirePresentation()
        {
            var type = typeof(RitualDeckAnchor).Assembly.GetType("TarotUnity.Presentation.RitualDeckPresentation");
            Assert.That(type, Is.Not.Null, "Native reference deck presentation is not implemented.");
            presentation = deck.GetComponent(type);
            Assert.That(presentation, Is.Not.Null, "The actual room must opt in, not just a unit fixture.");
        }
        private IReadOnlyList<Renderer> Layers() => (IReadOnlyList<Renderer>)presentation.GetType().GetProperty("LayerRenderers").GetValue(presentation);
        private IEnumerator Resize(Vector2Int size)
        {
            var next = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = next;
            camera.aspect = (float)size.x / size.y;
            if (target != null) { target.Release(); Object.Destroy(target); }
            target = next;
            yield return null;
            yield return null;
        }
        private Texture2D RenderWorld()
        {
            var old = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                for (var i = 0; i < 4; i++) RenderPipeline.SubmitRenderRequest(camera, request);
                return Read(target);
            }
            finally { RenderTexture.active = old; }
        }
        private Texture2D Read(RenderTexture texture)
        {
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                var value = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                value.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                value.Apply();
                pixels.Add(value);
                return value;
            }
            finally { RenderTexture.active = old; }
        }
        private static void Offline(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "ReadingRoom") typeof(ReadingRoomController).GetField("backendMode", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(), BackendIntegrationMode.LocalSimulation);
        }
    }
}
