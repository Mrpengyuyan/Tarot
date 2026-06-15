using System.IO;
using NUnit.Framework;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 31 guards the HD visual archive artifacts so the review set cannot silently
    /// go missing. The capture itself is a manual editor tool; this asserts its output.
    /// </summary>
    public sealed class Phase31VisualArchiveTests
    {
        private const string ArchiveFolder = "Docs/VisualReview/Phase31_HDArchive";
        private const string DocPath = "Docs/PHASE31_HD_VISUAL_ARCHIVE.md";

        private static readonly string[] ArchiveShots =
        {
            "01_MainMenu.png",
            "02_ReadingRoom.png",
            "03_Result_default.png",
            "04_Result_long.png",
        };

        [Test]
        public void ArchiveShotsExistAndAreNonTrivial()
        {
            foreach (var shot in ArchiveShots)
            {
                var path = Path.Combine(ArchiveFolder, shot);
                Assert.That(File.Exists(path), Is.True, $"Missing archive shot {path}");
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096), $"Archive shot {path} is unexpectedly small");
            }

            var manifest = Path.Combine(ArchiveFolder, "phase31_archive_manifest.json");
            Assert.That(File.Exists(manifest), Is.True, $"Missing archive manifest {manifest}");
            Assert.That(File.ReadAllText(manifest), Does.Contain("2560x1440"));
        }

        [Test]
        public void Phase31DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing Phase31 doc at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("archive"));
            Assert.That(text, Does.Contain("2560x1440"));
            Assert.That(text, Does.Contain("not final high-end VFX"));
        }
    }
}
