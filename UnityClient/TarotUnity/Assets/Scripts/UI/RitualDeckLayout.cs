using UnityEngine;

namespace TarotUnity.UI
{
    public readonly struct RitualDeckPose
    {
        public readonly Vector2 Origin, Size;
        public readonly float Pitch, Roll, Scale, Opacity, Density;
        public RitualDeckPose(Vector2 origin, Vector2 size, float pitch, float roll, float scale, float opacity, float density)
        { Origin = origin; Size = size; Pitch = pitch; Roll = roll; Scale = scale; Opacity = opacity; Density = density; }
    }

    public readonly struct RitualDeckLayerPose
    {
        public readonly Vector3 Offset;
        public readonly float Roll;
        public readonly float Pitch;
        public readonly float Yaw;
        public RitualDeckLayerPose(Vector3 offset, float roll = 0f, float pitch = 0f, float yaw = 0f)
        {
            Offset = offset;
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }
    }

    /// <summary>Deck layout and ceremonial center-stage shuffle choreography in top-left-origin pixels.</summary>
    public static class RitualDeckLayout
    {
        public const int LayerCount = 12;
        public const float PickupSeconds = 0.9f, ShuffleSeconds = 5.4f, SettleSeconds = 0.4f, ReturnSeconds = 0.85f;

        public static float Density(float width, float height) => width / Mathf.Lerp(1024f, 1280f,
            Mathf.InverseLerp(4f / 3f, 16f / 9f, width / height));

        public static RitualDeckPose Parked(float width, float height, bool question)
        {
            var density = Density(width, height);
            var size = new Vector2(width - 2f * density, height - 2f * density);
            return new RitualDeckPose(
                Vector2.one * density + Vector2.Scale(size, new Vector2(0.045f, 0.615f)),
                new Vector2(size.x * 0.088f, size.x * 0.153f),
                46f,
                -12f,
                1f,
                question ? 0.65f : 0.48f,
                density);
        }

        public static RitualDeckPose Raised(float width, float height)
        {
            var rest = Parked(width, height, true);
            return new RitualDeckPose(
                new Vector2((width - rest.Size.x) * 0.5f, (height - rest.Size.y) * 0.5f),
                rest.Size,
                20f,
                0f,
                1.62f,
                1f,
                rest.Density);
        }

        public static RitualDeckPose Pickup(float width, float height, float time)
        {
            var rest = Parked(width, height, true);
            var target = Raised(width, height);
            // Smooth rising diagonal arc directly from bottom-left into screen center (no upper-left detour).
            var lift = new RitualDeckPose(
                new Vector2(
                    Mathf.Lerp(rest.Origin.x, target.Origin.x, 0.52f),
                    Mathf.Lerp(rest.Origin.y, target.Origin.y, 0.58f) - (height - 2f * rest.Density) * 0.04f),
                rest.Size,
                30f,
                -5f,
                1.38f,
                1f,
                rest.Density);
            var pose = time <= 0.45f
                ? Mix(rest, lift, Ease(time / 0.45f, 0.22f, 0.72f, 0.22f, 1f))
                : Mix(lift, target, Ease((time - 0.45f) / 0.55f, 0.22f, 0.72f, 0.22f, 1f));
            return new RitualDeckPose(
                pose.Origin,
                pose.Size,
                pose.Pitch,
                pose.Roll,
                pose.Scale,
                Mathf.Lerp(0.65f, 1f, Ease(time * PickupSeconds / 0.3f, 0.25f, 0.1f, 0.25f, 1f)),
                pose.Density);
        }

        public static RitualDeckPose Returning(float width, float height, float time)
        {
            var pose = Mix(Raised(width, height), Parked(width, height, false), Ease(time, 0.25f, 0.1f, 0.25f, 1f));
            return new RitualDeckPose(
                pose.Origin,
                pose.Size,
                pose.Pitch,
                pose.Roll,
                pose.Scale,
                Mathf.Lerp(1f, 0.48f, Ease(time * ReturnSeconds / 0.3f, 0.25f, 0.1f, 0.25f, 1f)),
                pose.Density);
        }

