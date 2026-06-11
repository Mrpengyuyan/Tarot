using TarotUnity.Core;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualActionVfxController : MonoBehaviour
    {
        [SerializeField] private RitualParticleSystemController particleSystemController;
        [SerializeField] private bool playAmbientOnShuffle = true;
        [SerializeField] private bool playAmbientOnDeal = true;
        [SerializeField] private bool burstOnFlip = true;
        [SerializeField] private bool burstOnResult = true;
        [SerializeField] private float shuffleIntensity = 0.76f;
        [SerializeField] private float dealIntensity = 0.66f;
        [SerializeField] private float flipIntensity = 0.95f;
        [SerializeField] private float resultIntensity = 0.72f;

        public PresentationCueId LastCue { get; private set; }
        public Transform LastAnchor { get; private set; }
        public int CueCount { get; private set; }

        public void PlayCue(PresentationCueId cue)
        {
            PlayCue(cue, null);
        }

        public void PlayCue(PresentationCueId cue, Transform anchor)
        {
            LastCue = cue;
            LastAnchor = anchor;
            CueCount++;

            if (particleSystemController == null)
            {
                return;
            }

            switch (cue)
            {
                case PresentationCueId.ShuffleStarted:
                    particleSystemController.SetIntensity(shuffleIntensity);
                    if (playAmbientOnShuffle)
                    {
                        particleSystemController.PlayAmbient();
                    }

                    break;
                case PresentationCueId.CardDealt:
                    particleSystemController.SetIntensity(dealIntensity);
                    if (playAmbientOnDeal)
                    {
                        particleSystemController.PlayAmbient();
                    }

                    break;
                case PresentationCueId.CardFlipped:
                    particleSystemController.SetIntensity(flipIntensity);
                    if (burstOnFlip)
                    {
                        particleSystemController.TriggerRevealBurst();
                    }

                    break;
                case PresentationCueId.ResultReady:
                case PresentationCueId.ResultReveal:
                    particleSystemController.SetIntensity(resultIntensity);
                    particleSystemController.PlayAmbient();
                    if (burstOnResult)
                    {
                        particleSystemController.TriggerRevealBurst();
                    }

                    break;
            }
        }
    }
}
