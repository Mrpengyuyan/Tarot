using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase24TypographyPlayModeTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator ThemeAppliesFontsByRoleAtRuntime()
        {
            // Two distinct probe fonts let us assert the runtime applier routes
            // each text to the font for its role without needing the real asset.
            var bodyFont = new Font("Phase24_BodyProbe");
            var displayFont = new Font("Phase24_DisplayProbe");

            root = new GameObject("Phase24_ThemeRoot");
            var theme = root.AddComponent<TarotUiTheme>();

            var bodyText = CreateText("Body", 18);
            var titleText = CreateText("Title", 44);

            SetPrivateFont(theme, "bodyFont", bodyFont);
            SetPrivateFont(theme, "displayFont", displayFont);

            theme.Apply();
            yield return null;

            Assert.That(bodyText.font, Is.EqualTo(bodyFont), "Body text should receive the body font");
            Assert.That(titleText.font, Is.EqualTo(displayFont), "Large title should receive the display font");
        }

        [UnityTest]
        public IEnumerator ThemeReplacesLegacyFontOnAwake()
        {
            var bodyFont = new Font("Phase24_BodyProbe");

            root = new GameObject("Phase24_AwakeRoot");
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var bodyText = CreateText("Body", 18);
            bodyText.font = legacy;

            var theme = root.AddComponent<TarotUiTheme>();
            SetPrivateFont(theme, "bodyFont", bodyFont);
            theme.Apply();
            yield return null;

            Assert.That(bodyText.font, Is.Not.EqualTo(legacy), "Theme should replace the built-in legacy font");
            Assert.That(bodyText.font, Is.EqualTo(bodyFont));
        }

        private Text CreateText(string name, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var text = go.AddComponent<Text>();
            text.fontSize = size;
            text.text = name;
            return text;
        }

        private static void SetPrivateFont(TarotUiTheme theme, string field, Font font)
        {
            typeof(TarotUiTheme)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(theme, font);
        }
    }
}
