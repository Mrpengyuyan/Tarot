using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    // New presentation types are resolved at runtime so the first red gate compiles
    // before either component exists. No room, network or draw flow is started here.
    public sealed class RitualMotionTests
    {
        private const string AnchorTypeName = "TarotUnity.Presentation.RitualDeckAnchor";
        private const string StagingTypeName = "TarotUnity.Presentation.PickedCardStaging";
        private const string ApplyPresentation = "ApplyRitualPresentation";
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private GameObject rig;
        private Camera viewCamera;
        private CameraChoreographyController choreography;
        private RitualMotionCoroutineHost host;
        private UnityEngine.Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            randomState = UnityEngine.Random.state;
            rig = new GameObject("RitualMotionTests_Rig");
            host = rig.AddComponent<RitualMotionCoroutineHost>();
            var cameraObject = Child("DrawCamera");
            cameraObject.SetActive(false);
            viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.enabled = false;
            viewCamera.aspect = 16f / 9f;
            viewCamera.fieldOfView = 60f;
            viewCamera.nearClipPlane = 0.01f;
            viewCamera.farClipPlane = 100f;
            viewCamera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -11f), Quaternion.Euler(35f, 0f, 0f));
            choreography = cameraObject.AddComponent<CameraChoreographyController>();
            SetField(choreography, "targetCamera", viewCamera);
            cameraObject.SetActive(true);
            choreography.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            host.StopAllCoroutines();
            Object.DestroyImmediate(rig);
            UnityEngine.Random.state = randomState;
        }

        [TestCase(AnchorTypeName)]
        [TestCase(StagingTypeName)]
        public void PresentationTypeExistsAsAComponent(string name)
        {
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(RequireType(name)), Is.True);
        }

        [Test]
        public void PresentationComponentsExposeCoordinatorContractsWithoutCompileTimeDependencies()
        {
            var anchor = RequireType(AnchorTypeName);
            var attach = anchor.GetMethod("Attach", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(DeckShuffleChoreographer), typeof(CameraChoreographyController) }, null);
            Assert.That(attach, Is.Not.Null, "Attach must reuse the supplied physical deck and camera");
            Assert.That(attach.ReturnType, Is.EqualTo(anchor));
            Assert.That(RequireMethod(anchor, "Pickup").ReturnType, Is.EqualTo(typeof(IEnumerator)));
            Assert.That(RequireMethod(anchor, "Park").ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(RequireMethod(anchor, "Clear").ReturnType, Is.EqualTo(typeof(void)));
            RequireProperty(anchor, "PickupSeconds", typeof(float));

            var staging = RequireType(StagingTypeName);
            Assert.That(RequireMethod(staging, "Prepare", typeof(int), typeof(Camera)).ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(RequireMethod(staging, "Slot", typeof(int)).ReturnType, Is.EqualTo(typeof(Transform)));
            Assert.That(RequireMethod(staging, "Settle", typeof(IReadOnlyList<CardView>), typeof(IList<Transform>)).ReturnType,
                Is.EqualTo(typeof(IEnumerator)));
            Assert.That(RequireMethod(staging, "Clear").ReturnType, Is.EqualTo(typeof(void)));
            RequireProperty(staging, "SlotCount", typeof(int));
            RequireTransferEvent(staging);
        }

        [TestCase(1, 16f / 9f)]
        [TestCase(3, 16f / 9f)]
        [TestCase(10, 16f / 9f)]
        [TestCase(1, 4f / 3f)]
        [TestCase(3, 4f / 3f)]
        [TestCase(10, 4f / 3f)]
        public void PrepareCreatesCenteredUpperPhysicalRowsWithRoomForTheFan(int count, float aspect)
        {
            viewCamera.aspect = aspect;
            var staging = CreateStaging(count);
            var slots = Slots(staging, count);
            var points = slots.Select(s => viewCamera.WorldToViewportPoint(s.position)).ToArray();
            var columns = count == 10 ? 5 : count;
            Assert.That(Property<int>(staging, "SlotCount"), Is.EqualTo(count));
            Assert.That(slots.Distinct().Count(), Is.EqualTo(count), "each pick needs its own destination");
            for (var row = 0; row < count / columns; row++)
            {
                var first = row * columns;
                var last = first + columns - 1;
                Assert.That((points[first].x + points[last].x) * 0.5f, Is.EqualTo(0.5f).Within(0.015f),
                    "each staged row shares the horizontal center of the frame");
                for (var column = 0; column < columns; column++)
                {
                    var i = first + column;
                    Assert.That(points[i].z, Is.GreaterThan(viewCamera.nearClipPlane));
                    Assert.That(points[i].x, Is.InRange(0.08f, 0.92f));
                    Assert.That(points[i].y, Is.InRange(0.60f, 0.91f), "the lower fan stays readable");
                    Assert.That(points[i].y, Is.EqualTo(points[first].y).Within(0.005f));
                    Assert.That(slots[i].position.y, Is.EqualTo(slots[0].position.y).Within(0.001f),
                        "destinations share one raised horizontal staging plane");
                    Assert.That(slots[i].position.y, Is.InRange(1f, 1.2f));
                    Assert.That(Vector3.Dot(slots[i].up, -viewCamera.transform.forward), Is.GreaterThan(0.999f),
                        "the physical back faces the viewer rather than lying flat on the table");
                    Assert.That(Vector3.Dot(slots[i].forward, viewCamera.transform.up), Is.GreaterThan(0.999f),
                        "the card's long edge stays upright in the draw camera");
                    if (column > 0)
                    {
                        Assert.That(points[i].x - points[i - 1].x, Is.GreaterThan(0.05f),
                            "ordered backs must not collapse into an overlapping pile");
                    }

                    // CardView's authored artwork footprint is 0.64 by 1 metre.
                    foreach (var x in new[] { -0.32f, 0.32f })
                    foreach (var z in new[] { -0.5f, 0.5f })
                    {
                        var world = slots[i].TransformPoint(new Vector3(x, 0f, z));
                        Assert.That(world.y, Is.GreaterThan(0.16f), "the upright back never enters the physical table");
                        var corner = viewCamera.WorldToViewportPoint(world);
                        Assert.That(corner.x, Is.InRange(0.02f, 0.98f));
                        Assert.That(corner.y, Is.InRange(0.52f, 0.97f), "even the back's edge clears the lower fan band");
                    }
                }
            }

            if (count == 10)
            {
                Assert.That(points[0].y - points[5].y, Is.GreaterThan(0.08f), "ten cards use two distinct rows");
            }
        }

        [Test]
        public void PrepareDoesNotCloneTheDeckOrCreatePlaceholderCardViews()
        {
            var deck = CreateDeck();
            var card = CreateCard(0, Child("ExistingCards").transform);
            var original = new LocalPose(card.transform);
            var cardCount = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var deckCount = Object.FindObjectsByType<DeckShuffleChoreographer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            CreateStaging(10);
            Assert.That(Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(cardCount), "only the real picked cards may fill the rail");
            Assert.That(Object.FindObjectsByType<DeckShuffleChoreographer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(deckCount));
            Assert.That(deck.transform.childCount, Is.EqualTo(9));
            original.AssertMatches(card.transform);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(10)]
        public void PrepareUsesTheRaisedWorldPlaneWhenHostedOnATranslatedUiCanvas(int count)
        {
            var staging = Child("TranslatedUiCanvasHost").AddComponent(RequireType(StagingTypeName));
            staging.transform.position = new Vector3(640f, 360f, 0f);
            var hostPose = new LocalPose(staging.transform);
            Call(staging, "Prepare", count, viewCamera);
            foreach (var slot in Slots(staging, count))
            {
                Assert.That(slot.position.y, Is.EqualTo(1.1f).Within(0.001f),
                    "the raised staging plane remains independent of UI host coordinates");
                var point = viewCamera.WorldToViewportPoint(slot.position);
                Assert.That(point.z, Is.GreaterThan(viewCamera.nearClipPlane));
                Assert.That(point.x, Is.InRange(0.08f, 0.92f));
                Assert.That(point.y, Is.InRange(0.60f, 0.91f));
            }

            Call(staging, "Clear");
            hostPose.AssertMatches(staging.transform);
        }

        [TestCase(1, 16f / 9f)]
        [TestCase(3, 16f / 9f)]
        [TestCase(10, 16f / 9f)]
        [TestCase(1, 4f / 3f)]
        [TestCase(3, 4f / 3f)]
        [TestCase(10, 4f / 3f)]
        public void ActualCardBackProjectsAsAReadablePortraitAboveTheFan(int count, float aspect)
        {
#if !UNITY_EDITOR
            Assert.Ignore("The authored card prefab is loaded through the editor asset database.");
#else
            viewCamera.aspect = aspect;
            viewCamera.fieldOfView = 38f;
            viewCamera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -11f), Quaternion.Euler(46f, 0f, 0f));
            var staging = CreateStaging(count);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(prefab, Is.Not.Null);
            var physical = Object.Instantiate(prefab, rig.transform);
            var card = physical.GetComponent<CardView>();
            card.SetFaceUp(false);
            var scale = physical.transform.localScale;
            var back = physical.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "MP_CardBackFace");
            Assert.That(back.enabled && back.gameObject.activeInHierarchy, Is.True);
            for (var i = 0; i < count; i++)
            {
                var slot = Slot(staging, i);
                physical.transform.SetPositionAndRotation(slot.position, slot.rotation);
                var bounds = back.localBounds;
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                for (var corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    var world = back.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                    var point = viewCamera.WorldToViewportPoint(world);
                    Assert.That(world.y, Is.GreaterThan(0.16f), "the actual back's lower edge clears the table");
                    Assert.That(point.z, Is.GreaterThan(viewCamera.nearClipPlane));
                    Assert.That(point.x, Is.InRange(0.02f, 0.98f));
                    Assert.That(point.y, Is.InRange(0.52f, 0.97f), "the actual footprint clears the fan and screen edge");
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }

                var pixelAspect = (max.y - min.y) / ((max.x - min.x) * aspect);
                Assert.That(pixelAspect, Is.InRange(1.2f, 2.2f), "the real back is portrait, not a foreshortened thumbnail");
                Assert.That(max.y - min.y, Is.InRange(0.08f, 0.22f), "the physical back has a readable projected height");
                Assert.That(card.IsFaceUp, Is.False);
                AssertVector(physical.transform.localScale, scale, "staging changes orientation, not authored card scale");
            }
