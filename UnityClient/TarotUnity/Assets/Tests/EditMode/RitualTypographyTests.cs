using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class RitualTypographyTests
    {
        private const string Title = "\u5854\u7f57\u4eea\u5f0f";
        private const string Slogan = "\u4f60\u5c1a\u672a\u5f00\u53e3\uff0c\u724c\u5df2\u542c\u89c1\u3002";
        private const string PlayerQuestion = "\u804c\u4e1a\u89c4\u5212\u4e0e\u5bb6\u5ead\u8d23\u4efb";
        private Scene preview;

        [TearDown]
        public void TearDown()
        {
            if (preview.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [Test]
        public void PublicPresentationEntryPointsAreAvailableWithoutSceneReauthoring()
        {
            RequireApi("RitualTypography", "ApplyHierarchy", typeof(Transform));
            RequireApi("RitualMenuView", "Attach", typeof(GameObject));
        }

        [TestCase(1280f, 720f)]
        [TestCase(1024f, 768f)]
        public void CoverHasALargerCenteredTitleAndSeparatedCenteredActions(float width, float height)
        {
            var root = OpenMenu(width, height);
            Attach(root.gameObject);
            var title = Find(root, "TitleText").GetComponent<TMP_Text>();
            var controller = root.GetComponent<MainMenuController>();
            var start = Field<Button>(controller, "startReadingButton");
            var quit = Field<Button>(controller, "quitButton");
            var titleBounds = BoundsIn(root, title.rectTransform);
            var startBounds = BoundsIn(root, (RectTransform)start.transform);
            var quitBounds = BoundsIn(root, (RectTransform)quit.transform);

            Assert.That(title.text, Is.EqualTo(Title));
            Assert.That(title.fontSize, Is.GreaterThan(64f), "The approved cover enlarges the old 64-point title.");
            Assert.That(title.fontSize, Is.GreaterThan(start.GetComponentInChildren<TMP_Text>().fontSize * 3f));
            Assert.That(titleBounds.center.x, Is.EqualTo(root.rect.center.x).Within(1f), "Title center");
            Assert.That(startBounds.center.x, Is.EqualTo(root.rect.center.x).Within(1f), "Start center");
            Assert.That(quitBounds.center.x, Is.EqualTo(root.rect.center.x).Within(1f), "Quit shares the action column.");
            Assert.That(titleBounds.yMin - startBounds.yMax, Is.GreaterThanOrEqualTo(16f));
            Assert.That(startBounds.yMin - quitBounds.yMax, Is.GreaterThanOrEqualTo(8f));
            foreach (var bounds in new[] { titleBounds, startBounds, quitBounds })
            {
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(root.rect.xMin + 8f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(root.rect.xMax - 8f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(root.rect.yMin + 8f));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(root.rect.yMax - 8f));
            }
        }

        [Test]
        public void CoverHidesTheSloganAndOldFramesWithoutReplacingTheParlor()
        {
            var root = OpenMenu();
            var parlor = FindInScene("MP_ParlorBackdrop");
            var stage = FindInScene("MP_MenuStage");
            var renderer = parlor.GetComponent<Renderer>();
            Assert.That(renderer, Is.Not.Null, "The existing 3D parlor is the backdrop.");
            var material = renderer.sharedMaterial;
            var backgroundImages = root.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.Contains("Backdrop")).ToArray();
            var sprites = backgroundImages.Select(image => image.sprite).ToArray();

            Attach(root.gameObject);

            Assert.That(root.GetComponentsInChildren<TMP_Text>(true)
                .Any(text => text.text.Contains(Slogan) && IsVisible(text)), Is.False,
                "The cover no longer carries the old slogan.");
            foreach (var image in root.GetComponentsInChildren<Image>(true).Where(IsOldFrame))
            {
                Assert.That(IsVisible(image), Is.False, image.name);
                if (image.gameObject.activeInHierarchy && image.enabled)
                {
                    Assert.That(image.raycastTarget, Is.False, $"Hidden {image.name} must not intercept input.");
                }
            }

            Assert.That(parlor.gameObject.activeSelf, Is.True);
            Assert.That(stage.gameObject.activeSelf, Is.True);
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            for (var i = 0; i < backgroundImages.Length; i++)
            {
                Assert.That(backgroundImages[i].sprite, Is.SameAs(sprites[i]), "Do not replace the backdrop with a web capture.");
            }
        }

        [Test]
        public void AttachReusesBothWiredButtonsAndAddsGoldRulesInsteadOfPlates()
        {
            var root = OpenMenu();
            var controller = root.GetComponent<MainMenuController>();
            var start = Field<Button>(controller, "startReadingButton");
            var quit = Field<Button>(controller, "quitButton");
            var startClick = start.onClick;
            var quitClick = quit.onClick;
            var startLabel = start.GetComponentInChildren<TMP_Text>(true).text;
            var quitLabel = quit.GetComponentInChildren<TMP_Text>(true).text;
            var buttons = root.GetComponentsInChildren<Button>(true);

            Attach(root.gameObject);

            Assert.That(Field<Button>(controller, "startReadingButton"), Is.SameAs(start));
            Assert.That(Field<Button>(controller, "quitButton"), Is.SameAs(quit));
            Assert.That(start.onClick, Is.SameAs(startClick));
            Assert.That(quit.onClick, Is.SameAs(quitClick));
            CollectionAssert.AreEquivalent(buttons, root.GetComponentsInChildren<Button>(true));
            foreach (var button in new[] { start, quit })
            {
                Assert.That(button.gameObject.activeInHierarchy && button.enabled && button.interactable, Is.True);
                Assert.That(button.targetGraphic, Is.Not.Null);
                Assert.That(button.targetGraphic.enabled && button.targetGraphic.raycastTarget, Is.True,
                    "A transparent action must keep its clickable target.");
                foreach (var image in button.GetComponentsInChildren<Image>(true).Where(IsVisible))
                {
                    var rect = image.rectTransform.rect;
                    Assert.That(Mathf.Min(rect.width, rect.height), Is.LessThanOrEqualTo(3f),
                        $"{button.name}/{image.name} is an old opaque plate or frame, not a fine rule.");
                }
            }

            Assert.That(start.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(startLabel));
            Assert.That(quit.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(quitLabel));
            var rules = start.GetComponentsInChildren<Image>(true).Where(image => IsVisible(image)
                && image.rectTransform.rect.width >= 60f && image.rectTransform.rect.height <= 3f).ToArray();
            Assert.That(rules.Length, Is.GreaterThanOrEqualTo(2), "The main action has fine gold rules above and below.");
            foreach (var rule in rules)
            {
                Assert.That(rule.color.r, Is.GreaterThan(rule.color.g));
                Assert.That(rule.color.g, Is.GreaterThan(rule.color.b));
                Assert.That(rule.raycastTarget, Is.False, "Decorative rules never block the action.");
            }
        }

        [Test]
        public void BrushTitleAndReadableSerifUseDistinctBundledSourcesWithFullChineseFallback()
        {
            var root = OpenMenu();
            var theme = root.GetComponent<TarotUiTheme>();
            Attach(root.gameObject);
            var title = Find(root, "TitleText").GetComponent<TMP_Text>();
            var label = Field<Button>(root.GetComponent<MainMenuController>(), "startReadingButton")
                .GetComponentInChildren<TMP_Text>(true);
            var sources = Resources.LoadAll<Font>("RitualFonts");

            Assert.That(title.font, Is.Not.Null);
            Assert.That(label.font, Is.Not.Null);
            Assert.That(title.font.sourceFontFile, Is.Not.Null);
            Assert.That(label.font.sourceFontFile, Is.Not.Null);
            Assert.That(sources, Does.Contain(title.font.sourceFontFile));
            Assert.That(sources, Does.Contain(label.font.sourceFontFile));
            Assert.That(title.font.sourceFontFile, Is.Not.SameAs(label.font.sourceFontFile),
                "Only the title uses the brush subset; actions stay readable serif.");
            Assert.That(HasSourceGlyph(title.font, '\u5854'), Is.True);
            Assert.That(HasSourceGlyph(title.font, '\u804c'), Is.False,
                "The approved brush source is the four-character title subset, not a full replacement font.");
            var gold = title.enableVertexGradient ? title.colorGradient.topLeft : title.color;
            Assert.That(gold.a, Is.GreaterThan(0.8f));
            Assert.That(gold.r, Is.GreaterThan(gold.g), "The large brush title keeps its warm gold treatment.");
            Assert.That(gold.g, Is.GreaterThan(gold.b));
            Assert.That(HasFullFallback(label.font, theme, new HashSet<TMP_FontAsset>()), Is.True);
            Assert.That(HasFullFallback(title.font, theme, new HashSet<TMP_FontAsset>()), Is.True);
            AssertBundledFallbackResolves(title.font, theme, '\u804c');
        }

        [Test]
        public void HierarchyPreservesRealQuestionContentAndSingleLineInputWhileChangingItsFace()
        {
            preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ReadingRoom.unity");
            var root = FindInScene("ReadingRoomCanvas");
            var theme = root.GetComponent<TarotUiTheme>();
            theme.Apply();
            var input = root.GetComponentInChildren<TMP_InputField>(true);
            input.text = PlayerQuestion;
            Assert.That(input.lineType, Is.EqualTo(TMP_InputField.LineType.SingleLine));

            ApplyHierarchy(root);

            Assert.That(input.text, Is.EqualTo(PlayerQuestion), "Typography must never edit the player's question.");
            Assert.That(input.lineType, Is.EqualTo(TMP_InputField.LineType.SingleLine));
            Assert.That(input.textComponent.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(input.fontAsset, Is.SameAs(input.textComponent.font), "TMP input owns the same font as its label.");
            Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(input.textComponent.font.sourceFontFile));
            Assert.That(HasFullFallback(input.textComponent.font, theme, new HashSet<TMP_FontAsset>()), Is.True);
            foreach (var character in PlayerQuestion)
            {
                AssertBundledFallbackResolves(input.textComponent.font, theme, character);
                Assert.That(input.textComponent.font.HasCharacter(character, true, true), Is.True,
                    $"Player character U+{(int)character:X4} must resolve through the bundled fallback.");
            }
        }

        [Test]
        public void RepeatedAttachDoesNotDuplicateControlsRulesOrScaleTheTitleAgain()
        {
            var root = OpenMenu();
            Attach(root.gameObject);
            var count = root.GetComponentsInChildren<Transform>(true).Length;
            var title = Find(root, "TitleText").GetComponent<TMP_Text>();
            var size = title.fontSize;
            var font = title.font;
            var bounds = BoundsIn(root, title.rectTransform);

            Attach(root.gameObject);
            ApplyHierarchy(root);

            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            Assert.That(title.fontSize, Is.EqualTo(size));
            Assert.That(title.font, Is.SameAs(font), "The general serif pass must not overwrite the cover brush face.");
            Assert.That(BoundsIn(root, title.rectTransform), Is.EqualTo(bounds));
        }

        [Test]
        public void FontSourcesStayCompactDynamicAndShipBothOflLicenses()
        {
            var fonts = Resources.LoadAll<Font>("RitualFonts");
            Assert.That(fonts.Length, Is.InRange(2, 3), "Bundle the brush plus only the needed readable serif weights.");
            long bytes = 0;
            foreach (var font in fonts)
            {
                var path = AssetDatabase.GetAssetPath(font);
                var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.fontTextureCase, Is.EqualTo(FontTextureCase.Dynamic));
                Assert.That(importer.includeFontData, Is.True, "Do not depend on fonts installed on the player's machine.");
                bytes += new FileInfo(path).Length;
            }

            Assert.That(bytes, Is.LessThan(160 * 1024), "Use compact source subsets, not full duplicate Chinese fonts.");
            foreach (var license in new[] { "MaShanZheng-OFL.txt", "NotoSerifSC-OFL.txt" })
            {
                Assert.That(File.Exists($"Assets/Resources/RitualFonts/{license}"), Is.True, license);
            }
        }

        [Test]
        public void GeneratedFontAssetsAreReusedInMemoryAndNeverSerializedAlongsideSources()
        {
            var root = OpenMenu();
            Attach(root.gameObject);
            var title = Find(root, "TitleText").GetComponent<TMP_Text>();
            var font = title.font;
            var material = title.fontSharedMaterial;

            ApplyHierarchy(root);
            Attach(root.gameObject);

            Assert.That(title.font, Is.SameAs(font));
            Assert.That(title.fontSharedMaterial, Is.SameAs(material));
            foreach (var runtime in new UnityEngine.Object[] { font, font.material, font.atlasTexture })
            {
                Assert.That(EditorUtility.IsPersistent(runtime), Is.False, runtime.name);
                Assert.That(runtime.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
            }
            Assert.That(AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Resources/RitualFonts" }), Is.Empty,
                "The bundled folder contains source subsets and licenses, never generated font atlases.");
        }

        private RectTransform OpenMenu(float width = 1280f, float height = 720f)
        {
            preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
            var root = (RectTransform)FindInScene("MainMenuCanvas");
            // Preview scenes have no Game view. Give the authored canvas a real design rectangle.
            root.GetComponent<Canvas>().enabled = false;
            root.GetComponent<CanvasScaler>().enabled = false;
            root.localScale = Vector3.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(width, height);
            root.GetComponent<TarotUiTheme>().Apply();
            return root;
        }

        private Transform FindInScene(string name)
        {
            var result = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
            Assert.That(result, Is.Not.Null, $"Existing scene node {name} must remain available.");
            return result;
        }

        private static Transform Find(Transform root, string name)
        {
            var result = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            Assert.That(result, Is.Not.Null, name);
            return result;
        }

        private static Rect BoundsIn(RectTransform root, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var points = corners.Select(root.InverseTransformPoint).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y),
                points.Max(point => point.x), points.Max(point => point.y));
        }

        private static bool IsVisible(Graphic graphic)
        {
            return graphic.isActiveAndEnabled && graphic.color.a > 0.01f
                && graphic.GetComponentsInParent<CanvasGroup>(true).All(group => group.alpha > 0.01f);
        }

        private static bool IsOldFrame(Image image)
        {
            return image.name == "Phase11_MenuDepthFrame" || image.name == "Phase11_ActionRail"
                || image.name == "Phase8_StartButtonGoldTrim";
        }

        private static bool HasSourceGlyph(TMP_FontAsset font, char character)
        {
            // Font.HasCharacter can use native fallbacks; TMP lookups can cache fallback characters too.
            // The loaded source face's glyph index tests the actual subset, independently of either cache.
            Assert.That(font.sourceFontFile, Is.Not.Null);
            Assert.That(FontEngine.LoadFontFace(font.sourceFontFile, (int)font.faceInfo.pointSize),
                Is.EqualTo(FontEngineError.Success));
            return FontEngine.TryGetGlyphWithUnicodeValue((uint)character, GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph)
                && glyph != null && glyph.index != 0;
        }

        private static void AssertBundledFallbackResolves(TMP_FontAsset font, TarotUiTheme theme, char character)
        {
            Assert.That(HasSourceGlyph(font, character), Is.False,
                $"U+{(int)character:X4} must be absent from the subset to exercise fallback.");
            var resolved = TMP_FontAssetUtilities.GetCharacterFromFontAsset(character, font, true,
                FontStyles.Normal, FontWeight.Regular, out _);
            Assert.That(resolved, Is.Not.Null, $"Local bundled fallbacks must resolve U+{(int)character:X4}.");
            Assert.That(resolved.unicode, Is.EqualTo((uint)character));
            Assert.That(resolved.glyph, Is.Not.Null);
            Assert.That(resolved.glyph.index, Is.Not.EqualTo(0u));
            var owner = resolved.textAsset as TMP_FontAsset;
            Assert.That(owner, Is.Not.Null);
            Assert.That(owner == theme.TmpBodyFont || owner == theme.TmpDisplayFont
                || (owner.sourceFontFile != null && (owner.sourceFontFile == theme.BodyFont || owner.sourceFontFile == theme.DisplayFont)),
                Is.True, $"U+{(int)character:X4} must come from the full bundled Chinese face, not an OS/default fallback.");
        }

        private static bool HasFullFallback(TMP_FontAsset font, TarotUiTheme theme, HashSet<TMP_FontAsset> visited)
        {
            if (font == null || !visited.Add(font))
            {
                return false;
            }

            if (font == theme.TmpBodyFont || font == theme.TmpDisplayFont
                || (font.sourceFontFile != null && (font.sourceFontFile == theme.BodyFont || font.sourceFontFile == theme.DisplayFont)))
            {
                return true;
            }

            return font.fallbackFontAssetTable != null
                && font.fallbackFontAssetTable.Any(fallback => HasFullFallback(fallback, theme, visited));
        }

        private static T Field<T>(MainMenuController controller, string name)
        {
            var field = typeof(MainMenuController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(controller);
        }

        private static MethodInfo RequireApi(string typeName, string methodName, Type argument)
        {
            var type = typeof(MainMenuController).Assembly.GetType($"TarotUnity.UI.{typeName}");
            Assert.That(type, Is.Not.Null, $"Task1 requires the new {typeName} presentation type.");
            Assert.That(type.IsPublic, Is.True);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new[] { argument }, null);
            Assert.That(method, Is.Not.Null, $"Expected public static {typeName}.{methodName}({argument.Name}).");
            return method;
        }

        private static void Attach(GameObject host)
        {
            RequireApi("RitualMenuView", "Attach", typeof(GameObject)).Invoke(null, new object[] { host });
        }

        private static void ApplyHierarchy(Transform root)
        {
            RequireApi("RitualTypography", "ApplyHierarchy", typeof(Transform)).Invoke(null, new object[] { root });
        }
    }
}
