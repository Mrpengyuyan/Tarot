using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the result page and the menu's start button.</summary>
    public sealed class Phase69ResultMenuRestyleTests
    {
        [Test]
        public void ResultPanelsAreGlass()
        {
            var root = OpenResult();
            foreach (var path in new[] { "ResultReadingScroll", "Phase12_ResultCardShowcase" })
            {
                var image = root.Find(path).GetComponent<Image>();
                Assert.That(image.sprite?.name, Is.EqualTo("GlassPanel"), path);
                Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f), path);
            }

            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var frame = band.GetChild(i).Find("ReversePivot/Frame")?.GetComponent<Image>();
                Assert.That(frame, Is.Not.Null, $"cell {i}");
                Assert.That(frame.sprite?.name, Is.EqualTo("GlassPanel"), $"cell {i}");
            }
        }

        [TestCase("BackToMenuButton", true)]
        [TestCase("Phase66_RetryInterpretationButton", true)]
        [TestCase("Phase66_OfflineInterpretationButton", false)]
        public void ResultButtonsAreSkinnedAndFit(string name, bool cardStock)
        {
            var root = OpenResult();
            var rect = root.Find(name) as RectTransform;
            var skin = rect.GetComponent<UiSkinState>();
            Assert.That(skin, Is.Not.Null, name);
            Assert.That(skin.IsEmphasized, Is.EqualTo(cardStock), name);
            AssertFits(rect, name);
        }

        [Test]
        public void ResultTypeIsScaledButTheCardCaptionsAreNot()
        {
            var root = OpenResult();
            Assert.That(root.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(UiFitLayout.MutedSizeThresholdScaled));
            Assert.That(root.Find("BackToMenuButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(19.5f));
            var band = root.Find("MP_ResultSpreadBand");
            for (var i = 0; i < band.childCount; i++)
            {
                var label = band.GetChild(i).Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.fontSize, Is.EqualTo(22f), "the presenter sizes card captions at runtime (Phase 60 default 22)");
            }
        }

        [Test]
        public void MenuStartButtonIsCardStockAndOnlyItsTextGrew()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var root = GameObject.Find("MainMenuCanvas").transform;
            var start = root.Find("StartReadingButton") as RectTransform;
            var skin = start.GetComponent<UiSkinState>();
            Assert.That(skin, Is.Not.Null);
            Assert.That(skin.IsEmphasized, Is.True);
            Assert.That(start.GetComponent<Image>().sprite?.name, Is.EqualTo("CardStock"));
            Assert.That(start.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(start.Find("Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
            Assert.That(root.Find("QuitButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(18f), "离席 unchanged");
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(16f));
            Assert.That(start.sizeDelta.x, Is.GreaterThanOrEqualTo(308f), "the invitation keeps its authored width");
            AssertFits(start, "StartReadingButton");
        }

        private static Transform OpenResult()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            return GameObject.Find("ResultCanvas").transform;
        }

        private static void AssertFits(RectTransform rect, string name)
        {
            var label = rect.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var preferred = label.GetPreferredValues(label.text);
            var needed = UiFitLayout.FitSize(preferred, label.fontSize);
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(needed.x - 0.01f), $"{name}: width");
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(needed.y - 0.01f), $"{name}: height");
        }
    }
}