#endif
        }

        [UnityTest]
        public IEnumerator PrepareAgainAndClearRemoveOnlyOwnedSlots()
        {
            var staging = CreateStaging(10);
            var authoredHost = new LocalPose(staging.transform);
            var oldSlots = Slots(staging, 10);
            var unrelated = Child("UnrelatedCard").AddComponent<CardView>();
            var unrelatedPose = new LocalPose(unrelated.transform);
            Call(staging, "Prepare", 3, viewCamera);
            yield return null;
            AssertDestroyed(oldSlots);
            var newSlots = Slots(staging, 3);
            Assert.That(Property<int>(staging, "SlotCount"), Is.EqualTo(3));
            Call(staging, "Clear");
            Call(staging, "Clear");
            yield return null;
            AssertDestroyed(newSlots);
            Assert.That(Property<int>(staging, "SlotCount"), Is.Zero);
            authoredHost.AssertMatches(staging.transform);
            unrelatedPose.AssertMatches(unrelated.transform);
            Call(staging, "Prepare", 1, viewCamera);
            Assert.That(Slot(staging, 0), Is.Not.Null, "the component can be reused after cleanup");
        }

        [UnityTest]
        public IEnumerator StagingDestinationsRemainInWorldSpaceWhenTheCameraMoves()
        {
            var staging = CreateStaging(10);
            var slots = Slots(staging, 10);
            var positions = slots.Select(s => s.position).ToArray();
            var rotations = slots.Select(s => s.rotation).ToArray();
            viewCamera.transform.SetPositionAndRotation(new Vector3(2f, 3f, -4f), Quaternion.Euler(50f, 8f, 0f));
            viewCamera.fieldOfView = 43f;
            yield return null;
            yield return null;
            for (var i = 0; i < slots.Length; i++)
            {
                AssertVector(slots[i].position, positions[i], "camera travel must not drag the physical rail");
                AssertRotation(slots[i].rotation, rotations[i]);
            }
        }

        [UnityTest]
        public IEnumerator SettleHoldsFullSelectionThenMovesThoseSameCardsToTheFinalSpread()
        {
            foreach (var count in new[] { 1, 3, 10 })
            {
                var staging = CreateStaging(count);
                var cards = CreateStagedCards(staging, count);
                var starts = cards.Select(c => new LocalPose(c.transform)).ToArray();
                var data = cards.Select(c => c.DrawData).ToArray();
                var finalSlots = FinalSlots(count);
                var firstMoved = -1f;
                var sawIntermediateRotation = false;
                var completed = -1f;
                var began = Time.time;
                var transferStarted = -1f;
                var transferCount = 0;
                SubscribeToTransfer(staging, () =>
                {
                    transferCount++;
                    transferStarted = Time.time;
                    for (var i = 0; i < count; i++)
                    {
                        starts[i].AssertMatches(cards[i].transform);
                    }
                });
                host.StartCoroutine(Track(Settle(staging, cards, finalSlots), () => completed = Time.time));
                while (completed < 0f && Time.time - began < 4f)
                {
                    var elapsed = Time.time - began;
                    for (var i = 0; i < count; i++)
                    {
                        if (elapsed < 1.20f)
                        {
                            starts[i].AssertMatches(cards[i].transform);
                        }

                        if (firstMoved < 0f && Vector3.Distance(cards[i].transform.localPosition, starts[i].Position) > 0.002f)
                        {
                            firstMoved = Time.time;
                        }

                        sawIntermediateRotation |= Quaternion.Angle(cards[i].transform.localRotation, starts[i].Rotation) > 1f
                            && Quaternion.Angle(cards[i].transform.rotation, finalSlots[i].rotation) > 1f;

                        Assert.That(cards[i].IsFaceUp, Is.False, "staging must never reveal a pick early");
                        Assert.That(cards[i].DrawData, Is.SameAs(data[i]), "presentation cannot replace draw identity");
                        Assert.That(cards[i].DrawData.is_reversed, Is.EqualTo(i % 2 == 1));
                    }

                    if (elapsed > 1.35f)
                    {
                        viewCamera.transform.position = new Vector3(2f, 3f, -4f);
                    }

                    yield return null;
                }

                Assert.That(firstMoved - began, Is.InRange(1.20f, 1.55f), "hold the complete selection for 1.25 seconds");
                Assert.That(completed - firstMoved, Is.InRange(0.55f, 0.95f), "the final transfer takes about 0.7 seconds");
                Assert.That(completed - began, Is.InRange(1.90f, 2.40f));
                Assert.That(transferCount, Is.EqualTo(1), "the coordinator starts gather and camera travel only once");
                Assert.That(sawIntermediateRotation, Is.True, "the viewer-facing cards rotate smoothly back to final authored poses");
                Assert.That(transferStarted - began, Is.InRange(1.20f, 1.55f));
                Assert.That(firstMoved, Is.GreaterThanOrEqualTo(transferStarted), "notification precedes any transfer motion");
                for (var i = 0; i < count; i++)
                {
                    AssertVector(cards[i].transform.position, finalSlots[i].position, "land in the actual final slot");
                    AssertRotation(cards[i].transform.rotation, finalSlots[i].rotation);
                    AssertVector(cards[i].transform.localScale, starts[i].Scale, "restore each card's own authored scale");
                    Assert.That(cards[i].transform.parent, Is.SameAs(starts[i].Parent));
                }

                yield return Settle(staging, cards, finalSlots);
                Assert.That(transferCount, Is.EqualTo(1), "a prepared selection can start transfer only once");
                var landed = cards.Select(c => new LocalPose(c.transform)).ToArray();
                Call(staging, "Clear");
                yield return null;
                for (var i = 0; i < count; i++)
                {
                    landed[i].AssertMatches(cards[i].transform);
                }

                viewCamera.transform.position = new Vector3(0f, 8f, -11f);
            }
        }

        [UnityTest]
        public IEnumerator ResizingTheFinalSlotsDuringHoldDoesNotSnapScaleOnLanding()
        {
            var staging = CreateStaging(1);
            var cards = CreateStagedCards(staging, 1);
            var destination = FinalSlots(1);
            destination[0].gameObject.AddComponent<TarotUnity.Presentation.RitualSpreadSlot>();
            var completed = false;
            host.StartCoroutine(Track(Settle(staging, cards, destination), () => completed = true));
            yield return new WaitForSeconds(0.3f);
            destination[0].localScale *= 3f;
            yield return new WaitForSeconds(1.55f);
            Assert.That(completed, Is.False, "Sample the transfer just before docking.");
            Assert.That(Vector3.Distance(cards[0].transform.lossyScale, destination[0].lossyScale),
                Is.LessThan(destination[0].lossyScale.magnitude * 0.15f),
                "Approach the resized destination smoothly, not the obsolete pre-hold size.");
            while (!completed) yield return null;
        }

        [UnityTest]
        public IEnumerator ClearDuringTransferRestoresOwnedCardStateAndInvalidatesAnExternalRoutine()
        {
            yield return CancelSettle(1.5f, false);
        }

        [UnityTest]
        public IEnumerator DisableDuringSelectionHoldRestoresOwnedCardStateAndCleansSlots()
        {
            yield return CancelSettle(0.3f, true);
        }

        [UnityTest]
        public IEnumerator TransferStartedDoesNotFireForInvalidOrCancelledSelections()
        {
            var staging = CreateStaging(3);
            var cards = CreateStagedCards(staging, 3);
            var finalSlots = FinalSlots(3);
            var transferCount = 0;
            SubscribeToTransfer(staging, () => transferCount++);
            yield return Settle(staging, null, finalSlots);
            yield return Settle(staging, new List<CardView>(), finalSlots);
            yield return Settle(staging, cards.Take(2).ToList(), finalSlots);
            yield return Settle(staging, new List<CardView> { cards[0], null, cards[2] }, finalSlots);
            yield return Settle(staging, cards, new List<Transform> { finalSlots[0], null, finalSlots[2] });
            Assert.That(transferCount, Is.Zero);
            host.StartCoroutine(Settle(staging, cards, finalSlots));
            yield return new WaitForSeconds(0.3f);
            Call(staging, "Clear");
            yield return new WaitForSeconds(2f);
            Assert.That(transferCount, Is.Zero, "cancellation before the hold expires cannot start camera travel");
        }

        [UnityTest]
        public IEnumerator TransferStartedHandlerCanClearStagingBeforeAnyCardMoves()
        {
            var staging = CreateStaging(3);
            var cards = CreateStagedCards(staging, 3);
            var starts = cards.Select(c => new LocalPose(c.transform)).ToArray();
            var transferCount = 0;
            SubscribeToTransfer(staging, () =>
            {
                transferCount++;
                Call(staging, "Clear");
            });
            yield return Settle(staging, cards, FinalSlots(3));
            yield return null;
            Assert.That(transferCount, Is.EqualTo(1));
            Assert.That(Property<int>(staging, "SlotCount"), Is.Zero);
            for (var i = 0; i < cards.Count; i++)
            {
                starts[i].AssertMatches(cards[i].transform);
            }
        }

        [UnityTest]
        public IEnumerator DestroyingTheStagingHostCleansSlotsWithoutDestroyingSelectedCards()
        {
            var staging = CreateStaging(3);
            var slots = Slots(staging, 3);
            var cards = CreateStagedCards(staging, 3);
            var starts = cards.Select(c => new LocalPose(c.transform)).ToArray();
            host.StartCoroutine(Settle(staging, cards, FinalSlots(3)));
            yield return new WaitForSeconds(0.2f);
            Object.Destroy(staging.gameObject);
            yield return null;
            AssertDestroyed(slots);
            yield return new WaitForSeconds(2.1f);
            for (var i = 0; i < cards.Count; i++)
            {
                Assert.That(cards[i] != null, Is.True, "staging does not own the physical picked cards");
                starts[i].AssertMatches(cards[i].transform);
            }
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        public void AttachParksTheSameDeckAndRepeatedAttachPreservesItsAuthoredPose(float aspect)
        {
            viewCamera.aspect = aspect;
            var deck = CreateDeck();
            var original = new LocalPose(deck.transform);
            var cards = deck.transform.Cast<Transform>().ToArray();
            var cardPoses = cards.Select(c => new LocalPose(c)).ToArray();
            var deckCount = Object.FindObjectsByType<DeckShuffleChoreographer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var anchor = Attach(deck);
            var viewport = viewCamera.WorldToViewportPoint(deck.transform.position);
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.InRange(0.03f, 0.23f), "idle deck parks near the left screen edge");
            Assert.That(viewport.y, Is.InRange(0.08f, 0.32f), "idle deck belongs below the central question and cards");
            Assert.That(Attach(deck), Is.SameAs(anchor), "Attach is idempotent and must not recapture the parked pose");
            Assert.That(Object.FindObjectsByType<DeckShuffleChoreographer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(deckCount));
            Assert.That(deck.transform.Cast<Transform>().ToArray(), Is.EqualTo(cards));
            for (var i = 0; i < cards.Length; i++)
            {
                cardPoses[i].AssertMatches(cards[i]);
            }

            Call(anchor, "Clear");
            Call(anchor, "Clear");
            original.AssertMatches(deck.transform);
        }

        [UnityTest]
        public IEnumerator PickupVisiblyReturnsTheSameDeckToItsAuthoredShuffleOriginBeforeFocusDeck()
        {
            var deck = CreateDeck();
            var original = new LocalPose(deck.transform);
            var cameraPosition = viewCamera.transform.position;
            var cameraRotation = viewCamera.transform.rotation;
            var focusPose = Child("AuthoredDeckCameraPose").transform;
            focusPose.SetPositionAndRotation(new Vector3(-1.55f, 1.7f, 0.3f), Quaternion.Euler(55f, 60f, 0f));
            SetField(choreography, "deckPose", focusPose);
            var anchor = Attach(deck);
            var parked = deck.transform.position;
            var moving = false;
            var lifted = false;
            var began = Time.time;
            var completed = false;
            host.StartCoroutine(Track((IEnumerator)Call(anchor, "Pickup"), () => completed = true));
            while (!completed && Time.time - began < 2f)
            {
                moving |= Vector3.Distance(deck.transform.position, parked) > 0.01f
                    && Vector3.Distance(deck.transform.localPosition, original.Position) > 0.01f;
                lifted |= deck.transform.position.y > Mathf.Max(parked.y, original.Position.y) + 0.04f;
                AssertVector(viewCamera.transform.position, cameraPosition, "pickup precedes the original camera turn");
                AssertRotation(viewCamera.transform.rotation, cameraRotation);
                yield return null;
            }

            Assert.That(completed, Is.True, "pickup must terminate");
            Assert.That(moving, Is.True, "the deck travels instead of teleporting");
            Assert.That(lifted, Is.True, "pickup visibly clears the table");
            Assert.That(Time.time - began, Is.EqualTo(Property<float>(anchor, "PickupSeconds")).Within(0.2f));
            original.AssertMatches(deck.transform);
            choreography.enabled = true;
            choreography.FocusDeck();
            var deadline = Time.time + 2f;
            while (choreography.IsMoving && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(choreography.IsMoving, Is.False);
            AssertVector(viewCamera.transform.position, focusPose.position, "retain the original authored camera framing");
            AssertRotation(viewCamera.transform.rotation, focusPose.rotation);
            Call(anchor, "Park");
            Call(anchor, "Clear");
            original.AssertMatches(deck.transform);
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        public void ParkFitsAllPhysicalRendererCornersAcrossCameraDistances(float aspect)
        {
            viewCamera.aspect = aspect;
            var deck = CreateDeck();
            var original = new LocalPose(deck.transform);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "PhysicalDeckBounds";
            body.transform.SetParent(deck.transform, false);
            body.transform.localPosition = new Vector3(0.45f, 0.08f, 0.2f);
            body.transform.localScale = new Vector3(0.8f, 0.25f, 1.18f);
            var bounds = body.GetComponent<MeshFilter>().sharedMesh.bounds;
            viewCamera.transform.SetPositionAndRotation(new Vector3(0f, 2.45f, -3.55f), Quaternion.Euler(30f, 0f, 0f));
            var anchor = Attach(deck);
            for (var distance = 0; distance < 2; distance++)
            {
                if (distance == 1)
                {
                    viewCamera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -11f), Quaternion.Euler(46f, 0f, 0f));
                    viewCamera.fieldOfView = 38f;
                }

                Call(anchor, "Park");
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                for (var corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    var local = bounds.center + Vector3.Scale(bounds.extents, sign);
                    var point = viewCamera.WorldToViewportPoint(body.transform.TransformPoint(local));
                    Assert.That(point.z, Is.GreaterThan(viewCamera.nearClipPlane));
                    Assert.That(point.x, Is.InRange(0.02f, 0.20f), "the whole deck clears both the left edge and central content");
                    Assert.That(point.y, Is.InRange(0.12f, 0.36f), "the whole deck clears the bottom navigation");
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }

                Assert.That(max.x - min.x, Is.InRange(0.08f, 0.10f), "parked deck occupies about nine percent of screen width");
                Assert.That(max.y - min.y, Is.LessThanOrEqualTo(0.16f));
                Assert.That((min.x + max.x) * 0.5f, Is.EqualTo(0.10f).Within(0.015f));
                Assert.That((min.y + max.y) * 0.5f, Is.EqualTo(0.24f).Within(0.015f));
            }

            Assert.That(Attach(deck), Is.SameAs(anchor));
            Call(anchor, "Clear");
            original.AssertMatches(deck.transform);
        }

        [UnityTest]
        public IEnumerator DisablingTheAnchorDuringExternalPickupRestoresTheOriginalDeckPose()
        {
            var deck = CreateDeck();
            var original = new LocalPose(deck.transform);
            var anchor = Attach(deck);
            host.StartCoroutine((IEnumerator)Call(anchor, "Pickup"));
            yield return null;
            ((Behaviour)anchor).enabled = false;
            yield return null;
            original.AssertMatches(deck.transform);
            yield return new WaitForSeconds(1.2f);
            original.AssertMatches(deck.transform);
        }

        [Test]
        public void ShufflePresentationOptInOverridesOldSerializedTimingWithoutChangingOtherDecks()
        {
            var deck = CreateDeck();
            JsonUtility.FromJsonOverwrite("{\"anticipationSeconds\":0.25,\"cutSpread\":0.42,\"cutSeconds\":0.72,\"recutSeconds\":0.52,\"riffleLift\":0.055,\"riffleCardSeconds\":0.26,\"riffleStagger\":0.088,\"settleSeconds\":0.22,\"squareHoldSeconds\":0.55}", deck);
            var legacyDeck = CreateDeck();
            var legacySeconds = legacyDeck.PlannedSeconds;
            var anchor = Attach(deck);
            Call(deck, ApplyPresentation);
            Assert.That(deck.PlannedSeconds + Property<float>(anchor, "PickupSeconds"), Is.InRange(6.2f, 7.2f),
                "pickup and the original two-riffle shuffle together take about 6.7 seconds");
            Assert.That(Field<float>(deck, "cutSpread"), Is.GreaterThanOrEqualTo(0.65f), "packets visibly separate");
            Assert.That(Field<float>(deck, "riffleLift"), Is.GreaterThanOrEqualTo(0.12f), "packets lift clearly off the cloth");
            var appliedSeconds = deck.PlannedSeconds;
            Call(deck, ApplyPresentation);
            Assert.That(deck.PlannedSeconds, Is.EqualTo(appliedSeconds).Within(0.0001f));
            Assert.That(legacyDeck.PlannedSeconds, Is.EqualTo(legacySeconds).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator OptedInShuffleStillCutsTwiceAndRestoresEveryAuthoredCardPose()
        {
            var deck = CreateDeck();
            Call(deck, ApplyPresentation);
            SetField(deck, "cameraChoreography", choreography);
            var cards = deck.transform.Cast<Transform>().ToArray();
            var poses = cards.Select(c => new LocalPose(c)).ToArray();
            var rootPose = new LocalPose(deck.transform);
            var cutCount = 0;
            var separated = false;
            var maxLift = 0f;
            var began = Time.time;
            deck.Play();
            while (deck.IsPlaying && Time.time - began < 9f)
            {
                var width = cards.Max(c => c.localPosition.x) - cards.Min(c => c.localPosition.x);
                if (!separated && width > 1.1f)
                {
                    cutCount++;
                    separated = true;
                }
                else if (separated && width < 0.4f)
                {
                    separated = false;
                }

                maxLift = Mathf.Max(maxLift, cards.Select((c, i) => c.localPosition.y - poses[i].Position.y).Max());
                yield return null;
            }

            Assert.That(deck.IsPlaying, Is.False);
            Assert.That(cutCount, Is.EqualTo(2), "keep the two cut/interleave passes, not a single shake");
            Assert.That(maxLift, Is.GreaterThan(0.07f));
            Assert.That(Time.time - began, Is.EqualTo(deck.PlannedSeconds).Within(0.6f));
            rootPose.AssertMatches(deck.transform);
            for (var i = 0; i < cards.Length; i++)
            {
                poses[i].AssertMatches(cards[i]);
            }
        }

        [Test]
        public void FlipPresentationOptInOverridesSerializedTurnAndRestrainsSheenWithoutChangingLegacyDefaults()
        {
            var flip = Child("OptedInFlip").AddComponent<CardFlipController>();
            JsonUtility.FromJsonOverwrite("{\"flipDuration\":0.62,\"sheenIntensity\":0.85}", flip);
            var legacy = Child("LegacyFlip").AddComponent<CardFlipController>();
            var legacyTurn = Field<float>(legacy, "flipDuration");
            var legacySheen = Field<float>(legacy, "sheenIntensity");
            Call(flip, ApplyPresentation);
            Assert.That(Field<float>(flip, "flipDuration"), Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(Field<float>(flip, "sheenIntensity"), Is.InRange(0f, 0.25f), "no overstrong reveal sheen");
            Call(flip, ApplyPresentation);
            Assert.That(Field<float>(flip, "flipDuration"), Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(Field<float>(legacy, "flipDuration"), Is.EqualTo(legacyTurn));
            Assert.That(Field<float>(legacy, "sheenIntensity"), Is.EqualTo(legacySheen));
        }

        [UnityTest]
        public IEnumerator OptedInFlipActuallyUsesTheLongerTurnAndRestoresTheCardsLocalPose()
        {
            var card = CreateCard(1, Child("AuthoredFlipParent").transform);
            var data = card.DrawData;
            var flip = card.gameObject.AddComponent<CardFlipController>();
            Call(flip, ApplyPresentation);
            // Isolate the turn's timing from lift, inspection and landing beats.
            JsonUtility.FromJsonOverwrite("{\"anticipationPause\":0.02,\"faceRevealPause\":0.01,\"settleSeconds\":0.01,\"setDownSquashSeconds\":0.01,\"cameraPunchEnabled\":false}", flip);
            SetField(flip, "ritualFeedback", Child("LocalFeedback").AddComponent<RitualFeedbackController>());
            var original = new LocalPose(card.transform);
            var revealedAt = -1f;
            var began = Time.time;
            var maxTurn = 0f;
            card.FaceChanged += (_, faceUp) => { if (faceUp) revealedAt = Time.time; };
            var routine = flip.FlipRoutine(card, true);
            while (routine.MoveNext())
            {
                Assert.That(Time.time - began, Is.LessThan(2f), "the flip must release its state");
                maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(card.transform.localRotation, original.Rotation));
                yield return routine.Current;
            }

            Assert.That(revealedAt - began, Is.InRange(0.55f, 0.85f), "the face swaps near the middle of a 1.2-second turn");
            Assert.That(Time.time - began, Is.InRange(1.20f, 1.60f), "the longer duration must drive actual motion");
            Assert.That(maxTurn, Is.GreaterThan(75f), "the physical card still turns over its edge");
            Assert.That(card.IsFaceUp, Is.True);
            Assert.That(card.DrawData, Is.SameAs(data));
            Assert.That(card.ShowsReversed, Is.True);
            Assert.That(flip.IsFlipping, Is.False);
            original.AssertMatches(card.transform);
        }

        private IEnumerator CancelSettle(float afterSeconds, bool disable)
        {
            var staging = CreateStaging(3);
            var slots = Slots(staging, 3);
            var cards = CreateStagedCards(staging, 3);
            var starts = cards.Select(c => new LocalPose(c.transform)).ToArray();
            var unrelated = CreateCard(7, cards[0].transform.parent);
            var unrelatedPose = new LocalPose(unrelated.transform);
            host.StartCoroutine(Settle(staging, cards, FinalSlots(3)));
            yield return new WaitForSeconds(afterSeconds);
            if (!disable)
            {
                Assert.That(Vector3.Distance(cards[0].transform.localPosition, starts[0].Position), Is.GreaterThan(0.01f),
                    "control: Clear interrupts an actual transfer, not just the hold");
                Call(staging, "Clear");
            }
            else
            {
                ((Behaviour)staging).enabled = false;
            }

            yield return null;
            AssertDestroyed(slots);
            Assert.That(Property<int>(staging, "SlotCount"), Is.Zero);
            for (var i = 0; i < cards.Count; i++)
            {
                starts[i].AssertMatches(cards[i].transform);
            }

            unrelatedPose.AssertMatches(unrelated.transform);
            var nextPose = cards[0].transform.localPosition + Vector3.right * 0.25f;
            cards[0].transform.localPosition = nextPose;
            yield return new WaitForSeconds(2.1f);
            AssertVector(cards[0].transform.localPosition, nextPose, "cancelled external routines cannot overwrite later owners");
            for (var i = 1; i < cards.Count; i++)
            {
                starts[i].AssertMatches(cards[i].transform);
            }

            unrelatedPose.AssertMatches(unrelated.transform);
        }

        private Component CreateStaging(int count)
        {
            var staging = Child("StagingHost").AddComponent(RequireType(StagingTypeName));
            Call(staging, "Prepare", count, viewCamera);
            return staging;
        }

        private DeckShuffleChoreographer CreateDeck()
        {
            var parent = Child("AuthoredDeckParent").transform;
            parent.localPosition = new Vector3(0.2f, 0f, 0.3f);
            parent.localRotation = Quaternion.Euler(0f, 8f, 0f);
            var deck = Child("AuthoredDeck", parent).AddComponent<DeckShuffleChoreographer>();
            deck.transform.localPosition = new Vector3(-2.4f, 0.35f, 1.5f);
            deck.transform.localRotation = Quaternion.Euler(0f, -12f, 0f);
            deck.transform.localScale = new Vector3(0.9f, 1.1f, 0.95f);
            for (var i = 0; i < 9; i++)
            {
                var card = Child("PhysicalDeckCard_" + i, deck.transform).transform;
                card.localPosition = new Vector3((i % 3 - 1) * 0.006f, i * 0.023f, i * 0.001f);
                card.localRotation = Quaternion.Euler(0f, i - 4f, 0f);
                card.localScale = new Vector3(0.8f, 0.02f, 1.18f);
            }

            return deck;
        }

        private Component Attach(DeckShuffleChoreographer deck)
        {
            var type = RequireType(AnchorTypeName);
            var method = type.GetMethod("Attach", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(DeckShuffleChoreographer), typeof(CameraChoreographyController) }, null);
            Assert.That(method, Is.Not.Null, "missing RitualDeckAnchor.Attach(deck, camera)");
            var anchor = method.Invoke(null, new object[] { deck, choreography }) as Component;
            Assert.That(anchor, Is.Not.Null);
            return anchor;
        }

        private List<CardView> CreateStagedCards(Component staging, int count)
        {
            var parent = Child("PickedCards").transform;
            parent.localPosition = new Vector3(0.4f, 0.1f, 0.2f);
            parent.localRotation = Quaternion.Euler(0f, 17f, 0f);
            parent.localScale = Vector3.one * 1.1f;
            var cards = new List<CardView>();
            for (var i = 0; i < count; i++)
            {
                var card = CreateCard(i, parent);
                var slot = Slot(staging, i);
                card.transform.SetPositionAndRotation(slot.position, slot.rotation);
                cards.Add(card);
            }

            return cards;
        }

        private CardView CreateCard(int index, Transform parent)
        {
            var card = Child("SelectedCard_" + index, parent).AddComponent<CardView>();
            card.transform.localPosition = new Vector3(index * 0.2f, 0.2f, 0.4f);
            card.transform.localRotation = Quaternion.Euler(0f, 9f + index, 0f);
            card.transform.localScale = new Vector3(0.62f + index * 0.01f, 0.62f, 1f);
            card.Bind(new CardDrawData { id = 100 + index, position = index + 1, is_reversed = index % 2 == 1 });
            return card;
        }

        private List<Transform> FinalSlots(int count)
        {
            var parent = Child("FinalSpread").transform;
            parent.SetPositionAndRotation(new Vector3(0.3f, 0.12f, -2f), Quaternion.Euler(0f, -11f, 0f));
            var slots = new List<Transform>();
            for (var i = 0; i < count; i++)
            {
                var slot = Child("FinalSlot_" + i, parent).transform;
                slot.localPosition = new Vector3((i % 5 - 2) * 0.9f, 0f, (i / 5) * 1.3f);
                slot.localRotation = Quaternion.Euler(0f, i == 1 ? 90f : i * 3f, 0f);
                slots.Add(slot);
            }

            return slots;
        }

        private GameObject Child(string name, Transform parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent != null ? parent : rig.transform, false);
            return child;
        }

        private static Type RequireType(string name)
        {
            var type = typeof(DeckShuffleChoreographer).Assembly.GetType(name);
            Assert.That(type, Is.Not.Null, "Task 3 presentation component is not implemented yet: " + name);
            return type;
        }

        private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters)
        {
            var method = type.GetMethod(name, PublicInstance, null, parameters, null);
            Assert.That(method, Is.Not.Null, "Missing public coordinator contract: " + type.Name + "." + name);
            return method;
        }

        private static void RequireProperty(Type type, string name, Type expectedType)
        {
            var property = type.GetProperty(name, PublicInstance);
            Assert.That(property, Is.Not.Null, "Missing observable presentation state: " + type.Name + "." + name);
            Assert.That(property.PropertyType, Is.EqualTo(expectedType));
            Assert.That(property.GetGetMethod(), Is.Not.Null);
        }

        private static EventInfo RequireTransferEvent(Type type)
        {
            var transfer = type.GetEvent("TransferStarted", PublicInstance);
            Assert.That(transfer, Is.Not.Null, "the coordinator needs a single post-hold transfer notification");
            Assert.That(transfer.EventHandlerType, Is.EqualTo(typeof(Action)));
            return transfer;
        }

        private static void SubscribeToTransfer(Component staging, Action handler)
        {
            RequireTransferEvent(staging.GetType()).AddEventHandler(staging, handler);
        }

        private static T Property<T>(Component target, string name)
        {
            RequireProperty(target.GetType(), name, typeof(T));
            return (T)target.GetType().GetProperty(name, PublicInstance).GetValue(target);
        }

        private static object Call(Component target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, PublicInstance);
            Assert.That(method, Is.Not.Null, "Missing presentation operation: " + target.GetType().Name + "." + name);
            return method.Invoke(target, args);
        }

        private static T Field<T>(Component target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing existing serialized field: " + name);
            return (T)field.GetValue(target);
        }

        private static void SetField(Component target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing existing dependency field: " + name);
            field.SetValue(target, value);
        }

        private static Transform Slot(Component staging, int index) => (Transform)Call(staging, "Slot", index);

        private static Transform[] Slots(Component staging, int count) => Enumerable.Range(0, count).Select(i => Slot(staging, i)).ToArray();

        private static IEnumerator Settle(Component staging, List<CardView> cards, List<Transform> slots) =>
            (IEnumerator)Call(staging, "Settle", cards, slots);

        private static IEnumerator Track(IEnumerator routine, Action completed)
        {
            yield return routine;
            completed();
        }

        private static void AssertDestroyed(IEnumerable<Transform> slots)
        {
            Assert.That(slots.All(s => s == null), Is.True, "temporary staging slots must not survive cleanup");
        }

        private static void AssertVector(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0005f), message);
        }

        private static void AssertRotation(Quaternion actual, Quaternion expected)
        {
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.05f), "restore the authored rotation exactly");
        }

        private readonly struct LocalPose
        {
            public readonly Transform Parent;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public LocalPose(Transform transform)
            {
                Parent = transform.parent;
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }

            public void AssertMatches(Transform transform)
            {
                Assert.That(transform.parent, Is.SameAs(Parent), "presentation must preserve authored parenting");
                AssertVector(transform.localPosition, Position, "restore the authored local position exactly");
                AssertRotation(transform.localRotation, Rotation);
                AssertVector(transform.localScale, Scale, "restore the authored local scale exactly");
            }
        }
    }

    public sealed class RitualMotionCoroutineHost : MonoBehaviour { }
}
