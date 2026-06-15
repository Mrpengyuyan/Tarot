using System.IO;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 30 verifies and guards the secondary spacing/alignment the three core
    /// screens already share: one canvas-scaler definition, symmetric centering of every
    /// centered heading/footer, and heading/sub-label pairs that never overlap. These are
    /// regression guards - if a later phase nudges one screen out of step, a test fails.
    /// </summary>
    public sealed class Phase30CrossScreenConsistencyTests
    {
        private const string DocPath = "Docs/PHASE30_CROSS_SCREEN_CONSISTENCY.md";

        private static readonly (string scene, string canvas)[] Screens =
        {
            ("Assets/Scenes/MainMenu.unity", "MainMenuCanvas"),
            ("Assets/Scenes/ReadingRoom.unity", "ReadingRoomCanvas"),
            ("Assets/Scenes/Result.unity", "ResultCanvas"),
        };

        [Test]
        public void CoreScenesShareOneCanvasScalerDefinition()
        {
            foreach (var (scene, canvasName) in Screens)
            {
                EditorSceneManager.OpenScene(scene);
                var canvas = GameObject.Find(canvasName);
                Assert.That(canvas, Is.Not.Null, canvasName);

                var scaler = canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null, $"{canvasName} needs a CanvasScaler");
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize),
                    $"{canvasName} must scale with screen size for consistent layout");
                Assert.That(scaler.referenceResolution.x, Is.EqualTo(TarotUiSpacing.ReferenceWidth));
                Assert.That(scaler.referenceResolution.y, Is.EqualTo(TarotUiSpacing.ReferenceHeight));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(TarotUiSpacing.ScreenMatchWidthOrHeight).Within(0.001f),
                    $"{canvasName} must use the shared match factor");
            }
        }

        [Test]
        public void CenteredHeadingsAndFootersAreHorizontallySymmetric()
        {
            // Elements that are meant to be centered on their screen.
            var centered = new (string scene, string canvas, string[] elements)[]
            {
                ("Assets/Scenes/MainMenu.unity", "MainMenuCanvas",
                    new[] { "TitleText", "SubtitleText", "StatusText", "StartReadingButton" }),
                ("Assets/Scenes/Result.unity", "ResultCanvas",
                    new[] { "QuestionText", "SpreadNameText", "BackToMenuButton" }),
            };

            foreach (var (scene, canvasName, elements) in centered)
            {
                EditorSceneManager.OpenScene(scene);
                var canvas = GameObject.Find(canvasName);
                Assert.That(canvas, Is.Not.Null, canvasName);
                var canvasRect = canvas.GetComponent<RectTransform>();
                var canvasCenterX = CenterX(canvasRect);

                foreach (var name in elements)
                {
                    var rt = FindRect(canvas.transform, name);
                    Assert.That(rt, Is.Not.Null, $"{canvasName}/{name}");
                    Assert.That(CenterX(rt), Is.EqualTo(canvasCenterX).Within(TarotUiSpacing.CenterSymmetryTolerance),
                        $"{name} on {canvasName} should be horizontally centered");
                }
            }
        }

        [Test]
        public void HeadingSubLabelPairsDoNotOverlap()
        {
            var pairs = new (string scene, string canvas, string heading, string subLabel)[]
            {
                ("Assets/Scenes/MainMenu.unity", "MainMenuCanvas", "TitleText", "SubtitleText"),
                ("Assets/Scenes/Result.unity", "ResultCanvas", "QuestionText", "SpreadNameText"),
            };

            foreach (var (scene, canvasName, headingName, subLabelName) in pairs)
            {
                EditorSceneManager.OpenScene(scene);
                var canvas = GameObject.Find(canvasName);
                Assert.That(canvas, Is.Not.Null, canvasName);

                var heading = FindRect(canvas.transform, headingName);
                var subLabel = FindRect(canvas.transform, subLabelName);
                Assert.That(heading, Is.Not.Null, headingName);
                Assert.That(subLabel, Is.Not.Null, subLabelName);

                // Sub-label sits below the heading; its top must not rise above the
                // heading's bottom by more than the allowed overlap.
                var overlap = TopY(subLabel) - BottomY(heading);
                Assert.That(overlap, Is.LessThanOrEqualTo(TarotUiSpacing.HeadingSubLabelMaxOverlap),
                    $"{subLabelName} overlaps {headingName} on {canvasName} by {overlap}");
                Assert.That(BottomY(subLabel), Is.LessThan(BottomY(heading)),
                    $"{subLabelName} should sit below {headingName} on {canvasName}");
            }
        }

        [Test]
        public void Phase30DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase30 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("consistency"));
            Assert.That(text, Does.Contain("CanvasScaler"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private static float CenterX(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return (CornerBuffer[0].x + CornerBuffer[2].x) * 0.5f;
        }

        private static float TopY(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[1].y;
        }

        private static float BottomY(RectTransform r)
        {
            r.GetWorldCorners(CornerBuffer);
            return CornerBuffer[0].y;
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t as RectTransform;
                }
            }

            return null;
        }
    }
}
