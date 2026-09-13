using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 67 (spec C 4.2): the bottom fade of the reading panel - a plain quad whose vertex
    /// alpha runs from topAlpha at its top edge to bottomAlpha at its bottom edge, so no
    /// gradient texture is needed.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class ReadingFadeGradient : BaseMeshEffect
    {
        [SerializeField, Range(0f, 1f)] private float topAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float bottomAlpha = 1f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh == null || vh.currentVertCount == 0)
            {
                return;
            }

            var vertex = new UIVertex();
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            var height = Mathf.Max(0.0001f, maxY - minY);
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var t = (vertex.position.y - minY) / height;
                var color = vertex.color;
                color.a = (byte)Mathf.RoundToInt(color.a * Mathf.Lerp(bottomAlpha, topAlpha, t));
                vertex.color = color;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
