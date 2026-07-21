using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 45 guards the menu's mid-ground and its type block. The specific
    /// trap this pins down: a Light added to a prop root shares that root's
    /// Transform, so any code that writes light.transform.localPosition drags the
    /// whole prop to the origin. That bug has now stacked props in the centre of
    /// this scene twice (Phase 42's candles, Phase 45's orb and censer), so the
    /// rule is asserted rather than remembered - prop lights live on children.
    /// </summary>
    public sealed class Phase45MenuDepthTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [Test]
        public void PropLightsLiveOnChildrenNotOnPropRoots()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var name in new[] { "MP_ScryingOrb", "MP_Censer" })
            {
                var prop = GameObject.Find(name);
                Assert.That(prop, Is.Not.Null, name);
                Assert.That(prop.GetComponent<Light>(), Is.Null,
                    $"{name}'s light must sit on a child; on the root, light.transform IS the prop");
                Assert.That(prop.GetComponentInChildren<Light>(true), Is.Not.Null,
                    $"{name} still needs its light");
            }
        }

        [Test]
        public void MidGroundPropsStandWhereTheyWereStaged()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var orb = GameObject.Find("MP_ScryingOrb");
            Assert.That(orb, Is.Not.Null);
            Assert.That(orb.transform.position.magnitude, Is.GreaterThan(0.5f),
                "the orb was dragged to the origin by a root light");
            Assert.That(orb.transform.Find("Orb"), Is.Not.Null);
            Assert.That(orb.transform.Find("Stand"), Is.Not.Null);

            var censer = GameObject.Find("MP_Censer");
            Assert.That(censer, Is.Not.Null);
            Assert.That(censer.transform.position.magnitude, Is.GreaterThan(0.5f),
                "the censer was dragged to the origin by a root light");
            Assert.That(censer.transform.Find("Coals"), Is.Not.Null, "the censer must read as burning");
            Assert.That(censer.transform.Find("Smoke")?.GetComponent<ParticleSystem>(), Is.Not.Null);
        }

        [Test]
        public void CandlesReadAtTwoDepths()
        {
            EditorSceneManager.OpenScene(ScenePath);

            foreach (var name in new[]
            {
                "Phase8_LeftCandle", "Phase8_RightCandle", "MP_BackCandle_L", "MP_BackCandle_R",
            })
            {
                Assert.That(GameObject.Find(name), Is.Not.Null, name);
            }

            var front = GameObject.Find("Phase8_LeftCandle").transform.position.z;
            var back = GameObject.Find("MP_BackCandle_L").transform.position.z;
            Assert.That(back, Is.GreaterThan(front + 1f),
                "one light at one depth is a lit strip, not a room");
        }

        [Test]
        public void TitleBlockIsBound()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var root = GameObject.Find("MainMenuCanvas").transform;

            var rule = root.Find("MP_TitleRule")?.GetComponent<Image>();
            Assert.That(rule, Is.Not.Null, "the gold rule joins the title to its subtitle");
            Assert.That(rule.sprite?.name, Is.EqualTo("TarotDivider"));

            var title = root.Find("TitleText").GetComponent<TMP_Text>();
            Assert.That(title.characterSpacing, Is.GreaterThan(5f),
                "the display cut is tracked open so it reads as inscribed");

            // Title, rule, subtitle read top-down as one block.
            var ruleY = ((RectTransform)rule.transform).anchoredPosition.y;
            var titleY = ((RectTransform)title.transform).anchoredPosition.y;
            var subtitleY = ((RectTransform)root.Find("SubtitleText")).anchoredPosition.y;
            Assert.That(ruleY, Is.LessThan(titleY));
            Assert.That(subtitleY, Is.LessThan(ruleY));

            var status = root.Find("StatusText").GetComponent<TMP_Text>();
            Assert.That(status.fontSize, Is.LessThan(subtitleY > 0 ? 20f : 20f),
                "the status line recedes to a footnote");
        }

        [Test]
        public void Phase45DocumentationExists()
        {
            const string doc = "Docs/PROJECT_CHRONICLE.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("light.transform"));
            Assert.That(File.Exists("Tools/UiKitGenerator/gen_arcane.py"), Is.True);
        }
    }
}
