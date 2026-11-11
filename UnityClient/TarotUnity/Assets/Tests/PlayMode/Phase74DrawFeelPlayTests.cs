using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 74: the flip turns the card over its long edge (it used to spin flat on the table),
    /// the last pick's flight carries the camera down to the spread, and nothing of the fan shows
    /// while the deck is shuffled.
    /// </summary>
    public sealed class Phase74DrawFeelPlayTests
    {
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";

        [UnityTest]
        public IEnumerator TheFlipTurnsTheCardOverItsLongEdgeAndSweepsTheFace()
        {
#if !UNITY_EDITOR
            yield break;
#else
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath));
            var card = go.GetComponent<CardView>();
            var flip = go.GetComponent<CardFlipController>();
            card.SetFaceUp(false);
            var picture = new Texture2D(57, 100);
            card.SetFaceArtwork(Sprite.Create(picture, new Rect(0, 0, 57, 100), new Vector2(0.5f, 0.5f), 100f));
            var t = go.transform;
            var body = go.GetComponentsInChildren<Renderer>(true).First(r => r.name == "Phase15_CardBody");
            var sheen = (Renderer)new SerializedObject(flip).FindProperty("revealSheen").objectReferenceValue;
            var restPosition = t.localPosition;
            var restBottom = body.bounds.min.y;

            var maxTurn = 0f;
            var maxYaw = 0f;
            var turnAtSwap = -1f;
            var lowest = float.MaxValue;
            var sheenShown = false;
            var sheenSpill = 0f;
            var routine = flip.FlipRoutine(card, true);
            flip.StartCoroutine(routine);
            yield return null;
            while (flip.IsFlipping)
            {
                // How far the card's face normal leans off vertical: 90 = standing on its edge.
                var turn = Vector3.Angle(t.up, Vector3.up);
                maxTurn = Mathf.Max(maxTurn, turn);
                var flatForward = Vector3.ProjectOnPlane(t.forward, Vector3.up);
                if (flatForward.sqrMagnitude > 0.01f)
                {
                    maxYaw = Mathf.Max(maxYaw, Vector3.Angle(flatForward, Vector3.forward));
                }

                if (turnAtSwap < 0f && card.IsFaceUp)
                {
                    turnAtSwap = turn;
                }

                lowest = Mathf.Min(lowest, body.bounds.min.y);
                if (sheen.enabled)
                {
                    sheenShown = true;
                    var art = card.FaceArtwork.bounds;
                    var light = sheen.bounds;
                    sheenSpill = Mathf.Max(sheenSpill,
                        Mathf.Max(art.min.x - light.min.x, light.max.x - art.max.x, art.min.z - light.min.z, light.max.z - art.max.z));
                }
                yield return null;
            }

            Assert.That(maxTurn, Is.GreaterThan(75f), "the card turns up onto its edge");
            Assert.That(turnAtSwap, Is.GreaterThan(60f), "the face swaps while the card is near edge-on, not while it lies flat");
            Assert.That(maxYaw, Is.LessThan(20f), "it turns over - it no longer spins flat on the table");
            Assert.That(lowest, Is.GreaterThan(restBottom - 0.03f), "it never sinks through the cloth");
            Assert.That(sheenShown, Is.True, "a light sweeps the face");
            Assert.That(sheenSpill, Is.LessThan(0.01f), "the light lies over the picture, not over the card body around it");
            yield return new WaitForSeconds(0.7f);
            Assert.That(sheen.enabled, Is.False, "the sweep ends");
            Assert.That(Vector3.Distance(t.localPosition, restPosition), Is.LessThan(0.0005f), "back on its slot exactly");
            Assert.That(Quaternion.Angle(t.localRotation, Quaternion.identity), Is.LessThan(0.05f));
            Assert.That(card.IsFaceUp, Is.True);
            Object.Destroy(go);
            Object.Destroy(picture);
