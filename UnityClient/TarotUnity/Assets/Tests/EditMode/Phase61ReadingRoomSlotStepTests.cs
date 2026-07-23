using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 61: the reading room's card sockets became recessed glowing slots and
    /// the step bar gained a live current-step highlight tied to the flow state.
    /// These guard the state→step mapping, the wired indicator, and the socket-glow
    /// linkage so a refactor cannot quietly return the bar to a dead row of chips.
    /// </summary>
    public sealed class Phase61ReadingRoomSlotStepTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string GlowGroup = "MP_SocketGlows";
        private const string DocPath = "Docs/PHASE61_READING_ROOM_SLOT_STEP.md";

        private static RitualStepIndicator OpenAndFindIndicator()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>();
            Assert.That(indicator, Is.Not.Null, "RitualStepIndicator missing - run the Phase 61 bootstrapper");
            return indicator;
        }

        private static Transform FindGlow(string slotKey)
        {
            // Glow quads start inactive, so GameObject.Find skips them; reach them
            // through the always-active group.
            var group = GameObject.Find(GlowGroup);
            Assert.That(group, Is.Not.Null, "socket glow group not built");
            return group.transform.Find($"MP_SocketGlow_{slotKey}");
        }

        [Test]
        public void StateMapsToTheRightStep()
        {
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.SpreadSelect), Is.EqualTo(0));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.QuestionInput), Is.EqualTo(1));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.ReadyToDraw), Is.EqualTo(1));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.Shuffling), Is.EqualTo(2));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.Drawing), Is.EqualTo(2));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.WaitingForFlip), Is.EqualTo(3));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.ResultReady), Is.EqualTo(4));
            Assert.That(RitualStepIndicator.StepForState(ReadingFlowState.MainMenu), Is.EqualTo(-1));
        }

        [Test]
        public void SocketGlowsAreBuiltForEverySlot()
        {
            OpenAndFindIndicator();
            foreach (var slot in new[] { "OneCardSlot", "PastSlot", "PresentSlot", "AdviceSlot" })
            {
                Assert.That(FindGlow(slot), Is.Not.Null, $"missing socket glow for {slot}");
            }
        }

        [Test]
        public void DrawStepLightsSocketsAndRevealTurnsThemOff()
        {
            var indicator = OpenAndFindIndicator();

            indicator.ApplyFlowState(ReadingFlowState.Shuffling);
            Assert.That(indicator.CurrentStep, Is.EqualTo(2));
            Assert.That(FindGlow("OneCardSlot").gameObject.activeSelf, Is.True,
                "the one-card socket should glow while drawing the default spread");

            indicator.ApplyFlowState(ReadingFlowState.ResultReady);
            Assert.That(indicator.CurrentStep, Is.EqualTo(4));
            Assert.That(FindGlow("OneCardSlot").gameObject.activeSelf, Is.False,
                "sockets should go dark once the reading is ready");
        }

        [Test]
        public void CurrentChipReadsBrighterThanAnUpcomingChip()
        {
            var indicator = OpenAndFindIndicator();
            indicator.SetStep(2); // 抽牌 is current; 解读 is still upcoming.

            var current = PlateBrightness("Phase7_Progress_DrawCards");
            var upcoming = PlateBrightness("Phase7_Progress_RevealResult");
            Assert.That(current, Is.GreaterThan(upcoming),
                "the current step chip should be lit brighter than an upcoming one");
        }

        private static float PlateBrightness(string chipName)
        {
            var chip = GameObject.Find(chipName);
            Assert.That(chip, Is.Not.Null, $"chip {chipName} missing");
            var plate = chip.transform.Find("Plate")?.GetComponent<Graphic>();
            Assert.That(plate, Is.Not.Null, $"chip {chipName} has no Plate graphic");
            var c = plate.color;
            return c.r + c.g + c.b;
        }

        [Test]
        public void Phase61DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase 61 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("socket"));
            Assert.That(text, Does.Contain("step"));
        }
    }
}
