using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase11VisualReviewTests
    {
        private const string ReviewDocPath = "Docs/PHASE11_VISUAL_REVIEW.md";
        private const string ReviewFolder = "Docs/VisualReview/Phase11";

        [Test]
        public void Phase11BuilderDefinesVisualReviewTargets()
        {
            var builderType = System.Type.GetType("TarotUnity.Editor.Phase11VisualReviewBuilder, Assembly-CSharp-Editor");
            Assert.That(builderType, Is.Not.Null);

            var folder = builderType.GetField("ReviewFolder")?.GetValue(null) as string;
            Assert.That(folder, Is.EqualTo(ReviewFolder));

            var screenshotNames = builderType.GetProperty("ScreenshotFileNames")?.GetValue(null) as string[];
            Assert.That(screenshotNames, Is.Not.Null);
            CollectionAssert.AreEquivalent(
                new[] { "MainMenu.png", "ReadingRoom.png", "Result.png" },
                screenshotNames);
        }

        [Test]
        public void Phase11ReviewDocCapturesVisualAndCardArtScope()
        {
            Assert.That(File.Exists(ReviewDocPath), Is.True);
            var text = File.ReadAllText(ReviewDocPath);

            Assert.That(text, Does.Contain("Phase 11"));
            Assert.That(text, Does.Contain("Main Menu"));
            Assert.That(text, Does.Contain("Reading Room"));
            Assert.That(text, Does.Contain("Result"));
            Assert.That(text, Does.Contain("Hearthstone"));
            Assert.That(text, Does.Contain("real tarot card artwork"));
            Assert.That(text, Does.Contain("license"));
        }

        [Test]
        public void Phase11ScreenshotArtifactsExistForCoreScenes()
        {
            AssertScreenshot("MainMenu.png");
            AssertScreenshot("ReadingRoom.png");
            AssertScreenshot("Result.png");

            var manifestPath = Path.Combine(ReviewFolder, "phase11_visual_review_manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True);

            var manifest = File.ReadAllText(manifestPath);
            Assert.That(manifest, Does.Contain("MainMenu.png"));
            Assert.That(manifest, Does.Contain("ReadingRoom.png"));
            Assert.That(manifest, Does.Contain("Result.png"));
        }

        [Test]
        public void Phase11FinalAdjustmentAnchorsExistInCoreScenes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menuCanvas = GameObject.Find("MainMenuCanvas");
            Assert.That(menuCanvas, Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Phase11_MenuDepthFrame"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Phase11_ActionRail"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("StartReadingButton")?.GetComponent<RectTransform>().anchoredPosition.y, Is.LessThanOrEqualTo(-70f));

            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            var roomCanvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(roomCanvas, Is.Not.Null);
            Assert.That(roomCanvas.transform.Find("Phase11_TableFocusFrame"), Is.Not.Null);
            Assert.That(roomCanvas.transform.Find("Phase11_ActionDock"), Is.Not.Null);
            Assert.That(roomCanvas.transform.Find("RevealResultButton")?.GetComponent<RectTransform>().anchoredPosition.y, Is.LessThanOrEqualTo(-210f));

            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            var resultCanvas = GameObject.Find("ResultCanvas");
            Assert.That(resultCanvas, Is.Not.Null);
            Assert.That(resultCanvas.transform.Find("Phase11_ResultReadingColumns"), Is.Not.Null);
            Assert.That(resultCanvas.transform.Find("Phase11_ResultCardPresence"), Is.Not.Null);

            // Phase 29 moved OverallText into the scroll Content, so it is no longer a
            // direct child of the canvas and its position is layout-driven. Find it
            // recursively, and assert the reading column container (the scroll panel that
            // now carries it) still sits in the right column.
            var overall = FindDescendant(resultCanvas.transform, "OverallText");
            Assert.That(overall, Is.Not.Null);

            var readingColumn = FindDescendant(resultCanvas.transform, "ResultReadingScroll");
            Assert.That(readingColumn, Is.Not.Null, "Phase 29 reading scroll column should exist");
            Assert.That(readingColumn.GetComponent<RectTransform>().anchoredPosition.x, Is.GreaterThanOrEqualTo(140f));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static void AssertScreenshot(string fileName)
        {
            var path = Path.Combine(ReviewFolder, fileName);
            Assert.That(File.Exists(path), Is.True, $"Missing screenshot {path}");
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096), $"Screenshot {path} is unexpectedly small.");

            var textureBytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(textureBytes), Is.True, $"Invalid PNG {path}");
                Assert.That(texture.width, Is.EqualTo(1280));
                Assert.That(texture.height, Is.EqualTo(720));
                Assert.That(HasVisibleContent(texture), Is.True, $"Screenshot {path} appears to be a uniform placeholder.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static bool HasVisibleContent(Texture2D texture)
        {
            var minLuminance = float.MaxValue;
            var maxLuminance = float.MinValue;
            var colorChanges = 0;
            var lastColor = default(Color32);
            var hasLast = false;

            for (var y = 0; y < texture.height; y += 24)
            {
                for (var x = 0; x < texture.width; x += 24)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = color.grayscale;
                    minLuminance = Mathf.Min(minLuminance, luminance);
                    maxLuminance = Mathf.Max(maxLuminance, luminance);

                    var color32 = (Color32)color;
                    if (hasLast && !color32.Equals(lastColor))
                    {
                        colorChanges++;
                    }

                    lastColor = color32;
                    hasLast = true;
                }
            }

            return maxLuminance - minLuminance > 0.08f && colorChanges > 16;
        }
    }
}
