using System;
using System.Collections;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualSpreadPresentationTests
    {
        private ApiClient previousClient;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousClient = ApiClient.Shared;
            ApiClient.ClearShared();
            SceneManager.sceneLoaded += KeepOffline;
            ReadingSessionStore.Clear();
            yield return SceneManager.LoadSceneAsync("ReadingRoom", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= KeepOffline;
            ReadingSessionStore.Clear();
            var scene = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("RitualSpreadCleanup");
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ApiClient.SetShared(previousClient);
            yield return null;
        }

        [UnityTest] public IEnumerator SingleCardMatchesTheApprovedProjection() { yield return CheckFlow(1); }
        [UnityTest] public IEnumerator ThreeCardsMatchTheApprovedProjection() { yield return CheckFlow(3); }
        [UnityTest] public IEnumerator TenCardsMatchTheApprovedProjection() { yield return CheckFlow(10); }

        [UnityTest]
        public IEnumerator AdapterRemovalDuringFlipKeepsCardsUntilTheDeckClears()
        {
            yield return CheckFlow(1);
            var deck = Object.FindFirstObjectByType<DeckController>();
            var card = deck.ActiveCards[0];
            var draw = card.DrawData;
            var flip = card.GetComponent<CardFlipController>();
            flip.Flip(card);
            Assert.That(flip.IsFlipping, Is.True);
            Object.Destroy(GameObject.Find("ReadingRoomCanvas").GetComponent<RitualSpreadPresentation>());
            yield return null;
            Assert.That(card != null, Is.True, "Removing presentation must not destroy a deck-owned card.");
            Assert.That(deck.ActiveCards[0], Is.SameAs(card));
            yield return Until(() => !flip.IsFlipping);
            Assert.That(card.DrawData, Is.SameAs(draw));
            Assert.That(card.IsFaceUp, Is.True);
            deck.Clear();
            yield return null;
            yield return null;
            Assert.That(GameObject.Find("RitualSpreadWorld"), Is.Null, "The detached card presentation must not leak after Clear.");
        }

        [UnityTest]
        public IEnumerator DestroyingTheAdapterRestoresAuthoredSlotsAndCleansOwnedLabels()
        {
            var canvas = GameObject.Find("ReadingRoomCanvas");
            var layouts = Object.FindFirstObjectByType<SpreadLayoutController>();
            var original = layouts.GetSlots(1).ToArray();
            var view = canvas.GetComponent<RitualSpreadPresentation>();
            view.Prepare(1);
            Assert.That(layouts.GetSlots(1)[0], Is.Not.SameAs(original[0]));
            Object.Destroy(view);
            yield return null;
            yield return null;
            Assert.That(layouts.GetSlots(1), Is.EqualTo(original), "Generated destinations must not leave a dead slot set.");
            Assert.That(canvas.GetComponentsInChildren<TMP_Text>(true).Any(t => t.name.StartsWith("RitualPositionLabel_")), Is.False);
            Assert.That(GameObject.Find("RitualSpreadWorld"), Is.Null);
        }

        [UnityTest]
        public IEnumerator AttachingAgainKeepsTheExistingAdapterAndDestinations()
        {
            var canvas = GameObject.Find("ReadingRoomCanvas");
            var view = canvas.GetComponent<RitualSpreadPresentation>();
            view.Prepare(3);
            var original = Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(3).ToArray();
            RitualSpreadPresentation second = null;
            Assert.DoesNotThrow(() => second = RitualSpreadPresentation.Attach(canvas,
                Object.FindFirstObjectByType<ReadingFlowController>(), Object.FindFirstObjectByType<DeckController>(),
                Object.FindFirstObjectByType<CameraChoreographyController>(), Object.FindFirstObjectByType<Camera>()));
            Assert.That(second, Is.SameAs(view));
            Assert.That(Object.FindFirstObjectByType<SpreadLayoutController>().GetSlots(3), Is.EqualTo(original));
            yield return null;
        }

        private IEnumerator CheckFlow(int count)
        {
            var type = typeof(RitualReadingView).Assembly.GetType("TarotUnity.UI.RitualSpreadPresentation");
            Assert.That(type, Is.Not.Null, "An explicit 3D framing adapter must implement the approved preview.");
            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas.GetComponent(type), Is.Not.Null, "The real scene must attach the adapter automatically.");
            canvas.transform.Find(count == 1 ? "OneCardButton" : count == 3 ? "ThreeCardButton" : "CelticCrossButton")
                .GetComponent<Button>().onClick.Invoke();
            canvas.GetComponentInChildren<TMP_InputField>(true).text = "A visual parity test question";
            canvas.transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            yield return Until(() => fan.AcceptingPicks);
            for (var i = 0; i < count; i++)
            {
                fan.RequestPick(fan.FanCards[fan.FanCards.Count / 2]);
                var picked = i + 1;
                yield return Until(() => deck.ActiveCards.Count >= picked);
            }
            yield return Until(() => flow.State == ReadingFlowState.WaitingForFlip);
            var fixture = LocalReadingSimulator.CreatePlaceholderDraws(count);
            for (var i = 0; i < fixture.Length; i++) fixture[i].is_reversed = i % 3 == 1;
            deck.BindDealtCards(fixture);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.isActiveAndEnabled && c.gameObject.scene == canvas.scene)
                .OrderByDescending(c => c.depth).First();
            var refresh = type.GetMethod("RefreshLayout", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(refresh, Is.Not.Null);
            var view = canvas.GetComponent(type);
            var originalTarget = camera.targetTexture;
            var originalAspect = camera.aspect;
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
                    new Vector2Int(2560, 1440), new Vector2Int(2048, 1536), new Vector2Int(1280, 720) })
                {
                    var texture = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = texture;
                    camera.aspect = (float)size.x / size.y;
                    refresh.Invoke(view, null);
                    yield return null;
                    for (var i = 0; i < count; i++) deck.ActiveCards[i].SetFaceUp(true);
                    yield return null;
                    // The test coroutine resumes before LateUpdate; measure the final rendered camera frame.
                    refresh.Invoke(view, null);
                    Canvas.ForceUpdateCanvases();
                    Physics.SyncTransforms();
                    var layoutType = type.Assembly.GetType("TarotUnity.UI.RitualSpreadLayout");
                    var layout = layoutType.GetMethod("Compute").Invoke(null, new object[] { count, (float)size.x, (float)size.y });
                    var expected = (Array)layout.GetType().GetField("Cards").GetValue(layout);
                    var slots = flow.GetSelectedSpreadSlots();
                    Assert.That(slots.Count, Is.EqualTo(count));
                    for (var i = 0; i < count; i++)
                    {
                        var card = deck.ActiveCards[i];
                        Assert.That(card.DrawData, Is.SameAs(fixture[i]), "Framing must never replace or redraw a card.");
                        Assert.That(card.transform.parent, Is.SameAs(slots[i]));
                        Assert.That(card.transform.localPosition.sqrMagnitude, Is.LessThan(0.00001f));
                        var target = (Rect)expected.GetValue(i).GetType().GetField("CardRect").GetValue(expected.GetValue(i));
                        var actual = ArtworkRect(card.FaceArtwork, camera, size.y);
                        Assert.That(actual.x, Is.EqualTo(target.x).Within(1f), $"{count} cards, {size}, card {i}: x");
                        Assert.That(actual.y, Is.EqualTo(target.y).Within(1f), $"{count} cards, {size}, card {i}: y");
                        Assert.That(actual.width, Is.EqualTo(target.width).Within(1f));
                        Assert.That(actual.height, Is.EqualTo(target.height).Within(1f));
                        var alignment = Vector3.Dot(card.FaceArtwork.transform.TransformVector(Vector3.up).normalized,
                            slots[i].TransformVector(Vector3.forward).normalized);
                        Assert.That(alignment * (fixture[i].is_reversed ? -1f : 1f), Is.GreaterThan(0.99f));
                        var ray = camera.ScreenPointToRay(new Vector3(actual.center.x, size.y - actual.center.y, 0f));
                        Assert.That(Physics.Raycast(ray, out var hit, 100f), Is.True, "The 3D card keeps a live collider.");
                        Assert.That(hit.collider.GetComponentInParent<CardView>(), Is.SameAs(card));
                        var label = canvas.transform.Find("RitualPositionLabel_" + i).GetComponent<TMP_Text>();
                        Assert.That(label.gameObject.activeInHierarchy && label.color.a > 0f, Is.True);
                        Assert.That(string.IsNullOrWhiteSpace(label.text), Is.False);
                        Assert.That(label.text.Contains("\u9006\u4f4d"), Is.EqualTo(fixture[i].is_reversed));
                        card.SetFaceUp(false);
                    }
                    yield return null;
                    for (var i = 0; i < count; i++)
                    {
                        Assert.That(canvas.transform.Find("RitualPositionLabel_" + i).gameObject.activeSelf, Is.False,
                            "A hidden face must not disclose its position label early.");
                        var visibleRenderers = deck.ActiveCards[i].GetComponentsInChildren<Renderer>(true)
                            .Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
                        Assert.That(visibleRenderers.Select(r => r.name), Is.EqualTo(new[] { "RitualPaperBack" }),
                            "Old shells and fake shadows must not reappear under the paper back.");
                        var back = (SpriteRenderer)visibleRenderers[0];
                        Assert.That(back.sprite, Is.Not.Null, "A clickable face-down card must also remain visible.");
                        Assert.That(back.sprite.texture.name, Is.EqualTo("TarotCardBack"));
                    }
                    camera.targetTexture = originalTarget;
                    camera.aspect = originalAspect;
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
            finally
            {
                camera.targetTexture = originalTarget;
                camera.aspect = originalAspect;
            }
        }

        private static Rect ArtworkRect(SpriteRenderer artwork, Camera camera, int height)
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var point in artwork.sprite.vertices)
            {
                var p = camera.WorldToScreenPoint(artwork.transform.TransformPoint(new Vector3(point.x, point.y, 0f)));
                var pixel = new Vector2(p.x, height - p.y);
                min = Vector2.Min(min, pixel);
                max = Vector2.Max(max, pixel);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static IEnumerator Until(Func<bool> predicate)
        {
            var deadline = Time.realtimeSinceStartup + 45f;
            while (!predicate())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return null;
            }
        }

        private static void KeepOffline(Scene scene, LoadSceneMode mode)
        {
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            if (scene.name != "ReadingRoom" || room == null) return;
            var field = typeof(ReadingRoomController).GetField("backendMode", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(room, Enum.Parse(field.FieldType, "LocalSimulation"));
        }
    }
}