#endif
        }

        [UnityTest]
        public IEnumerator TheHeldSelectionCarriesTheCameraDownDuringItsFinalTransfer()
        {
            yield return LoadRoom();
            // Keep this regression on the authored legacy journey. The v6 common
            // table shot is covered separately by RitualTableRenderingTests.
            var choreography = Object.FindFirstObjectByType<CameraChoreographyController>();
            while (choreography.IsMoving) yield return null;
            var tableField = typeof(CameraChoreographyController).GetField("ritualTablePose", BindingFlags.Instance | BindingFlags.NonPublic);
            var table = (Transform)tableField.GetValue(choreography);
            tableField.SetValue(choreography, null);
            if (table != null) Object.Destroy(table.gameObject);
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).OrderByDescending(c => c.depth).First().transform;
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            while (!fan.AcceptingPicks)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1f);
            var drawPosition = camera.position;
            var flightStarted = -1f;
            var landed = -1f;
            var landedPosition = Vector3.zero;
            var transferStarted = -1f;
            var transferStartPosition = Vector3.zero;
            var duringHoldTravel = 0f;
            var duringTransferTravel = 0f;
            var staging = Object.FindFirstObjectByType<PickedCardStaging>();
            staging.TransferStarted += () =>
            {
                transferStarted = Time.time;
                transferStartPosition = camera.position;
            };
            deck.CardFlightStarted += (_, _) => flightStarted = Time.time;
            deck.CardDealt += _ =>
            {
                landed = Time.time;
                landedPosition = camera.position;
            };
            fan.RequestPick(fan.FanCards[fan.FanCards.Count - 1]);
            while (flow.State != ReadingFlowState.WaitingForFlip)
            {
                if (landed >= 0f && transferStarted < 0f)
                    duringHoldTravel = Mathf.Max(duringHoldTravel, Vector3.Distance(landedPosition, camera.position));
                if (transferStarted >= 0f && Time.time - transferStarted < staging.TransferSeconds - 0.05f)
                    duringTransferTravel = Mathf.Max(duringTransferTravel, Vector3.Distance(transferStartPosition, camera.position));
                yield return null;
            }

            yield return new WaitForSeconds(1.5f);
            var spreadPosition = camera.position;
            var total = Vector3.Distance(drawPosition, spreadPosition);
            Assert.That(total, Is.GreaterThan(3f), "control: the spread pose is far from the draw pose");
            Assert.That(landed - flightStarted, Is.GreaterThan(0.7f), "a long flight is unhurried");
            Assert.That(Vector3.Distance(drawPosition, landedPosition) / total, Is.LessThan(0.05f),
                "the camera holds the upper selection while the first flight lands");
            Assert.That(transferStarted - landed, Is.GreaterThanOrEqualTo(1.2f), "the complete selection is held before transfer");
            Assert.That(duringHoldTravel / total, Is.LessThan(0.05f), "the camera stays still throughout the selection hold");
            Assert.That(duringTransferTravel / total, Is.GreaterThan(0.25f),
                "the camera travels with the final transfer, not after it");
            Assert.That(fan.FanCards.Count, Is.EqualTo(0), "the rest of the fan has gathered");
        }

        [UnityTest]
        public IEnumerator NothingOfTheFanShowsWhileTheDeckIsShuffled()
        {
            yield return LoadRoom();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            var shuffle = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var deadline = Time.realtimeSinceStartup + 15f;
            while (!shuffle.IsPlaying)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the same deck finishes its pickup before shuffling");
                Assert.That(fan.GetComponentsInChildren<Renderer>().Count(r => r.enabled), Is.EqualTo(0));
                yield return null;
            }
            var sawPrepared = 0;
            while (shuffle.IsPlaying)
            {
                sawPrepared = Mathf.Max(sawPrepared, fan.PreparedCount);
                var visible = fan.GetComponentsInChildren<Renderer>().Count(r => r.enabled);
                Assert.That(visible, Is.EqualTo(0), "no fan card draws during the shuffle");
                yield return null;
            }

            Assert.That(sawPrepared, Is.GreaterThan(0), "control: the fan was being prepared");
        }

        private static IEnumerator LoadRoom()
        {
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            GameObject.Find("ReadingRoomCanvas").GetComponentInChildren<TMP_InputField>().text = "What should I focus on?";
        }
    }
}
