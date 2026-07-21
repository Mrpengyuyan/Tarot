using System.IO;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 60: the Result screen used to render only draws[0], so a three-card
    /// spread showed one card. These drive the real presenter over the Result scene
    /// and assert a multi-card reading fills every cell (with the reversed card
    /// turned), while a one-card reading keeps the original single hero.
    /// </summary>
    public sealed class Phase60ResultSpreadTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const string BandName = "MP_ResultSpreadBand";
        private const string DocPath = "Docs/PHASE60_RESULT_SPREAD.md";

        private static ResultPanelPresenter OpenAndFindPresenter()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "ResultPanelPresenter missing from Result scene");
            return presenter;
        }

        private static ReadingSessionSnapshot Session(int cardCount)
        {
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            return LocalReadingSimulator.CreateSession(2, "三牌阵", "问题？", "general", draws);
        }

        [Test]
        public void ThreeCardSpreadFillsEveryCell()
        {
            var presenter = OpenAndFindPresenter();
            presenter.PresentSession(Session(3));

            var band = GameObject.Find(BandName);
            Assert.That(band, Is.Not.Null, "spread band not built - run the Phase 60 bootstrapper");
            Assert.That(band.activeInHierarchy, Is.True, "band should be visible for a three-card spread");

            for (var i = 0; i < 3; i++)
            {
                var art = band.transform.Find($"SpreadCell_{i}/ReversePivot/Artwork")?.GetComponent<Image>();
                Assert.That(art, Is.Not.Null, $"cell {i} has no Artwork image");
                Assert.That(art.sprite, Is.Not.Null, $"cell {i} shows no card - only draws[0] was rendered before Phase 60");

                var label = band.transform.Find($"SpreadCell_{i}/Label")?.GetComponent<TMPro.TMP_Text>();
                Assert.That(label, Is.Not.Null, $"cell {i} has no Label");
                Assert.That(string.IsNullOrEmpty(label.text), Is.False, $"cell {i} label is empty");
            }
        }

        [Test]
        public void ReversedCardIsTurned()
        {
            var presenter = OpenAndFindPresenter();
            presenter.PresentSession(Session(3)); // placeholder makes the third card reversed

            var pivot = GameObject.Find(BandName)?.transform.Find("SpreadCell_2/ReversePivot");
            Assert.That(pivot, Is.Not.Null);
            Assert.That(Mathf.DeltaAngle(pivot.localEulerAngles.z, 180f), Is.EqualTo(0f).Within(1f),
                "a reversed card should be turned 180 degrees");
        }

        [Test]
        public void OneCardKeepsTheSingleHero()
        {
            var presenter = OpenAndFindPresenter();
            presenter.PresentSession(Session(1));

            // The band is hidden (inactive), so Find returns null; the hero showcase stays.
            Assert.That(GameObject.Find(BandName), Is.Null, "band should be hidden for a one-card reading");
            Assert.That(GameObject.Find("Phase12_ResultCardShowcase"), Is.Not.Null,
                "the single-card hero showcase should stay for a one-card reading");
        }

        [Test]
        public void SpreadHidesTheSingleHero()
        {
            var presenter = OpenAndFindPresenter();
            presenter.PresentSession(Session(3));

            Assert.That(GameObject.Find("Phase12_ResultCardShowcase"), Is.Null,
                "the one-card hero should be hidden behind the three-card band");
        }

        [Test]
        public void Phase60DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase 60 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("spread"));
            Assert.That(text, Does.Contain("reversed"));
        }
    }
}
