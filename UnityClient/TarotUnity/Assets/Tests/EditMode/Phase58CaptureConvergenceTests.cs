using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 58 found that every visual-review capture in this project rendered once
    /// after loading a scene, and realtime GI has not resolved on that frame. The
    /// menu's front candle measured RGB(246,77,23) on render 1 and RGB(233,177,78)
    /// from render 2 onward - so the "saturated red candles" a whole pass was opened
    /// to fix existed only in the captures used to review the game.
    ///
    /// A review artefact that lies about the product is worse than none, so this
    /// guards the fix at its weakest point: the next capture builder someone writes.
    /// </summary>
    public sealed class Phase58CaptureConvergenceTests
    {
        private const string EditorFolder = "Assets/Editor";
        private const string DocPath = "Docs/PROJECT_CHRONICLE.md";

        // The probes deliberately drive Render() one frame at a time - measuring
        // convergence is the whole point of them.
        private static readonly HashSet<string> Exempt = new()
        {
            "CaptureRig.cs",
            "Phase58GiConvergenceProbe.cs",
            "Phase58CandleColourProbe.cs",
        };

        [Test]
        public void NoCaptureBuilderReadsPixelsFromASingleUnconvergedRender()
        {
            var offenders = new List<string>();

            foreach (var path in Directory.GetFiles(EditorFolder, "*.cs"))
            {
                var name = Path.GetFileName(path);
                if (Exempt.Contains(name))
                {
                    continue;
                }

                var source = File.ReadAllText(path);
                if (Regex.IsMatch(source, @"(?<!\w)\w+\.Render\(\)\s*;"))
                {
                    offenders.Add(name);
                }
            }

            Assert.That(offenders, Is.Empty,
                "these call Camera.Render() directly; route them through " +
                "CaptureRig.RenderConverged or the capture shows lighting that has not settled: " +
                string.Join(", ", offenders));
        }

        [Test]
        public void TheWarmupLeavesRoomBeyondTheMeasuredConvergencePoint()
        {
            // This asmdef cannot reference the editor assembly, so the value is read
            // out of the source rather than mirrored here - a mirrored copy would
            // pass happily while the shipped constant drifted back to 1.
            var source = File.ReadAllText($"{EditorFolder}/CaptureRig.cs");
            var match = Regex.Match(source, @"DefaultWarmupRenders\s*=\s*(\d+)");
            Assert.That(match.Success, Is.True, "CaptureRig.DefaultWarmupRenders is missing");

            var warmup = int.Parse(match.Groups[1].Value);
            // Convergence was measured at render 2 and held flat through 24.
            Assert.That(warmup, Is.GreaterThanOrEqualTo(2),
                "a single render is the defect this exists to prevent");
            Assert.That(warmup, Is.LessThanOrEqualTo(12),
                "warm-up is cheap but not free; the measurement showed no gain past a handful");
        }

        [Test]
        public void Phase58DocumentationExists()
        {
            Assert.That(File.Exists(DocPath), Is.True, $"Missing project chronicle at {DocPath}");
            var text = File.ReadAllText(DocPath);
            Assert.That(text, Does.Contain("converge"));
            Assert.That(text, Does.Contain("candle"));
        }
    }
}
