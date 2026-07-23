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
    /// Phase 62: the result band was three fixed cells, so a spread with more than
    /// three cards dropped the extras (the same class of bug Phase 60 fixed for the
    /// 3→1 case). The band is now a pool the presenter lays out per reading. These
    /// guard that a five-card reading fills five centred cells, the pool is deep
    /// enough for a Celtic Cross, and a three-card reading is unchanged.
    /// </summary>
    public sealed class Phase62ResultDynamicRowTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";
        private const string BandName = "MP_ResultSpreadBand";
        private const string DocPath = "Docs/PHASE62_RESULT_DYNAMIC_ROW.md";

        private static ResultPanelPresenter OpenAndPresent(int cardCount)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "ResultPanelPresenter missing from Result scene");
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            presenter.PresentSession(LocalReadingSimulator.CreateSession(2, "阵", "问题？", "general", draws));
            return presenter;
        }

        private static Transform Cell(int i)
        {
            return GameObject.Find(BandName)?.transform.Find($"SpreadCell_{i}");
        }

        [Test]
        public void BandHasRoomForACelticCross()
        {
            OpenAndPresent(3);
            Assert.That(Cell(9), Is.Not.Null, "the band pool should hold at least ten cells");
        }

        [Test]
        public void FiveCardSpreadFillsEveryDrawnCell()
        {
            OpenAndPresent(5);

            for (var i = 0; i < 5; i++)
            {
                var cell = Cell(i);
                Assert.That(cell, Is.Not.Null, $"cell {i} missing");
                Assert.That(cell.gameObject.activeInHierarchy, Is.True, $"cell {i} should be shown for a five-card spread");
                var art = cell.Find("ReversePivot/Artwork")?.GetComponent<Image>();
                Assert.That(art?.sprite, Is.Not.Null, $"cell {i} shows no card - a five-card spread must not drop cards");
            }

            // The unused pool cells stay hidden.
            Assert.That(Cell(5).gameObject.activeSelf, Is.False, "cell 5 should be hidden for a five-card spread");
        }

        [Test]
        public void FiveCardCellsAreCentredAndDistinct()
        {
            OpenAndPresent(5);

            var xs = new float[5];
            for (var i = 0; i < 5; i++)
            {
                xs[i] = ((RectTransform)Cell(i)).anchoredPosition.x;
            }

            Assert.That(xs[2], Is.EqualTo(0f).Within(0.5f), "the middle card should sit on centre");
            Assert.That(xs[0], Is.EqualTo(-xs[4]).Within(0.5f), "the row should be symmetric about centre");
            Assert.That(xs[0], Is.LessThan(xs[1]), "cells must not overlap");
            Assert.That(xs[1], Is.LessThan(xs[2]), "cells must not overlap");
        }

        [Test]
        public void ThreeCardStillFillsExactlyThree()
        {
            OpenAndPresent(3);

            for (var i = 0; i < 3; i++)
            {
                Assert.That(Cell(i).gameObject.activeInHierarchy, Is.True, $"cell {i} should be shown");
            }

            Assert.That(Cell(3).gameObject.activeSelf, Is.False, "cell 3 should be hidden for a three-card spread");
        }

        [Test]
        public void Phase62DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase 62 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("spread"));
            Assert.That(text, Does.Contain("card"));
        }
    }
}
