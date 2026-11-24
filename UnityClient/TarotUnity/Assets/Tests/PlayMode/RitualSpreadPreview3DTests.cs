using System;
using System.Collections;
using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualSpreadPreview3DTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded && !active.name.StartsWith("SpreadPreview3DCleanup"))
            {
                var cleanup = SceneManager.CreateScene($"SpreadPreview3DCleanup_{Guid.NewGuid():N}");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(active);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpreadStageShowsFull3DFloatingFormationsAndHidesOnQuestionStage()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(0.7f);

            var preview = Object.FindFirstObjectByType<RitualSpreadPreview3D>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.IsReady, Is.True);
            Assert.That(preview.AreCardsVisible, Is.True);
            Assert.That(preview.CardCountForSpread(0), Is.EqualTo(1));
            Assert.That(preview.CardCountForSpread(1), Is.EqualTo(3));
            Assert.That(preview.CardCountForSpread(2), Is.EqualTo(10), "Celtic Cross preview renders all 10 cards in 3D.");
            Assert.That(preview.TotalCardCount, Is.EqualTo(14));

            // Flat 2D RawImage stickers are hidden via CanvasGroup while preserving layout & texture contracts.
            var canvas = GameObject.Find("ReadingRoomCanvas").transform;
            var symbol = canvas.Find("OneCardButton/RitualSpreadSymbol");
            Assert.That(symbol.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));

            // Levitation visibly animates position over time.
            var initialPos = preview.GetFormationWorldPosition(1);
            yield return new WaitForSecondsRealtime(0.25f);
            var nextPos = preview.GetFormationWorldPosition(1);
            Assert.That(Vector3.Distance(initialPos, nextPos), Is.GreaterThan(0.0005f), "3D spread formations should levitate gently.");

            // Hovering ThreeCardButton lifts the 3D formation closer toward the camera & upward.
            var threeButton = canvas.Find("ThreeCardButton").gameObject;
            var feedback = threeButton.GetComponent<RitualControlFeedback>();
            Assert.That(feedback, Is.Not.Null);
            var beforeHoverY = preview.GetFormationWorldPosition(1).y;
            feedback.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return new WaitForSecondsRealtime(0.45f);
            var hoveredY = preview.GetFormationWorldPosition(1).y;
            Assert.That(hoveredY, Is.GreaterThan(beforeHoverY + 0.005f), "Hovering a spread should visibly lift its 3D formation.");

            // Selecting a spread transitions to Question stage and hides the 3D spread preview.
            threeButton.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(preview.AreCardsVisible, Is.False);

            // Returning via 更换牌阵 brings the 3D formations back cleanly.
            canvas.Find("RitualChangeSpread").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(preview.AreCardsVisible, Is.True);
        }
    }
}
