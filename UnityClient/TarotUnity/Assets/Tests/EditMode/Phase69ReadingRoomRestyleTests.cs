using System.Linq;
using NUnit.Framework;
using TMPro;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: the reading room in glass and card stock, sized by its text.</summary>
    public sealed class Phase69ReadingRoomRestyleTests
    {
        private static readonly string[] Chips =
        {
            "Phase7_Progress_ChooseSpread", "Phase7_Progress_AskQuestion", "Phase7_Progress_DrawCards",
            "Phase7_Progress_FlipCards", "Phase7_Progress_RevealResult",
        };

        private static readonly string[] Buttons =
        {
            "OneCardButton", "ThreeCardButton", "CelticCrossButton", "DrawButton", "RevealResultButton",
        };

        private Transform root;

        [SetUp]
        public void Open()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");
            root = GameObject.Find("ReadingRoomCanvas").transform;
        }

        [Test]
        public void ContainersAreGlassWithCornerStars()
        {
            foreach (var path in new[] { "Phase7_RitualHudRoot/Phase7_HudPlate", "Phase11_ActionDock" })
            {
                var image = root.Find(path).GetComponent<Image>();
                Assert.That(image.sprite?.name, Is.EqualTo("GlassPanel"), path);
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), path);
                Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f), path);
                foreach (var star in new[] { "Phase69_Star_TL", "Phase69_Star_BR" })
                {
                    var s = root.Find($"{path}/{star}")?.GetComponent<Image>();
                    Assert.That(s, Is.Not.Null, $"{path}/{star}");
                    Assert.That(s.sprite?.name, Is.EqualTo("Sparkle"));
                    Assert.That(s.raycastTarget, Is.False);
                }
            }
        }

        [Test]
        public void EveryButtonAndChipIsSkinnedAndItsTextFits()
        {
            foreach (var name in Buttons)
            {
                AssertFits(root.Find(name) as RectTransform, name);
            }

            foreach (var name in Chips)
            {
                AssertFits(root.Find($"Phase7_RitualHudRoot/{name}") as RectTransform, name);
            }
        }

        [Test]
        public void ChipPlatesSitBehindTheirLabels()
        {
            foreach (var name in Chips)
            {
                var chip = root.Find($"Phase7_RitualHudRoot/{name}");
                Assert.That(chip.Find("Plate").GetSiblingIndex(), Is.EqualTo(0), name);
            }
        }

        [Test]
        public void OnlyTheCurrentStepWearsCardStock()
        {
            var indicator = Object.FindFirstObjectByType<RitualStepIndicator>();
            foreach (var state in new[]
            {
                ReadingFlowState.SpreadSelect, ReadingFlowState.QuestionInput, ReadingFlowState.Drawing,
                ReadingFlowState.WaitingForFlip, ReadingFlowState.ResultReady,
            })
            {
                indicator.ApplyFlowState(state);
                var current = RitualStepIndicator.StepForState(state);
                for (var i = 0; i < Chips.Length; i++)
                {
                    var skin = root.Find($"Phase7_RitualHudRoot/{Chips[i]}").GetComponent<UiSkinState>();
                    Assert.That(skin.IsEmphasized, Is.EqualTo(i == current), $"{state}: {Chips[i]}");
                }
            }
        }

        [Test]
        public void SceneOpensWithOneCardChosenAndTheActionsInCardStock()
        {
            Assert.That(Skin("OneCardButton").IsEmphasized, Is.True);
            Assert.That(Skin("ThreeCardButton").IsEmphasized, Is.False);
            Assert.That(Skin("CelticCrossButton").IsEmphasized, Is.False);
            Assert.That(Skin("DrawButton").IsEmphasized, Is.True);
            Assert.That(Skin("RevealResultButton").IsEmphasized, Is.True);
            Assert.That(Skin("ThreeCardButton").Glass?.name, Is.EqualTo("GlassPanel"));
            Assert.That(Skin("OneCardButton").CardStock?.name, Is.EqualTo("CardStock"));

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            controller.ApplySpreadEmphasis(10);
            Assert.That(Skin("CelticCrossButton").IsEmphasized, Is.True);
            Assert.That(Skin("OneCardButton").IsEmphasized, Is.False);
        }

        [Test]
        public void PrimaryActionsAreFlankedBySparkles()
        {
            foreach (var name in new[] { "DrawButton", "RevealResultButton" })
            {
                var left = root.Find($"{name}/Phase69_Flank_L") as RectTransform;
                var right = root.Find($"{name}/Phase69_Flank_R") as RectTransform;
                Assert.That(left, Is.Not.Null, name);
                Assert.That(right, Is.Not.Null, name);
                Assert.That(left.anchoredPosition.x, Is.EqualTo(-right.anchoredPosition.x).Within(0.01f));
                var button = root.Find(name) as RectTransform;
                Assert.That(right.anchoredPosition.x + right.rect.width / 2f,
                    Is.LessThanOrEqualTo(button.rect.width / 2f - UiFitLayout.SkinMargin), $"{name}: star inside the stock");
            }
        }

        [Test]
        public void QuestionIsAnUnderlineOverAClickableClearGround()
        {
            var input = root.Find("QuestionInput");
            var ground = input.GetComponent<Image>();
            Assert.That(ground.color.a, Is.EqualTo(0f));
            Assert.That(ground.raycastTarget, Is.True, "the field must stay clickable");
            Assert.That(input.GetComponent<TarotUiPreserveColor>(), Is.Not.Null);
            var line = input.Find("Phase69_InputUnderline") as RectTransform;
            Assert.That(line, Is.Not.Null);
            Assert.That(line.rect.height, Is.EqualTo(1f));
        }

        [Test]
        public void TheDockAndTheLinesBelowItDoNotOverlap()
        {
            var dock = Bounds("Phase11_ActionDock");
            var flow = Bounds("FlowStatusText");
            var release = Bounds("Phase10_ReleaseStatusText");
            Assert.That(dock.yMax, Is.EqualTo(UiFitLayout.DockTop).Within(0.01f));
            Assert.That(flow.yMax, Is.LessThanOrEqualTo(dock.yMin + 0.01f));
            Assert.That(release.yMax, Is.LessThanOrEqualTo(flow.yMin + 0.01f));
            Assert.That(release.yMin, Is.GreaterThanOrEqualTo(-UiFitLayout.CanvasHalfHeight));

            foreach (var name in Buttons.Concat(new[] { "QuestionInput" }))
            {
                var b = Bounds(name);
                Assert.That(b.yMin, Is.GreaterThanOrEqualTo(dock.yMin - 0.01f), name);
                Assert.That(b.yMax, Is.LessThanOrEqualTo(dock.yMax + 0.01f), name);
                Assert.That(b.xMin, Is.GreaterThanOrEqualTo(dock.xMin - 0.01f), name);
                Assert.That(b.xMax, Is.LessThanOrEqualTo(dock.xMax + 0.01f), name);
            }
        }

        [Test]
        public void TypeIsScaledOnceAndTheMutedLimitFollows()
        {
            Assert.That(root.GetComponent<UiTypeScale>().AppliedScale, Is.EqualTo(UiFitLayout.TypeScale));
            Assert.That(root.GetComponent<TarotUiTheme>().MutedSizeThreshold, Is.EqualTo(UiFitLayout.MutedSizeThresholdScaled));
            Assert.That(root.Find("OneCardButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(19.5f));
            Assert.That(root.Find("DrawButton/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
            Assert.That(root.Find("Phase7_RitualHudRoot/Phase7_Progress_DrawCards/Label").GetComponent<TMP_Text>().fontSize, Is.EqualTo(17.5f));
        }

        [Test]
        public void StepDotsSitBetweenTheChips()
        {
            for (var i = 0; i < 4; i++)
            {
                var dot = root.Find($"Phase7_RitualHudRoot/Phase69_StepDot_{i}") as RectTransform;
                Assert.That(dot, Is.Not.Null, $"dot {i}");
                var left = root.Find($"Phase7_RitualHudRoot/{Chips[i]}") as RectTransform;
                var right = root.Find($"Phase7_RitualHudRoot/{Chips[i + 1]}") as RectTransform;
                Assert.That(dot.anchoredPosition.x, Is.GreaterThan(left.anchoredPosition.x));
                Assert.That(dot.anchoredPosition.x, Is.LessThan(right.anchoredPosition.x));
                Assert.That(dot.GetComponent<Image>().raycastTarget, Is.False);
            }
        }

        private UiSkinState Skin(string name) => root.Find(name).GetComponent<UiSkinState>();

        private Rect Bounds(string name)
        {
            var rt = root.Find(name) as RectTransform;
            var p = rt.anchoredPosition;
            var s = rt.sizeDelta;
            return new Rect(p.x - s.x * rt.pivot.x, p.y - s.y * rt.pivot.y, s.x, s.y);
        }

        private static void AssertFits(RectTransform rect, string name)
        {
            Assert.That(rect, Is.Not.Null, name);
            Assert.That(rect.GetComponent<UiSkinState>(), Is.Not.Null, $"{name}: skinned");
            var label = rect.Find("Label").GetComponent<TMP_Text>();
            label.ForceMeshUpdate();
            var preferred = label.GetPreferredValues(label.text);
            var needed = UiFitLayout.FitSize(preferred, label.fontSize);
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(needed.x - 0.01f), $"{name}: width");
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(needed.y - 0.01f), $"{name}: height");
            Assert.That(label.rectTransform.rect.width, Is.GreaterThanOrEqualTo(preferred.x), $"{name}: label rect");
        }
    }
}
