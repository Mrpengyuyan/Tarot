using System;
using System.Collections;
using System.Collections.Generic;
using TarotUnity.Core;
using TarotUnity.Presentation;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>
    /// Phase 72: after the shuffle the deck spreads out on the table and the player picks the
    /// cards. The fan owns its props - face-down PF_TarotCard instances with no draw bound - and
    /// their motion: the spread, the hover, the pick queue (one pick waits while a card is in
    /// flight; further clicks are dropped), and the gather back into the deck. A picked card
    /// leaves the fan and becomes the card the player later flips; which card it is is decided
    /// by the backend or the local simulator, never by where the player clicked.
    /// Phase 73: the whole deck (78 cards) in concentric arcs around a pivot on the player's
    /// side, and a hover that picks the card up - it springs up, tips toward the camera and
    /// grows a little while its shadow stays on the cloth and spreads - and parts its
    /// neighbours along the arc. The tilt and growth apply to a pivot the card's visuals hang
    /// from while it is in the fan, never to the root that carries its pointer target, so a tipped
    /// card cannot rise over its neighbours' strips and steal the pointer.
    /// </summary>
    public sealed partial class SpreadFanController : MonoBehaviour
    {
        [Serializable]
        public sealed class FanRow
        {
            public int count = 39;
            public float radius = 4.5f;
            public float arcDegrees = 96f;
        }

        [SerializeField] private CardView cardPrefab;
        [Tooltip("The arcs' shared pivot, on the player's side of the table. Forward points toward the slots.")]
        [SerializeField] private Transform fanCenter;

        [Header("Arcs")]
        [Tooltip("Nearest the player first. Each row spans its own angle at its own radius around the pivot.")]
        [SerializeField] private FanRow[] rows =
        {
            new FanRow { count = 39, radius = 4.5f, arcDegrees = 96f },
            new FanRow { count = 39, radius = 5.9f, arcDegrees = 78f },
        };
        [Tooltip("Each card lies this much above the one to its left, so a row layers left to right.")]
        [SerializeField] private float layerStep = 0.002f;

        [Header("Spread")]
        [Tooltip("Cards made per frame ahead of the spread (Prepare), out of sight under the cloth.")]
        [SerializeField] private int prepareBatch = 10;
        [Tooltip("Delay between neighbouring cards of a row; the rows spread together.")]
        [SerializeField] private float spreadStagger = 0.028f;
        [SerializeField] private float spreadCardSeconds = 0.3f;
        [SerializeField] private float spreadArcHeight = 0.12f;
        [SerializeField] private float settleSquash = 0.05f;
        [SerializeField] private float settleSeconds = 0.12f;

        [Header("Hover: pick up and part")]
        [SerializeField] private float hoverLift = 0.12f;
        [Tooltip("How far the hovered card comes out toward the player.")]
        [SerializeField] private float hoverSlide = 0.18f;
        [Tooltip("How far the hovered card tips its far edge up, toward the camera.")]
        [SerializeField] private float hoverTiltDegrees = 14f;
        [SerializeField] private float hoverScale = 1.06f;
        [Tooltip("How far the nearest neighbours slide aside along the arc.")]
        [SerializeField] private float partDistance = 0.14f;
        [Tooltip("How many neighbours on each side make room, easing off with distance.")]
        [SerializeField] private float waveRadius = 3f;
        [Tooltip("How much the hovered card's shadow spreads on the cloth as the card rises.")]
        [SerializeField] private float shadowSpread = 0.25f;
        [Tooltip("Spring frequency (Hz) and damping of the hover; under 1 overshoots a little and settles.")]
        [SerializeField] private float springFrequency = 2.8f;
        [SerializeField] private float springDamping = 0.55f;
        [Tooltip("A pointer exit only drops the card after this long, so a card sliding out from under the pointer does not flicker.")]
        [SerializeField] private float hoverReleaseDelay = 0.12f;
        [Tooltip("Hover glow scale for a fan card: small enough that only a thin warm rim shows past its edges.")]
        [SerializeField] private float fanHoverHaloScale = 0.62f;

        [Header("Gather")]
        [SerializeField] private float gatherSeconds = 0.6f;

        [Header("Light")]
        [Tooltip("The fan lies nearer the player than the table's light pool, so it brings its own: faded in with the spread, out with the gather.")]
        [SerializeField] private Light fanLight;
        [SerializeField] private float fanLightIntensity = 260f;
        [SerializeField] private float fanLightFadeSeconds = 0.4f;

        private sealed class FanCard
        {
            public int index;
            public Transform pivot;
            public readonly List<Transform> visuals = new();
            public bool settled;
            public float hover;
            public float hoverVelocity;
            public float part;
            public float partVelocity;
            public BoxCollider box;
            public Vector3 boxCenter;
            public Transform shadow;
            public Vector3 shadowLocalPosition;
            public Quaternion shadowLocalRotation;
            public Vector3 shadowLocalScale;
            public Vector3 shadowRootOffset;
            public Quaternion shadowRootRotation;
        }

        private readonly List<CardView> fanCards = new();
        private readonly Dictionary<CardView, FanCard> state = new();
        private readonly List<CardView> prepared = new();
        private readonly Stack<CardView> pool = new();
        private readonly List<List<Renderer>> deckLayers = new();
        private Transform capturedDeckOrigin;
        private int lastVisibleDeckLayers = -1;
        private bool preparing;
        private Coroutine lightFade;
        private Vector3 baseScale = Vector3.one;
        private Vector3 prefabScale = Vector3.one;
        private bool prefabScaleCaptured;
        private bool spreadDone;
        private CardView hovered;
        private float hoverReleaseAt = -1f;
        private CardView pending;
        private bool busy;
        private int remainingPicks;

        public event Action<CardView> CardPicked;
        public RitualPickPresentation PickPresentation { get; set; }
        public void RefreshPresentation() { if (isActiveAndEnabled && PickPresentation != null) Update(); }
        public int InstantiatedCount { get; private set; }
        public int PooledCount => pool.Count;

        private void OnEnable() { if (PickPresentation != null) PickPresentation.SetFanVisible(true); }
        private void OnDisable() { if (PickPresentation != null) PickPresentation.SetFanVisible(false); }
        private void OnDestroy()
        {
            Clear();
            DestroyPrepared();
            DestroyPool();
            if (PickPresentation != null) PickPresentation.DetachFan();
        }

        public int CardCount
        {
            get
            {
                var total = 0;
                foreach (var row in rows)
                {
                    total += Mathf.Max(0, row.count);
                }

                return total;
            }
        }

        public int RowCount => rows.Length;

        /// <summary>The widest row's angle.</summary>
        public float ArcDegrees
        {
            get
            {
                var widest = 0f;
                foreach (var row in rows)
                {
                    widest = Mathf.Max(widest, row.arcDegrees);
                }

                return widest;
            }
        }

        public IReadOnlyList<CardView> FanCards => fanCards;
        public bool AcceptingPicks => remainingPicks > 0;
        public float RowArcDegrees(int row) => rows[row].arcDegrees;
        public int PreparedCount => prepared.Count;

        public int RowOf(int index)
        {
            Locate(index, out var row, out _);
            return row;
        }

        /// <summary>Phase 72 review: awake, wired, and holding at least as many cards as the spread needs.</summary>
        public bool CanSpread(int cardsNeeded)
        {
            return isActiveAndEnabled && cardPrefab != null && CardCount >= cardsNeeded;
        }

        /// <summary>Phase 72 review: drop the fan at once (after a fault mid-pick) - no gather, the light off.</summary>
        public void Abandon()
        {
            spreadDone = false;
            busy = false;
            Clear();
            DestroyPrepared();
            DestroyPool();
            SetLight(0f);
            ShowDeck(1f);
        }

        public void GetFanPose(int index, out Vector3 position, out Quaternion rotation)
        {
            PoseOnArc(index, 0f, out position, out rotation);
        }

        public void SetHovered(CardView card, bool on)
        {
            if (card == null || !state.ContainsKey(card))
            {
                return;
            }

            if (on)
            {
                if (hovered != card && spreadDone)
                {
                    AudioManager.Instance?.PlayCue(PresentationCueId.CardHoverRustle, 0.55f);
                }

                hovered = card;
                hoverReleaseAt = -1f;
            }
            else if (hovered == card)
            {
                hoverReleaseAt = Time.time + hoverReleaseDelay;
            }
        }

        public void RequestPick(CardView card)
        {
            if (!isActiveAndEnabled || card == null || !state.ContainsKey(card) || remainingPicks <= 0)
            {
                return;
            }

            if (!busy)
            {
                pending = card;
                return;
            }

            // One pick may wait while a card is in flight; further clicks are dropped.
            if (pending == null && remainingPicks > 1)
            {
                pending = card;
            }
        }

        public IEnumerator PickCards(int count, Func<CardView, int, IEnumerator> deliver)
        {
            remainingPicks = Mathf.Min(count, fanCards.Count);
            pending = null;
            for (var index = 0; index < count && remainingPicks > 0; index++)
            {
                while (pending == null)
                {
                    yield return null;
                }

                var card = pending;
                pending = null;
                busy = true;
                Release(card);
                remainingPicks--;
                CardPicked?.Invoke(card);
                if (deliver != null)
                {
                    yield return deliver(card, index);
                }

                busy = false;
            }

            remainingPicks = 0;
            pending = null;
        }

        /// <summary>Phase 75: how much of the deck on the table is showing, 0 to 1 (1 when there is none to track).</summary>
        public float DeckShowing
        {
            get
            {
                if (deckLayers.Count == 0)
                {
                    return 1f;
                }

                var shown = 0;
                foreach (var layer in deckLayers)
                {
                    if (layer.Count > 0 && layer[0] != null && layer[0].enabled)
                    {
                        shown++;
                    }
                }

                return (float)shown / deckLayers.Count;
            }
        }
    }
}