        public static RitualDeckLayerPose ShuffleLayer(int index, float time, float width, float height)
        {
            var d = Density(width, height);
            var w = width - 2f * d;
            var h = height - 2f * d;
            var side = index < 6 ? -1f : 1f;
            var lane = index % 6;
            var order = lane * 2 + (index < 6 ? 0 : 1);

            var rest = new RitualDeckLayerPose(new Vector3(0f, 0f, index * 1.2f * d));
            if (time <= 0f || time >= 1f)
            {
                return rest;
            }

            // Act 1 (0.00 - 0.16): Graceful twin-packet split with angled 3D tilt & gilded edge fan.
            var open = new RitualDeckLayerPose(
                new Vector3(side * w * 0.082f, -h * 0.008f - lane * 0.7f * d, (26f + lane * 2.4f) * d),
                side * (9f + lane * 0.6f),
                -4f,
                -side * 12f);
            var split = new RitualDeckLayerPose(
                new Vector3(side * w * 0.105f, side * h * 0.008f - lane * 0.5f * d, (22f + lane * 2.2f) * d),
                side * (13f + lane * 0.7f),
                -6f,
                -side * 16f);

            // Act 2 (0.16 - 0.52): Staggered 3D parabolic riffle bridge interleaving cleanly into center.
            var release = 0.20f + order * 0.015f;
            var arch = new RitualDeckLayerPose(
                new Vector3(side * w * 0.014f, -h * 0.056f, (78f + order * 1.8f) * d),
                -side * 5f,
                -14f,
                side * 6f);
            var woven = new RitualDeckLayerPose(new Vector3(0f, 0f, order * 1.2f * d));

            // Act 3 (0.52 - 0.80): Sweeping twin-packet 3D orbital cross-cut right at screen center.
            var orbitSide = index % 2 == 0 ? -1f : 1f;
            var orbitLane = index / 2;

            // Act 4 (0.80 - 1.00): Crisp ceremonial micro-cut, tap & square-up back into resting stack.
            var recutSide = index < 6 ? 1f : -1f;
            var recut = new RitualDeckLayerPose(
                new Vector3(recutSide * w * 0.062f, -h * 0.012f, (28f + lane * 2.0f) * d),
                recutSide * 8f,
                -5f,
                -recutSide * 10f);
            var square = new RitualDeckLayerPose(
                new Vector3(0f, -h * 0.006f, (10f + index * 1.2f) * d),
                0f,
                -2f,
                0f);

            var t = Ease(time, 0.40f, 0f, 0.60f, 1f);
            if (t < 0.09f) return Mix(rest, open, Smooth(t / 0.09f));
            if (t < 0.17f) return Mix(open, split, Smooth((t - 0.09f) / 0.08f));
            if (t < release) return split;
            if (t < release + 0.085f) return Mix(split, arch, Smooth((t - release) / 0.085f));
            if (t < release + 0.145f) return Mix(arch, woven, Smooth((t - release - 0.085f) / 0.06f));
            if (t < 0.54f) return woven;
            if (t < 0.80f)
            {
                var u = (t - 0.54f) / 0.26f;
                var theta = Smooth(u) * Mathf.PI;
                var sin = Mathf.Sin(theta);
                var cos = Mathf.Cos(theta);
                var ampX = w * 0.096f * sin;
                var ampY = -h * 0.036f * sin * orbitSide - h * 0.014f * sin;
                var depthLift = (orbitSide > 0f ? 52f : 18f) * sin + orbitLane * 2.1f;
                var x = orbitSide * ampX * (0.35f + 0.65f * Mathf.Abs(cos));
                // Smooth figure-8 crossover so both packets sweep around center and merge cleanly at u=1.
                var crossX = orbitSide * w * 0.098f * Mathf.Sin(theta * 2f) * 0.5f + x * 0.45f;
                var orbitRoll = orbitSide * 14f * sin;
                var orbitPitch = -10f * sin;
                var orbitYaw = -orbitSide * 14f * sin;
                return new RitualDeckLayerPose(
                    new Vector3(crossX, ampY, Mathf.Lerp(order * 1.2f, index * 1.2f, Smooth(u)) * d + depthLift * d),
                    orbitRoll,
                    orbitPitch,
                    orbitYaw);
            }
            if (t < 0.88f) return Mix(rest, recut, Smooth((t - 0.80f) / 0.08f));
            if (t < 0.94f) return Mix(recut, square, Smooth((t - 0.88f) / 0.06f));
            return Mix(square, rest, Smooth((t - 0.94f) / 0.06f));
        }

        public static Vector3 ProjectCorner(RitualDeckPose pose, RitualDeckLayerPose layer, int corner)
            => ProjectPoint(pose, layer, new Vector2(corner == 0 || corner == 3 ? 0f : 1f, corner < 2 ? 0f : 1f));

        internal static Vector3 ProjectPoint(RitualDeckPose pose, RitualDeckLayerPose layer, Vector2 point)
        {
            var p = new Vector3((point.x - .5f) * pose.Size.x, (point.y - .5f) * pose.Size.y, 0f);
            p = Quaternion.Euler(layer.Pitch, layer.Yaw, layer.Roll) * p + layer.Offset;
            // CSS opacity groups flatten child depth before the parent's transform.
            if (pose.Opacity < 0.99999f) p.z = 0f;
            p.x *= pose.Scale;
            p.y *= pose.Scale;
            p = Quaternion.AngleAxis(pose.Pitch, Vector3.right)
                * (Quaternion.AngleAxis(pose.Roll, Vector3.forward) * p);
            var divisor = 1f - p.z / (900f * pose.Density);
            var center = pose.Origin + pose.Size * 0.5f;
            return new Vector3(center.x + p.x / divisor, center.y + p.y / divisor, p.z);
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float Ease(float time, float a, float b, float c, float d)
            => time <= 0f ? 0f : time >= 1f ? 1f : RitualPickLayout.Bezier(time, a, b, c, d);

        private static RitualDeckPose Mix(RitualDeckPose a, RitualDeckPose b, float t)
            => new(Vector2.Lerp(a.Origin, b.Origin, t), Vector2.Lerp(a.Size, b.Size, t), Mathf.Lerp(a.Pitch, b.Pitch, t),
                Mathf.Lerp(a.Roll, b.Roll, t), Mathf.Lerp(a.Scale, b.Scale, t), Mathf.Lerp(a.Opacity, b.Opacity, t), a.Density);

        private static RitualDeckLayerPose Mix(RitualDeckLayerPose a, RitualDeckLayerPose b, float t)
            => new(
                Vector3.Lerp(a.Offset, b.Offset, t),
                Mathf.Lerp(a.Roll, b.Roll, t),
                Mathf.Lerp(a.Pitch, b.Pitch, t),
                Mathf.Lerp(a.Yaw, b.Yaw, t));
    }
}
