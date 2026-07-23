using System;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 61: the reading-room step bar (choose spread → write question → draw
    /// → flip → reveal) used to be five static chips with no sense of progress, and
    /// the card sockets sat as inert outlines on the cloth. This drives both off the
    /// <see cref="ReadingFlowController"/> state machine: the chip for the current
    /// step lights gold, the steps behind it read as completed, and once the draw
    /// begins the sockets for the selected spread glow to invite the cards.
    /// </summary>
    public sealed class RitualStepIndicator : MonoBehaviour
    {
        /// <summary>One step chip: its root (for the scale accent), background plate and label.</summary>
        [Serializable]
        public sealed class StepChip
        {
            public RectTransform root;
            public Graphic plate;
            public Graphic label; // UI.Text or TMP_Text — both derive from Graphic.
        }

        [SerializeField] private ReadingFlowController flowController;
        [SerializeField] private StepChip[] chips = Array.Empty<StepChip>();

        [Header("Socket glow — lit for the selected spread at draw time")]
        [SerializeField] private GameObject oneCardSocketGlow;
        [SerializeField] private GameObject[] threeCardSocketGlows = Array.Empty<GameObject>();

        [Header("Chip palette")]
        [SerializeField] private Color upcomingPlate = new Color(0.10f, 0.07f, 0.14f, 0.86f);
        [SerializeField] private Color completedPlate = new Color(0.32f, 0.225f, 0.08f, 0.95f);
        [SerializeField] private Color currentPlate = new Color(0.76f, 0.56f, 0.18f, 0.98f);
        [SerializeField] private Color upcomingLabel = new Color(0.55f, 0.50f, 0.42f, 1f);
        [SerializeField] private Color completedLabel = new Color(0.93f, 0.80f, 0.48f, 1f);
        [SerializeField] private Color currentLabel = new Color(1f, 0.97f, 0.88f, 1f);
        [SerializeField] private float currentScale = 1.07f;

        [Header("Socket glow breathing")]
        [SerializeField] private float glowPulseSpeed = 2.2f;
        [SerializeField] private float glowScaleMin = 0.94f;
        [SerializeField] private float glowScaleMax = 1.08f;

        private int currentStep = -1;
        private bool socketsLit;
        private Vector3 oneCardGlowBaseScale = Vector3.one;
        private Vector3[] threeCardGlowBaseScales = Array.Empty<Vector3>();

        /// <summary>The highlighted step (0..4), or -1 before the ritual begins.</summary>
        public int CurrentStep => currentStep;

        /// <summary>Maps a flow state onto the step-bar index; -1 means "leave the bar as-is".</summary>
        public static int StepForState(ReadingFlowState state)
        {
            switch (state)
            {
                case ReadingFlowState.SpreadSelect: return 0;
                case ReadingFlowState.QuestionInput:
                case ReadingFlowState.ReadyToDraw: return 1;
                case ReadingFlowState.Shuffling:
                case ReadingFlowState.Drawing: return 2;
                case ReadingFlowState.WaitingForFlip: return 3;
                case ReadingFlowState.ResultReady: return 4;
                default: return -1; // MainMenu / Error — nothing to advance.
            }
        }

        private void Awake()
        {
            if (flowController == null)
            {
                flowController = FindFirstObjectByType<ReadingFlowController>();
            }

            CacheGlowScales();
        }

        private void OnEnable()
        {
            if (flowController != null)
            {
                flowController.StateChanged += ApplyFlowState;
                ApplyFlowState(flowController.State);
            }
            else
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (flowController != null)
            {
                flowController.StateChanged -= ApplyFlowState;
            }
        }

        /// <summary>Advance the bar (and socket glow) to match a flow state.</summary>
        public void ApplyFlowState(ReadingFlowState state)
        {
            var step = StepForState(state);
            if (step >= 0)
            {
                currentStep = step;
            }

            // The sockets glow from the draw through the flip — the window in which
            // the empty slots are actually asking for cards.
            SetSocketsLit(step == 2 || step == 3);
            Refresh();
        }

        /// <summary>Direct entry point used by tests and the bootstrapper preview.</summary>
        public void SetStep(int step)
        {
            currentStep = Mathf.Clamp(step, -1, chips.Length - 1);
            Refresh();
        }

        private void Refresh()
        {
            for (var i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                if (chip == null)
                {
                    continue;
                }

                var isCurrent = i == currentStep;
                var plateColor = i < currentStep ? completedPlate : isCurrent ? currentPlate : upcomingPlate;
                var labelColor = i < currentStep ? completedLabel : isCurrent ? currentLabel : upcomingLabel;

                if (chip.plate != null)
                {
                    chip.plate.color = plateColor;
                }

                if (chip.label != null)
                {
                    chip.label.color = labelColor;
                }

                if (chip.root != null)
                {
                    chip.root.localScale = isCurrent ? Vector3.one * currentScale : Vector3.one;
                }
            }
        }

        private void SetSocketsLit(bool lit)
        {
            socketsLit = lit;
            var count = flowController != null ? flowController.SelectedSpreadCardCount : 1;

            if (oneCardSocketGlow != null)
            {
                oneCardSocketGlow.SetActive(lit && count != 3);
            }

            if (threeCardSocketGlows != null)
            {
                foreach (var glow in threeCardSocketGlows)
                {
                    if (glow != null)
                    {
                        glow.SetActive(lit && count == 3);
                    }
                }
            }
        }

        private void Update()
        {
            if (!socketsLit)
            {
                return;
            }

            var t = Mathf.Lerp(glowScaleMin, glowScaleMax,
                0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed));

            if (oneCardSocketGlow != null && oneCardSocketGlow.activeSelf)
            {
                oneCardSocketGlow.transform.localScale = oneCardGlowBaseScale * t;
            }

            if (threeCardSocketGlows != null)
            {
                for (var i = 0; i < threeCardSocketGlows.Length; i++)
                {
                    var glow = threeCardSocketGlows[i];
                    if (glow != null && glow.activeSelf && i < threeCardGlowBaseScales.Length)
                    {
                        glow.transform.localScale = threeCardGlowBaseScales[i] * t;
                    }
                }
            }
        }

        private void CacheGlowScales()
        {
            if (oneCardSocketGlow != null)
            {
                oneCardGlowBaseScale = oneCardSocketGlow.transform.localScale;
            }

            if (threeCardSocketGlows != null)
            {
                threeCardGlowBaseScales = new Vector3[threeCardSocketGlows.Length];
                for (var i = 0; i < threeCardSocketGlows.Length; i++)
                {
                    threeCardGlowBaseScales[i] = threeCardSocketGlows[i] != null
                        ? threeCardSocketGlows[i].transform.localScale
                        : Vector3.one;
                }
            }
        }
    }
}
