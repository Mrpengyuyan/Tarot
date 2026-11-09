using System;
using System.Collections;
using System.Collections.Generic;
using TarotUnity.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarotUnity.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PickedCardStaging : MonoBehaviour
    {
        private const float StagingHeight = 1.1f;
        private readonly List<Transform> slots = new();
        private readonly List<CardPose> ownedCards = new();
        private GameObject slotsRoot;
        private int generation;
        private bool transferStarted;

        /// <summary>Once per prepared selection, after its hold and before any transfer movement.</summary>
        public event Action TransferStarted;

        public int SlotCount => slots.Count;
        public float HoldSeconds => 1.25f;
        public float TransferSeconds => 0.7f;
        public bool IsSettling { get; private set; }
        public Transform StagingRoot => slotsRoot != null ? slotsRoot.transform : null;

        public void Prepare(int count, Camera camera)
        {
            Clear();
            if (count != 1 && count != 3 && count != 10)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Staging supports 1, 3 or 10 selected cards.");
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            slotsRoot = new GameObject("PickedCardStaging_Slots");
            SceneManager.MoveGameObjectToScene(slotsRoot, gameObject.scene);
            var columns = count == 10 ? 5 : count;
            var step = count == 10 ? 0.13f : 0.18f;
            // Raised, viewer-facing physical backs match the upper display without
            // changing card scale. The UI owner's coordinates are not a table pose.
            var plane = new Plane(Vector3.up, Vector3.up * StagingHeight);
            var rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var x = 0.5f + (i % columns - (columns - 1) * 0.5f) * step;
                var y = count == 10 ? 0.83f - row * 0.17f : 0.76f;
                var ray = camera.ViewportPointToRay(new Vector3(x, y, 0f));
                if (!plane.Raycast(ray, out var distance))
                {
                    Clear();
                    throw new InvalidOperationException("The draw camera must face the staging table plane.");
                }

                var slot = new GameObject("PickedSlot_" + i).transform;
                slot.SetParent(slotsRoot.transform, false);
                slot.SetPositionAndRotation(ray.GetPoint(distance), rotation);
                slots.Add(slot);
            }
        }

        public Transform Slot(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return slots[index];
        }

        public IEnumerator Settle(IReadOnlyList<CardView> cards, IList<Transform> finalSlots) =>
            SettleRoutine(cards, finalSlots, generation);

        public void Clear()
        {
            generation++;
            ReleaseCards(false);
            IsSettling = false;
            transferStarted = false;
            slots.Clear();
            if (slotsRoot != null)
            {
                slotsRoot.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(slotsRoot);
                }
                else
                {
                    DestroyImmediate(slotsRoot);
                }

                slotsRoot = null;
            }
        }

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            Clear();
            TransferStarted = null;
        }

        private IEnumerator SettleRoutine(IReadOnlyList<CardView> cards, IList<Transform> finalSlots, int token)
        {
            if (!IsCurrent(token) || IsSettling || transferStarted || !ValidSelection(cards, finalSlots))
            {
                yield break;
            }

            IsSettling = true;
            var destinations = new Transform[cards.Count];
            var targetScales = new Vector3[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                ownedCards.Add(new CardPose(cards[i]));
                destinations[i] = finalSlots[i];
                targetScales[i] = cards[i].transform.localScale;
            }

            var completed = false;
            try
            {
                for (var elapsed = 0f; elapsed < HoldSeconds; elapsed += Time.deltaTime)
                {
                    if (!IsCurrent(token) || !SelectionStillExists(destinations))
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (!IsCurrent(token) || !SelectionStillExists(destinations))
                {
                    yield break;
                }

                transferStarted = true;
                var starts = new CardPose[ownedCards.Count];
                for (var i = 0; i < starts.Length; i++) starts[i] = new CardPose(ownedCards[i].Card, false);
                TransferStarted?.Invoke();
                // A recovery handler may clear or disable staging during notification.
                if (!IsCurrent(token) || !SelectionStillExists(destinations))
                {
                    yield break;
                }

                for (var elapsed = 0f; elapsed < TransferSeconds; elapsed += Time.deltaTime)
                {
                    if (!IsCurrent(token) || !SelectionStillExists(destinations))
                    {
                        yield break;
                    }

                    var t = elapsed / TransferSeconds;
                    var eased = t * t * (3f - 2f * t);
                    for (var i = 0; i < ownedCards.Count; i++)
                    {
                        var pose = starts[i];
                        pose.Card.transform.SetPositionAndRotation(
                            Vector3.Lerp(pose.WorldPosition, destinations[i].position, eased),
                            Quaternion.Slerp(pose.WorldRotation, destinations[i].rotation, eased));
                        var slot = destinations[i].GetComponent<RitualSpreadSlot>();
                        var targetScale = slot != null ? slot.ScaleFor(pose.Card.transform) : targetScales[i];
                        pose.Card.transform.localScale = Vector3.Lerp(pose.Scale, targetScale, eased);
                    }

                    yield return null;
                }

                if (!IsCurrent(token) || !SelectionStillExists(destinations))
                {
                    yield break;
                }

                for (var i = 0; i < ownedCards.Count; i++)
                {
                    ownedCards[i].Card.transform.SetPositionAndRotation(destinations[i].position, destinations[i].rotation);
                    ownedCards[i].Card.transform.localScale = ownedCards[i].Scale;
                    var destination = destinations[i].GetComponent<RitualSpreadSlot>();
                    if (destination != null) destination.Dock(ownedCards[i].Card);
                }

                completed = true;
            }
            finally
            {
                if (this != null && generation == token)
                {
                    ReleaseCards(completed);
                    IsSettling = false;
                }
            }
        }

        private bool IsCurrent(int token) => this != null && generation == token && isActiveAndEnabled;

        private bool ValidSelection(IReadOnlyList<CardView> cards, IList<Transform> finalSlots)
        {
            if (cards == null || finalSlots == null || cards.Count == 0 || cards.Count != slots.Count
                || finalSlots.Count != cards.Count)
            {
                return false;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || finalSlots[i] == null || slots[i] == null)
                {
                    return false;
                }

                for (var j = 0; j < i; j++)
                {
                    if (cards[i] == cards[j] || finalSlots[i] == finalSlots[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool SelectionStillExists(Transform[] destinations)
        {
            if (slotsRoot == null || ownedCards.Count != destinations.Length || slots.Count != destinations.Length)
            {
                return false;
            }

            for (var i = 0; i < destinations.Length; i++)
            {
                if (ownedCards[i].Card == null || destinations[i] == null || slots[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ReleaseCards(bool completed)
        {
            foreach (var pose in ownedCards)
            {
                if (pose.Card == null)
                {
                    continue;
                }

                if (!completed)
                {
                    var card = pose.Card.transform;
                    card.localPosition = pose.Position;
                    card.localRotation = pose.Rotation;
                    card.localScale = pose.Scale;
                }

                pose.ReleaseHover();
            }

            ownedCards.Clear();
        }

        private sealed class CardPose
        {
            public readonly CardView Card;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;
            public readonly Vector3 WorldPosition;
            public readonly Quaternion WorldRotation;
            private readonly CardHoverTiltController hover;
            private readonly bool hoverEnabled;
            private readonly bool hoverSuspended;

            public CardPose(CardView card, bool suspendHover = true)
            {
                Card = card;
                var pose = card.transform;
                Position = pose.localPosition;
                Rotation = pose.localRotation;
                Scale = pose.localScale;
                WorldPosition = pose.position;
                WorldRotation = pose.rotation;
                hover = card.GetComponent<CardHoverTiltController>();
                if (hover != null && suspendHover)
                {
                    hoverEnabled = hover.enabled;
                    hoverSuspended = hover.IsSuspended;
                    hover.enabled = false;
                }
            }

            public void ReleaseHover()
            {
                if (hover == null)
                {
                    return;
                }

                // Re-capture the final slot on the next hover instead of snapping
                // back to an old rail pose; retain the caller's suspension state.
                hover.Resume();
                if (hoverSuspended)
                {
                    hover.Suspend();
                }

                hover.enabled = hoverEnabled;
            }
        }
    }
}
