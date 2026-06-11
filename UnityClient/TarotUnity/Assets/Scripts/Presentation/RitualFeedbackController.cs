using TarotUnity.Core;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualFeedbackController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem shuffleParticles;
        [SerializeField] private ParticleSystem dealParticles;
        [SerializeField] private ParticleSystem flipParticles;
        [SerializeField] private ParticleSystem resultParticles;
        [SerializeField] private bool moveParticlesToAnchor = true;
        [SerializeField] private RitualActionVfxController actionVfxController;
        [SerializeField] private bool forwardCuesToActionVfx = true;

        public PresentationCueId LastCue { get; private set; }
        public int CueCount { get; private set; }

        public void PlayCue(PresentationCueId cue)
        {
            PlayCue(cue, null);
        }

        public void PlayCue(PresentationCueId cue, Transform anchor)
        {
            LastCue = cue;
            CueCount++;

            AudioManager.Instance?.PlayCue(cue);

            var particles = ResolveParticles(cue);
            if (particles != null)
            {
                if (moveParticlesToAnchor && anchor != null)
                {
                    particles.transform.position = anchor.position;
                }

                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(true);
            }

            if (forwardCuesToActionVfx)
            {
                actionVfxController?.PlayCue(cue, anchor);
            }
        }

        private ParticleSystem ResolveParticles(PresentationCueId cue)
        {
            return cue switch
            {
                PresentationCueId.ShuffleStarted => shuffleParticles,
                PresentationCueId.CardDealt => dealParticles,
                PresentationCueId.CardFlipped => flipParticles,
                PresentationCueId.ResultReady => resultParticles,
                PresentationCueId.ResultReveal => resultParticles,
                _ => null,
            };
        }
    }
}
