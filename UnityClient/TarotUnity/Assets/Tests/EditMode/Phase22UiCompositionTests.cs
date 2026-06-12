using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase22UiCompositionTests
    {
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string Phase22DocPath = "Docs/PHASE22_UI_COMPOSITION.md";

        // The projected card band in the seated framing spans roughly
        // y = +86 .. -108 in 1280x720 center-anchored canvas coordinates.
        private const float CardBandTop = 95f;
        private const float CardBandBottom = -115f;

        [Test]
        public void InteractiveControlsLiveInTheBottomActionTray()
        {
            var canvas = OpenReadingRoomCanvas();

            foreach (var controlName in new[]
            {
                "QuestionInput", "OneCardButton", "ThreeCardButton", "DrawButton", "RevealResultButton",
            })
            {
                var rect = canvas.transform.Find(controlName)?.GetComponent<RectTransform>();
                Assert.That(rect, Is.Not.Null, $"Missing control {controlName}");
                Assert.That(rect.anchoredPosition.y, Is.LessThanOrEqualTo(-125f),
                    $"{controlName} must sit in the bottom action tray, below the card stage");
            }
        }

        [Test]
        public void ActionTrayControlsDoNotOverlapHorizontally()
        {
            var canvas = OpenReadingRoomCanvas();

            var buttonRow = new[] { "OneCardButton", "ThreeCardButton", "DrawButton", "RevealResultButton" };
            for (var i = 0; i < buttonRow.Length - 1; i++)
            {
                var left = canvas.transform.Find(buttonRow[i])?.GetComponent<RectTransform>();
                var right = canvas.transform.Find(buttonRow[i + 1])?.GetComponent<RectTransform>();
                Assert.That(left, Is.Not.Null);
                Assert.That(right, Is.Not.Null);

                var leftEdge = left.anchoredPosition.x + left.sizeDelta.x * 0.5f;
                var rightEdge = right.anchoredPosition.x - right.sizeDelta.x * 0.5f;
                Assert.That(leftEdge, Is.LessThan(rightEdge),
                    $"{buttonRow[i]} overlaps {buttonRow[i + 1]} in the action tray");
            }
        }

        [Test]
        public void CardStageBandIsClearOfBlockingUi()
        {
            var canvas = OpenReadingRoomCanvas();

            // Setup/flow controls must not cover the card band. Status texts at
            // the top zone are allowed because they sit above the band.
            foreach (var controlName in new[]
            {
                "QuestionInput", "Phase8_QuestionPanelFrame", "OneCardButton", "ThreeCardButton",
                "DrawButton", "RevealResultButton", "Phase11_ActionDock",
            })
            {
                var rect = canvas.transform.Find(controlName)?.GetComponent<RectTransform>();
                Assert.That(rect, Is.Not.Null, $"Missing element {controlName}");

                var topEdge = rect.anchoredPosition.y + rect.sizeDelta.y * 0.5f;
                Assert.That(topEdge, Is.LessThanOrEqualTo(CardBandBottom + 5f),
                    $"{controlName} intrudes into the card stage band");
            }
        }

        [Test]
        public void FlatEraTableOverlaysAreDeactivated()
        {
            var canvas = OpenReadingRoomCanvas();

            foreach (var overlayName in new[]
            {
                "Phase11_TableFocusFrame", "Phase8_CardSlotsGlow", "Phase12_CardFocusVignette",
            })
            {
                var overlay = canvas.transform.Find(overlayName);
                Assert.That(overlay, Is.Not.Null,
                    $"{overlayName} must still exist (deactivated, not deleted)");
                Assert.That(overlay.gameObject.activeSelf, Is.False,
                    $"{overlayName} fakes a flat table over the real 3D table and must stay deactivated");
            }
        }

        [Test]
        public void RevealInstructionMovedToTopInfoZone()
        {
            var canvas = OpenReadingRoomCanvas();

            var instruction = canvas.transform.Find("Phase12_RevealInstruction")?.GetComponent<RectTransform>();
            Assert.That(instruction, Is.Not.Null);
            Assert.That(instruction.anchoredPosition.y, Is.GreaterThanOrEqualTo(CardBandTop),
                "Reveal instruction belongs to the top info zone, above the card band");
        }

        [Test]
        public void DeckStackIsVisibleInSeatedFraming()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var deck = GameObject.Find("DeckStack");
            Assert.That(deck, Is.Not.Null);
            Assert.That(deck.transform.position.x, Is.GreaterThanOrEqualTo(-2.6f),
                "Deck stack must sit inside the seated default framing");
        }

        [Test]
        public void Phase22DocumentationExists()
        {
            Assert.That(File.Exists(Phase22DocPath), Is.True, $"Missing Phase22 doc at {Phase22DocPath}");

            var text = File.ReadAllText(Phase22DocPath);
            Assert.That(text, Does.Contain("UI Composition"));
            Assert.That(text, Does.Contain("action tray"));
            Assert.That(text, Does.Contain("information far, actions near"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static GameObject OpenReadingRoomCanvas()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);
            return canvas;
        }
    }
}
