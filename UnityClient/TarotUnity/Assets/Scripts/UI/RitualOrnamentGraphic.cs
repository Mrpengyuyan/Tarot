using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>Native UI ink; never a screenshot, texture asset or pointer target.</summary>
    public sealed class RitualOrnamentGraphic : Image
    {
        public bool Seal { get; set; }
        public bool CenterMark { get; set; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            if (!Seal)
            {
                var c = color;
                var transparent = new Color(c.r, c.g, c.b, 0f);
                var top = rect.yMax;
                var bottom = rect.yMin;
                Quad(rect.xMin, rect.xMin + rect.width * 0.5f, bottom, top, transparent, c);
                Quad(rect.center.x, rect.xMax, bottom, top, c, transparent);
                if (CenterMark)
                {
                    var r = 2.5f * rect.width / 265.824f;
                    var n = mesh.currentVertCount;
                    mesh.AddVert(new Vector2(0f,r),c,Vector2.zero); mesh.AddVert(new Vector2(r,0f),c,Vector2.zero);
                    mesh.AddVert(new Vector2(0f,-r),c,Vector2.zero); mesh.AddVert(new Vector2(-r,0f),c,Vector2.zero);
                    mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
                }
                return;
            }
            var scale = rect.width / 80f;
            var center = rect.center;
            var ink = color;
            foreach (var ring in new[] { new Vector2(26f,0.6f), new Vector2(33f,0.35f), new Vector2(6f,0.7f) })
                for (var i = 0; i < 80; i++)
                {
                    var a = i * Mathf.PI / 40f;
                    var b = (i+1) * Mathf.PI / 40f;
                    Line(center + new Vector2(Mathf.Cos(a),Mathf.Sin(a)) * ring.x * scale,
                        center + new Vector2(Mathf.Cos(b),Mathf.Sin(b)) * ring.x * scale, ring.y * scale, ink);
                }
            for (var i = 0; i < 8; i++)
            {
                var direction = new Vector2(Mathf.Cos(i*Mathf.PI/4f), Mathf.Sin(i*Mathf.PI/4f));
                Line(center + direction * 26f * scale, center + direction * 38f * scale, 0.6f * scale, ink);
            }
            var star = new[] { new Vector2(0,20), new Vector2(5,5), new Vector2(20,0), new Vector2(5,-5),
                new Vector2(0,-20), new Vector2(-5,-5), new Vector2(-20,0), new Vector2(-5,5) };
            for (var i = 0; i < star.Length; i++) Line(center + star[i]*scale, center + star[(i+1)%star.Length]*scale, 0.8f*scale, ink);

            void Quad(float x1, float x2, float y1, float y2, Color left, Color right)
            {
                var n = mesh.currentVertCount;
                mesh.AddVert(new Vector3(x1,y1),left,Vector2.zero); mesh.AddVert(new Vector3(x1,y2),left,Vector2.zero);
                mesh.AddVert(new Vector3(x2,y2),right,Vector2.zero); mesh.AddVert(new Vector3(x2,y1),right,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
            }
            void Line(Vector2 a, Vector2 b, float width, Color c)
            {
                var direction = new Vector2(-(b-a).y,(b-a).x).normalized;
                var half = direction * width * 0.5f;
                var pixel = 1f / Mathf.Max(canvas != null ? canvas.scaleFactor : 1f, 0.001f);
                var edge = half + direction * pixel;
                c.a *= width / (width + pixel);
                var transparent = new Color(c.r,c.g,c.b,0f);
                // Coverage fringes retain subpixel SVG-like strokes without depending on camera MSAA.
                Strip(a-edge,a-half,b-half,b-edge,transparent,c);
                Strip(a-half,a+half,b+half,b-half,c,c);
                Strip(a+half,a+edge,b+edge,b+half,c,transparent);
            }
            void Strip(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color left, Color right)
            {
                var n = mesh.currentVertCount;
                mesh.AddVert(a,left,Vector2.zero); mesh.AddVert(b,right,Vector2.zero);
                mesh.AddVert(c,right,Vector2.zero); mesh.AddVert(d,left,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
