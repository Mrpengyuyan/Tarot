using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualInkParityTests
    {
        private ApiClient previous;
        private Transform root;
        private Camera camera;
        private static readonly Vector2Int[] Sizes = { new(1280,720), new(1024,768), new(2560,1440), new(2048,1536), new(1280,720) };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;
            root = GameObject.Find("ReadingRoomCanvas").transform;
            camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth).First();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Offline;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("InkParityCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator TheActualSerifShaderPreservesApprovedGoldAtTheGlyphCore()
        {
            var original = root.Find("DrawButton").GetComponentInChildren<TMP_Text>(true);
            var material = original.fontSharedMaterial;
            var face = material.GetColor("_FaceColor");
            var plate = new GameObject("InkProbe", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(root, false);
            var rect = (RectTransform)plate.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            plate.GetComponent<Image>().color = Color.black;
            plate.GetComponent<Image>().raycastTarget = false;
            var node = new GameObject("ProbeGlyph", typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(plate.transform, false);
            var glyph = node.GetComponent<TMP_Text>();
            glyph.font = original.font;
            glyph.fontSharedMaterial = material;
            glyph.text = "\u4e00";
            glyph.fontSize = 128f;
            glyph.color = new Color(240f/255f,214f/255f,165f/255f);
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.rectTransform.anchorMin = Vector2.zero; glyph.rectTransform.anchorMax = Vector2.one;
            glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;
            glyph.raycastTarget = false;
            using (var capture = new RitualFrameCapture(camera, root.GetComponent<Canvas>(), 1280, 720))
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var pixels = capture.Render();
                try
                {
                    var brightest = pixels.GetPixels32().OrderByDescending(c => c.r + c.g + c.b).First();
                    var actual = (Color)brightest;
                    Assert.That(actual.r, Is.EqualTo(240f/255f).Within(0.02f));
                    Assert.That(actual.g, Is.EqualTo(214f/255f).Within(0.02f));
                    Assert.That(actual.b, Is.EqualTo(165f/255f).Within(0.02f));
                    var directory = Environment.GetEnvironmentVariable("TAROT_INK_REPORT_DIR");
                    if (!string.IsNullOrEmpty(directory)) File.WriteAllText(Path.Combine(directory, "serif-ink-probe.txt"),
                        $"glyph-core={brightest}; source={original.font.sourceFontFile.name}; face={face}\n");
                }
                finally { Object.Destroy(pixels); }
            }
            Assert.That(original.fontSharedMaterial, Is.SameAs(material));
            Assert.That(material.GetColor("_FaceColor"), Is.EqualTo(face));
        }

        [UnityTest]
        public IEnumerator PositionAndResultInkSurviveThemeReapplicationWithoutAResize()
        {
            yield return PrepareReady(3);
            var before = ((RectTransform)root).rect.size;
            var labels = root.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name.StartsWith("RitualPositionLabel_")).ToArray();
            root.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            yield return null;
            Assert.That(((RectTransform)root).rect.size, Is.EqualTo(before));
            foreach (var label in labels) AssertInk(label.color, 226, 203, 163);
            AssertInk(root.Find("RevealResultButton").GetComponentInChildren<TMP_Text>().color, 240, 214, 165);
        }

        [UnityTest] public IEnumerator SingleCardUsesReferenceTrackingAtEveryDensity() { yield return CheckTracking(1); }
        [UnityTest] public IEnumerator ThreeCardsUseReferenceTrackingAtEveryDensity() { yield return CheckTracking(3); }
        [UnityTest] public IEnumerator TenCardsUseReferenceTrackingAtEveryDensity() { yield return CheckTracking(10); }

        [UnityTest]
        public IEnumerator PickAndRevealCountersSwitchInkWithoutChangingTheirMeaning()
        {
            root.Find("ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            flow.SetQuestion("A stage ink check", "general");
            flow.BeginShuffle(); flow.BeginDeal();
            root.GetComponent<RitualSpreadPresentation>().Prepare(3);
            root.GetComponent<RitualPickPresentation>().Prepare(3);
            yield return null;
            yield return null;
            var progress = root.Find("RitualProgress").GetComponent<TMP_Text>();
            yield return CheckCounterTracking(progress, false);
            flow.WaitForCardFlips();
            yield return null;
            yield return null;
            yield return CheckCounterTracking(progress, true);
        }

        private IEnumerator CheckCounterTracking(TMP_Text progress, bool reveal)
        {
            foreach (var size in Sizes)
            {
                using var capture = new RitualFrameCapture(camera, root.GetComponent<Canvas>(), size.x, size.y);
                yield return null;
                root.GetComponent<RitualPickPresentation>().RefreshLayout();
                root.GetComponent<RitualSpreadPresentation>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                if (reveal)
                {
                    AssertInk(progress.color, 183, 160, 128);
                    AssertAdvance(progress, '\u9010', '\u5f20', 1.15f);
                    AssertGlyphInk(progress, '0', 204, 173, 117);
                    AssertGlyphInk(progress, '3', 204, 173, 117);
                    Assert.That(progress.GetParsedText(), Is.EqualTo("\u9010\u5f20\u63ed\u5f00 0 / 3"));
                }
                else
                {
                    AssertInk(progress.color, 203, 179, 141);
                    AssertAdvance(progress, '\u8fd8', '\u53ef', 1.15f);
                    AssertGlyphInk(progress, '3', 240, 212, 162);
                    Assert.That(progress.GetParsedText().Replace("  ", " "), Is.EqualTo("\u8fd8\u53ef\u9009 3 \u5f20"));
                }
            }
        }

        private IEnumerator CheckTracking(int count)
        {
            yield return PrepareReady(count);
            var action = root.Find("RevealResultButton").GetComponentInChildren<TMP_Text>();
            var label = root.Find("RitualPositionLabel_" + (count == 10 ? 1 : 0)).GetComponent<TMP_Text>();
            foreach (var size in Sizes)
            {
                using var capture = new RitualFrameCapture(camera, root.GetComponent<Canvas>(), size.x, size.y);
                yield return null;
                root.GetComponent<RitualSpreadPresentation>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                AssertAdvance(action, '\u63ed', '\u793a', 1.20f);
                AssertAdvance(label, count == 1 ? '\u6b64' : count == 3 ? '\u8fc7' : '\u9006',
                    count == 1 ? '\u523b' : count == 3 ? '\u53bb' : '\u4f4d', 1.12f);
                Assert.That(action.alignment, Is.EqualTo(TextAlignmentOptions.Center));
                Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.Center));
            }
        }

        private IEnumerator PrepareReady(int count)
        {
            root.Find(count == 1 ? "OneCardButton" : count == 3 ? "ThreeCardButton" : "CelticCrossButton").GetComponent<Button>().onClick.Invoke();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            flow.SetQuestion("An offline text presentation test", "general");
            flow.BeginShuffle(); flow.BeginDeal();
            root.GetComponent<RitualSpreadPresentation>().Prepare(count);
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(count);
            for (var i = 0; i < count; i++) draws[i].is_reversed = i % 3 == 1;
            var deck = Object.FindFirstObjectByType<DeckController>();
            yield return deck.DealCards(draws, flow.GetSelectedSpreadSlots());
            flow.WaitForCardFlips();
            foreach (var card in deck.ActiveCards) { card.SetFaceUp(true); flow.RegisterCardFlipped(card); }
            yield return null;
            yield return null;
            Assert.That(flow.State, Is.EqualTo(ReadingFlowState.ResultReady));
        }

        private static void AssertAdvance(TMP_Text text, char first, char second, float em)
        {
            text.ForceMeshUpdate(true);
            var glyphs = text.textInfo.characterInfo;
            for (var i = 0; i + 1 < text.textInfo.characterCount; i++)
                if (glyphs[i].character == first && glyphs[i+1].character == second)
                {
                    Assert.That((glyphs[i+1].origin - glyphs[i].origin) / text.fontSize, Is.EqualTo(em).Within(0.025f), text.name);
                    return;
                }
            Assert.Fail($"{text.name} must visibly render {first}{second}.");
        }

        private static void AssertGlyphInk(TMP_Text text, char character, byte r, byte g, byte b)
        {
            text.ForceMeshUpdate(true);
            var glyph = text.textInfo.characterInfo.Take(text.textInfo.characterCount).First(c => c.character == character);
            var color = text.textInfo.meshInfo[glyph.materialReferenceIndex].colors32[glyph.vertexIndex];
            AssertInk(color, r, g, b);
        }

        private static void AssertInk(Color color, byte r, byte g, byte b)
        {
            Assert.That(color.r, Is.EqualTo(r/255f).Within(0.005f));
            Assert.That(color.g, Is.EqualTo(g/255f).Within(0.005f));
            Assert.That(color.b, Is.EqualTo(b/255f).Within(0.005f));
        }

        private static void Offline(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "ReadingRoom") typeof(ReadingRoomController).GetField("backendMode", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(), BackendIntegrationMode.LocalSimulation);
        }
    }
}
