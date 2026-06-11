using System.Reflection;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase7ImmersiveUiTests
    {
        [Test]
        public void MainMenuHasImmersiveRitualEntry()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            Assert.That(GameObject.Find("Phase7_ImmersiveMenuRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_MenuCardHalo"), Is.Not.Null);

            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);

            AssertText(canvas.transform, "TitleText", "塔罗仪式");
            AssertText(canvas.transform, "SubtitleText", "在安静的牌桌前，把问题交给牌面。");
            AssertText(canvas.transform, "StartReadingButton/Label", "开始占卜");
            AssertTextDoesNotContain(canvas.transform, "StatusText", "graybox");
            AssertTextDoesNotContain(canvas.transform, "SubtitleText", "Graybox");

            var controller = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(controller, Is.Not.Null);
            InvokePrivate(controller, "Start");
            AssertText(canvas.transform, "StatusText", "准备开始一场安静的占卜。");
        }

        [Test]
        public void ReadingRoomHasRitualHudAndChineseGuidance()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            Assert.That(GameObject.Find("Phase7_RitualHudRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_TableVignette"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_Progress_ChooseSpread"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_Progress_AskQuestion"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_Progress_DrawCards"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_Progress_FlipCards"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_Progress_RevealResult"), Is.Not.Null);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);

            AssertText(canvas.transform, "OneCardButton/Label", "一张牌");
            AssertText(canvas.transform, "ThreeCardButton/Label", "三张牌");
            AssertText(canvas.transform, "DrawButton/Label", "洗牌抽取");
            AssertText(canvas.transform, "RevealResultButton/Label", "揭示结果");

            var placeholder = canvas.transform.Find("QuestionInput/Placeholder")?.GetComponent<Text>();
            Assert.That(placeholder, Is.Not.Null);
            Assert.That(placeholder.text, Does.Contain("此刻"));
        }

        [Test]
        public void ResultSceneHasOracleReadingFrame()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            Assert.That(GameObject.Find("Phase7_ResultOracleFrame"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_ResultSectionSummary"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_ResultSectionOverall"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_ResultSectionCards"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase7_ResultSectionAdvice"), Is.Not.Null);

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            AssertText(canvas.transform, "BackToMenuButton/Label", "回到牌桌");

            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null);

            Assert.That(GetTextReference(presenter, "summaryText").alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(GetTextReference(presenter, "overallText").alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(GetTextReference(presenter, "cardAnalysisText").alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(GetTextReference(presenter, "adviceText").alignment, Is.EqualTo(TextAnchor.UpperLeft));
        }

        [Test]
        public void CardPrefabHasPhase7ReadableFaceDetails()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.transform.Find("Front/Phase7_TitleBand"), Is.Not.Null);
            Assert.That(card.transform.Find("Back/Phase7_MoonSigil"), Is.Not.Null);
        }

        private static void AssertText(Transform root, string path, string expected)
        {
            var text = root.Find(path)?.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, $"Missing text at {path}");
            Assert.That(text.text, Is.EqualTo(expected));
        }

        private static void AssertTextDoesNotContain(Transform root, string path, string unexpected)
        {
            var text = root.Find(path)?.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, $"Missing text at {path}");
            Assert.That(text.text, Does.Not.Contain(unexpected).IgnoreCase);
        }

        private static Text GetTextReference(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized field {propertyName} on {target.GetType().Name}");
            return property.objectReferenceValue as Text;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName} on {target.GetType().Name}");
            method.Invoke(target, null);
        }
    }
}
