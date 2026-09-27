using NUnit.Framework;
using TMPro;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 70: narrow windows show the whole menu and reading room, the question is centred,
    /// 揭示结果 takes 洗牌抽取's place in a centred four-button row, and the step bar and the
    /// Phase 16 table aura no longer draw (their objects stay active for Find-based tests).
    /// </summary>
    public sealed class Phase70UiPolishTests
    {
        private const string ReadingRoomPath = "Assets/Scenes/ReadingRoom.unity";

        private static readonly string[] TableAuraVisuals =
        {
            "Phase16_GlowPool", "Phase16_RuneRingOuter", "Phase16_RuneRingInner",
            "Phase16_ParticleAnchorNorth", "Phase16_ParticleAnchorEast",
            "Phase16_ParticleAnchorSouth", "Phase16_ParticleAnchorWest",
        };

        [TestCase("Assets/Scenes/MainMenu.unity", "MainMenuCanvas")]
        [TestCase(ReadingRoomPath, "ReadingRoomCanvas")]
        public void CanvasFitsNarrowWindows(string scenePath, string canvasName)
        {
            EditorSceneManager.OpenScene(scenePath);
            var canvas = GameObject.Find(canvasName);
            var fit = canvas.GetComponent<ResultCanvasAspectFit>();
            Assert.That(fit, Is.Not.Null, $"{canvasName}: matches width on screens narrower than 16:9");
            var so = new SerializedObject(fit);
            Assert.That(so.FindProperty("scaler").objectReferenceValue, Is.SameAs(canvas.GetComponent<CanvasScaler>()));
            Assert.That(so.FindProperty("canvasRect").objectReferenceValue, Is.SameAs(canvas.transform));
            Assert.That(so.FindProperty("pinned").arraySize, Is.EqualTo(0), "nothing here is pinned to an edge");
        }

        [Test]
        public void QuestionTextIsCentredOverItsUnderline()
        {
            var root = OpenRoom();
            var input = root.Find("QuestionInput").GetComponent<TMP_InputField>();
            Assert.That(input.textComponent.horizontalAlignment, Is.EqualTo(HorizontalAlignmentOptions.Center));
            Assert.That(((TMP_Text)input.placeholder).horizontalAlignment, Is.EqualTo(HorizontalAlignmentOptions.Center));
        }

        [Test]
        public void RevealSharesTheDrawSlotAndTheRowIsCentred()
        {
            var root = OpenRoom();
            var draw = (RectTransform)root.Find("DrawButton");
            var reveal = (RectTransform)root.Find("RevealResultButton");
            Assert.That(reveal.anchoredPosition, Is.EqualTo(draw.anchoredPosition));
            Assert.That(reveal.sizeDelta, Is.EqualTo(draw.sizeDelta));

            var one = (RectTransform)root.Find("OneCardButton");
            var left = one.anchoredPosition.x - one.sizeDelta.x / 2f;
            var right = draw.anchoredPosition.x + draw.sizeDelta.x / 2f;
            Assert.That(left, Is.EqualTo(-right).Within(0.5f), "the four visible buttons are centred");

            var dock = (RectTransform)root.Find("Phase11_ActionDock");
            Assert.That(dock.sizeDelta.x, Is.EqualTo(right - left + 2f * UiFitLayout.DockPad.x + 2f * UiFitLayout.SkinMargin).Within(0.5f),
                "the dock hugs the four-button row");
            var inputRect = (RectTransform)root.Find("QuestionInput");
            Assert.That(inputRect.sizeDelta.x, Is.EqualTo(Mathf.Round(UiFitLayout.InputWidthRatio * dock.sizeDelta.x)));
        }

        [Test]
        public void StepBarNoLongerDrawsButStaysActive()
        {
            var root = OpenRoom();
            var hud = root.Find("Phase7_RitualHudRoot");
            Assert.That(hud.gameObject.activeSelf, Is.True, "Phase7ImmersiveUiTests finds it with GameObject.Find");
            var group = hud.GetComponent<CanvasGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
        }

        [Test]
        public void TableAuraVisualsAreHiddenButObjectsStayActive()
        {
            OpenRoom();
            var aura = GameObject.Find("Phase16_RitualAuraRoot");
            Assert.That(aura, Is.Not.Null, "the aura root also carries the Phase 18/19 particles and must stay active");
            foreach (var name in TableAuraVisuals)
            {
                var t = aura.transform.Find(name);
                Assert.That(t, Is.Not.Null, name);
                Assert.That(t.gameObject.activeInHierarchy, Is.True, name);
                Assert.That(t.GetComponent<MeshRenderer>().enabled, Is.False, $"{name} no longer draws");
            }
        }

        private static Transform OpenRoom()
        {
            EditorSceneManager.OpenScene(ReadingRoomPath);
            return GameObject.Find("ReadingRoomCanvas").transform;
        }
    }
}
