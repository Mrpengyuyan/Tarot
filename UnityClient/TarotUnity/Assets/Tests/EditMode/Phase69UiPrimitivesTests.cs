using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the fit maths and the glass / card-stock switch.</summary>
    public sealed class Phase69UiPrimitivesTests
    {
        [TestCase(13f, 15f)]
        [TestCase(15f, 17.5f)]
        [TestCase(16f, 18.5f)]
        [TestCase(17f, 19.5f)]
        [TestCase(18f, 20.5f)]
        [TestCase(20f, 23f)]
        [TestCase(22f, 25.5f)]
        [TestCase(23f, 26.5f)]
        [TestCase(24f, 27.5f)]
        public void TypeScaleRoundsHalfUpToHalfPoints(float size, float expected)
        {
            Assert.That(UiFitLayout.ScaledFontSize(size, UiFitLayout.TypeScale), Is.EqualTo(expected));
        }

        [Test]
        public void ScalingByOneLeavesTheSizeAlone()
        {
            Assert.That(UiFitLayout.ScaledFontSize(17.5f, 1f), Is.EqualTo(17.5f));
            Assert.That(UiFitLayout.ScaledFontSize(19.5f, UiFitLayout.TypeScale / UiFitLayout.TypeScale), Is.EqualTo(19.5f));
        }

        [Test]
        public void MutedThresholdTracksTheScaledSixteen()
        {
            Assert.That(UiFitLayout.MutedSizeThresholdScaled, Is.EqualTo(UiFitLayout.ScaledFontSize(16f, UiFitLayout.TypeScale)));
        }

        [Test]
        public void FitSizeWrapsTextInPaddingAndTheSkinMargin()
        {
            var size = UiFitLayout.FitSize(new Vector2(52.3f, 22.1f), 17.5f);
            Assert.That(UiFitLayout.Padding(17.5f), Is.EqualTo(new Vector2(24f, 9f)), "Mathf.Round: 24.5 -> 24, 8.75 -> 9");
            Assert.That(size.x, Is.EqualTo(53f + 2f * 24f + 2f * UiFitLayout.SkinMargin));
            Assert.That(size.y, Is.EqualTo(23f + 2f * 9f + 2f * UiFitLayout.SkinMargin));
        }

        [Test]
        public void RowCentersAreSymmetricWithTheGivenGap()
        {
            var centers = UiFitLayout.RowCenters(new[] { 100f, 60f, 100f }, 20f);
            Assert.That(centers, Is.EqualTo(new[] { -100f, 0f, 100f }));
            var single = UiFitLayout.RowCenters(new[] { 80f }, 20f);
            Assert.That(single, Is.EqualTo(new[] { 0f }));
        }

        [Test]
        public void EmphasisSwapsSpriteAndLabelColour()
        {
            var (skin, plate, label, glass, stock) = MakeSkin(withGlass: true);
            skin.SetEmphasis(false);
            Assert.That(skin.IsEmphasized, Is.False);
            Assert.That(plate.sprite, Is.SameAs(glass));
            Assert.That(plate.enabled, Is.True);
            Assert.That(label.color, Is.EqualTo(UiSkinState.GlassLabel));

            skin.SetEmphasis(true);
            Assert.That(skin.IsEmphasized, Is.True);
            Assert.That(plate.sprite, Is.SameAs(stock));
            Assert.That(plate.color, Is.EqualTo(Color.white));
            Assert.That(label.color, Is.EqualTo(UiSkinState.CardStockLabel));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void NoGlassMeansNoPlateWhenNotEmphasized()
        {
            var (skin, plate, _, _, stock) = MakeSkin(withGlass: false);
            skin.SetEmphasis(false);
            Assert.That(plate.enabled, Is.False);
            Assert.That(plate.color, Is.EqualTo(Color.clear), "Phase 61 compares plate brightness by colour");
            skin.SetEmphasis(true);
            Assert.That(plate.enabled, Is.True);
            Assert.That(plate.sprite, Is.SameAs(stock));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void ButtonsGetTheSkinColorBlock()
        {
            var (skin, _, _, _, _) = MakeSkin(withGlass: true);
            var button = skin.gameObject.AddComponent<Button>();
            skin.SetEmphasis(true);
            Assert.That(button.colors.normalColor, Is.EqualTo(Color.white));
            Assert.That(button.colors.disabledColor.a, Is.EqualTo(0.45f).Within(0.001f));
            Object.DestroyImmediate(skin.gameObject);
        }

        [Test]
        public void LabelColourCanBeLeftToAnotherDriver()
        {
            var go = new GameObject("chip", typeof(RectTransform));
            var plate = go.AddComponent<Image>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.color = Color.red;
            var skin = go.AddComponent<UiSkinState>();
            skin.Configure(plate, label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), false);
            skin.SetEmphasis(true);
            Assert.That(label.color, Is.EqualTo(Color.red));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TypeScaleMarkerDefaultsToOne()
        {
            var go = new GameObject("marker");
            var marker = go.AddComponent<UiTypeScale>();
            Assert.That(marker.AppliedScale, Is.EqualTo(1f));
            marker.AppliedScale = UiFitLayout.TypeScale;
            Assert.That(marker.AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Object.DestroyImmediate(go);
        }

        private static (UiSkinState, Image, TMPro.TMP_Text, Sprite, Sprite) MakeSkin(bool withGlass)
        {
            var go = new GameObject("skinned", typeof(RectTransform));
            var plate = go.AddComponent<Image>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            var glass = withGlass ? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero) : null;
            var stock = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var skin = go.AddComponent<UiSkinState>();
            skin.Configure(plate, label, glass, stock, true);
            return (skin, plate, label, glass, stock);
        }
    }
}
