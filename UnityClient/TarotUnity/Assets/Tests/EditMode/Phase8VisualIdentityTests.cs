using NUnit.Framework;
using TMPro;
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
            // Phase 40 deleted the green slab; the velvet menu stage replaced it.
            Assert.That(GameObject.Find("Phase8_MenuTableSurface"), Is.Null);
            Assert.That(GameObject.Find("MP_MenuStage"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_LeftCandle"), Is.Not.Null);
            Assert.That(GameObject.Find("Phase8_RightCandle"), Is.Not.Null);

            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_TitleConstellation"), Is.Not.Null);
            Assert.That(canvas.transform.Find("Phase8_MenuCrest"), Is.Not.Null);
            Assert.That(canvas.transform.Find("StartReadingButton/Phase8_StartButtonGoldTrim"), Is.Not.Null);

            // Phase 43 migrated the menu to TMP SDF.
            var title = canvas.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            Assert.That(title, Is.Not.Null);
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(60f));
        }

        [Test]
        public void ReadingRoomHasPremiumTableAndControlFraming()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            // Phase 38 deleted the Phase 8 flat cloth and ring; the velvet stage
            // took their place.
            Assert.That(GameObject.Find("Phase8_TableClothSurface"), Is.Null);
            Assert.That(GameObject.Find("Phase8_DeckFocusRing"), Is.Null);
            Assert.That(GameObject.Find("MP_TableStage"), Is.Not.Null);

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

            // Phase 29 moved OverallText into the scroll Content, so it is no longer a
            // direct child of the canvas and its width is layout-driven (not sizeDelta).
            // Find it recursively and assert the resolved width keeps the premium column.
            var overall = FindDescendantText(canvas.transform, "OverallText");
            Assert.That(overall, Is.Not.Null);
            Assert.That(overall.alignment, Is.EqualTo(TextAlignmentOptions.TopLeft));

            var overallRect = overall.GetComponent<RectTransform>();
            // Width is driven by the parent VerticalLayoutGroup, so rebuild the Content
            // (OverallText's parent), not the leaf, before reading the resolved width.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)overallRect.parent);
            Assert.That(overallRect.rect.width, Is.GreaterThanOrEqualTo(700f),
                "Result reading body must keep the premium-width column even inside the scroll panel");
        }

        private static TMP_Text FindDescendantText(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        [Test]
        public void CardPrefabHasPhase8TarotFaceAndBackDetails()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Prefabs/Cards/PF_TarotCard.prefab");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.transform.Find("Front/Phase8_ArcanaFrame"), Is.Not.Null);
            // Phase 38 replaced the primitive back ornaments with the composed
            // celestial back texture.
            Assert.That(card.transform.Find("Back/Phase8_BackPatternTop"), Is.Null);
            Assert.That(card.transform.Find("Back/Phase8_BackPatternBottom"), Is.Null);
            Assert.That(card.transform.Find("Back/Phase8_CenterGem"), Is.Null);
            Assert.That(card.transform.Find("Back/MP_CardBackFace"), Is.Not.Null);

            Assert.That(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Phase8_CardIvory.mat"), Is.Not.Null);
            // MAT_Phase8_TableGreen was deleted with its cloth surface (Phase 41).
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Phase8_TableGreen.mat"), Is.Null);
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
