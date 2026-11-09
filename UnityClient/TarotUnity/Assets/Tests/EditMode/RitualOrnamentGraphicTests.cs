using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualOrnamentGraphicTests
    {
        [Test]
        public void SubpixelSealStrokesHaveTransparentCoverageEdges()
        {
            var node = new GameObject("NativeSealTest", typeof(RectTransform), typeof(RitualOrnamentGraphic));
            var mesh = new Mesh();
            try
            {
                var art = node.GetComponent<RitualOrnamentGraphic>();
                art.Seal = true;
                art.rectTransform.sizeDelta = new Vector2(47.3f,47.3f);
                using var helper = new VertexHelper();
                typeof(RitualOrnamentGraphic).GetMethod("OnPopulateMesh", BindingFlags.NonPublic|BindingFlags.Instance,
                    null,new[] {typeof(VertexHelper)},null)
                    .Invoke(art,new object[] { helper });
                helper.FillMesh(mesh);
                Assert.That(mesh.colors32.Any(c=>c.a==0), Is.True,
                    "Subpixel vector strokes need a transparent coverage fringe, not broken binary pixel coverage.");
                Assert.That(mesh.colors32.Any(c=>c.a>0), Is.True);
                Assert.That(mesh.colors32.Max(c=>c.a), Is.LessThan(128),
                    "Coverage fringes must preserve subpixel stroke energy, not make the seal several times brighter.");
            }
            finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(node); }
        }
    }
}
