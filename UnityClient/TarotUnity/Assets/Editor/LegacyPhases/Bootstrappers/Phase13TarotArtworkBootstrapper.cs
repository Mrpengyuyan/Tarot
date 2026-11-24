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
    public static class Phase13TarotArtworkBootstrapper
    {
        public const string ArtworkFolder = "Assets/Art/Tarot/RWS1909";
        public const string CatalogPath = "Assets/Resources/TarotArt/RWS1909_CardArtworkCatalog.asset";
        public const string ReadingRoomScenePath = "Assets/Scenes/ReadingRoom.unity";
        public const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/Tarot Unity/Run Phase 13 Tarot Artwork Bootstrap")]
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

            // Phase 13 is superseded by Phase 27, which re-sourced the deck at HD into
            // RWS1909_HD and removed this low-res folder. Without this guard the bootstrap
            // would throw DirectoryNotFoundException; degrade gracefully and point the user
            // at the current pipeline instead.
            if (!AssetDatabase.IsValidFolder(ArtworkFolder))
            {
                Debug.LogWarning(
                    $"Phase 13 artwork folder '{ArtworkFolder}' no longer exists. This phase is " +
                    "superseded by Phase 27 (HD pipeline, RWS1909_HD); run 'Run Phase 27 HD Artwork Bootstrap' instead.");
                return;
            }

            ConfigureTextureImporters();
            AssetDatabase.Refresh();

            var catalog = BuildCatalog();
            AssignReadingRoomCatalog(catalog);
            AssignResultCatalog(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 13 tarot artwork bootstrap complete.");
        }

        private static void ConfigureTextureImporters()
        {
            foreach (var path in Directory.GetFiles(ArtworkFolder, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;
                changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
                changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
                changed |= SetIfDifferent(importer.maxTextureSize, 1024, value => importer.maxTextureSize = value);
                changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
                changed |= SetIfDifferent(importer.alphaIsTransparency, false, value => importer.alphaIsTransparency = value);
                changed |= SetIfDifferent(importer.sRGBTexture, true, value => importer.sRGBTexture = value);

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static CardArtworkCatalog BuildCatalog()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/TarotArt");

            var entries = new List<CardArtworkEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtworkFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                if (!TryBuildEntry(path, sprite, out var entry))
                {
                    continue;
                }

                entries.Add(entry);
            }

            entries.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.Ordinal));

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
                SetSerializedReference(deck, "artworkCatalog", catalog);
                EditorUtility.SetDirty(deck);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void AssignResultCatalog(CardArtworkCatalog catalog)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<ResultPanelPresenter>();
            var artworkSlot = GameObject.Find("ResultCanvas")?.transform
                .Find("Phase12_ResultCardArtworkSlot")
                ?.GetComponent<Image>();

            if (presenter != null)
            {
                var serializedPresenter = new SerializedObject(presenter);
                serializedPresenter.FindProperty("cardArtworkCatalog").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CardArtworkCatalog>(CatalogPath);
                serializedPresenter.FindProperty("resultCardArtworkSlot").objectReferenceValue = artworkSlot;
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }

            if (artworkSlot != null)
            {
                artworkSlot.preserveAspect = true;
                artworkSlot.color = Color.white;
                EditorUtility.SetDirty(artworkSlot);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static bool TryBuildEntry(string path, Sprite sprite, out CardArtworkEntry entry)
        {
            entry = null;
            var name = Path.GetFileNameWithoutExtension(path);
            var parts = name.Split('_');
            if (parts.Length < 2)
            {
                return false;
            }

            var first = parts[0];
            if (!int.TryParse(parts[1], out var number))
            {
                return false;
            }

            var isMajor = string.Equals(first, "major", StringComparison.OrdinalIgnoreCase);
            var key = isMajor ? $"major_{number:00}" : $"{first}_{number:00}";
            entry = new CardArtworkEntry
            {
                key = key,
                cardNameEn = FormatCardName(first, number),
                arcana = isMajor ? "major" : "minor",
                suit = isMajor ? string.Empty : first,
                number = number,
                artwork = sprite,
                sourceTitle = ToCommonsTitle(first, number),
                sourceUrl = "https://commons.wikimedia.org/wiki/Category:Rider-Waite_tarot_deck_(Roses_%26_Lilies)",
            };
            return true;
        }

        private static string FormatCardName(string suitOrMajor, int number)
        {
            if (string.Equals(suitOrMajor, "major", StringComparison.OrdinalIgnoreCase))
            {
                return number switch
                {
                    0 => "The Fool",
                    1 => "The Magician",
                    2 => "The High Priestess",
                    3 => "The Empress",
                    4 => "The Emperor",
                    5 => "The Hierophant",
                    6 => "The Lovers",
                    7 => "The Chariot",
                    8 => "Strength",
                    9 => "The Hermit",
                    10 => "Wheel of Fortune",
                    11 => "Justice",
                    12 => "The Hanged Man",
                    13 => "Death",
                    14 => "Temperance",
                    15 => "The Devil",
                    16 => "The Tower",
                    17 => "The Star",
                    18 => "The Moon",
                    19 => "The Sun",
                    20 => "Judgement",
                    21 => "The World",
                    _ => $"Major {number:00}",
                };
            }

            var rank = number switch
            {
                1 => "Ace",
                11 => "Page",
                12 => "Knight",
                13 => "Queen",
                14 => "King",
                _ => number.ToString("00"),
            };

            return $"{rank} of {UppercaseFirst(suitOrMajor)}";
        }

        private static string ToCommonsTitle(string suitOrMajor, int number)
        {
            if (string.Equals(suitOrMajor, "major", StringComparison.OrdinalIgnoreCase))
            {
                var name = FormatCardName(suitOrMajor, number).Replace("The ", string.Empty);
                return $"RWS1909 - {number:00} {name}";
            }

            return $"RWS1909 - {UppercaseFirst(suitOrMajor)} {number:00}";
        }

        private static string UppercaseFirst(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
