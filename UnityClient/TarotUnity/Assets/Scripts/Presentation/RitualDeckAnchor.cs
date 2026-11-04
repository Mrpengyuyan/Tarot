using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RitualDeckAnchor : MonoBehaviour
    {
        private static readonly Vector3 ParkedViewport = new Vector3(0.1f, 0.24f, 0f);
        private const float ParkedWidth = 0.09f;
        private const float ParkedHeight = 0.155f;
        private readonly Vector3[] renderedCorners = new Vector3[8];
        private DeckShuffleChoreographer deck;
        private Camera viewCamera;
        private Vector3 authoredPosition;
        private Quaternion authoredRotation;
        private Vector3 authoredScale;
        private bool captured;
        private bool ownsPose;
        private bool hasRenderedBounds;
        private int generation;

        public float PickupSeconds => 0.9f;
        public bool IsParked { get; private set; }
        public bool IsPickingUp { get; private set; }

        public static RitualDeckAnchor Attach(DeckShuffleChoreographer deck, CameraChoreographyController camera)
        {
            if (deck == null)
            {
                return null;
            }

            var anchor = deck.GetComponent<RitualDeckAnchor>();
            if (anchor == null)
            {
                anchor = deck.gameObject.AddComponent<RitualDeckAnchor>();
            }

            anchor.deck = deck;
            anchor.viewCamera = ResolveCamera(camera, deck);
            if (!anchor.captured)
            {
                var pose = deck.transform;
                anchor.authoredPosition = pose.localPosition;
                anchor.authoredRotation = pose.localRotation;
                anchor.authoredScale = pose.localScale;
                anchor.CacheRenderedBounds();
                anchor.captured = true;
                anchor.Park();
            }

            return anchor;
        }

        public void Park()
        {
            if (!captured || deck == null || deck.IsPlaying)
            {
                return;
            }

            generation++;
            ownsPose = true;
            IsPickingUp = false;
            IsParked = true;
            UpdateParkedPose();
        }

        public IEnumerator Pickup() => PickupRoutine(generation);

        public void Clear()
        {
            generation++;
            IsParked = false;
            IsPickingUp = false;
            if (ownsPose && deck != null)
            {
                RestoreAuthoredPose();
            }

            ownsPose = false;
        }

        private void LateUpdate()
        {
            if (IsParked)
            {
                UpdateParkedPose();
            }
        }

        private void OnDisable() => Clear();

        private void OnDestroy() => Clear();

        private IEnumerator PickupRoutine(int token)
        {
            if (!IsCurrent(token) || !IsParked || IsPickingUp)
            {
                yield break;
            }

            IsParked = false;
            IsPickingUp = true;
            var pose = deck.transform;
            var fromPosition = pose.position;
            var fromRotation = pose.localRotation;
            var fromScale = pose.localScale;
            try
            {
                for (var elapsed = 0f; elapsed < PickupSeconds; elapsed += Time.deltaTime)
                {
                    if (!IsCurrent(token))
                    {
                        yield break;
                    }

                    var t = elapsed / PickupSeconds;
                    var eased = t * t * (3f - 2f * t);
                    pose.position = Vector3.Lerp(fromPosition, AuthoredWorldPosition(), eased)
                        + Vector3.up * (Mathf.Sin(Mathf.PI * t) * 0.32f);
                    pose.localRotation = Quaternion.Slerp(fromRotation, authoredRotation, eased);
                    pose.localScale = Vector3.Lerp(fromScale, authoredScale, eased);
                    yield return null;
                }

                if (IsCurrent(token))
                {
                    RestoreAuthoredPose();
                    ownsPose = false;
                    IsPickingUp = false;
                }
            }
            finally
            {
                if (this != null && generation == token && IsPickingUp)
                {
                    Clear();
                }
            }
        }

        private bool IsCurrent(int token) => this != null && generation == token && isActiveAndEnabled && deck != null;

        private Vector3 AuthoredWorldPosition()
        {
            var parent = deck.transform.parent;
            return parent != null ? parent.TransformPoint(authoredPosition) : authoredPosition;
        }

        private void UpdateParkedPose()
        {
            if (deck == null || viewCamera == null)
            {
                return;
            }

            var plane = new Plane(Vector3.up, AuthoredWorldPosition());
            var ray = viewCamera.ViewportPointToRay(ParkedViewport);
            if (!plane.Raycast(ray, out var distance))
            {
                return;
            }

            var pose = deck.transform;
            pose.position = ray.GetPoint(distance);
            pose.localRotation = authoredRotation;
            pose.localScale = authoredScale * 0.82f;
            if (!hasRenderedBounds)
            {
                return;
            }

            var target = pose.position;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                if (!TryMeasureViewportBounds(out var min, out var max))
                {
                    pose.localScale *= 0.25f;
                    continue;
                }

                var size = max - min;
                if (size.x < 0.000001f || size.y < 0.000001f)
                {
                    break;
                }

                var fit = Mathf.Min(ParkedWidth / size.x, ParkedHeight / size.y);
                pose.localScale *= Mathf.Clamp(fit, 0.01f, 100f);
                if (TryMeasureViewportBounds(out min, out max))
                {
                    var center = (min + max) * 0.5f;
                    var centerRay = viewCamera.ViewportPointToRay(new Vector3(center.x, center.y, 0f));
                    if (plane.Raycast(centerRay, out var centerDistance))
                    {
                        pose.position += target - centerRay.GetPoint(centerDistance);
                    }
                }
            }
        }

        private void CacheRenderedBounds()
        {
            var bounds = new Bounds();
            var pose = deck.transform;
            foreach (var renderer in deck.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var local = renderer.localBounds;
                for (var i = 0; i < 8; i++)
                {
                    var point = local.center + Vector3.Scale(local.extents, CornerSign(i));
                    point = pose.InverseTransformPoint(renderer.transform.TransformPoint(point));
                    if (!hasRenderedBounds)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasRenderedBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            // Cache one conservative local box, not live world AABBs that would
            // expand with rotation or allocate renderer/corner arrays every frame.
            for (var i = 0; i < renderedCorners.Length; i++)
            {
                renderedCorners[i] = bounds.center + Vector3.Scale(bounds.extents, CornerSign(i));
            }
        }

        private bool TryMeasureViewportBounds(out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            var pose = deck.transform;
            for (var i = 0; i < renderedCorners.Length; i++)
            {
                var point = viewCamera.WorldToViewportPoint(pose.TransformPoint(renderedCorners[i]));
                if (point.z <= viewCamera.nearClipPlane)
                {
                    return false;
                }

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return true;
        }

        private static Vector3 CornerSign(int corner) => new Vector3((corner & 1) == 0 ? -1f : 1f,
            (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);

        private void RestoreAuthoredPose()
        {
            var pose = deck.transform;
            pose.localPosition = authoredPosition;
            pose.localRotation = authoredRotation;
            pose.localScale = authoredScale;
        }

        private static Camera ResolveCamera(CameraChoreographyController choreography, DeckShuffleChoreographer deck)
        {
            if (choreography != null && choreography.TryGetComponent<Camera>(out var attached))
            {
                return attached;
            }

            // The existing scene keeps choreography on a separate object and its
            // world camera is untagged. Resolve locally rather than changing that rig.
            var scene = choreography != null ? choreography.gameObject.scene : deck.gameObject.scene;
            Camera best = null;
            foreach (var candidate in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == scene && candidate.isActiveAndEnabled
                    && (best == null || candidate.depth > best.depth))
                {
                    best = candidate;
                }
            }

            return best;
        }
    }
}
