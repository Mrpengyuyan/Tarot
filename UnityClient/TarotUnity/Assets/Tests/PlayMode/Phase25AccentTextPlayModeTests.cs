using System.Collections;
using NUnit.Framework;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class Phase25AccentTextPlayModeTests
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
        public IEnumerator AccentHeaderStaysGoldWhilePlainBodyDoesNot()
        {
            root = new GameObject("Phase25_ThemeRoot");
            var theme = root.AddComponent<TarotUiTheme>();

            var header = CreateText("Header", 20);
            header.gameObject.AddComponent<TarotUiAccentText>();
            var body = CreateText("Body", 20);

            theme.Apply();
            yield return null;

            // The gold accent has a much lower blue channel than the ivory body,
            // so the marker is what keeps a header gold through the theme pass.
            Assert.That(header.color.b, Is.LessThan(0.45f), "Accent header should be gold (low blue)");
            Assert.That(body.color.b, Is.GreaterThan(0.6f), "Plain body should be ivory (high blue)");
            Assert.That(header.color, Is.Not.EqualTo(body.color));
        }

        private Text CreateText(string name, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var text = go.AddComponent<Text>();
            text.fontSize = size;
            text.color = Color.white;
            return text;
        }
    }
}
