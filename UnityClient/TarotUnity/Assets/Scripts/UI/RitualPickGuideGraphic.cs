using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RitualPickGuideGraphic : MaskableGraphic
    {
        public bool Tick;
        private bool filled;
        public bool Filled { get => filled; set { if(filled==value)return;filled=value;SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (Tick && Filled)
            {
                Rect(vh,r);
                return;
            }
            if (!Tick) Quad(vh, new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),new Color(0.7f,0.53f,0.24f,0.016f));
            var thickness = Mathf.Max(0.5f, r.width / (Tick ? 5.112f : 83f));
            Rect(vh, new Rect(r.xMin,r.yMin,r.width,thickness));
            Rect(vh, new Rect(r.xMin,r.yMax-thickness,r.width,thickness));
            Rect(vh, new Rect(r.xMin,r.yMin,thickness,r.height));
            Rect(vh, new Rect(r.xMax-thickness,r.yMin,thickness,r.height));
        }
        private void Rect(VertexHelper vh, Rect r) => Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),color);
        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink)
        {
            var start = vh.currentVertCount;
            vh.AddVert(a,ink,Vector2.zero); vh.AddVert(b,ink,Vector2.zero); vh.AddVert(c,ink,Vector2.zero); vh.AddVert(d,ink,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
