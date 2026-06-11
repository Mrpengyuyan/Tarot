using TarotUnity.Core;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class RitualRhythmDirector : MonoBehaviour
    {
        [SerializeField] private float shuffleBreathSeconds = 0.65f;
        [SerializeField] private float dealSettleSeconds = 0.24f;
        [SerializeField] private float resultBreathSeconds = 0.42f;
        [SerializeField] private PresentationCueId[] cueOrder =
        {
            PresentationCueId.ShuffleStarted,
            PresentationCueId.CardDealt,
            PresentationCueId.CardFlipped,
            PresentationCueId.ResultReady,
            PresentationCueId.ResultReveal,
        };

        public float ShuffleBreathSeconds => Mathf.Max(0f, shuffleBreathSeconds);
        public float DealSettleSeconds => Mathf.Max(0f, dealSettleSeconds);
        public float ResultBreathSeconds => Mathf.Max(0f, resultBreathSeconds);
        public System.Array CueOrder => cueOrder;

        public float ResolvePause(PresentationCueId cue)
        {
            return cue switch
            {
                PresentationCueId.ShuffleStarted => ShuffleBreathSeconds,
                PresentationCueId.CardDealt => DealSettleSeconds,
                PresentationCueId.ResultReady => ResultBreathSeconds,
                PresentationCueId.ResultReveal => ResultBreathSeconds,
                _ => 0f,
            };
        }
    }
}
