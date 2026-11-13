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
        public RitualDeckLayerPose(Vector3 offset, float roll = 0f) { Offset = offset; Roll = roll; }
    }

    /// <summary>The frozen v6 deck transforms in top-left-origin pixels, including CSS group flattening.</summary>
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
            return new RitualDeckPose(Vector2.one * density + Vector2.Scale(size, new Vector2(0.05f, 0.71f)),
                new Vector2(size.x * 0.065f, size.x * 0.113f), 50f, -14f, 1f, question ? 0.65f : 0.48f, density);
        }

        public static RitualDeckPose Raised(float width, float height)
        {
            var rest = Parked(width, height, true);
            return new RitualDeckPose(new Vector2((width - rest.Size.x) * 0.5f,
                rest.Density + (height - 2f * rest.Density) * 0.28f), rest.Size, 23f, -3f, 1.75f, 1f, rest.Density);
        }

        public static RitualDeckPose Pickup(float width, float height, float time)
        {
            var rest = Parked(width, height, true);
            var lift = new RitualDeckPose(new Vector2(rest.Density + (width - 2f * rest.Density) * 0.24f,
                rest.Density + (height - 2f * rest.Density) * 0.28f), rest.Size, 12f, -12f, 1.8f, 1f, rest.Density);
            var pose = time <= 0.45f ? Mix(rest, lift, Ease(time / 0.45f, 0.2f, 0.7f, 0.2f, 1f))
                : Mix(lift, Raised(width, height), Ease((time - 0.45f) / 0.55f, 0.2f, 0.7f, 0.2f, 1f));
            return new RitualDeckPose(pose.Origin, pose.Size, pose.Pitch, pose.Roll, pose.Scale,
                Mathf.Lerp(0.65f, 1f, Ease(time * PickupSeconds / 0.3f, 0.25f, 0.1f, 0.25f, 1f)), pose.Density);
        }

        public static RitualDeckPose Returning(float width, float height, float time)
        {
            var pose = Mix(Raised(width, height), Parked(width, height, false), Ease(time, 0.25f, 0.1f, 0.25f, 1f));
            return new RitualDeckPose(pose.Origin, pose.Size, pose.Pitch, pose.Roll, pose.Scale,
                Mathf.Lerp(1f, 0.48f, Ease(time * ReturnSeconds / 0.3f, 0.25f, 0.1f, 0.25f, 1f)), pose.Density);
        }

        public static RitualDeckLayerPose ShuffleLayer(int index, float time, float width, float height)
        {
            var d = Density(width, height);
            var w = width - 2f * d;
            var h = height - 2f * d;
            var side = index < 6 ? -1f : 1f;
            var lane = index % 6;
            var order = lane * 2 + (index < 6 ? 0 : 1);
            var release = 0.32f + order * 0.018f;
            var rest = new RitualDeckLayerPose(new Vector3(0f, 0f, index * 1.2f * d));
            var open = new RitualDeckLayerPose(new Vector3(side * w * 0.072f, -h * 0.012f, (32f + lane * 2f) * d), side * 8f);
            var split = new RitualDeckLayerPose(new Vector3(side * w * 0.088f, side * h * 0.007f, (24f + lane * 1.8f) * d), side * 10f);
            var arch = new RitualDeckLayerPose(new Vector3(side * w * 0.006f, -h * 0.07f, (75f + order * 1.5f) * d), side * -4f);
            var woven = new RitualDeckLayerPose(new Vector3(0f, 0f, order * 1.2f * d));
            var recutSide = index % 2 == 1 ? 1f : -1f;
            var recut = new RitualDeckLayerPose(new Vector3(recutSide * w * 0.055f, -h * 0.01f, (24f + lane * 2f) * d), recutSide * 7f);
            var square = new RitualDeckLayerPose(new Vector3(0f, -h * 0.004f, (8f + order * 1.2f) * d));
            var t = Ease(time, 0.42f, 0f, 0.58f, 1f);
            if (t < 0.16f) return Mix(rest, open, t / 0.16f);
            if (t < 0.30f) return Mix(open, split, (t - 0.16f) / 0.14f);
            if (t < release) return split;
            if (t < release + 0.10f) return Mix(split, arch, (t - release) / 0.10f);
            if (t < release + 0.17f) return Mix(arch, woven, (t - release - 0.10f) / 0.07f);
            if (t < 0.78f) return woven;
            if (t < 0.86f) return Mix(woven, recut, (t - 0.78f) / 0.08f);
            if (t < 0.94f) return Mix(recut, square, (t - 0.86f) / 0.08f);
            return Mix(square, rest, (t - 0.94f) / 0.06f);
        }

        public static Vector3 ProjectCorner(RitualDeckPose pose, RitualDeckLayerPose layer, int corner)
            => ProjectPoint(pose,layer,new Vector2(corner==0 || corner==3 ? 0f : 1f,corner<2 ? 0f : 1f));

        internal static Vector3 ProjectPoint(RitualDeckPose pose,RitualDeckLayerPose layer,Vector2 point)
        {
            var p = new Vector3((point.x-.5f)*pose.Size.x,(point.y-.5f)*pose.Size.y,0f);
            p = Quaternion.AngleAxis(layer.Roll, Vector3.forward) * p + layer.Offset;
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

        private static float Ease(float time, float a, float b, float c, float d)
            => time <= 0f ? 0f : time >= 1f ? 1f : RitualPickLayout.Bezier(time, a, b, c, d);
        private static RitualDeckPose Mix(RitualDeckPose a, RitualDeckPose b, float t)
            => new(Vector2.Lerp(a.Origin, b.Origin, t), Vector2.Lerp(a.Size, b.Size, t), Mathf.Lerp(a.Pitch, b.Pitch, t),
                Mathf.Lerp(a.Roll, b.Roll, t), Mathf.Lerp(a.Scale, b.Scale, t), Mathf.Lerp(a.Opacity, b.Opacity, t), a.Density);
        private static RitualDeckLayerPose Mix(RitualDeckLayerPose a, RitualDeckLayerPose b, float t)
            => new(Vector3.Lerp(a.Offset, b.Offset, t), Mathf.Lerp(a.Roll, b.Roll, t));
    }
}
