using System;
using System.Collections.Generic;
using System.IO;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 27 upgrades the tarot card faces to HD. Phase 13 wired an authentic
    /// public-domain RWS1909 deck, but only the low-resolution scans (400x666)
    /// got imported because Wikimedia rate-limited the original batch. This pass
    /// imports the HD re-download (~740x1280 full-card scans from Wikimedia
    /// Commons, public domain) from a new folder and re-points the existing
    /// catalog at those sprites, so flipping a card and the result hero now show
    /// crisp art. The old low-res folder is left untouched for the user to remove.
    /// </summary>
    public static class Phase27HdArtworkBootstrapper
    {
        public const string ArtworkFolder = "Assets/Art/Tarot/RWS1909_HD";
        public const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const int MaxTextureSize = 2048;

        [MenuItem("Tools/Tarot Unity/Run Phase 27 HD Artwork Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ArtworkFolder))
            {
                Debug.LogError($"Phase 27 bootstrap: HD artwork folder missing at {ArtworkFolder}.");
                return;
            }

            ConfigureTextureImporters();
            AssetDatabase.Refresh();

            var catalog = BuildCatalog();
            AssignReadingRoomCatalog(catalog);
            AssignResultCatalog(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Tarot Unity Phase 27 HD artwork bootstrap complete. Entries: {catalog.Entries.Count}.");
        }

        private static void ConfigureTextureImporters()
        {
            foreach (var path in Directory.GetFiles(ArtworkFolder, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                var changed = false;
                changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite, v => importer.textureType = v);
                changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);
                changed |= SetIfDifferent(importer.maxTextureSize, MaxTextureSize, v => importer.maxTextureSize = v);
                changed |= SetIfDifferent(importer.mipmapEnabled, false, v => importer.mipmapEnabled = v);
                changed |= SetIfDifferent(importer.alphaIsTransparency, false, v => importer.alphaIsTransparency = v);
                changed |= SetIfDifferent(importer.sRGBTexture, true, v => importer.sRGBTexture = v);

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static CardArtworkCatalog BuildCatalog()
        {
            var entries = new List<CardArtworkEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtworkFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null && TryBuildEntry(path, sprite, out var entry))
                {
                    entries.Add(entry);
                }
            }

            entries.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));

            var catalog = AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CardArtworkCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignReadingRoomCatalog(CardArtworkCatalog catalog)
        {
            EditorSceneManager.OpenScene(ReadingRoomScenePath);
            var deck = UnityEngine.Object.FindFirstObjectByType<DeckController>();
            if (deck != null)
            {
                var serialized = new SerializedObject(deck);
                serialized.FindProperty("artworkCatalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(deck);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void AssignResultCatalog(CardArtworkCatalog catalog)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            if (presenter != null)
            {
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("cardArtworkCatalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static bool TryBuildEntry(string path, Sprite sprite, out CardArtworkEntry entry)
        {
            entry = null;
            var name = Path.GetFileNameWithoutExtension(path);
            var parts = name.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var number))
            {
                return false;
            }

            var first = parts[0];
            var isMajor = string.Equals(first, "major", StringComparison.OrdinalIgnoreCase);
            entry = new CardArtworkEntry
            {
                key = isMajor ? $"major_{number:00}" : $"{first}_{number:00}",
                cardNameEn = FormatCardName(first, number),
                arcana = isMajor ? "major" : "minor",
                suit = isMajor ? string.Empty : first,
                number = number,
                artwork = sprite,
                sourceTitle = $"RWS1909 HD - {FormatCardName(first, number)}",
                sourceUrl = "https://commons.wikimedia.org/wiki/Category:Rider-Waite_tarot_deck",
            };
            return true;
        }

        private static string FormatCardName(string suitOrMajor, int number)
        {
            if (string.Equals(suitOrMajor, "major", StringComparison.OrdinalIgnoreCase))
            {
                return number switch
                {
                    0 => "The Fool", 1 => "The Magician", 2 => "The High Priestess", 3 => "The Empress",
                    4 => "The Emperor", 5 => "The Hierophant", 6 => "The Lovers", 7 => "The Chariot",
                    8 => "Strength", 9 => "The Hermit", 10 => "Wheel of Fortune", 11 => "Justice",
                    12 => "The Hanged Man", 13 => "Death", 14 => "Temperance", 15 => "The Devil",
                    16 => "The Tower", 17 => "The Star", 18 => "The Moon", 19 => "The Sun",
                    20 => "Judgement", 21 => "The World", _ => $"Major {number:00}",
                };
            }

            var rank = number switch
            {
                1 => "Ace", 11 => "Page", 12 => "Knight", 13 => "Queen", 14 => "King", _ => number.ToString("00"),
            };
            var suit = string.IsNullOrEmpty(suitOrMajor)
                ? suitOrMajor
                : char.ToUpperInvariant(suitOrMajor[0]) + suitOrMajor.Substring(1);
            return $"{rank} of {suit}";
        }

        private static bool SetIfDifferent<T>(T current, T next, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(current, next))
            {
                return false;
            }

            setter(next);
            return true;
        }
    }
}
