using NUnit.Framework;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the small runtime pieces of the Result reading - aspect-aware scaling with
    /// edge-pinned header and buttons, the bottom fade, scroll targeting, card hover, and
    /// reveal companions.
    /// </summary>
    public sealed class Phase67ResultReadingComponentsTests
    {
        private GameObject root;

        [TearDown]
        public void DestroyRoot()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(1920, 1080, 1f)]
        [TestCase(2560, 1080, 1f)]
        [TestCase(1440, 900, 0f)]
        [TestCase(1024, 768, 0f)]
        public void AspectFitMatchesHeightOnWideScreensAndWidthOnNarrowOnes(int width, int height, float expected)
        {
            Assert.That(ResultCanvasAspectFit.MatchFor(width, height), Is.EqualTo(expected));
        }

        [Test]
        public void PinnedElementsKeepTheirReferenceOffsetsFromTheEdges()
        {
            Assert.That(ResultCanvasAspectFit.PinnedY(720f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(300f),
                "QuestionText keeps its saved position at 16:9");
            Assert.That(ResultCanvasAspectFit.PinnedY(720f, ResultCanvasAspectFit.Edge.Bottom, 60f), Is.EqualTo(-300f),
                "BackToMenuButton keeps its saved position at 16:9");
            Assert.That(ResultCanvasAspectFit.PinnedY(960f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(420f));
            Assert.That(ResultCanvasAspectFit.PinnedY(960f, ResultCanvasAspectFit.Edge.Bottom, 60f), Is.EqualTo(-420f));
            Assert.That(ResultCanvasAspectFit.PinnedY(0f, ResultCanvasAspectFit.Edge.Top, 60f), Is.EqualTo(300f),
                "an unsized canvas falls back to the reference height");
        }

        [Test]
        public void FadeGradientRunsFromTransparentTopToOpaqueBottom()
        {
            root = new GameObject("Phase67_FadeTest", typeof(RectTransform));
            root.AddComponent<Image>();
            var gradient = root.AddComponent<ReadingFadeGradient>();

            var mesh = new VertexHelper();
            try
            {
                AddVertex(mesh, 0f, 0f);
                AddVertex(mesh, 0f, 36f);
                AddVertex(mesh, 100f, 36f);
                AddVertex(mesh, 100f, 0f);

                gradient.ModifyMesh(mesh);

                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, 0);
                Assert.That(vertex.color.a, Is.EqualTo(255), "the bottom edge stays opaque");
                mesh.PopulateUIVertex(ref vertex, 1);
                Assert.That(vertex.color.a, Is.EqualTo(0), "the top edge is transparent");
                mesh.PopulateUIVertex(ref vertex, 2);
                Assert.That(vertex.color.r, Is.EqualTo(200), "control: the colour itself is untouched");
            }
            finally
            {
                mesh.Dispose();
            }
        }

        [Test]
        public void NavigatorScrollTargetKeepsTheHeadingNearTheTopAndClamps()
        {
            // Content 1000 tall, viewport 250 -> 750 scrollable; a heading 400 down aims 30 (12%) above it.
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(400f, 1000f, 250f, 0.12f),
                Is.EqualTo(1f - (400f - 30f) / 750f).Within(0.0001f));
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(10f, 1000f, 250f, 0.12f), Is.EqualTo(1f),
                "a heading near the top clamps to the top");
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(990f, 1000f, 250f, 0.12f), Is.EqualTo(0f),
                "a heading near the end clamps to the bottom");
            Assert.That(ResultReadingNavigator.NormalizedPositionFor(400f, 200f, 250f, 0.12f), Is.EqualTo(1f),
                "text that fits does not scroll");
        }

        [Test]
        public void FadeShowsOnlyWhileMoreTextIsBelow()
        {
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 1f), Is.True, "at the top of long text");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 0f), Is.False, "at the bottom");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 1000f, 250f, 7f / 750f), Is.False, "within 8 of the bottom");
            Assert.That(ResultReadingNavigator.ShouldShowFade(true, 200f, 250f, 1f), Is.False, "text that fits");
            Assert.That(ResultReadingNavigator.ShouldShowFade(false, 1000f, 250f, 1f), Is.False, "pending or failed readings");
        }

        [Test]
        public void HoverLiftsTheCellOnlyWhileTheReadingIsInteractive()
        {
            root = new GameObject("Phase67_CellTest", typeof(RectTransform));
            var navigator = root.AddComponent<ResultReadingNavigator>();
            var cell = new GameObject("Cell", typeof(RectTransform));
            cell.transform.SetParent(root.transform, false);
            var target = cell.AddComponent<ResultSpreadCellTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("navigator").objectReferenceValue = navigator;
            so.ApplyModifiedPropertiesWithoutUndo();
            target.SetBaseScale(0.52f);

            target.OnPointerEnter(null);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f).Within(0.0001f), "no lift while the reading is hidden");

            navigator.SetInteractive(true);
            target.OnPointerEnter(null);
            Assert.That(target.IsHovered, Is.True);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f * 1.04f).Within(0.0001f));

            target.OnPointerExit(null);
            Assert.That(cell.transform.localScale.x, Is.EqualTo(0.52f).Within(0.0001f));
        }

        [Test]
        public void RevealDirectorShowsCompanionsWithTheirGroup()
        {
            root = new GameObject("Phase67_RevealTest");
            root.SetActive(false);
            var director = root.AddComponent<ResultRevealDirector>();
            var primary = new GameObject("Primary").AddComponent<CanvasGroup>();
            var companion = new GameObject("Companion").AddComponent<CanvasGroup>();
            primary.transform.SetParent(root.transform);
            companion.transform.SetParent(root.transform);
            primary.alpha = 0f;
            companion.alpha = 0f;

            var so = new SerializedObject(director);
            var groups = so.FindProperty("revealGroups");
            groups.arraySize = 1;
            groups.GetArrayElementAtIndex(0).objectReferenceValue = primary;
            var companions = so.FindProperty("companions");
            Assert.That(companions, Is.Not.Null, "ResultRevealDirector needs a serialized companions array");
            companions.arraySize = 1;
            companions.GetArrayElementAtIndex(0).FindPropertyRelative("groupIndex").intValue = 0;
            companions.GetArrayElementAtIndex(0).FindPropertyRelative("group").objectReferenceValue = companion;
            so.ApplyModifiedPropertiesWithoutUndo();

            director.PlayReveal(); // an inactive director shows everything at once

            Assert.That(primary.alpha, Is.EqualTo(1f), "control: the primary group is shown");
            Assert.That(companion.alpha, Is.EqualTo(1f), "a companion is shown with its group");
            Assert.That(companion.blocksRaycasts, Is.True);
        }

        private static void AddVertex(VertexHelper mesh, float x, float y)
        {
            mesh.AddVert(new UIVertex { position = new Vector3(x, y, 0f), color = new Color32(200, 100, 50, 255) });
        }
    }
}
