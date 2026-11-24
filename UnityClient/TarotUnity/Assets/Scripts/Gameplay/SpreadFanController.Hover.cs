using UnityEngine;

namespace TarotUnity.Gameplay
{
    public sealed partial class SpreadFanController
    {
        private void Locate(int index, out int row, out int inRow)
        {
            var first = 0;
            for (row = 0; row < rows.Length; row++)
            {
                if (index < first + rows[row].count)
                {
                    inRow = index - first;
                    return;
                }

                first += rows[row].count;
            }

            row = rows.Length - 1;
            inRow = Mathf.Max(0, rows[row].count - 1);
        }

        private int LongestRow()
        {
            var longest = 1;
            foreach (var row in rows)
            {
                longest = Mathf.Max(longest, row.count);
            }

            return longest;
        }

        /// <summary>Where card <paramref name="index"/> lies on its arc, slid <paramref name="part"/> metres along it.</summary>
        private void PoseOnArc(int index, float part, out Vector3 position, out Quaternion rotation)
        {
            if (PickPresentation != null)
            {
                PickPresentation.FanPose(index, out position, out rotation);
                return;
            }
            Locate(index, out var r, out var inRow);
            var row = rows[r];
            var center = fanCenter != null ? fanCenter : transform;
            var t = row.count > 1 ? (float)inRow / (row.count - 1) - 0.5f : 0f;
            var angle = t * row.arcDegrees + part / Mathf.Max(0.01f, row.radius) * Mathf.Rad2Deg;
            var turn = Quaternion.AngleAxis(angle, center.up);
            position = center.position + turn * center.forward * row.radius + center.up * (inRow * layerStep);
            rotation = turn * center.rotation;
        }

        /// <summary>Phase 73 review: the gather starts from the resting pose, not a frozen hover.</summary>
        private void LetGoOfHover(CardView card, FanCard tracked)
        {
            tracked.hover = tracked.hoverVelocity = tracked.part = tracked.partVelocity = 0f;
            if (tracked.pivot != null)
            {
                tracked.pivot.localRotation = Quaternion.identity;
                tracked.pivot.localScale = Vector3.one;
            }

            RestoreShadow(tracked);
            GetFanPose(tracked.index, out var p, out var r);
            card.transform.SetPositionAndRotation(p, r);
            card.transform.localScale = baseScale;
        }

        private void Update()
        {
            if (!spreadDone)
            {
                return;
            }

            if (hovered != null && hoverReleaseAt > 0f && Time.time >= hoverReleaseAt)
            {
                hovered = null;
                hoverReleaseAt = -1f;
            }

            var hoveredIndex = -1;
            var hoveredRow = -1;
            var hoveredInRow = 0;
            if (hovered != null && state.TryGetValue(hovered, out var h))
            {
                hoveredIndex = h.index;
                Locate(h.index, out hoveredRow, out hoveredInRow);
            }

            var dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            var omega = 2f * Mathf.PI * Mathf.Max(0.1f, springFrequency);
            foreach (var card in fanCards)
            {
                var tracked = state[card];
                if (PickPresentation != null)
                {
                    if (PickPresentation.ApplyFanHover(card, tracked.index, card == hovered) && tracked.box != null)
                    {
                        GetFanPose(tracked.index, out var rest, out var rotation);
                        tracked.box.center = card.transform.InverseTransformPoint(rest + rotation * tracked.boxCenter);
                    }
                    continue;
                }
                Locate(tracked.index, out var row, out var inRow);

                var hoverTarget = tracked.index == hoveredIndex ? 1f : 0f;
                var partTarget = 0f;
                if (row == hoveredRow && tracked.index != hoveredIndex)
                {
                    var k = inRow - hoveredInRow;
                    var d = Mathf.Abs(k);
                    if (d <= waveRadius)
                    {
                        var ease = 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (d - 1f) / waveRadius);
                        partTarget = Mathf.Sign(k) * partDistance * ease;
                    }
                }

                // A card whose springs are at rest is left alone (78 cards, most of them idle).
                var atRest = hoverTarget == 0f && partTarget == 0f;
                if (atRest && tracked.settled)
                {
                    continue;
                }

                Spring(ref tracked.hover, ref tracked.hoverVelocity, hoverTarget, omega, dt);
                Spring(ref tracked.part, ref tracked.partVelocity, partTarget, omega, dt);
                tracked.settled = false;
                if (atRest && Mathf.Abs(tracked.hover) < 1e-4f && Mathf.Abs(tracked.hoverVelocity) < 1e-3f
                    && Mathf.Abs(tracked.part) < 1e-4f && Mathf.Abs(tracked.partVelocity) < 1e-3f)
                {
                    tracked.hover = tracked.hoverVelocity = tracked.part = tracked.partVelocity = 0f;
                    tracked.settled = true;
                }

                PoseOnArc(tracked.index, tracked.part, out var p, out var r);
                var towardPlayer = -(r * Vector3.forward);
                var slide = towardPlayer * (hoverSlide * tracked.hover);
                var grow = 1f + (hoverScale - 1f) * tracked.hover;
                card.transform.SetPositionAndRotation(p + Vector3.up * (hoverLift * tracked.hover) + slide, r);
                if (tracked.pivot != null)
                {
                    tracked.pivot.localRotation = Quaternion.AngleAxis(-hoverTiltDegrees * tracked.hover, Vector3.right);
                    tracked.pivot.localScale = Vector3.one * grow;
                }

                // The shadow stays on the cloth under the card and spreads as the card rises.
                if (tracked.shadow != null)
                {
                    var shadowRest = p + r * tracked.shadowRootOffset;
                    tracked.shadow.SetPositionAndRotation(shadowRest + slide, r * tracked.shadowRootRotation);
                    tracked.shadow.localScale = tracked.shadowLocalScale * ((1f + shadowSpread * tracked.hover) / grow);
                }

                // The card moves but its pointer target stays on its resting rectangle; otherwise
                // a card moving out from under the pointer would fall back under it and loop.
                if (tracked.box != null)
                {
                    GetFanPose(tracked.index, out var restPosition, out var restRotation);
                    tracked.box.center = card.transform.InverseTransformPoint(restPosition + restRotation * tracked.boxCenter);
                }
            }
        }

        private void Spring(ref float x, ref float v, float target, float omega, float dt)
        {
            v += (omega * omega * (target - x) - 2f * springDamping * omega * v) * dt;
            x += v * dt;
        }
    }
}
