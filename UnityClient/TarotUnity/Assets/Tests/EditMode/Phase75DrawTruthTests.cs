using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 75: a four-second shuffle, and an offline draw that is a real draw - the shuffled
    /// 78-card deck, no card twice, each reversed half the time like the backend's, with the
    /// meaning of the way it lies, and a reversed card's picture upside down on the table.
    /// </summary>
    public sealed class Phase75DrawTruthTests
    {
        private const string ScenePath = "Assets/Scenes/ReadingRoom.unity";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/PF_TarotCard.prefab";
        private const string DocPath = "Docs/PHASE75_DRAW_TRUTH.md";

        [Test]
        public void TheShuffleTakesAboutFourSeconds()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var shuffle = Object.FindFirstObjectByType<DeckShuffleChoreographer>();
            Assert.That(shuffle.PlannedSeconds, Is.InRange(3.9f, 4.1f));
        }

        [Test]
        public void TheClientDeckIsTheBackendsDeck()
        {
            // The seed file is a bare array; JsonUtility needs an object around it.
            var seed = File.ReadAllText(Path.Combine(Application.dataPath, "../../../Server/data/tarotCards.json"));
            var server = JsonUtility.FromJson<TarotDeckData>("{\"cards\":" + seed + "}").cards;
            var client = TarotDeck.Cards;
            Assert.That(server.Length, Is.EqualTo(78), "control: the backend's deck");
            Assert.That(client.Count, Is.EqualTo(78), "the whole deck, not twelve placeholders");
            Assert.That(client.Select(c => c.id).Distinct().Count(), Is.EqualTo(78));
            for (var i = 0; i < server.Length; i++)
            {
                var s = server[i];
                var c = client[i];
                Assert.That((c.id, c.nameZh, c.nameEn, c.cardNumber, c.type, c.suit ?? string.Empty, c.uprightMeaning, c.reversedMeaning),
                    Is.EqualTo((s.id, s.nameZh, s.nameEn, s.cardNumber, s.type, s.suit ?? string.Empty, s.uprightMeaning, s.reversedMeaning)),
                    $"card {s.id} matches the backend - re-run the Phase 75 bootstrap if the backend changed");
            }
        }

        [Test]
        public void ReversedOddsMatchTheBackend()
        {
            var records = File.ReadAllText(Path.Combine(Application.dataPath, "../../../Server/app/api/v1/endpoints/records.py"));
            var match = Regex.Match(records, @"REVERSED_PROBABILITY\s*=\s*([0-9.]+)");
            Assert.That(match.Success, Is.True, "control: the backend names its odds");
            Assert.That(LocalReadingSimulator.ReversedProbability, Is.EqualTo(double.Parse(match.Groups[1].Value)));
            Assert.That(LocalReadingSimulator.ReversedProbability, Is.EqualTo(0.5));
        }

        [Test]
        public void ADrawNeverRepeatsACardAndCoversTheWholeDeck()
        {
            var rng = new System.Random(75);
            var seen = new HashSet<int>();
            for (var round = 0; round < 300; round++)
            {
                var draws = LocalReadingSimulator.DrawFromDeck(10, null, null, rng);
                Assert.That(draws.Length, Is.EqualTo(10));
                Assert.That(draws.Select(d => d.tarot_card_id).Distinct().Count(), Is.EqualTo(10), $"round {round}: no card twice");
                seen.UnionWith(draws.Select(d => d.tarot_card_id));
            }

            Assert.That(seen.Count, Is.EqualTo(78), "every card of the deck can come up");
        }

        [Test]
        public void AboutHalfTheCardsComeUpReversedWithThatMeaning()
        {
            var rng = new System.Random(7);
            var byId = TarotDeck.Cards.ToDictionary(c => c.id);
            var reversed = 0;
            var total = 0;
            for (var round = 0; round < 400; round++)
            {
                foreach (var draw in LocalReadingSimulator.DrawFromDeck(10, null, null, rng))
                {
                    total++;
                    reversed += draw.is_reversed ? 1 : 0;
                    var card = byId[draw.tarot_card_id];
                    Assert.That(draw.card_meaning.is_reversed, Is.EqualTo(draw.is_reversed));
                    Assert.That(draw.card_meaning.meaning, Is.EqualTo(draw.is_reversed ? card.reversedMeaning : card.uprightMeaning),
                        "the meaning of the way the card lies");
                    Assert.That(draw.tarot_card.name_zh, Is.EqualTo(card.nameZh));
                }
            }

            Assert.That((double)reversed / total, Is.InRange(0.46, 0.54));
        }

        [Test]
        public void ADrawFollowsItsSeedAndKeepsThePositions()
        {
            var names = new[] { "过去", "现在", "未来" };
            var a = LocalReadingSimulator.DrawFromDeck(3, names, null, new System.Random(3));
            var b = LocalReadingSimulator.DrawFromDeck(3, names, null, new System.Random(3));
            Assert.That(a.Select(d => (d.tarot_card_id, d.is_reversed)), Is.EqualTo(b.Select(d => (d.tarot_card_id, d.is_reversed))));
            Assert.That(a.Select(d => d.position_name), Is.EqualTo(names));
            Assert.That(a.Select(d => d.position), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void EveryCardOfTheDeckHasItsArtwork()
        {
            var catalog = Resources.Load<CardArtworkCatalog>("TarotArt/RWS1909_CardArtworkCatalog");
            Assert.That(catalog, Is.Not.Null);
            var missing = new List<string>();
            // One whole deck dealt out is every card.
            foreach (var draw in LocalReadingSimulator.DrawFromDeck(78, null, null, new System.Random(0)))
            {
                if (catalog.FindSprite(draw) == null)
                {
                    missing.Add(draw.tarot_card.name_en);
                }
            }

            Assert.That(missing, Is.Empty, "minor arcana too");
        }

        [Test]
        public void AReversedCardsPictureLiesUpsideDown()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var upright = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<CardView>();
            var reversed = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<CardView>();
            try
            {
                var draw = LocalReadingSimulator.DrawFromDeck(1, null, null, new System.Random(1))[0];
                draw.is_reversed = false;
                upright.Bind(draw);
                var turned = LocalReadingSimulator.DrawFromDeck(1, null, null, new System.Random(1))[0];
                turned.is_reversed = true;
                reversed.Bind(turned);

                var up = upright.FaceArtwork.transform;
                var down = reversed.FaceArtwork.transform;
                Assert.That(Vector3.Dot(up.up, down.up), Is.LessThan(-0.99f), "the picture's top points the other way");
                Assert.That(Vector3.Dot(up.forward, down.forward), Is.GreaterThan(0.99f), "still facing up off the card");

                reversed.Bind(draw);
                Assert.That(Vector3.Dot(up.up, reversed.FaceArtwork.transform.up), Is.GreaterThan(0.99f), "rebinding upright turns it back");
            }
            finally
            {
                Object.DestroyImmediate(upright.gameObject);
                Object.DestroyImmediate(reversed.gameObject);
            }
        }

        [Test]
        public void Phase75DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("逆位"));
            Assert.That(text, Does.Contain("78"));
            Assert.That(text, Does.Contain("牌堆"));
        }
    }
}
