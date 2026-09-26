using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 69: TarotUiTheme recolours every button and text on Awake. Skinned elements must
    /// keep their card stock and ink; the muted-text limit follows the type scale.
    /// </summary>
    public sealed class Phase69ThemeSkinTests
    {
        private static readonly Color ThemeText = new Color(0.96f, 0.91f, 0.80f, 1f);
        private static readonly Color ThemeMuted = new Color(0.74f, 0.72f, 0.76f, 1f);
        private GameObject root;

        [TearDown]
        public void Clean()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkinnedButtonKeepsItsColoursAndInk()
        {
            var theme = MakeTheme();
            var (button, label) = MakeButton("Skinned");
            var skin = button.gameObject.AddComponent<UiSkinState>();
            skin.Configure(button.GetComponent<Image>(), label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), true);
            skin.SetEmphasis(true);

            theme.Apply();

            Assert.That(button.colors.normalColor, Is.EqualTo(Color.white));
            Assert.That(button.targetGraphic.color, Is.EqualTo(Color.white));
            Assert.That(label.color, Is.EqualTo(UiSkinState.CardStockLabel));
        }

        [Test]
        public void UnskinnedButtonStillGetsTheThemeColours()
        {
            var theme = MakeTheme();
            var (button, _) = MakeButton("Plain");

            theme.Apply();

            Assert.That(button.colors.normalColor, Is.Not.EqualTo(Color.white), "control: the theme still styles plain buttons");
        }

        [Test]
        public void MutedLimitIsSerializedAndDrivesTheColour()
        {
            var theme = MakeTheme();
            var text = MakeText("Small", 17.5f);

            theme.Apply();
            Assert.That(text.color, Is.EqualTo(ThemeText), "17.5 is above the default 16 limit");

            var so = new SerializedObject(theme);
            so.FindProperty("mutedSizeThreshold").floatValue = UiFitLayout.MutedSizeThresholdScaled;
            so.ApplyModifiedPropertiesWithoutUndo();
            theme.Apply();
            Assert.That(theme.MutedSizeThreshold, Is.EqualTo(18.5f));
            Assert.That(text.color, Is.EqualTo(ThemeMuted));
        }

        [Test]
        public void PreservedInputKeepsItsTransparentGround()
        {
            var theme = MakeTheme();
            var go = new GameObject("Input", typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var ground = go.AddComponent<Image>();
            ground.color = new Color(1f, 1f, 1f, 0f);
            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = ground;
            go.AddComponent<TarotUiPreserveColor>();

            theme.Apply();

            Assert.That(ground.color.a, Is.EqualTo(0f));
        }

        private TarotUiTheme MakeTheme()
        {
            root = new GameObject("ThemeRoot", typeof(RectTransform));
            return root.AddComponent<TarotUiTheme>();
        }

        private (Button, TMP_Text) MakeButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = UiSkinState.SkinColors;
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 19.5f;
            return (button, label);
        }

        private TMP_Text MakeText(string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            return text;
        }
    }
}
