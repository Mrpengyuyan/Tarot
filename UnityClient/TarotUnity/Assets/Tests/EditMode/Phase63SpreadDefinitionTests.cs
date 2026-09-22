using System.IO;
using NUnit.Framework;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 63: spreads became data (SpreadCatalog) and the ten-card Celtic Cross
    /// was added. These guard the catalog, the ten Celtic table slots / sockets /
    /// glows, the socket-glow linkage by card count, and the offline reader picking
    /// up the Celtic position names — so the data-driven path can't silently rot.
    /// </summary>
    public sealed class Phase63SpreadDefinitionTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string DocPath = "Docs/PHASE63_SPREAD_DEFINITION.md";
        private const string SeedPath = "../../Server/data/spreads.json";

        [System.Serializable]
        private sealed class SeedPosition
        {
            public string name;
        }

        [System.Serializable]
        private sealed class SeedSpread
        {
            public string name;
            public int cardCount;
            public SeedPosition[] positions;
        }

        [System.Serializable]
        private sealed class SeedSpreadList
        {
            public SeedSpread[] items;
        }

        private static SpreadCatalog LoadCatalog()
        {
            var catalog = Resources.Load<SpreadCatalog>(SpreadCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null, "SpreadCatalog missing from Resources - run the Phase 63 bootstrapper");
            return catalog;
        }

        [Test]
        public void CatalogDefinesTheCelticCross()
        {
            var catalog = LoadCatalog();
            Assert.That(catalog.GetByCardCount(1), Is.Not.Null, "one-card spread missing");
            Assert.That(catalog.GetByCardCount(3), Is.Not.Null, "three-card spread missing");

            var celtic = catalog.GetByCardCount(10);
            Assert.That(celtic, Is.Not.Null, "Celtic Cross missing from catalog");
            Assert.That(celtic.slots.Length, Is.EqualTo(10), "Celtic Cross needs ten slots");
            Assert.That(celtic.positionNames.Length, Is.EqualTo(10), "Celtic Cross needs ten position names");
            Assert.That(celtic.positionNames[0], Is.EqualTo("现状"));
            Assert.That(celtic.positionNames[9], Is.EqualTo("结果"));
        }

        [Test]
        public void LayoutHasTenCelticSlots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var layout = Object.FindFirstObjectByType<SpreadLayoutController>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.GetSlots(10).Count, Is.EqualTo(10), "the Celtic Cross should register ten table slots");
            // The existing spreads stay wired.
            Assert.That(layout.GetSlots(1).Count, Is.EqualTo(1));
            Assert.That(layout.GetSlots(3).Count, Is.EqualTo(3));
        }

        [Test]
        public void CelticSocketsAndGlowsAreBuilt()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sockets = GameObject.Find("MP_CelticSockets");
            var glows = GameObject.Find("MP_CelticSocketGlows");
            Assert.That(sockets, Is.Not.Null, "Celtic sockets group missing");
            Assert.That(glows, Is.Not.Null, "Celtic socket glows group missing");
            Assert.That(sockets.transform.childCount, Is.EqualTo(10), "ten Celtic sockets");
            Assert.That(glows.transform.childCount, Is.EqualTo(10), "ten Celtic socket glows");
        }

        [Test]
        public void CelticSocketsLightForATenCardDraw()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>();
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            Assert.That(indicator, Is.Not.Null);
            Assert.That(flow, Is.Not.Null);

            flow.SelectSpread(3, 10);
            indicator.ApplyFlowState(ReadingFlowState.Shuffling);

            var glow = GameObject.Find("MP_CelticSocketGlows")?.transform.Find("MP_SocketGlow_Celtic_00");
            Assert.That(glow, Is.Not.Null);
            Assert.That(glow.gameObject.activeSelf, Is.True, "a Celtic socket should glow while drawing the ten-card spread");
        }

        [Test]
        public void CelticButtonAndCameraPoseExist()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.That(GameObject.Find("CelticCrossButton"), Is.Not.Null, "the Celtic Cross spread button should exist");
            Assert.That(GameObject.Find("MP_CelticCrossPose"), Is.Not.Null, "the Celtic camera pose should exist");
        }

        [Test]
        public void OfflineReaderUsesCelticPositionNames()
        {
            var celtic = LoadCatalog().GetByCardCount(10);
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(10, celtic.positionNames, celtic.positionMeanings);
            Assert.That(draws.Length, Is.EqualTo(10));
            Assert.That(draws[0].position_name, Is.EqualTo("现状"));
            Assert.That(draws[9].position_name, Is.EqualTo("结果"));
            // Ten distinct placeholder cards, not repeats.
            Assert.That(draws[0].tarot_card.name_zh, Is.Not.EqualTo(draws[9].tarot_card.name_zh));
        }

        // Phase 67 follow-up: the offline catalog and the backend's seed data name the same spread
        // the same way, so a reading reads alike online and offline. The ten-card spread keeps the
        // classic position names (现状/挑战/根基...) on purpose and is compared by name only.
        [Test]
        public void CatalogNamesMatchTheBackendSeedData()
        {
            if (!File.Exists(SeedPath))
            {
                Assert.Ignore($"backend seed data not present at {SeedPath}");
            }

            var seeds = JsonUtility.FromJson<SeedSpreadList>("{\"items\":" + File.ReadAllText(SeedPath) + "}");
            Assert.That(seeds?.items, Is.Not.Null.And.Not.Empty, "control: the seed file parsed");

            var catalog = LoadCatalog();
            foreach (var cardCount in new[] { 1, 3, 10 })
            {
                var seed = System.Array.Find(seeds.items, entry => entry.cardCount == cardCount);
                Assert.That(seed, Is.Not.Null, $"seed data has no {cardCount}-card spread");
                var def = catalog.GetByCardCount(cardCount);
                Assert.That(def, Is.Not.Null, $"catalog has no {cardCount}-card spread");
                Assert.That(def.displayName, Is.EqualTo(seed.name), $"{cardCount}-card spread name");
                if (cardCount == 10)
                {
                    continue;
                }

                for (var i = 0; i < cardCount; i++)
                {
                    Assert.That(def.positionNames[i], Is.EqualTo(seed.positions[i].name),
                        $"{cardCount}-card position {i + 1}");
                }
            }
        }

        [Test]
        public void Phase63DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase 63 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("spread"));
            Assert.That(text, Does.Contain("Celtic"));
        }
    }
}
