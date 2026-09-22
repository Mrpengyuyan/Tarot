using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>
    /// Phase 67: the presenter applies ResultSpreadLayout to the scene, shows external text
    /// literally, splits the card analysis into blocks whose positions match TMP, and routes the
    /// offline notice, the 提醒 section and the bottom divider by source and layout.
    /// </summary>
    public sealed class Phase67ResultPresenterTests
    {
        private const string ScenePath = "Assets/Scenes/Result.unity";

        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        private ResultPanelPresenter presenter;
        private SerializedObject presenterSo;

        [SetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            presenter = Object.FindFirstObjectByType<ResultPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "control: the Result scene has its presenter");
            presenterSo = new SerializedObject(presenter);
        }

        private T Field<T>(string name) where T : Object
        {
            return (T)presenterSo.FindProperty(name).objectReferenceValue;
        }

        private static ReadingSessionSnapshot Offline(int cardCount, string question = "问题？")
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            return LocalReadingSimulator.CreateSession(2, "牌阵", question, "general", draws);
        }

        [Test]
        public void SpreadLayoutIsAppliedToTheSceneForTenCards()
        {
            presenter.PresentSession(Offline(10));
            presenter.ApplyLayout(new Vector2(1280f, 720f));
            var layout = ResultSpreadLayout.Compute(10, 720f);
            var band = GameObject.Find("MP_ResultSpreadBand").transform;

            for (var i = 0; i < 10; i++)
            {
                var cell = (RectTransform)band.Find($"SpreadCell_{i}");
                Assert.That(cell.gameObject.activeSelf, Is.True, $"control: cell {i} is used");
                Assert.That(Vector2.Distance(cell.anchoredPosition, layout.Cells[i].Position), Is.LessThan(0.01f), $"cell {i} position");
                Assert.That(cell.localScale.x, Is.EqualTo(layout.Cells[i].Scale).Within(0.0001f), $"cell {i} scale");
                var label = cell.Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.fontSize * cell.localScale.x, Is.EqualTo(14f).Within(0.01f), $"cell {i} label renders at 14");
            }

            var scroll = Field<RectTransform>("readingScrollRect");
            Assert.That(Vector2.Distance(scroll.anchoredPosition, layout.ReadingPosition), Is.LessThan(0.01f));
            Assert.That(Vector2.Distance(scroll.sizeDelta, layout.ReadingSize), Is.LessThan(0.01f));
        }

        [Test]
        public void OfflineReadingShowsTheNoticeAndHidesTheWarningSection()
        {
            var notice = Field<TMP_Text>("offlineNoticeText");
            var warningHeading = Field<GameObject>("warningHeading");
            var warning = Field<TMP_Text>("warningText");

            foreach (var cardCount in new[] { 1, 3 })
            {
                var session = Offline(cardCount);
                presenter.PresentSession(session);

                Assert.That(notice.gameObject.activeSelf, Is.True, $"{cardCount} card(s): the offline notice shows");
                Assert.That(notice.text, Is.EqualTo(ReleaseUxCopy.OfflineWarning));
                Assert.That(warningHeading.activeSelf, Is.False, $"{cardCount} card(s): no 提醒 section for offline text");
                Assert.That(warning.gameObject.activeSelf, Is.False);
                Assert.That(warning.text, Is.EqualTo(session.warning), "control: the warning copy is still set");
            }
        }

        [Test]
        public void OnlineWarningShowsAsASectionOnSpreads()
        {
            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 67, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(3));
            ReadingSessionMapper.ApplyInterpretation(online, new InterpretationResponse
            {
                id = 1,
                summary = "概要",
                overall_interpretation = "整体",
                warning = "解读仅供参考。",
                model_used = "deepseek-chat",
            });

            presenter.PresentSession(online);

            Assert.That(GameObject.Find("MP_ResultSpreadBand"), Is.Not.Null, "control: the spread layout is showing");
            Assert.That(Field<TMP_Text>("offlineNoticeText").gameObject.activeSelf, Is.False);
            Assert.That(Field<GameObject>("warningHeading").activeSelf, Is.True);
            var warning = Field<TMP_Text>("warningText");
            Assert.That(warning.gameObject.activeSelf, Is.True);
            Assert.That(warning.text, Is.EqualTo("解读仅供参考。"));
        }

        [Test]
        public void ExternalTextIsShownLiterally()
        {
            presenter.PresentSession(Offline(1, "<size=200>大"));
            var question = Field<TMP_Text>("questionText");

            Assert.That(question.text, Is.EqualTo("<noparse><size=200>大</noparse>"));
            question.ForceMeshUpdate();
            Assert.That(question.textInfo.characterCount, Is.EqualTo(11), "every character of the question is laid out");
            Assert.That(question.textInfo.characterInfo[0].character, Is.EqualTo('<'));
            Assert.That(question.textInfo.characterInfo[10].pointSize, Is.EqualTo(question.fontSize).Within(0.01f),
                "the size tag is not applied");

            presenter.PresentSession(Offline(1, "我接下来该专注什么？"));
            Assert.That(question.text, Is.EqualTo("我接下来该专注什么？"), "control: plain text stays byte-identical");
        }

        [Test]
        public void CardBlockHeadingsLineUpWithTmpCharacters()
        {
            var session = Offline(3);
            presenter.PresentSession(session);
            var navigator = Field<ResultReadingNavigator>("readingNavigator");
            var text = Field<TMP_Text>("cardAnalysisText");

            Assert.That(navigator.Blocks.Count, Is.EqualTo(3), "control: the offline analysis was split into blocks");
            text.ForceMeshUpdate();
            var info = text.textInfo;
            for (var i = 0; i < 3; i++)
            {
                var block = navigator.Blocks[i];
                var heading = CardAnalysisFormatter.BuildHeading(session.cardDraws[i]);
                Assert.That(block.HeadingLength, Is.EqualTo(heading.Length));
                Assert.That(info.characterInfo[block.HeadingStart].character, Is.EqualTo(heading[0]),
                    $"block {i}: first heading character");
                Assert.That(info.characterInfo[block.HeadingStart + block.HeadingLength - 1].character,
                    Is.EqualTo(heading[heading.Length - 1]), $"block {i}: last heading character");
            }
        }

        [Test]
        public void SpreadHidesTheBottomDividerAndSingleShowsIt()
        {
            var divider = Field<GameObject>("bottomDivider");

            presenter.PresentSession(Offline(3));
            Assert.That(divider.activeSelf, Is.False, "the divider would cross the taller spread reading");

            presenter.PresentSession(Offline(1));
            Assert.That(divider.activeSelf, Is.True, "the single-card composition keeps its divider");
            var scroll = Field<RectTransform>("readingScrollRect");
            Assert.That(scroll.anchoredPosition, Is.EqualTo(new Vector2(160f, 4f)));
            Assert.That(scroll.sizeDelta, Is.EqualTo(new Vector2(772f, 448f)));
        }

        [Test]
        public void PendingReadingOffersOfflineTextFromTwentySeconds()
        {
            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 68, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(3));
            var offline = Field<Button>("offlineInterpretationButton");
            var retry = Field<Button>("retryInterpretationButton");
            var status = Field<TMP_Text>("interpretationStatusText");

            presenter.PresentSession(online);
            Assert.That(online.interpretationState, Is.EqualTo(InterpretationState.Pending), "control: generating");

            presenter.RefreshPendingState(19.9f);
            Assert.That(offline.gameObject.activeSelf, Is.False, "no offline button before 20 seconds");
            Assert.That(status.text, Is.EqualTo(ReleaseUxCopy.ResultPending));

            presenter.RefreshPendingState(ResultPanelPresenter.PendingSlowNoticeSeconds);
            Assert.That(offline.gameObject.activeSelf, Is.True, "查看离线解读 appears with the slow notice");
            Assert.That(retry.gameObject.activeSelf, Is.False, "retry stays reserved for failures");
            Assert.That(status.text, Does.Contain(ReleaseUxCopy.ResultPendingSlow));
        }

        // Final review M1: heading offsets count what TMP lays out, so characters TMP merges or skips in an
        // earlier body must not push later headings off their first character.

        [Test]
        public void CardBlockHeadingsLineUpWithTmpAfterSupplementaryCharacters()
        {
            var session = Offline(3);
            session.cardAnalysis = "过去：愚者 — 敢于开始的勇气\U0001F319仍在\U0001F319。\n现在：魔术师 — 资源齐备。\n" +
                "建议：女祭司（逆位）— 别只听外界的声音。";

            AssertLaterHeadingsLineUpWithTmp(session);
        }

        [Test]
        public void CardBlockHeadingsLineUpWithTmpAfterVariationSelectors()
        {
            // The first body opens with a selector (TMP skips it after the line feed before the body) and has
            // another one straight after a character.
            var session = Offline(3);
            session.cardAnalysis = "过去：愚者 — ️敢于开始的勇气️仍在。\n现在：魔术师 — 资源齐备。\n" +
                "建议：女祭司（逆位）— 别只听外界的声音。";

            AssertLaterHeadingsLineUpWithTmp(session);
        }

        [Test]
        public void CardBlockHeadingsLineUpWithTmpAfterAnEmojiSprite()
        {
            // The body font has no U+1F60A, so TMP draws it from the default sprite asset (EmojiOne) - and
            // keeps the entry for the variation selector that follows a sprite. Visible drops selectors first.
            var session = Offline(3);
            session.cardAnalysis = "过去：愚者 — 敬于开始的勇气😊️仍在。\n现在：魔术师 — 资源齐备。\n" +
                "建议：女祭司（逆位）— 别只听外界的声音。";

            AssertLaterHeadingsLineUpWithTmp(session);
        }

        private void AssertLaterHeadingsLineUpWithTmp(ReadingSessionSnapshot session)
        {
            presenter.PresentSession(session);
            var navigator = Field<ResultReadingNavigator>("readingNavigator");
            var text = Field<TMP_Text>("cardAnalysisText");
            Assert.That(navigator.Blocks.Count, Is.EqualTo(3), "control: the analysis was split into blocks");

            text.ForceMeshUpdate();
            var info = text.textInfo;
            for (var k = 1; k < 3; k++)
            {
                var block = navigator.Blocks[k];
                var heading = CardAnalysisFormatter.BuildHeading(session.cardDraws[k]);
                Assert.That(block.HeadingStart, Is.LessThan(info.characterCount), $"block {k}: the heading is inside the laid-out text");
                Assert.That(info.characterInfo[block.HeadingStart].character, Is.EqualTo(heading[0]),
                    $"block {k}: first heading character");
            }
        }

        // Final review M4: a label's hit area is as wide as its row pitch allows, so neighbours never overlap.
        [Test]
        public void AdjacentLabelHitAreasDoNotOverlapWithinARow()
        {
            presenter.PresentSession(Offline(3));
            presenter.ApplyLayout(new Vector2(1280f, 720f));
            var band = GameObject.Find("MP_ResultSpreadBand").transform;
            var threeCards = ResultSpreadLayout.Compute(3, 720f);
            for (var i = 0; i < 3; i++)
            {
                var label = (RectTransform)band.Find($"SpreadCell_{i}/Label");
                Assert.That(label.sizeDelta.x, Is.EqualTo(ResultSpreadLayout.BasePitch / threeCards.Cells[i].Scale).Within(0.01f),
                    $"control: with 3 cards label {i} keeps its width");
            }

            foreach (var cardCount in new[] { 5, 10 })
            {
                presenter.PresentSession(Offline(cardCount));
                presenter.ApplyLayout(new Vector2(1280f, 720f));
                var compared = 0;
                for (var i = 0; i + 1 < cardCount; i++)
                {
                    var left = (RectTransform)band.Find($"SpreadCell_{i}");
                    var right = (RectTransform)band.Find($"SpreadCell_{i + 1}");
                    if (Mathf.Abs(left.anchoredPosition.y - right.anchoredPosition.y) > 0.01f)
                    {
                        continue; // card i + 1 starts the second row
                    }

                    compared++;
                    var leftEdges = HorizontalEdgesIn(band, (RectTransform)left.Find("Label"));
                    var rightEdges = HorizontalEdgesIn(band, (RectTransform)right.Find("Label"));
                    Assert.That(leftEdges.max, Is.LessThanOrEqualTo(rightEdges.min + 0.5f),
                        $"{cardCount} cards: label {i}'s hit area overlaps label {i + 1}'s");
                }

                Assert.That(compared, Is.EqualTo(cardCount == 10 ? 8 : 4), $"control: {cardCount} cards, every same-row pair compared");
            }
        }

        // A rect's horizontal extent in an ancestor's space, from local transforms only: an EditMode overlay
        // canvas has no size, so world space is not reliable here.
        private static (float min, float max) HorizontalEdgesIn(Transform ancestor, RectTransform rect)
        {
            var toAncestor = Matrix4x4.identity;
            for (Transform t = rect; t != ancestor; t = t.parent)
            {
                toAncestor = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * toAncestor;
            }

            var r = rect.rect;
            var a = toAncestor.MultiplyPoint3x4(new Vector3(r.xMin, r.center.y, 0f)).x;
            var b = toAncestor.MultiplyPoint3x4(new Vector3(r.xMax, r.center.y, 0f)).x;
            return (Mathf.Min(a, b), Mathf.Max(a, b));
        }

        // Final review M5: presenting a finished detail ends the generating state the way ShowReady does.
        [Test]
        public void PresentingADetailAfterGeneratingLeavesNoGeneratingState()
        {
            var online = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse { id = 69, question = "问题？" }, LocalReadingSimulator.CreatePlaceholderDraws(3));
            var status = Field<TMP_Text>("interpretationStatusText");
            var offline = Field<Button>("offlineInterpretationButton");
            var retry = Field<Button>("retryInterpretationButton");
            var reading = Field<CanvasGroup>("readingContentGroup");
            var navigator = Field<ResultReadingNavigator>("readingNavigator");

            presenter.ShowPending(online);

            // The state ShowReady leaves behind is the reference.
            presenter.ShowReady(online, false);
            Assert.That(navigator.IsInteractive, Is.True, "control: a finished reading is clickable");
            var readyStatusShown = status.gameObject.activeSelf;
            var readyStatusText = status.text;
            var readyOffline = offline.gameObject.activeSelf;
            var readyRetry = retry.gameObject.activeSelf;
            var readyAlpha = reading.alpha;

            presenter.ShowPending(online);
            presenter.RefreshPendingState(ResultPanelPresenter.PendingSlowNoticeSeconds);
            Assert.That(IsPending(), Is.True, "control: generating again");
            Assert.That(status.gameObject.activeSelf, Is.True, "control: the status line shows while generating");
            Assert.That(offline.gameObject.activeSelf, Is.True, "control: 查看离线解读 is offered after 20 seconds");
            Assert.That(navigator.IsInteractive, Is.False, "a generating reading leaves the navigator non-interactive");

            presenter.Present(new PredictionDetailResponse
            {
                id = 69,
                question = "问题？",
                card_draws = LocalReadingSimulator.CreatePlaceholderDraws(3),
                interpretation = new InterpretationResponse
                {
                    id = 1,
                    summary = "概要",
                    overall_interpretation = "整体",
                    model_used = "deepseek-chat",
                },
            });

            Assert.That(IsPending(), Is.False, "Present ends the generating state");
            Assert.That(status.gameObject.activeSelf, Is.EqualTo(readyStatusShown), "the status line is left as ShowReady leaves it");
            Assert.That(status.text, Is.EqualTo(readyStatusText), "the status text is left as ShowReady leaves it");
            Assert.That(offline.gameObject.activeSelf, Is.EqualTo(readyOffline), "查看离线解读 is left as ShowReady leaves it");
            Assert.That(retry.gameObject.activeSelf, Is.EqualTo(readyRetry), "重新解读 is left as ShowReady leaves it");
            Assert.That(reading.alpha, Is.EqualTo(readyAlpha), "the reading is shown as ShowReady leaves it");
            Assert.That(navigator.IsInteractive, Is.True, "the cards are clickable again, as ShowReady leaves them");
        }

        private bool IsPending()
        {
            var field = typeof(ResultPanelPresenter).GetField("isPending", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "ResultPanelPresenter.isPending");
            return (bool)field.GetValue(presenter);
        }
    }
}
