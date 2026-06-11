using System;
using System.IO;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase13TarotArtworkPipelineTests
    {
        private const string ArtworkFolder = "Assets/Art/Tarot/RWS1909";
        private const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        private const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string Phase13DocPath = "Docs/PHASE13_TAROT_ARTWORK_PIPELINE.md";

        [Test]
        public void Phase13ArtworkCatalogTypeExists()
        {
            var catalogType = Type.GetType("TarotUnity.Gameplay.CardArtworkCatalog, TarotUnity.Runtime");
            Assert.That(catalogType, Is.Not.Null);
            Assert.That(catalogType.IsSubclassOf(typeof(ScriptableObject)), Is.True);
            Assert.That(catalogType.GetMethod("FindSprite", new[] { typeof(CardDrawData) }), Is.Not.Null);
        }

        [Test]
        public void Rws1909ArtworkFolderContainsFullDeckSprites()
        {
            Assert.That(AssetDatabase.IsValidFolder(ArtworkFolder), Is.True);

            var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { ArtworkFolder });
            Assert.That(spriteGuids.Length, Is.GreaterThanOrEqualTo(78));

            AssertSpriteImporter("major_00_fool");
            AssertSpriteImporter("major_21_world");
            AssertSpriteImporter("cups_01_ace_of_cups");
            AssertSpriteImporter("wands_14_king_of_wands");
        }

        [Test]
        public void DefaultCatalogMapsBackendCardDataToSprites()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var serializedCatalog = new SerializedObject(catalog);
            var entries = serializedCatalog.FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(78));

            AssertCatalogContains(entries, "major_00");
            AssertCatalogContains(entries, "major_21");
            AssertCatalogContains(entries, "cups_01");
            AssertCatalogContains(entries, "wands_14");

            var method = catalog.GetType().GetMethod("FindSprite", new[] { typeof(CardDrawData) });
            Assert.That(method, Is.Not.Null);

            var foolSprite = method.Invoke(catalog, new object[] { CreateDraw("major", string.Empty, 0, "The Fool") });
            Assert.That(foolSprite, Is.TypeOf<Sprite>());

            var kingOfWandsSprite = method.Invoke(catalog, new object[] { CreateDraw("minor", "wands", 14, "King of Wands") });
            Assert.That(kingOfWandsSprite, Is.TypeOf<Sprite>());
        }

        [Test]
        public void ReadingRoomDeckUsesDefaultArtworkCatalog()
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);

            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            Assert.That(deck, Is.Not.Null);

            var serializedDeck = new SerializedObject(deck);
            var catalog = serializedDeck.FindProperty("artworkCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void ResultPresenterUsesArtworkCatalogAndShowcaseSlot()
        {
            EditorSceneManager.OpenScene(ResultScenePath);

            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null);

            var serializedPresenter = new SerializedObject(presenter);
            var catalog = serializedPresenter.FindProperty("cardArtworkCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.objectReferenceValue, Is.Not.Null);

            var slot = serializedPresenter.FindProperty("resultCardArtworkSlot");
            Assert.That(slot, Is.Not.Null);
            Assert.That(slot.objectReferenceValue, Is.TypeOf<Image>());
        }

        [Test]
        public void ResultPresenterShowsPrimaryCardArtworkFromSession()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var presenterObject = new GameObject("ResultPresenter");
            var slotObject = new GameObject("ArtworkSlot");
            var image = slotObject.AddComponent<Image>();
            slotObject.transform.SetParent(presenterObject.transform);

            try
            {
                var presenter = presenterObject.AddComponent<ResultPanelPresenter>();
                var serializedPresenter = new SerializedObject(presenter);
                serializedPresenter.FindProperty("cardArtworkCatalog").objectReferenceValue = catalog;
                serializedPresenter.FindProperty("resultCardArtworkSlot").objectReferenceValue = image;
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

                presenter.PresentSession(new ReadingSessionSnapshot
                {
                    cardDraws = new[] { CreateDraw("major", string.Empty, 0, "The Fool") },
                });

                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void CardViewShowsAssignedArtworkOnlyAfterFlip()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var method = catalog.GetType().GetMethod("FindSprite", new[] { typeof(CardDrawData) });
            var sprite = method.Invoke(catalog, new object[] { CreateDraw("major", string.Empty, 0, "The Fool") }) as Sprite;
            Assert.That(sprite, Is.Not.Null);

            var cardObject = new GameObject("Card");
            var artworkObject = new GameObject("Artwork");
            var artworkRenderer = artworkObject.AddComponent<SpriteRenderer>();
            artworkObject.transform.SetParent(cardObject.transform);

            try
            {
                var card = cardObject.AddComponent<CardView>();
                var serializedCard = new SerializedObject(card);
                serializedCard.FindProperty("faceArtworkRenderer").objectReferenceValue = artworkRenderer;
                serializedCard.ApplyModifiedPropertiesWithoutUndo();

                card.SetFaceArtwork(sprite);
                Assert.That(artworkRenderer.enabled, Is.False);

                card.SetFaceUp(true);
                Assert.That(artworkRenderer.enabled, Is.True);

                card.SetFaceUp(false);
                Assert.That(artworkRenderer.enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void Phase13DocumentsArtworkSourceAndLicense()
        {
            Assert.That(File.Exists(Phase13DocPath), Is.True);

            var text = File.ReadAllText(Phase13DocPath);
            Assert.That(text, Does.Contain("RWS1909"));
            Assert.That(text, Does.Contain("Wikimedia Commons"));
            Assert.That(text, Does.Contain("Public Domain Mark"));
            Assert.That(text, Does.Contain("Rider-Waite tarot deck (Roses & Lilies)"));
        }

        private static void AssertSpriteImporter(string fileNameWithoutExtension)
        {
            var guids = AssetDatabase.FindAssets(fileNameWithoutExtension, new[] { ArtworkFolder });
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(1), $"Missing artwork asset {fileNameWithoutExtension}");

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, $"Missing TextureImporter for {path}");
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), $"{path} must import as Sprite");
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), $"{path} must be a single Sprite");
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), $"{path} should stay desktop-friendly");
        }

        private static void AssertCatalogContains(SerializedProperty entries, string key)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var keyProperty = entry.FindPropertyRelative("key");
                var artworkProperty = entry.FindPropertyRelative("artwork");

                if (keyProperty != null && keyProperty.stringValue == key)
                {
                    Assert.That(artworkProperty, Is.Not.Null);
                    Assert.That(artworkProperty.objectReferenceValue, Is.TypeOf<Sprite>());
                    return;
                }
            }

            Assert.Fail($"Catalog missing card key {key}");
        }

        private static CardDrawData CreateDraw(string arcana, string suit, int number, string nameEn)
        {
            return new CardDrawData
            {
                tarot_card = new TarotCardSimple
                {
                    arcana = arcana,
                    suit = suit,
                    number = number,
                    name_en = nameEn,
                },
            };
        }
    }
}
