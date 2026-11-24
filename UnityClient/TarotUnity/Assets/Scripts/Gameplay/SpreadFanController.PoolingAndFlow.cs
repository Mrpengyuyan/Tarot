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
            var hidden = (origin != null ? origin.position : transform.position) + Vector3.down * 2f;
            for (var i = 0; i < CardCount; i++)
            {
                // Phase 74: face down and switched off until the spread - a fresh prefab shows its
                // face and its back at once, and nothing of it should show through the shuffle.
                var card = AcquireCard(hidden, Quaternion.identity, false);
                prepared.Add(card);
                if ((i + 1) % Mathf.Max(1, prepareBatch) == 0)
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

            for (var i = 0; i < CardCount; i++)
            {
                var card = i < prepared.Count && prepared[i] != null
                    ? prepared[i]
                    : AcquireCard(origin.position, origin.rotation, true);
                card.name = $"FanCard_{i:00}";
                card.transform.SetPositionAndRotation(origin.position, origin.rotation);
                card.gameObject.SetActive(true);
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
                if (PickPresentation != null) PickPresentation.PrepareFanCard(card, i);
                fanCards.Add(card);
                state[card] = Track(card, i);
            }

            prepared.Clear();

            baseScale = fanCards.Count > 0 ? fanCards[0].transform.localScale : Vector3.one;
            FadeLight(fanLightIntensity);
            CaptureDeck(origin);
            var start = origin.position;
            var startRotation = origin.rotation;
            var total = spreadCardSeconds + spreadStagger * (LongestRow() - 1);
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                foreach (var card in fanCards)
                {
                    var i = state[card].index;
                    Locate(i, out _, out var inRow);
                    var k = Mathf.Clamp01((elapsed - inRow * spreadStagger) / spreadCardSeconds);
                    var e = 1f - (1f - k) * (1f - k);
                    GetFanPose(i, out var p, out var r);
                    card.transform.SetPositionAndRotation(
                        Vector3.Lerp(start, p, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * spreadArcHeight),
                        Quaternion.Slerp(startRotation, r, e));
                }

                ShowDeck(1f - elapsed / total);
                yield return null;
            }

            ShowDeck(0f);
            foreach (var card in fanCards)
            {
                GetFanPose(state[card].index, out var p, out var r);
                card.transform.SetPositionAndRotation(p, r);
            }

            // The whole fan settles with one small squash.
            var squashed = new Vector3(baseScale.x * (1f + settleSquash * 0.6f), baseScale.y * (1f - settleSquash), baseScale.z);
            for (var elapsed = 0f; elapsed < settleSeconds; elapsed += Time.deltaTime)
            {
                var k = elapsed / settleSeconds;
                foreach (var card in fanCards)
                {
                    card.transform.localScale = Vector3.Lerp(squashed, baseScale, 1f - (1f - k) * (1f - k));
                }

                yield return null;
            }

            foreach (var card in fanCards)
            {
                card.transform.localScale = baseScale;
            }

            spreadDone = true;
        }

        public IEnumerator Gather(Transform origin)
        {
            spreadDone = false;
            hovered = null;
            FadeLight(0f);
            foreach (var card in fanCards)
            {
                LetGoOfHover(card, state[card]);
            }

            var cards = new List<CardView>(fanCards);
            if (origin == null || cards.Count == 0)
            {
                Clear();
                ShowDeck(1f);
                yield break;
            }

            var starts = new List<Vector3>();
            foreach (var card in cards)
            {
                starts.Add(card.transform.position);
            }

            // Each row gathers from both ends toward its middle.
            var mid = (LongestRow() - 1) / 2f;
            var cardSeconds = gatherSeconds * 0.6f;
            var stagger = mid > 0f ? gatherSeconds * 0.4f / mid : 0f;
            for (var elapsed = 0f; elapsed < gatherSeconds; elapsed += Time.deltaTime)
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    Locate(state[cards[i]].index, out var row, out var inRow);
                    var rowMid = (rows[row].count - 1) / 2f;
                    var order = rowMid - Mathf.Abs(inRow - rowMid);
                    var k = Mathf.Clamp01((elapsed - order * stagger) / cardSeconds);
                    cards[i].transform.position = Vector3.Lerp(starts[i], origin.position, k * k);
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
                if (tracked.box != null) tracked.box.center = tracked.boxCenter;
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

            var finish = card.GetComponent<RitualPaperFinish>();
            if (finish != null)
            {
                finish.enabled = false;
                Destroy(finish);
            }

            card.SetFaceUp(false);
            card.transform.SetParent(transform, false);
            card.transform.localScale = prefabScaleCaptured ? prefabScale : Vector3.one;
            card.gameObject.SetActive(false);
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
