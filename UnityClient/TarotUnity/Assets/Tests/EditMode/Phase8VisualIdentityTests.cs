using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase8VisualIdentityTests
    {
        [Test]
        public void MainMenuHasPremiumRitualTableComposition()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            Assert.That(GameObject.Find("Phase8_VisualIdentityRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_MenuTableSurface"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_LeftCandle"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_RightCandle"), Is.Not.Null);

            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_TitleConstellation"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_MenuCrest"), Is.Not.Null);
            Assert.That(canvas.transform.Find("StartReadingButton/Phase8_StartButtonGoldTrim"), Is.Not.Null);

            var title = canvas.transform.Find("TitleText")?.GetComponent<Text>();
            Assert.That(title, Is.Not.Null);
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(60));
        }

        [Test]
        public void ReadingRoomHasPremiumTableAndControlFraming()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            Assert.That(GameObject.Find("Phase8_TableClothSurface"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_DeckFocusRing"), Is.Not.Null);

            var canvas = GameObject.Find("ReadingRoomCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_SpreadChoiceFrame"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_QuestionPanelFrame"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_CardSlotsGlow"), Is.Not.Null);

            var inputRect = canvas.transform.Find("QuestionInput")?.GetComponent<RectTransform>();
            Assert.That(inputRect, Is.Not.Null);
            Assert.That(inputRect.sizeDelta.x, Is.GreaterThanOrEqualTo(720f));
        }

        [Test]
        public void ResultSceneHasPremiumOracleReadingLayout()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Result.unity");

            var canvas = GameObject.Find("ResultCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_ResultScrollPanel"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_ResultCrest"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_ResultGoldDividerTop"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_ResultGoldDividerBottom"), Is.Not.Null);

            var overall = canvas.transform.Find("OverallText")?.GetComponent<Text>();
            Assert.That(overall, Is.Not.Null);
            Assert.That(overall.alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(overall.GetComponent<RectTransform>().sizeDelta.x, Is.GreaterThanOrEqualTo(700f));
        }

        [Test]
        public void CardPrefabHasPhase8TarotFaceAndBackDetails()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.transform.Find("Front/Phase8_ArcanaFrame"), Is.Not.Null);
            Assert.That(card.transform.Find("Back/Phase8_BackPatternTop"), Is.Not.Null);
            Assert.That(card.transform.Find("Back/Phase8_BackPatternBottom"), Is.Not.Null);
            Assert.That(card.transform.Find("Back/Phase8_CenterGem"), Is.Not.Null);

            Assert.That(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Phase8_CardIvory.mat"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Phase8_TableGreen.mat"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/TX_Phase8_TableWeave.png"), Is.Not.Null);
        }

        [Test]
        public void ThemeUsesGoldIvoryAndGreenAccents()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            var theme = Object.FindFirstObjectByType<TarotUiTheme>();
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.ThemeName, Is.EqualTo("Moonlit Tarot"));

            var serializedTheme = new SerializedObject(theme);
            var gold = GetColor(serializedTheme, "accentGoldColor");
            var green = GetColor(serializedTheme, "tableGreenColor");
            var ivory = GetColor(serializedTheme, "panelIvoryColor");

            Assert.That(gold.r, Is.GreaterThan(0.70f));
            Assert.That(gold.g, Is.GreaterThan(0.50f));
            Assert.That(gold.b, Is.LessThan(0.30f));
            Assert.That(green.g, Is.GreaterThan(green.r));
            Assert.That(green.g, Is.GreaterThan(green.b));
            Assert.That(ivory.r, Is.GreaterThan(0.80f));
            Assert.That(ivory.g, Is.GreaterThan(0.75f));
        }

        private static Color GetColor(SerializedObject serializedObject, string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing color property {propertyName}");
            return property.colorValue;
        }
    }
}
