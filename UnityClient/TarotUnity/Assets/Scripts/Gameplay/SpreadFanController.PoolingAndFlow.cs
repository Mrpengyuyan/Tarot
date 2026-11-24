using System.Collections;
using System.Collections.Generic;
using TarotUnity.Presentation;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed partial class SpreadFanController
    {
        /// <summary>
        /// Phase 73 review: makes the fan's cards a few per frame (the whole deck is about 2,100
        /// objects), out of sight under the cloth, so the spread does not hitch. Runs during the
        /// shuffle; <see cref="Spread"/> waits for it and uses what it made.
        /// </summary>
        public IEnumerator Prepare(Transform origin)
        {
            ReturnPreparedToPool();
            if (cardPrefab == null)
            {
                yield break;
            }

            preparing = true;
            var usePick = PickPresentation != null;
            var batch = usePick ? Mathf.Max(1, Mathf.Min(prepareBatch, 6)) : Mathf.Max(1, prepareBatch);
            var hidden = (origin != null ? origin.position : transform.position) + Vector3.down * 2f;
            for (var i = 0; i < CardCount; i++)
            {
                // Phase 74: face down and switched off until the spread - a fresh prefab shows its
                // face and its back at once, and nothing of it should show through the shuffle.
                var card = AcquireCard(hidden, Quaternion.identity, false);
                var tilt = card.GetComponent<CardHoverTiltController>();
                if (tilt != null)
                {
                    tilt.Suspend();
                }

                if (card.GetComponent<CardClickHandler>() == null)
                {
                    card.gameObject.AddComponent<CardClickHandler>();
                }

                if (usePick)
                {
                    PickPresentation.PrewarmFanCard(card, i);
                }

                prepared.Add(card);
                if ((i + 1) % batch == 0)
                {
                    yield return null;
                }
            }

            preparing = false;
        }

        public IEnumerator Spread(Transform origin)
        {
            Clear();
            spreadDone = false;
            if (cardPrefab == null || origin == null)
            {
                yield break;
            }

            while (preparing)
            {
                yield return null;
            }

            var count = CardCount;
            var usePick = PickPresentation != null;
            var start = origin.position;
            var startRotation = usePick ? PickPresentation.SpreadOriginRotation() : origin.rotation;

            var cards = new CardView[count];
            var trackedCards = new FanCard[count];
            var inRows = new int[count];
            var targetPos = new Vector3[count];
            var targetRot = new Quaternion[count];
            var launched = new bool[count];
            var landed = new bool[count];

            for (var i = 0; i < count; i++)
            {
                var card = i < prepared.Count && prepared[i] != null
                    ? prepared[i]
                    : AcquireCard(start, startRotation, !usePick);
                card.name = $"FanCard_{i:00}";
                card.SetFaceUp(false);
                card.HoverHaloScale = fanHoverHaloScale;
                var tilt = card.GetComponent<CardHoverTiltController>();
                if (tilt != null)
                {
                    tilt.Suspend();
                }

                var click = card.GetComponent<CardClickHandler>();
                if (click == null)
                {
                    click = card.gameObject.AddComponent<CardClickHandler>();
                }

                click.Clicked += RequestPick;
                click.HoverChanged += SetHovered;
                if (usePick)
                {
                    PickPresentation.PrepareFanCard(card, i);
                    PickPresentation.SetFanCardInTransit(card, true);
                }

                var tracked = Track(card, i);
                if (tracked.box != null)
                {
                    tracked.box.enabled = false;
                }

                Locate(i, out _, out inRows[i]);
                GetFanPose(i, out targetPos[i], out targetRot[i]);
                card.transform.SetPositionAndRotation(start, startRotation);

                // In the ritual pick view, activate each card progressively as it peels off the deck
                // (~2 cards/frame) instead of waking all 78 cards and their dynamic finish meshes on frame 0.
                if (usePick)
                {
                    if (card.gameObject.activeSelf)
                    {
                        card.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (!card.gameObject.activeSelf)
                    {
                        card.gameObject.SetActive(true);
                    }
                    launched[i] = true;
                }

                cards[i] = card;
                trackedCards[i] = tracked;
                fanCards.Add(card);
                state[card] = tracked;
            }

            prepared.Clear();

            baseScale = fanCards.Count > 0 ? fanCards[0].transform.localScale : Vector3.one;
            FadeLight(fanLightIntensity);
            CaptureDeck(origin);
            var arcHeight = usePick ? spreadArcHeight * 0.45f : spreadArcHeight;
            var total = spreadCardSeconds + spreadStagger * (LongestRow() - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Mathf.Min(Time.deltaTime, 1f / 15f))
            {
                for (var i = 0; i < count; i++)
                {
                    if (landed[i])
                    {
                        continue;
                    }

                    var local = elapsed - inRows[i] * spreadStagger;
                    if (local < 0f)
                    {
                        continue;
                    }

                    var card = cards[i];
                    if (!launched[i])
                    {
                        launched[i] = true;
                        if (!card.gameObject.activeSelf)
                        {
                            card.gameObject.SetActive(true);
                        }
                    }

                    var k = local / spreadCardSeconds;
                    if (k >= 1f)
                    {
                        landed[i] = true;
                        card.transform.SetPositionAndRotation(targetPos[i], targetRot[i]);
                        if (trackedCards[i].box != null)
                        {
                            trackedCards[i].box.enabled = true;
                        }
                        if (usePick)
                        {
                            PickPresentation.SetFanCardInTransit(card, false);
                        }
                        continue;
                    }

                    var inv = 1f - k;
                    var e = 1f - inv * inv * inv;
                    card.transform.SetPositionAndRotation(
                        Vector3.Lerp(start, targetPos[i], e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arcHeight),
                        Quaternion.Slerp(startRotation, targetRot[i], e));
                }

                ShowDeck(1f - elapsed / total);
                yield return null;
            }

            ShowDeck(0f);
            for (var i = 0; i < count; i++)
            {
                var card = cards[i];
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                }
                card.transform.SetPositionAndRotation(targetPos[i], targetRot[i]);
                if (trackedCards[i].box != null)
                {
                    trackedCards[i].box.enabled = true;
                }
                if (usePick)
                {
                    PickPresentation.SetFanCardInTransit(card, false);
                }
            }

            // Legacy 3D tabletop fan keeps its brief settle squash; the ritual paper fan settles cleanly on landing
            // so all 78 dynamic perimeter/shadow meshes do not rebuild 8 more times in a burst.
            if (!usePick)
            {
                var squashed = new Vector3(baseScale.x * (1f + settleSquash * 0.6f), baseScale.y * (1f - settleSquash), baseScale.z);
                for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
                {
                    var k = elapsed / settleSeconds;
                    var scale = Vector3.Lerp(squashed, baseScale, 1f - (1f - k) * (1f - k));
                    for (var i = 0; i < count; i++)
                    {
                        cards[i].transform.localScale = scale;
                    }

                    yield return null;
                }

                for (var i = 0; i < count; i++)
                {
                    cards[i].transform.localScale = baseScale;
                }
            }

            spreadDone = true;
        }

        public IEnumerator Gather(Transform origin)
        {
            spreadDone = false;
            hovered = null;
            FadeLight(0f);
            var n = fanCards.Count;
            if (origin == null || n == 0)
            {
                Clear();
                ShowDeck(1f);
                yield break;
            }

            var usePick = PickPresentation != null;
            var cards = new CardView[n];
            var starts = new Vector3[n];
            var startRots = new Quaternion[n];
            var delays = new float[n];
            var retired = new bool[n];

            // Each row gathers from both ends toward its middle.
            var mid = (LongestRow() - 1) / 2f;
            var cardSeconds = Mathf.Max(0.01f, gatherSeconds * 0.6f);
            var stagger = mid > 0f ? gatherSeconds * 0.4f / mid : 0f;

            for (var i = 0; i < n; i++)
            {
                var card = fanCards[i];
                var tracked = state[card];
                LetGoOfHover(card, tracked);
                if (tracked.box != null)
                {
                    tracked.box.enabled = false;
                }
                if (usePick)
                {
                    PickPresentation.SetFanCardInTransit(card, true);
                }

                cards[i] = card;
                starts[i] = card.transform.position;
                startRots[i] = card.transform.rotation;
                Locate(tracked.index, out var row, out var inRow);
                var rowMid = (rows[row].count - 1) / 2f;
                var order = rowMid - Mathf.Abs(inRow - rowMid);
                delays[i] = order * stagger;
            }

            var gatherRotation = usePick ? PickPresentation.SpreadOriginRotation() : origin.rotation;
            for (var elapsed = 0f; elapsed < gatherSeconds; elapsed += Mathf.Min(Time.deltaTime, 1f / 15f))
            {
                var targetPos = origin.position;
                for (var i = 0; i < n; i++)
                {
                    if (retired[i])
                    {
                        continue;
                    }

                    var local = elapsed - delays[i];
                    if (local <= 0f)
                    {
                        continue;
                    }

                    var k = local / cardSeconds;
                    if (k >= 1f)
                    {
                        retired[i] = true;
                        if (cards[i].gameObject.activeSelf)
                        {
                            cards[i].gameObject.SetActive(false);
                        }
                        continue;
                    }

                    var e = k * k;
                    if (usePick)
                    {
                        cards[i].transform.SetPositionAndRotation(
                            Vector3.Lerp(starts[i], targetPos, e),
                            Quaternion.Slerp(startRots[i], gatherRotation, e));
                    }
                    else
                    {
                        cards[i].transform.position = Vector3.Lerp(starts[i], targetPos, e);
                    }
                }

                ShowDeck(elapsed / gatherSeconds);
                yield return null;
            }

            Clear(releaseToPool: true);
            ShowDeck(1f);
            SetLight(0f);
        }

        private CardView AcquireCard(Vector3 position, Quaternion rotation, bool active)
        {
            if (!prefabScaleCaptured && cardPrefab != null)
            {
                prefabScale = cardPrefab.transform.localScale;
                prefabScaleCaptured = true;
            }

            CardView card = null;
            while (pool.Count > 0 && card == null)
            {
                card = pool.Pop();
            }

            if (card != null)
            {
                card.transform.SetParent(transform, false);
                card.transform.SetPositionAndRotation(position, rotation);
                card.transform.localScale = prefabScale;
                card.SetFaceUp(false);
                card.gameObject.SetActive(active);
                return card;
            }

            card = Instantiate(cardPrefab, position, rotation, transform);
            InstantiatedCount++;
            card.SetFaceUp(false);
            card.gameObject.SetActive(active);
            return card;
        }

        /// <summary>
        /// Phase 75: the fan is the whole deck, so the deck on the table empties as the fan
        /// spreads - its layers go from the top down as the cards leave - and fills again as
        /// they gather. Remembers which of the deck's renderers were on, and only touches those.
        /// </summary>
        private void CaptureDeck(Transform origin)
        {
            deckLayers.Clear();
            capturedDeckOrigin = origin;
            lastVisibleDeckLayers = -1;
            if (origin == null)
            {
                return;
            }

            var native = origin.GetComponent<RitualDeckPresentation>();
            if (native != null && native.IsReady)
            {
                foreach (var renderer in native.LayerRenderers) deckLayers.Add(new List<Renderer> { renderer });
                return;
            }

            var layers = new List<Transform>();
            foreach (Transform child in origin)
            {
                layers.Add(child);
            }

            layers.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
            foreach (var layer in layers)
            {
                var shown = new List<Renderer>();
                foreach (var renderer in layer.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.enabled)
                    {
                        shown.Add(renderer);
                    }
                }

                deckLayers.Add(shown);
            }
        }

        internal void RefreshDeckVisuals(Transform origin, float fraction)
        {
            if (capturedDeckOrigin != origin) return;
            CaptureDeck(origin);
            ShowDeck(fraction);
        }

        /// <summary>Shows the bottom <paramref name="fraction"/> of the deck's layers.</summary>
        private void ShowDeck(float fraction)
        {
            var visible = Mathf.RoundToInt(deckLayers.Count * Mathf.Clamp01(fraction));
            if (visible == lastVisibleDeckLayers)
            {
                return;
            }

            lastVisibleDeckLayers = visible;
            for (var i = 0; i < deckLayers.Count; i++)
            {
                foreach (var renderer in deckLayers[i])
                {
                    if (renderer != null)
                    {
                        renderer.enabled = i < visible;
                    }
                }
            }
        }

        private FanCard Track(CardView card, int index)
        {
            var tracked = new FanCard { index = index, box = card.GetComponent<BoxCollider>() };
            if (tracked.box != null)
            {
                tracked.boxCenter = tracked.box.center;
            }

            // RitualPickPresentation drives hover lift directly in screen projection without tilting
            // a child pivot or legacy 3D drop shadow, so skip 78x pivot creation and child reparenting.
            if (PickPresentation != null)
            {
                return tracked;
            }

            // The visuals hang from a pivot while the card is in the fan; the root keeps the collider.
            tracked.pivot = new GameObject("FanHoverPivot").transform;
            foreach (Transform child in card.transform)
            {
                tracked.visuals.Add(child);
            }

            tracked.pivot.SetParent(card.transform, false);
            foreach (var child in tracked.visuals)
            {
                child.SetParent(tracked.pivot, false);
            }

            foreach (var t in card.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Phase15_CardDropShadow")
                {
                    tracked.shadow = t;
                    tracked.shadowLocalPosition = t.localPosition;
                    tracked.shadowLocalRotation = t.localRotation;
                    tracked.shadowLocalScale = t.localScale;
                    tracked.shadowRootOffset = card.transform.InverseTransformPoint(t.position);
                    tracked.shadowRootRotation = Quaternion.Inverse(card.transform.rotation) * t.rotation;
                    break;
                }
            }

            return tracked;
        }

        private void FadeLight(float target)
        {
            if (fanLight == null)
            {
                return;
            }

            // An inactive fan cannot run the fade; set the light outright instead of throwing.
            if (!isActiveAndEnabled)
            {
                SetLight(target);
                return;
            }

            if (lightFade != null)
            {
                StopCoroutine(lightFade);
            }

            lightFade = StartCoroutine(FadeLightRoutine(target));
        }

        private IEnumerator FadeLightRoutine(float target)
        {
            var from = fanLight.enabled ? fanLight.intensity : 0f;
            fanLight.enabled = true;
            for (var elapsed = 0f; elapsed < fanLightFadeSeconds; elapsed += Time.deltaTime)
            {
                fanLight.intensity = Mathf.Lerp(from, target, elapsed / fanLightFadeSeconds);
                yield return null;
            }

            SetLight(target);
            lightFade = null;
        }

        private void SetLight(float intensity)
        {
            if (fanLight == null)
            {
                return;
            }

            if (lightFade != null && intensity <= 0f)
            {
                StopCoroutine(lightFade);
                lightFade = null;
            }

            fanLight.intensity = intensity;
            fanLight.enabled = intensity > 0.001f;
        }

        private void Release(CardView card)
        {
            var click = card.GetComponent<CardClickHandler>();
            if (click != null)
            {
                click.Clicked -= RequestPick;
                click.HoverChanged -= SetHovered;
            }

            card.SetHovered(false);
            card.ClearHoverHaloScale();
            if (state.TryGetValue(card, out var tracked))
            {
                if (tracked.box != null)
                {
                    tracked.box.center = tracked.boxCenter;
                    tracked.box.enabled = true;
                }

                RestoreShadow(tracked);

                // The flight starts from the pose the hover left it in: the pivot's tilt and growth
                // move onto the root (DealPickedCard eases them out), and the visuals go back home.
                if (tracked.pivot != null)
                {
                    card.transform.rotation = card.transform.rotation * tracked.pivot.localRotation;
                    card.transform.localScale = Vector3.Scale(card.transform.localScale, tracked.pivot.localScale);
                    foreach (var child in tracked.visuals)
                    {
                        child.SetParent(card.transform, false);
                    }

                    Destroy(tracked.pivot.gameObject);
                }

                state.Remove(card);
            }

            fanCards.Remove(card);
            if (hovered == card)
            {
                hovered = null;
            }
        }

        private void Clear(bool releaseToPool = false)
        {
            foreach (var card in fanCards)
            {
                if (card == null) continue;
                if (releaseToPool) ReturnToPool(card);
                else Destroy(card.gameObject);
            }

            fanCards.Clear();
            state.Clear();
            hovered = null;
            pending = null;
            remainingPicks = 0;
        }

        private void ReturnToPool(CardView card)
        {
            var click = card.GetComponent<CardClickHandler>();
            if (click != null)
            {
                click.Clicked -= RequestPick;
                click.HoverChanged -= SetHovered;
            }

            card.SetHovered(false);
            card.ClearHoverHaloScale();
            if (state.TryGetValue(card, out var tracked))
            {
                if (tracked.box != null)
                {
                    tracked.box.center = tracked.boxCenter;
                    tracked.box.enabled = true;
                }
                RestoreShadow(tracked);
                if (tracked.pivot != null)
                {
                    foreach (var child in tracked.visuals)
                    {
                        if (child != null) child.SetParent(card.transform, false);
                    }
                    Destroy(tracked.pivot.gameObject);
                }
            }

            card.SetFaceUp(false);
            card.transform.SetParent(transform, false);
            card.transform.localScale = prefabScaleCaptured ? prefabScale : Vector3.one;
            if (card.gameObject.activeSelf)
            {
                card.gameObject.SetActive(false);
            }
            pool.Push(card);
        }

        private void ReturnPreparedToPool()
        {
            foreach (var card in prepared)
            {
                if (card == null) continue;
                card.SetFaceUp(false);
                card.transform.SetParent(transform, false);
                card.transform.localScale = prefabScaleCaptured ? prefabScale : Vector3.one;
                card.gameObject.SetActive(false);
                pool.Push(card);
            }

            prepared.Clear();
            preparing = false;
        }

        private void DestroyPrepared()
        {
            foreach (var card in prepared)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            prepared.Clear();
            preparing = false;
        }

        private void DestroyPool()
        {
            while (pool.Count > 0)
            {
                var card = pool.Pop();
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
        }

        private static void RestoreShadow(FanCard tracked)
        {
            if (tracked.shadow != null)
            {
                tracked.shadow.localPosition = tracked.shadowLocalPosition;
                tracked.shadow.localRotation = tracked.shadowLocalRotation;
                tracked.shadow.localScale = tracked.shadowLocalScale;
            }
        }
    }
}
