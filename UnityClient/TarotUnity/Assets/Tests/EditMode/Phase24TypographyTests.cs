using System.IO;
using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase24TypographyTests
    {
        private const string BodyFontPath = "Assets/Fonts/LXGWWenKai-Regular.ttf";
        private const string DisplayFontPath = "Assets/Fonts/LXGWWenKai-Medium.ttf";
        private const string TmpBodyFontPath = "Assets/Fonts/LXGWWenKai-Regular SDF.asset";
        private const string TmpDisplayFontPath = "Assets/Fonts/LXGWWenKai-Medium SDF.asset";
        private const string OflPath = "Assets/Fonts/OFL.txt";
        private const string Phase24DocPath = "Docs/PHASE24_TYPOGRAPHY.md";
        private const int DisplaySizeThreshold = 30;

        private static readonly string[] UiScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ReadingRoom.unity",
            "Assets/Scenes/Result.unity",
        };

        [Test]
        public void BundledFontsAndLicenseExist()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath), Is.Not.Null, "Body font (LXGW WenKai Regular) missing");
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath), Is.Not.Null, "Display font (LXGW WenKai Medium) missing");
            Assert.That(File.Exists(OflPath), Is.True, "OFL license text must ship alongside the bundled OFL fonts");
        }

        [Test]
        public void BundledFontsAreDynamicWithEmbeddedData()
        {
            foreach (var path in new[] { BodyFontPath, DisplayFontPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                Assert.That(importer, Is.Not.Null, $"No TrueType font importer at {path}");
                Assert.That(importer.fontTextureCase, Is.EqualTo(FontTextureCase.Dynamic),
                    "Dynamic so arbitrary runtime glyphs (AI interpretation text) rasterize on demand");
                Assert.That(importer.includeFontData, Is.True,
                    "Embedded font data so the shipped build never depends on a system font");
            }
        }

        [Test]
        public void EveryUiSceneThemeCarriesBundledFonts()
        {
            var body = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            var display = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);

            foreach (var scenePath in UiScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath);
                var themes = Object.FindObjectsByType<TarotUiTheme>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(themes.Length, Is.GreaterThan(0), $"{scenePath} has no TarotUiTheme");
                foreach (var theme in themes)
                {
                    Assert.That(theme.BodyFont, Is.EqualTo(body), $"{scenePath} theme body font not wired");
                    Assert.That(theme.DisplayFont, Is.EqualTo(display), $"{scenePath} theme display font not wired");
                }
            }
        }

        [Test]
        public void EveryActiveSceneTextUsesBundledFontByRole()
        {
            var body = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            var display = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);

            foreach (var scenePath in UiScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath);
                var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var tmpTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                // Phase 43 migrates screens to TMP SDF one at a time, so a scene is
                // valid on either system - it just may not be on neither.
                Assert.That(texts.Length + tmpTexts.Length, Is.GreaterThan(0),
                    $"{scenePath} has no active text of either kind");

                var tmpBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpBodyFontPath);
                var tmpDisplay = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpDisplayFontPath);
                foreach (var tmp in tmpTexts)
                {
                    var tmpRole = TarotUiTheme.ClassifyRole(tmp, DisplaySizeThreshold);
                    var expectedTmp = tmpRole == TarotUiTheme.TextRole.Body ? tmpBody : tmpDisplay;
                    Assert.That(tmp.font, Is.EqualTo(expectedTmp),
                        $"{scenePath}:{tmp.name} should use the bundled SDF font for role {tmpRole}");
                }

                foreach (var text in texts)
                {
                    var role = TarotUiTheme.ClassifyRole(text, DisplaySizeThreshold);
                    var expected = role == TarotUiTheme.TextRole.Body ? body : display;
                    Assert.That(text.font, Is.EqualTo(expected),
                        $"{scenePath}:{text.name} ({role}, size {text.fontSize}) should use the bundled font for its role");
                }
            }
        }

        [Test]
        public void DisplayTitlesCarryLegibilityOutline()
        {
            foreach (var scenePath in UiScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath);
                foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (TarotUiTheme.ClassifyRole(text, DisplaySizeThreshold) == TarotUiTheme.TextRole.Display)
                    {
                        Assert.That(text.GetComponent<Outline>(), Is.Not.Null,
                            $"{scenePath}:{text.name} is a display title and should carry a legibility outline");
                    }
                }
            }
        }

        [Test]
        public void ClassifyRoleFollowsSizeAndButtonRules()
        {
            var displayGo = new GameObject("display");
            var bodyGo = new GameObject("body");
            var buttonGo = new GameObject("button");
            var labelGo = new GameObject("label");
            try
            {
                var display = displayGo.AddComponent<Text>();
                display.fontSize = 40;
                Assert.That(TarotUiTheme.ClassifyRole(display, DisplaySizeThreshold), Is.EqualTo(TarotUiTheme.TextRole.Display));

                var body = bodyGo.AddComponent<Text>();
                body.fontSize = 16;
                Assert.That(TarotUiTheme.ClassifyRole(body, DisplaySizeThreshold), Is.EqualTo(TarotUiTheme.TextRole.Body));

                buttonGo.AddComponent<Button>();
                labelGo.transform.SetParent(buttonGo.transform);
                var label = labelGo.AddComponent<Text>();
                label.fontSize = 18;
                Assert.That(TarotUiTheme.ClassifyRole(label, DisplaySizeThreshold), Is.EqualTo(TarotUiTheme.TextRole.Emphasis));
            }
            finally
            {
                Object.DestroyImmediate(displayGo);
                Object.DestroyImmediate(bodyGo);
                Object.DestroyImmediate(buttonGo);
            }
        }

        [Test]
        public void FontlessCardLabelsStayDeactivated()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(prefab, Is.Not.Null);
            foreach (var labelName in new[] { "TitleLabel", "PositionLabel" })
            {
                var label = FindDeep(prefab.transform, labelName);
                Assert.That(label, Is.Not.Null, $"{labelName} should still exist");
                Assert.That(label.gameObject.activeSelf, Is.False,
                    $"Phase 24 must not revive the fontless {labelName}");
            }
        }

        [Test]
        public void KeyFontSizesPreserved()
        {
            // The typography pass changes fonts, not the size hierarchy other
            // phases assert (display title >= 60, overall body >= 19).
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var maxMenu = 0f;
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                maxMenu = Mathf.Max(maxMenu, t.fontSize);
            }

            // The menu is on TMP since Phase 43; its sizes live on TMP_Text now.
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                maxMenu = Mathf.Max(maxMenu, t.fontSize);
            }

            Assert.That(maxMenu, Is.GreaterThanOrEqualTo(60f), "Main menu lost its display title size");

            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");
            var overall = FindDeepByName("OverallText");
            Assert.That(overall, Is.Not.Null, "Result scene should keep OverallText");
            Assert.That(overall.GetComponent<TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(19f));
        }

        [Test]
        public void Phase24DocumentationExists()
        {
            Assert.That(File.Exists(Phase24DocPath), Is.True, $"Missing Phase24 doc at {Phase24DocPath}");
            var text = File.ReadAllText(Phase24DocPath);
            Assert.That(text, Does.Contain("LXGW WenKai"));
            Assert.That(text, Does.Contain("SIL OFL"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindDeepByName(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }

            return null;
        }
    }
}
