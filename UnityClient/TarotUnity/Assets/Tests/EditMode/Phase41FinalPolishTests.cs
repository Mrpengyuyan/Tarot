using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 41 guards the redesign close-out: the orphaned legacy materials
    /// removed by the sweep stay deleted, the HD archive reflects the Midnight
    /// Parlor screens, and the close-out is documented.
    /// </summary>
    public sealed class Phase41FinalPolishTests
    {
        [Test]
        public void SweptLegacyMaterialsStayDeleted()
        {
            foreach (var name in new[]
            {
                "MAT_Phase8_TableGreen", "MAT_Phase12_CardRevealStage", "MAT_Phase12_RevealBackdrop",
                "MAT_Phase14_TableDepthPlane", "MAT_Phase14_CardRevealPool", "MAT_Phase14_CardEdge",
                "MAT_Phase14_CastShadow", "MAT_Phase14_FaceRimLight", "MAT_Phase14_ArtworkGlass",
                "MAT_Phase15_RitualTableSurface", "MAT_Phase15_TableDepthRing",
                "MAT_Phase7_DeepVelvet", "MAT_CardAccent", "MAT_Table",
            })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat"),
                    Is.Null, $"{name} was orphaned by the redesign and must stay deleted");
            }
        }

        [Test]
        public void SurvivingSharedMaterialsStillExist()
        {
            foreach (var name in new[] { "MAT_Phase7_MoonGold", "MAT_DeckStack", "MAT_SpreadSlot", "MAT_Phase14_RevealGlow" })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat"),
                    Is.Not.Null, $"{name} is still referenced and must survive the sweep");
            }
        }

        [Test]
        public void HdArchiveIsRegenerated()
        {
            foreach (var file in new[]
            {
                "01_MainMenu.png", "02_ReadingRoom.png", "03_Result_default.png", "04_Result_long.png",
            })
            {
                Assert.That(File.Exists($"Docs/VisualReview/Phase31_HDArchive/{file}"), Is.True, file);
            }
        }

        [Test]
        public void Phase41DocumentationExists()
        {
            const string doc = "Docs/PHASE41_FINAL_POLISH.md";
            Assert.That(File.Exists(doc), Is.True);
            Assert.That(File.ReadAllText(doc), Does.Contain("sweep"));
        }
    }
}
