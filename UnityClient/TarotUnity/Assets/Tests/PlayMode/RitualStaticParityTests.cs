using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualStaticParityTests
    {
        private ApiClient previous;
        private RenderTexture target;
        private Camera captureCamera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += KeepOffline;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= KeepOffline;
            ReadingSessionStore.Clear();
            if (captureCamera != null) captureCamera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("StaticParityCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator CoverMatchesReferenceAndKeepsDecorationsOutOfHitTesting()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return null;
            var root = (RectTransform)GameObject.Find("MainMenuCanvas").transform;
            var title = root.Find("TitleText").GetComponent<TMP_Text>();
            var start = root.Find("StartReadingButton").GetComponent<Button>();
            var clicks = start.onClick;
            var font = title.font;
            root.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            foreach (var key in new[] { "TitleText", "RitualCoverSeal", "RitualTitleRule", "RitualCoverCaption", "StartReadingButton", "QuitButton" })
                Assert.That(root.Find(key), Is.Not.Null, key);
            var caption = root.Find("RitualCoverCaption").GetComponent<TMP_Text>();
            caption.ForceMeshUpdate(true);
            for (var i = 0; i < caption.textInfo.characterCount; i++)
                if (!char.IsWhiteSpace(caption.textInfo.characterInfo[i].character))
                    Assert.That(caption.textInfo.characterInfo[i].fontAsset.sourceFontFile.name, Does.StartWith("noto-"),
                        "The fixed Latin caption must not silently switch to a sans-serif fallback.");
            foreach (var size in Sizes)
            {
                yield return CheckSize(root, "cover", size);
                Assert.That(start.onClick, Is.SameAs(clicks));
                Assert.That(title.font, Is.SameAs(font));
                Assert.That(title.fontStyle, Is.EqualTo(FontStyles.Normal), "The brush source must not receive fake bold.");
                title.ForceMeshUpdate(true);
                var advance = title.textInfo.characterInfo[1].origin - title.textInfo.characterInfo[0].origin;
                Assert.That(advance / title.fontSize, Is.EqualTo(1.11f).Within(0.02f), "Tracking must match CSS em, not raw pixel values.");
                AssertPointer(root, start);
                AssertPointer(root, root.Find("QuitButton").GetComponent<Button>());
            }
            foreach (var key in new[] { "RitualCoverSeal", "RitualTitleRule", "RitualCoverCaption" })
                Assert.That(root.Find(key).GetComponent<Graphic>().raycastTarget, Is.False);
        }

        [UnityTest]
        public IEnumerator ChoicesAndQuestionKeepReferenceGeometryAndPlayerInputThroughResize()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;
            var root = (RectTransform)GameObject.Find("ReadingRoomCanvas").transform;
            var view = root.GetComponent<RitualReadingView>();
            var input = root.Find("QuestionInput").GetComponent<TMP_InputField>();
            foreach (var size in Sizes)
            {
                yield return CheckSize(root, "spread", size);
                foreach (var name in new[] { "OneCardButton", "ThreeCardButton", "CelticCrossButton" })
                {
                    var button = root.Find(name).GetComponent<Button>();
                    AssertPointer(root, button);
                    var subtitle = button.transform.Find("RitualSpreadSubtitle").GetComponent<TMP_Text>();
                    subtitle.ForceMeshUpdate(true);
                    Assert.That(subtitle.textInfo.lineCount, Is.EqualTo(1));
                    Assert.That(subtitle.font.sourceFontFile.name, Is.EqualTo("noto-0"), "Small subtitles use regular, not button semibold.");
                    Assert.That(subtitle.color, Is.EqualTo(new Color(173f/255f, 150f/255f, 116f/255f)).Using(ColorComparer));
                }
            }
            root.Find("ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var draw = root.Find("DrawButton").GetComponent<Button>();
            input.text = "   ";
            yield return null;
            Assert.That(draw.interactable, Is.False);
            input.text = "\u804c\u4e1a\u89c4\u5212\u4e0e\u5bb6\u5ead\u8d23\u4efb";
            var question = input.text;
            var listeners = draw.onClick;
            root.GetComponent<TarotUiTheme>().Apply();
            foreach (var size in Sizes)
            {
                yield return CheckSize(root, "question", size);
                Assert.That(view.Stage, Is.EqualTo(RitualReadingView.ViewStage.Question));
                Assert.That(input.text, Is.EqualTo(question));
                Assert.That(draw.onClick, Is.SameAs(listeners));
                Assert.That(draw.interactable, Is.True);
                Assert.That(root.Find("RitualQuestionLabel").GetComponent<TMP_Text>().font.sourceFontFile.name, Is.EqualTo("noto-1"));
                AssertPointer(root, draw);
            }
        }

        [UnityTest]
        public IEnumerator ThemeKeepsQuestionInkAtTheSameWindowSize()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;
            var root = GameObject.Find("ReadingRoomCanvas");
            root.transform.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var size = ((RectTransform)root.transform).rect.size;
            var input = root.transform.Find("QuestionInput").GetComponent<TMP_InputField>();
            root.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            yield return null;
            Assert.That(((RectTransform)root.transform).rect.size, Is.EqualTo(size));
            Assert.That(input.textComponent.color, Is.EqualTo(new Color(242f/255f,223f/255f,190f/255f)).Using(ColorComparer));
            Assert.That((input.placeholder as TMP_Text).color, Is.EqualTo(new Color(203f/255f,182f/255f,150f/255f,0.85f)).Using(ColorComparer));
        }

        [UnityTest]
        public IEnumerator FixedQuestionGlyphsResolveWithinTheSerifFamilyWithoutFakeWeight()
        {
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;
            var root = GameObject.Find("ReadingRoomCanvas");
            root.transform.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            root.GetComponent<TarotUiTheme>().Apply();
            yield return null;
            var label = root.transform.Find("RitualQuestionLabel").GetComponent<TMP_Text>();
            label.ForceMeshUpdate(true);
            Assert.That(label.fontSharedMaterial, Is.SameAs(label.font.material), "Legacy material presets must not thicken the approved typeface.");
            Assert.That(label.fontStyle, Is.EqualTo(FontStyles.Normal));
            var input = root.transform.Find("QuestionInput").GetComponent<TMP_InputField>();
            var subtitle = root.transform.Find("ThreeCardButton/RitualSpreadSubtitle").GetComponent<TMP_Text>();
            foreach (var fixedText in new[] { label, input.placeholder as TMP_Text, subtitle })
            {
                fixedText.ForceMeshUpdate(true);
                for (var i = 0; i < fixedText.textInfo.characterCount; i++)
                    Assert.That(fixedText.textInfo.characterInfo[i].fontAsset.sourceFontFile.name, Does.StartWith("noto-"),
                        $"{fixedText.name}: fixed U+{(int)fixedText.textInfo.characterInfo[i].character:X4} must not silently switch to the legacy handwritten fallback.");
            }
        }

        private static readonly Vector2[] Sizes = { new(1280,720), new(1024,768), new(2560,1440), new(2048,1536), new(1280,720) };
        private static readonly IEqualityComparer<Color> ColorComparer = new UnityEngine.TestTools.Utils.ColorEqualityComparer(0.001f);

        private IEnumerator CheckSize(RectTransform root, string stage, Vector2 size)
        {
            captureCamera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.isActiveAndEnabled && c.gameObject.scene == root.gameObject.scene)
                .OrderByDescending(c => c.depth).First();
            var next = new RenderTexture((int)size.x, (int)size.y, 24);
            captureCamera.targetTexture = next;
            captureCamera.aspect = size.x / size.y;
            if (target != null) { target.Release(); Object.Destroy(target); }
            target = next;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = captureCamera.nearClipPlane + 0.05f;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            canvas.scaleFactor = 1f;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Vector2.Distance(root.rect.size, size), Is.LessThan(1f), "The test must actually render at the target size.");
            if (stage == "cover") root.GetComponent<RitualMenuView>().SendMessage("RefreshLayout");
            else root.GetComponent<RitualReadingView>().RefreshLayout();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var type = typeof(RitualMenuView).Assembly.GetType("TarotUnity.UI.RitualStaticLayout");
            Assert.That(type, Is.Not.Null);
            var frame = (IDictionary)type.GetMethod("Compute").Invoke(null, new object[] { stage, size.x, size.y });
            foreach (DictionaryEntry element in frame)
            {
                var key = (string)element.Key;
                RectTransform rect;
                if (key.EndsWith("/Title")) rect = root.Find(key.Replace("/Title", "")).GetComponentInChildren<TMP_Text>(true).rectTransform;
                else rect = root.Find(key) as RectTransform;
                Assert.That(rect, Is.Not.Null, key);
                var expected = (Rect)element.Value.GetType().GetField("Rect").GetValue(element.Value);
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                var min = Vector2.one * float.PositiveInfinity;
                var max = Vector2.one * float.NegativeInfinity;
                foreach (var corner in corners)
                {
                    var local = root.InverseTransformPoint(corner);
                    var pixel = new Vector2(local.x + size.x/2f, size.y/2f - local.y);
                    min = Vector2.Min(min,pixel); max = Vector2.Max(max,pixel);
                }
                var actual = Rect.MinMaxRect(min.x,min.y,max.x,max.y);
                var angle = (float)element.Value.GetType().GetField("Rotation").GetValue(element.Value) * Mathf.Deg2Rad;
                var extent = new Vector2(Mathf.Abs(expected.width*Mathf.Cos(angle))+Mathf.Abs(expected.height*Mathf.Sin(angle)),
                    Mathf.Abs(expected.width*Mathf.Sin(angle))+Mathf.Abs(expected.height*Mathf.Cos(angle)));
                expected = new Rect(expected.center - extent/2f,extent);
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(1f), key+" x");
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(1f), key+" y");
                Assert.That(actual.width, Is.EqualTo(expected.width).Within(1f), key+" width");
                Assert.That(actual.height, Is.EqualTo(expected.height).Within(1f), key+" height");
            }
        }

        private static void AssertPointer(RectTransform root, Button button)
        {
            var rect = (RectTransform)button.transform;
            var canvas = root.GetComponent<Canvas>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>();
            root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
        }

        private static void KeepOffline(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ReadingRoom") return;
            var room = Object.FindFirstObjectByType<ReadingRoomController>();
            typeof(ReadingRoomController).GetField("backendMode", BindingFlags.NonPublic|BindingFlags.Instance)
                .SetValue(room, BackendIntegrationMode.LocalSimulation);
        }
    }
}
