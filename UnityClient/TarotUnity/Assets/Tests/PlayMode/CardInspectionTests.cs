using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class CardInspectionTests
    {
        private Component inspection;
        private DeckController deck;
        private ReadingFlowController flow;
        private Transform canvas;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return null;
            canvas = GameObject.Find("ReadingRoomCanvas").transform;
            deck = Object.FindFirstObjectByType<DeckController>();
            flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var type = typeof(ReadingRoomController).Assembly.GetType("TarotUnity.Presentation.CardInspectionController");
            Assert.That(type, Is.Not.Null, "the reading room needs the new card inspection behavior");
            inspection = Object.FindFirstObjectByType(type) as Component;
            Assert.That(inspection, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (inspection != null)
            {
                Call("CloseImmediate");
            }

            SceneManager.LoadScene("MainMenu");
            yield return null;
            ReadingSessionStore.Clear();
        }

        [UnityTest] public IEnumerator SingleCardShowsBeforeResultReady() { yield return AutomaticRound(1); }
        [UnityTest] public IEnumerator ThreeCardsEachShowBeforeResultReady() { yield return AutomaticRound(3); }
        [UnityTest] public IEnumerator TenCardsEachShowBeforeResultReady() { yield return AutomaticRound(10); }

        private IEnumerator AutomaticRound(int count)
        {
            yield return Draw(count);
            foreach (var card in deck.ActiveCards.ToArray())
            {
                var position = card.transform.localPosition;
                var rotation = card.transform.localRotation;
                var scale = card.transform.localScale;
                Click(card);
                yield return Until(() => Open, "the flip should open a close-up");
                Assert.That(Current, Is.SameAs(card));
                Assert.That(flow.State, Is.EqualTo(ReadingFlowState.WaitingForFlip), "even the last card waits for its close-up");
                Assert.That(canvas.Find("RevealResultButton").gameObject.activeSelf, Is.False);
                Assert.That((bool)Call("Show", card, false), Is.False, "a second open must not replace captured state");
                var hidden = deck.ActiveCards.FirstOrDefault(c => !c.IsFaceUp);
                if (hidden != null)
                {
                    Click(hidden);
                    yield return null;
                    Assert.That(hidden.IsFaceUp, Is.False, "a click cannot leak another card during inspection");
                }

                Call("Close");
                yield return Until(() => !Open && !card.GetComponent<CardFlipController>().IsFlipping, "the card should return");
                yield return null;
                AssertPose(card, position, rotation, scale);
            }

            yield return Until(() => flow.State == ReadingFlowState.ResultReady, "all revealed cards should make the reading ready");
            Assert.That(canvas.Find("RevealResultButton").gameObject.activeSelf, Is.True);
            var last = deck.ActiveCards.Last();
            var session = ReadingSessionStore.Current;
            Click(last);
            yield return Until(() => Open, "a revealed card stays inspectable after ResultReady");
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ResultReady));
            Assert.That(ReadingSessionStore.Current, Is.SameAs(session));
            Call("CloseImmediate");
        }

        [UnityTest]
        public IEnumerator ManualInspectionPreservesPoseTintAndMaterialBlockWhenDisabled()
        {
            yield return Draw(3);
            var card = deck.ActiveCards[0];
            Assert.That((bool)Call("Show", card, false), Is.False, "hidden artwork is private");
            card.SetFaceUp(true);
            var position = card.transform.localPosition;
            var rotation = card.transform.localRotation;
            var scale = card.transform.localScale;
            var color = card.FaceArtwork.color;
            var artScale = card.FaceArtwork.transform.localScale;
            var shadow = card.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Phase15_CardDropShadow");
            var shadowEnabled = shadow.enabled;
            var body = card.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Phase15_CardBody");
            var bodyEnabled = body.enabled;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_GlareIntensity", 0.37f);
            card.FaceArtwork.SetPropertyBlock(block);
            Assert.That((bool)Call("Show", card, false), Is.True);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(Open, Is.True, "manual viewing must not expire");
            Assert.That(card.FaceArtwork.color, Is.EqualTo(Color.white), "neutral, opaque artwork in close-up");
            Assert.That(shadow.enabled, Is.False, "the table-only fake shadow must not fly in front of the player");
            Assert.That(body.enabled, Is.False, "the legacy table slab must not appear as a black rectangle behind the enlarged art");
            ((Behaviour)inspection).enabled = false;
            yield return null;
            Assert.That(Open, Is.False);
            AssertPose(card, position, rotation, scale);
            Assert.That(card.FaceArtwork.color, Is.EqualTo(color));
            Assert.That(card.FaceArtwork.transform.localScale, Is.EqualTo(artScale));
            Assert.That(shadow.enabled, Is.EqualTo(shadowEnabled));
            Assert.That(body.enabled, Is.EqualTo(bodyEnabled));
            card.FaceArtwork.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_GlareIntensity"), Is.EqualTo(0.37f));
            Assert.That(canvas.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(card.GetComponent<CardHoverTiltController>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator AutomaticInspectionHoldsThenReturnsWithoutInput()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            Click(card);
            yield return Until(() => Open, "automatic close-up should open");
            yield return new WaitForSecondsRealtime(1.45f);
            Assert.That(Open, Is.True, "motion must not consume the 1.5-second readable hold");
            yield return Until(() => !Open, "automatic inspection should close without a click");
            yield return Until(() => flow.State == ReadingFlowState.ResultReady, "the flip must release after viewing");
        }

        [UnityTest]
        public IEnumerator ReversedArtFitsEveryViewportWithUprightLabels()
        {
            yield return Draw(3);
            var card = deck.ActiveCards[0];
            var sprite = card.FaceArtwork.sprite;
            card.DrawData.is_reversed = true;
            card.Bind(card.DrawData);
            card.SetFaceArtwork(sprite);
            card.SetFaceUp(true);
            var camera = Object.FindFirstObjectByType<Camera>();
            var previousAspect = camera.aspect;
            foreach (var size in new[] { new Vector2(1440, 900), new Vector2(1280, 720), new Vector2(1024, 768) })
            {
                camera.aspect = size.x / size.y;
                Assert.That((bool)Call("Show", card, false), Is.True);
                yield return new WaitForSecondsRealtime(0.45f);
                var art = card.FaceArtwork.transform;
                var bounds = sprite.bounds;
                var bottom = camera.WorldToViewportPoint(art.TransformPoint(bounds.center - Vector3.up * bounds.extents.y));
                var top = camera.WorldToViewportPoint(art.TransformPoint(bounds.center + Vector3.up * bounds.extents.y));
                Assert.That(Mathf.Abs(top.y - bottom.y), Is.InRange(0.55f, 0.65f));
                var left = camera.WorldToViewportPoint(art.TransformPoint(bounds.center - Vector3.right * bounds.extents.x));
                var right = camera.WorldToViewportPoint(art.TransformPoint(bounds.center + Vector3.right * bounds.extents.x));
                var displayedAspect = Mathf.Abs(right.x - left.x) * camera.aspect / Mathf.Abs(top.y - bottom.y);
                Assert.That(displayedAspect, Is.EqualTo(bounds.size.x / bounds.size.y).Within(0.01f), "close-up must not squeeze the source picture");
                Assert.That(Vector3.Dot(art.up, camera.transform.up), Is.LessThan(-0.99f), "reversed stays upside down");
                foreach (var x in new[] { bounds.min.x, bounds.max.x })
                foreach (var y in new[] { bounds.min.y, bounds.max.y })
                {
                    var point = camera.WorldToViewportPoint(art.TransformPoint(new Vector3(x, y, 0)));
                    Assert.That(point.x, Is.InRange(0.05f, 0.95f));
                    Assert.That(point.y, Is.InRange(0.15f, 0.85f));
                }

                var labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                    .Where(t => t.transform.root.name == "CardInspectionOverlay").ToArray();
                Assert.That(labels.Any(t => t.text.Contains(card.DrawData.tarot_card.name_zh)), Is.True);
                Assert.That(labels.Any(t => t.text.Contains("逆位")), Is.True);
                Assert.That(labels.All(t => Quaternion.Angle(t.transform.rotation, Quaternion.identity) < 0.1f), Is.True);
                Call("CloseImmediate");
            }

            camera.aspect = previousAspect;
        }

        [UnityTest]
        public IEnumerator SceneExitDuringInspectionDoesNotLeaveOverlayOrState()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            card.SetFaceUp(true);
            Assert.That((bool)Call("Show", card, false), Is.True);
            SceneManager.LoadScene("MainMenu");
            yield return null;
            Assert.That(GameObject.Find("CardInspectionOverlay"), Is.Null);
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            yield return null;
            var type = inspection.GetType();
            inspection = Object.FindFirstObjectByType(type) as Component;
            Assert.That(Open, Is.False);
        }

        [UnityTest]
        public IEnumerator RemovingTheInspectedCardRestoresTheRoom()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            card.SetFaceUp(true);
            Call("Show", card, false);
            Object.Destroy(card.gameObject);
            yield return null;
            yield return null;
            Assert.That(Open, Is.False);
            Assert.That(canvas.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(canvas.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Assert.That(GameObject.Find("CardInspectionOverlay"), Is.Null);
        }

        [UnityTest]
        public IEnumerator CloseButtonAndBackgroundBothReturnTheCard()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            card.SetFaceUp(true);
            foreach (var target in new[] { "CloseButton", "CloseBackground" })
            {
                Assert.That((bool)Call("Show", card, false), Is.True);
                yield return new WaitForSecondsRealtime(0.35f);
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                    { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) }, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.name, Is.EqualTo("CloseBackground"), "the close-up must intercept clicks before the table collider");
                GameObject.Find("CardInspectionOverlay").transform.Find(target).GetComponent<Button>().onClick.Invoke();
                yield return Until(() => !Open, "both close affordances must return the card");
                Assert.That(canvas.GetComponent<CanvasGroup>().interactable, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator EscapeClosesManualInspection()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            card.SetFaceUp(true);
            Assert.That((bool)Call("Show", card, false), Is.True);
            yield return new WaitForSecondsRealtime(0.35f);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                Assert.That(keyboard.escapeKey.isPressed, Is.True, "the synthetic input reached the input system");
                yield return Until(() => !Open, "Escape should dismiss the card without switching scenes");
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ReadingRoom"));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator InterruptedFlipDoesNotLockOtherCards()
        {
            yield return Draw(3);
            var first = deck.ActiveCards[0];
            Click(first);
            Assert.That(first.IsFaceUp, Is.False, "interrupt before the reveal event");
            Object.Destroy(first.GetComponent<CardFlipController>());
            yield return null;
            var second = deck.ActiveCards[1];
            Click(second);
            yield return Until(() => second.IsFaceUp, "a cancelled flip must not lock the remaining cards");
            yield return Until(() => Open, "the next card can still be viewed");
            Call("CloseImmediate");
        }

        [UnityTest]
        public IEnumerator RemovingFlipComponentCannotSkipTheLastCloseUp()
        {
            yield return Draw(1);
            var card = deck.ActiveCards[0];
            Click(card);
            yield return Until(() => Open, "the close-up must open");
            Object.Destroy(card.GetComponent<CardFlipController>());
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.WaitingForFlip), "inspection owns the final hold even if its caller disappears");
            Call("Close");
            yield return Until(() => flow.State == ReadingFlowState.ResultReady, "closing releases result framing");
        }

        [UnityTest]
        public IEnumerator CaptureCloseUpsForAllSpreadsAndWindowSizes()
        {
            var directory = Environment.GetEnvironmentVariable("TAROT_INSPECTION_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                Assert.Ignore("Opt-in rendered review; set TAROT_INSPECTION_CAPTURE_DIR.");
            }

            Directory.CreateDirectory(directory);
            foreach (var count in new[] { 1, 3, 10 })
            {
                if (count != 1)
                {
                    yield return SetUp();
                }

                yield return Draw(count);
                foreach (var card in deck.ActiveCards)
                {
                    card.SetFaceUp(true);
                }

                yield return new WaitForSecondsRealtime(1.1f);
                var inspected = deck.ActiveCards[count - 1];
                foreach (var size in new[] { new Vector2Int(1440, 900), new Vector2Int(1280, 720), new Vector2Int(1024, 768) })
                {
                    var camera = Object.FindFirstObjectByType<Camera>();
                    var texture = new RenderTexture(size.x, size.y, 24);
                    var previousTarget = camera.targetTexture;
                    var previousAspect = camera.aspect;
                    camera.targetTexture = texture;
                    camera.aspect = (float)size.x / size.y;
                    Call("Show", inspected, false);
                    yield return new WaitForSecondsRealtime(0.45f);
                    var overlayCanvas = GameObject.Find("CardInspectionOverlay").GetComponent<Canvas>();
                    Capture(camera, overlayCanvas, texture, Path.Combine(directory, $"closeup-{count}-{size.x}x{size.y}.png"));
                    Call("CloseImmediate");
                    Capture(camera, canvas.GetComponent<Canvas>(), texture, Path.Combine(directory, $"table-{count}-{size.x}x{size.y}.png"));
                    camera.targetTexture = previousTarget;
                    camera.aspect = previousAspect;
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
        }

        private static void Capture(Camera camera, Canvas ui, RenderTexture target, string path)
        {
            var mode = ui.renderMode;
            var previousCamera = ui.worldCamera;
            var distance = ui.planeDistance;
            var previousActive = RenderTexture.active;
            ui.renderMode = RenderMode.ScreenSpaceCamera;
            ui.worldCamera = camera;
            ui.planeDistance = camera.nearClipPlane + 0.05f;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Object.Destroy(pixels);
            RenderTexture.active = previousActive;
            ui.renderMode = mode;
            ui.worldCamera = previousCamera;
            ui.planeDistance = distance;
        }

        private IEnumerator Draw(int count)
        {
            var name = count == 10 ? "CelticCrossButton" : count == 3 ? "ThreeCardButton" : "OneCardButton";
            canvas.Find(name).GetComponent<Button>().onClick.Invoke();
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return DrawRitualTestDriver.PickAll();
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip, "the reading should deal normally");
            yield return new WaitForSeconds(0.4f);
            var catalog = Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
            foreach (var card in deck.ActiveCards)
            {
                var key = CardArtworkCatalog.ResolveKey(card.DrawData.tarot_card);
                Assert.That(card.FaceArtwork.sprite, Is.Not.Null,
                    $"dealt card {key} needs its real artwork; catalog entries={catalog?.Entries.Count}, resolved={catalog?.FindSprite(key)}");
            }
        }

        private bool Open => (bool)inspection.GetType().GetProperty("IsOpen").GetValue(inspection);
        private CardView Current => (CardView)inspection.GetType().GetProperty("CurrentCard").GetValue(inspection);
        private object Call(string name, params object[] args) => inspection.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance).Invoke(inspection, args);
        private static void Click(CardView card) => card.GetComponent<CardClickHandler>().OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });

        private static void AssertPose(CardView card, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Assert.That(Vector3.Distance(card.transform.localPosition, position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(card.transform.localRotation, rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(card.transform.localScale, scale), Is.LessThan(0.0001f));
        }

        private static IEnumerator Until(Func<bool> condition, string message)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }
    }
}
