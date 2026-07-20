using System.Collections.Generic;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Builds candle geometry as a surface of revolution instead of stacked Unity
    /// primitives. The built-in cylinder carries 20 radial segments - 18 degrees per
    /// facet - and has no profile at all, so a candle assembled from it is a tube
    /// with a disc collar screwed on top. This lathes a real profile at 40-64
    /// segments: the base pool flows out of the body with no seam, the shoulder
    /// swells where wax softened and re-froze, the rim rolls over instead of
    /// stepping, the top dishes into a burn crater around the wick, and the burn
    /// line is uneven because a level one is the machined tell.
    ///
    /// UVs are kept on Phase 46's contract - U wraps once around, V=0 at the base
    /// and V=1 at the rim - so the existing wax colour/translucency maps still land
    /// where they were painted to land. The drips are lathed at the same angles the
    /// texture paints them, so the geometry and the painting agree.
    /// </summary>
    public static class CandleMeshBuilder
    {
        /// <summary>
        /// The nine drips Tools/UiKitGenerator/gen_wax.py paints, as
        /// (u around the candle, how far down it runs in V, its width in u).
        /// Extracted from the generator's seeded sequence so geometry matches paint.
        /// </summary>
        private static readonly (float U, float Run, float Width)[] PaintedDrips =
        {
            (0.2598f, 0.3633f, 0.0215f),
            (0.8789f, 0.1875f, 0.0352f),
            (0.3438f, 0.1680f, 0.0312f),
            (0.1230f, 0.2598f, 0.0371f),
            (0.9648f, 0.1504f, 0.0332f),
            (0.5527f, 0.1172f, 0.0195f),
            (0.0547f, 0.1289f, 0.0312f),
            (0.3984f, 0.2344f, 0.0312f),
            (0.4941f, 0.3574f, 0.0254f),
        };

        private const float ShoulderStart = 0.84f;
        private const float RimStart = 0.965f;

        /// <summary>
        /// One candle body, lathed. <paramref name="height"/> is the full wax height
        /// from the table to the rim; the mesh is built with its base at local y=0.
        /// A shorter candle has burnt longer, so it pools wider and craters deeper.
        /// </summary>
        public static Mesh BuildWax(string name, float radius, float height, int radialSegments)
        {
            var melt = Mathf.InverseLerp(0.62f, 0.26f, height);
            var poolRadius = radius * Mathf.Lerp(1.38f, 1.62f, melt);
            var poolHeight = Mathf.Lerp(0.022f, 0.038f, melt);
            // A shallow dish, not a hole. The first pass cut deep enough that the rim
            // read as the thin edge of a shell rather than as the top of a candle.
            var craterDepth = Mathf.Lerp(0.016f, 0.026f, melt);
            var wickSeat = radius * 0.16f;

            var profile = BuildProfile(radius, height, poolRadius, poolHeight, craterDepth, wickSeat);
            return Lathe(name, profile, radius, height, craterDepth, radialSegments);
        }

        /// <summary>
        /// The profile curve, bottom to top: pooled base, tapered body, melted
        /// shoulder, rolled rim, then inward and down into the burn crater. Returns
        /// points as (radius, y) plus the V each ring should carry.
        /// </summary>
        private static List<(float R, float Y, float V, float RimWeight)> BuildProfile(
            float radius, float height, float poolRadius, float poolHeight,
            float craterDepth, float wickSeat)
        {
            var points = new List<(float, float, float, float)>();

            // Base pool: wax that ran down and set where it met the cloth. Concave,
            // so it flows out of the body instead of butting against a disc.
            const int poolRings = 5;
            for (var i = 0; i < poolRings; i++)
            {
                var u = i / (float)(poolRings - 1);
                var y = poolHeight * u;
                var r = Mathf.Lerp(radius, poolRadius, (1f - u) * (1f - u));
                points.Add((r, y, y / height, 0f));
            }

            // Body: a hair narrower toward the top. A dead-parallel tube is extruded
            // plastic; real wax is poured into a slightly tapered mould.
            const int bodyRings = 14;
            var shoulderY = height * ShoulderStart;
            for (var i = 1; i <= bodyRings; i++)
            {
                var t = i / (float)bodyRings;
                var y = Mathf.Lerp(poolHeight, shoulderY, t);
                var r = radius * Mathf.Lerp(1f, 0.985f, t);
                points.Add((r, y, y / height, 0f));
            }

            // Shoulder: the swell below the burn, where wax softened and set again.
            // This is what replaces the old collar - it grows out of the body.
            const int shoulderRings = 6;
            var rimY = height * RimStart;
            for (var i = 1; i <= shoulderRings; i++)
            {
                var t = i / (float)shoulderRings;
                var y = Mathf.Lerp(shoulderY, rimY, t);
                var r = radius * (Mathf.Lerp(0.985f, 1.02f, t) + 0.05f * Mathf.Sin(t * Mathf.PI));
                // The shoulder stays level: letting the burn wobble reach this far
                // down bent the whole upper body and read as a torn tube.
                points.Add((r, y, y / height, 0f));
            }

            // Rim: rolled over on a quarter arc. The old lip presented two hard
            // 90-degree edges; melted wax has no sharp edge anywhere.
            const int rimRings = 5;
            for (var i = 1; i <= rimRings; i++)
            {
                var t = i / (float)rimRings;
                var a = t * Mathf.PI * 0.5f;
                var y = Mathf.Lerp(rimY, height, t);
                var r = radius * (1.02f - 0.12f * Mathf.Sin(a));
                points.Add((r, y, y / height, t));
            }

            // Crater: the top dishes down to the wick. Steep at the rim, flattening
            // toward the middle - the shape a flame actually melts.
            const int craterRings = 7;
            for (var i = 1; i <= craterRings; i++)
            {
                var t = i / (float)craterRings;
                var r = Mathf.Lerp(radius * 0.90f, wickSeat, t);
                var y = height - craterDepth * Mathf.Sin(t * Mathf.PI * 0.5f);
                // The dish samples just below the burn line rather than sitting on
                // it. Held at V=1 the whole crater took the map's maximum glow and
                // blew out to a flat white disc - the modelled dish disappeared.
                points.Add((r, y, Mathf.Lerp(1f, 0.90f, t), 1f));
            }

            return points.ConvertAll(p => (p.Item1, p.Item2, p.Item3, p.Item4));
        }

        private static Mesh Lathe(string name, List<(float R, float Y, float V, float RimWeight)> profile,
            float radius, float height, float craterDepth, int radialSegments)
        {
            var ringCount = profile.Count;
            var columns = radialSegments + 1; // duplicate seam column so U can reach 1
            var vertices = new List<Vector3>(ringCount * columns + 2);
            var uvs = new List<Vector2>(ringCount * columns + 2);
            var triangles = new List<int>();

            for (var ring = 0; ring < ringCount; ring++)
            {
                var (r, y, v, rimWeight) = profile[ring];
                for (var col = 0; col < columns; col++)
                {
                    var u = col / (float)radialSegments;
                    var theta = u * Mathf.PI * 2f;

                    // Drips ride on the surface at the angles the texture paints.
                    var dripBulge = DripBulge(u, v) * radius;

                    // The burn line is uneven. Weighted in only near the top, so the
                    // body stays true and only the melted part wanders.
                    // Small on purpose: enough that the burn line is not a lathe-true
                    // circle, far short of looking cut open.
                    var wobble = craterDepth *
                                 (0.055f * Mathf.Sin(theta * 2f + 0.7f) + 0.030f * Mathf.Sin(theta * 3f + 2.1f)) *
                                 rimWeight;

                    var rr = r + dripBulge;
                    vertices.Add(new Vector3(Mathf.Cos(theta) * rr, y + wobble, Mathf.Sin(theta) * rr));
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                for (var col = 0; col < radialSegments; col++)
                {
                    var a = ring * columns + col;
                    var b = a + 1;
                    var c = a + columns;
                    var d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            // Crater floor, then the hidden underside so the mesh is closed.
            var craterCentre = vertices.Count;
            vertices.Add(new Vector3(0f, height - craterDepth, 0f));
            uvs.Add(new Vector2(0.5f, 0.90f));
            var lastRing = (ringCount - 1) * columns;
            for (var col = 0; col < radialSegments; col++)
            {
                triangles.Add(lastRing + col);
                triangles.Add(craterCentre);
                triangles.Add(lastRing + col + 1);
            }

            var baseCentre = vertices.Count;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0f));
            for (var col = 0; col < radialSegments; col++)
            {
                triangles.Add(col);
                triangles.Add(col + 1);
                triangles.Add(baseCentre);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// How far the surface swells at this (u, v) because a drip runs there.
        /// Fattest just under the rim, tapering down its run to a bead at the tail -
        /// the shape gen_wax.py draws, in geometry.
        /// </summary>
        private static float DripBulge(float u, float v)
        {
            var total = 0f;
            foreach (var (dripU, run, width) in PaintedDrips)
            {
                var du = Mathf.Abs(Mathf.Repeat(u - dripU + 0.5f, 1f) - 0.5f);
                if (du > width)
                {
                    continue;
                }

                // Squared cosine: a rounded rivulet with edges that melt into the
                // body, rather than a ridge with a seam down each side.
                var across = Mathf.Cos(du / width * Mathf.PI * 0.5f);
                across *= across;

                // Each drip overflowed at its own moment. Starting them all on one
                // line welded them into a single bulging ring around the shoulder.
                var top = Mathf.Lerp(0.885f, 0.955f, Frac(dripU * 7.31f));
                var bottom = top - run;
                if (v < bottom || v > top)
                {
                    continue;
                }

                var along = Mathf.InverseLerp(bottom, top, v);
                // Thin at the tail, thickening as it climbs, with a bead where it
                // cooled and stopped.
                var body = 0.030f * Mathf.SmoothStep(0f, 0.18f, along) *
                           (0.35f + 0.65f * Mathf.SmoothStep(0f, 1f, along));
                var bead = 0.014f * Mathf.Exp(-Mathf.Pow((along - 0.13f) / 0.09f, 2f));
                total += across * (body + bead);
            }

            return total;
        }

        private static float Frac(float value) => value - Mathf.Floor(value);

        /// <summary>
        /// The wick: tapered and leaning, not a parallel pin. It thins toward the
        /// tip and bends the way a burnt wick curls over.
        /// </summary>
        public static Mesh BuildWick(string name, float baseRadius, float height, int radialSegments = 12)
        {
            const int rings = 7;
            var columns = radialSegments + 1;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (var ring = 0; ring < rings; ring++)
            {
                var t = ring / (float)(rings - 1);
                var r = Mathf.Lerp(baseRadius, baseRadius * 0.45f, t);
                var y = height * t;
                // A curl, not a hook: the first pass leaned nearly twice the wick's
                // own width and read as a bent blade beside the flame.
                var lean = height * 0.13f * t * t;
                for (var col = 0; col < columns; col++)
                {
                    var u = col / (float)radialSegments;
                    var theta = u * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(theta) * r + lean, y, Mathf.Sin(theta) * r));
                    uvs.Add(new Vector2(u, t));
                }
            }

            for (var ring = 0; ring < rings - 1; ring++)
            {
                for (var col = 0; col < radialSegments; col++)
                {
                    var a = ring * columns + col;
                    var b = a + 1;
                    var c = a + columns;
                    var d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            var tip = vertices.Count;
            vertices.Add(new Vector3(height * 0.13f, height, 0f));
            uvs.Add(new Vector2(0.5f, 0.90f));
            var lastRing = (rings - 1) * columns;
            for (var col = 0; col < radialSegments; col++)
            {
                triangles.Add(lastRing + col);
                triangles.Add(tip);
                triangles.Add(lastRing + col + 1);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
