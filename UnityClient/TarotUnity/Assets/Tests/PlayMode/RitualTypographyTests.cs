using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualTypographyTests
    {
        private const string PlayerQuestion = "\u804c\u4e1a\u89c4\u5212\u4e0e\u5bb6\u5ead\u8d23\u4efb";
        private GameObject menu;
        private MainMenuController controller;
        private Button originalStart;
        private Button originalQuit;
        private Button.ButtonClickedEvent originalStartClick;
        private Button.ButtonClickedEvent originalQuitClick;
        private ApiClient previousSharedClient;
        private readonly HashSet<Scene> loadedScenes = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousSharedClient = ApiClient.Shared;
            ApiClient.ClearShared();
            SceneManager.sceneLoaded += CaptureMenuAndKeepReadingStartupOffline;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            Assert.That(menu, Is.Not.Null, "The existing MainMenu scene must load.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= CaptureMenuAndKeepReadingStartupOffline;
            var empty = SceneManager.CreateScene("RitualTypographyTestCleanup");
            SceneManager.SetActiveScene(empty);
            foreach (var scene in loadedScenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            loadedScenes.Clear();
            ApiClient.SetShared(previousSharedClient);
            menu = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuStartAutomaticallyAppliesTheBrushCoverAfterThemeAwake()
        {
            RequireApi("RitualMenuView", "Attach", typeof(GameObject));
            RequireApi("RitualTypography", "ApplyHierarchy", typeof(Transform));
            var title = Find(menu.transform, "TitleText").GetComponent<TMP_Text>();
            Assert.That(title.text, Is.EqualTo("\u5854\u7f57\u4eea\u5f0f"));
            Assert.That(title.fontSize, Is.GreaterThan(64f), "Start must attach the view, not leave it to test setup.");
            Assert.That(title.font, Is.Not.SameAs(menu.GetComponent<TarotUiTheme>().TmpDisplayFont));
            Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(title.font.sourceFontFile));
            Assert.That(HasSourceGlyph(title.font, '\u5854'), Is.True);
            Assert.That(HasSourceGlyph(title.font, '\u804c'), Is.False, "Use the approved compact brush title source.");
            title.ForceMeshUpdate(true);
            Assert.That(title.textInfo.characterCount, Is.EqualTo(4));
            for (var i = 0; i < title.textInfo.characterCount; i++)
            {
                Assert.That(title.textInfo.characterInfo[i].fontAsset, Is.SameAs(title.font),
                    "All four title characters must render in the brush face rather than a fallback.");
            }
            AssertCleanCover();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThemeReapplicationDoesNotRestoreTheOldFontSloganOrButtonPlates()
        {
            RequireApi("RitualMenuView", "Attach", typeof(GameObject));
            var title = Find(menu.transform, "TitleText").GetComponent<TMP_Text>();
            var brush = title.font;
            var size = title.fontSize;
            Assert.That(size, Is.GreaterThan(64f));

            menu.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            yield return null;

            Assert.That(title.font, Is.SameAs(brush), "Legacy theme reapplication must not revert the approved brush face.");
            Assert.That(title.fontSize, Is.EqualTo(size));
            Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(originalStart.GetComponentInChildren<TMP_Text>().font.sourceFontFile));
            AssertCleanCover();
        }

        [UnityTest]
        public IEnumerator BothOriginalButtonsRemainVisibleInteractableAndReachableByPointer()
        {
            RequireApi("RitualMenuView", "Attach", typeof(GameObject));
            Assert.That(Field<Button>(controller, "startReadingButton"), Is.SameAs(originalStart));
            Assert.That(Field<Button>(controller, "quitButton"), Is.SameAs(originalQuit));
            Assert.That(originalStart.onClick, Is.SameAs(originalStartClick));
            Assert.That(originalQuit.onClick, Is.SameAs(originalQuitClick));
            Assert.That(originalStart.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("\u5165\u5e2d\u95ee\u724c"));
            Assert.That(originalQuit.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("\u79bb\u5e2d"));
            Canvas.ForceUpdateCanvases();

            foreach (var button in new[] { originalStart, originalQuit })
            {
                Assert.That(button.IsActive() && button.IsInteractable(), Is.True, button.name);
                var label = button.GetComponentInChildren<TMP_Text>();
                Assert.That(label.isActiveAndEnabled && label.color.a > 0.1f, Is.True, button.name);
                Assert.That(EventSystem.current, Is.Not.Null);
                var rect = (RectTransform)button.transform;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                };
                var hits = new List<RaycastResult>();
                menu.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), $"{button.name} must keep a live hit target without a plate.");
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
                    $"An old frame or decoration is covering {button.name}.");
            }

            // Invoking QuitGame would stop the test runner. Verify its retained binding and live hit target instead.
            Assert.That(typeof(MainMenuController).GetMethod("QuitGame", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OriginalStartPointerClickStillEntersTheExistingReadingRoom()
        {
            RequireApi("RitualMenuView", "Attach", typeof(GameObject));
            Assert.That(originalStart.onClick, Is.SameAs(originalStartClick));
            Assert.That(originalStart.IsActive() && originalStart.IsInteractable(), Is.True);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };

            ExecuteEvents.Execute(originalStart.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "ReadingRoom" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ReadingRoom"),
                "Restyling must preserve the original StartReading listener and route.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReadableSerifRendersOutOfSubsetPlayerTextThroughTheExistingChineseFallback()
        {
            var apply = RequireApi("RitualTypography", "ApplyHierarchy", typeof(Transform));
            yield return SceneManager.LoadSceneAsync("ReadingRoom", LoadSceneMode.Single);
            yield return null;
            var root = GameObject.Find("ReadingRoomCanvas");
            var theme = root.GetComponent<TarotUiTheme>();
            var input = root.GetComponentInChildren<TMP_InputField>(true);
            input.text = PlayerQuestion;
            input.textComponent.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 224f);
            var bodyObject = new GameObject("RitualFallbackBody", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(root.transform, false);
            var body = bodyObject.GetComponent<TMP_Text>();
            body.font = theme.TmpBodyFont;
            body.fontSize = 24f;
            body.rectTransform.sizeDelta = new Vector2(560f, 160f);
            body.text = PlayerQuestion;

            apply.Invoke(null, new object[] { root.transform });
            var serif = input.textComponent.font;
            theme.Apply();
            yield return null;
            yield return null;
            Assert.That(input.textComponent.font, Is.SameAs(serif), "The owning typography component restores overwritten input fonts.");
            Assert.That(body.font, Is.SameAs(serif), "Body type also survives legacy theme reapplication.");
            input.ForceLabelUpdate();
            input.textComponent.ForceMeshUpdate(true);
            body.ForceMeshUpdate(true);

            Assert.That(input.text, Is.EqualTo(PlayerQuestion));
            Assert.That(input.lineType, Is.EqualTo(TMP_InputField.LineType.SingleLine));
            Assert.That(input.textComponent.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(input.textComponent.textInfo.lineCount, Is.EqualTo(1), "A long question scrolls rather than wrapping.");
            Assert.That(input.fontAsset, Is.SameAs(input.textComponent.font));
            foreach (var text in new[] { input.textComponent, body })
            {
                Assert.That(Resources.LoadAll<Font>("RitualFonts"), Does.Contain(text.font.sourceFontFile));
                Assert.That(text.textInfo.characterCount, Is.GreaterThanOrEqualTo(PlayerQuestion.Length));
                for (var i = 0; i < PlayerQuestion.Length; i++)
                {
                    Assert.That(HasSourceGlyph(text.font, PlayerQuestion[i]), Is.False,
                        $"U+{(int)PlayerQuestion[i]:X4} must be absent from the primary subset to exercise fallback.");
                    var glyph = text.textInfo.characterInfo[i];
                    Assert.That(glyph.character, Is.EqualTo(PlayerQuestion[i]), $"{text.name}: character {i}");
                    Assert.That(glyph.textElement, Is.Not.Null);
                    Assert.That(glyph.textElement.glyph, Is.Not.Null);
                    Assert.That(glyph.textElement.glyph.index, Is.Not.EqualTo(0u), "Missing-glyph boxes are not acceptable fallback.");
                    Assert.That(IsExistingChineseFont(glyph.fontAsset, theme), Is.True,
                        $"{text.name}: U+{(int)PlayerQuestion[i]:X4} must render using the full existing Chinese face.");
                    Assert.That(glyph.textElement.textAsset, Is.SameAs(glyph.fontAsset));
                }
            }
        }

        private void CaptureMenuAndKeepReadingStartupOffline(Scene scene, LoadSceneMode mode)
        {
            loadedScenes.Add(scene);
            if (scene.name == "MainMenu")
            {
                menu = scene.GetRootGameObjects().First(root => root.name == "MainMenuCanvas");
                controller = menu.GetComponent<MainMenuController>();
                originalStart = Field<Button>(controller, "startReadingButton");
                originalQuit = Field<Button>(controller, "quitButton");
                originalStartClick = originalStart.onClick;
                originalQuitClick = originalQuit.onClick;
            }
            else if (scene.name == "ReadingRoom")
            {
                // SceneLoaded runs after Awake and before Start. This test covers menu routing and fonts, not network startup.
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var room in root.GetComponentsInChildren<ReadingRoomController>(true))
                    {
                        room.enabled = false;
                    }

                    foreach (var client in root.GetComponentsInChildren<ApiClient>(true))
                    {
                        client.ClearSession();
                    }
                }
            }
        }

        private void AssertCleanCover()
        {
            var slogan = menu.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.name == "SubtitleText");
            Assert.That(slogan == null || !IsVisible(slogan) || string.IsNullOrWhiteSpace(slogan.text), Is.True,
                "The cover has no slogan.");
            foreach (var image in menu.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Phase11_MenuDepthFrame" || image.name == "Phase11_ActionRail"
                    || image.name == "Phase8_StartButtonGoldTrim")
                {
                    Assert.That(IsVisible(image), Is.False, image.name);
                }
            }

            foreach (var button in new[] { originalStart, originalQuit })
            {
                foreach (var image in button.GetComponentsInChildren<Image>(true).Where(IsVisible))
                {
                    Assert.That(Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height),
                        Is.LessThanOrEqualTo(3f), $"{button.name}: a broad opaque plate returned.");
                }
            }
        }

        private static bool IsVisible(Graphic graphic)
        {
            return graphic.isActiveAndEnabled && graphic.color.a > 0.01f
                && graphic.GetComponentsInParent<CanvasGroup>(true).All(group => group.alpha > 0.01f);
        }

        private static bool IsExistingChineseFont(TMP_FontAsset font, TarotUiTheme theme)
        {
            return font != null && (font == theme.TmpBodyFont || font == theme.TmpDisplayFont
                || (font.sourceFontFile != null && (font.sourceFontFile == theme.BodyFont || font.sourceFontFile == theme.DisplayFont)));
        }

        private static bool HasSourceGlyph(TMP_FontAsset font, char character)
        {
            // Inspect the embedded face, not Unity's native fallback availability or TMP's cached fallback lookup.
            Assert.That(font.sourceFontFile, Is.Not.Null);
            Assert.That(FontEngine.LoadFontFace(font.sourceFontFile, (int)font.faceInfo.pointSize),
                Is.EqualTo(FontEngineError.Success));
            return FontEngine.TryGetGlyphWithUnicodeValue((uint)character, GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph)
                && glyph != null && glyph.index != 0;
        }

        private static Transform Find(Transform root, string name)
        {
            var result = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            Assert.That(result, Is.Not.Null, name);
            return result;
        }

        private static T Field<T>(MainMenuController target, string name)
        {
            var field = typeof(MainMenuController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }

        private static MethodInfo RequireApi(string typeName, string methodName, Type argument)
        {
            var type = typeof(MainMenuController).Assembly.GetType($"TarotUnity.UI.{typeName}");
            Assert.That(type, Is.Not.Null, $"Task1 requires the new {typeName} presentation type.");
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new[] { argument }, null);
            Assert.That(method, Is.Not.Null, $"Expected public static {typeName}.{methodName}({argument.Name}).");
            return method;
        }
    }
}
