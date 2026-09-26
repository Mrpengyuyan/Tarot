using System;
using System.IO;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 67 review shots of the Result reading: 1/3/5/10 cards at 16:9, 3 and 10 cards at
    /// 16:10, 3 cards at 4:3, and the generating (20 s), failed and offline states.
    /// READ-ONLY: it presents sample snapshots and renders; the scene is not saved. Each shot
    /// checks the canvas really took the expected height and fails if reading text shows between
    /// the frame's inner gold line and just below the frame. A control render of
    /// Result_10card_16x10 with the viewport mask off proves that check can see unclipped text.
    /// Set PHASE67_CAPTURE_DIR to render a review round somewhere other than Docs.
    /// </summary>
    public static class Phase67ResultReadingCaptureBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string DefaultOutFolder = "Docs/VisualReview/Phase67";
        private const float FrameInnerLineUnits = 14f; // Phase 69 GlassPanel: 8 px margin + inner line centred 5.5 px in, at multiplier 1
        private const float BelowFrameUnits = 6f;      // canvas units checked under the panel's bottom edge
        private const int CornerMargin = 40;           // skip the rounded frame corners
        private const float LightInkTolerance = 0.2f;  // rendered body glyphs sit about 0.1 darker than their authored ink (measured)
        private const float NoticeInkTolerance = 0.1f; // the notice's dark gold is only 0.22 from the frame gold, so stay tight
        private const string ControlShotFile = "Result_10card_16x10.png";

        // Inks reading text is drawn in: the theme's ivory and muted grey and the bootstrapper's body ink
        // (light inks), and the offline notice's dark gold. Gold headings share the frame's gold, so they
        // are not counted.
        private static readonly Color[] LightTextInks =
        {
            new Color(0.96f, 0.91f, 0.80f), new Color(0.92f, 0.88f, 0.78f), new Color(0.74f, 0.72f, 0.76f),
        };

        private static readonly Color NoticeTextInk = new Color(0.78f, 0.66f, 0.44f);

        private static readonly string[] CelticNames =
        {
            "现状", "挑战", "根基", "过去", "顶冠", "未来", "自我", "环境", "希望与恐惧", "结果",
        };

        private sealed class Shot
        {
            public Shot(string file, int width, int height, float expectedCanvasHeight, Action<ResultPanelPresenter> present)
            {
                File = file;
                Width = width;
                Height = height;
                ExpectedCanvasHeight = expectedCanvasHeight;
                Present = present;
            }

            public string File { get; }
            public int Width { get; }
            public int Height { get; }
            public float ExpectedCanvasHeight { get; }
            public Action<ResultPanelPresenter> Present { get; }
        }

        [MenuItem("Tools/Tarot Unity/Run Phase 67 Result Reading Capture")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            var outFolder = Environment.GetEnvironmentVariable("PHASE67_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = DefaultOutFolder;
            }

            var shots = BuildShots();
            Directory.CreateDirectory(outFolder);
            foreach (var shot in shots)
            {
                if (File.Exists(Path.Combine(outFolder, shot.File)))
                {
                    throw new InvalidOperationException($"{outFolder}/{shot.File} already exists; captures never overwrite.");
                }
            }

            foreach (var shot in shots)
            {
                Capture(outFolder, shot);
            }

            Debug.Log($"Phase 67 result reading capture complete -> {outFolder}");
        }

        private static Shot[] BuildShots()
        {
            return new[]
            {
                new Shot("Result_1card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(1))),
                new Shot("Result_3card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Ready())),
                new Shot("Result_5card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(5))),
                new Shot("Result_10card_16x9.png", 2560, 1440, 720f, p => p.PresentSession(Offline(10))),
                new Shot("Result_3card_16x10.png", 2560, 1600, 800f, p => p.PresentSession(Ready())),
                new Shot("Result_10card_16x10.png", 2560, 1600, 800f, p => p.PresentSession(Offline(10))),
                new Shot("Result_3card_4x3.png", 2560, 1920, 960f, p => p.PresentSession(Ready())),
                new Shot("Result_pending20s.png", 2560, 1440, 720f, p =>
                {
                    p.PresentSession(Online());
                    p.RefreshPendingState(ResultPanelPresenter.PendingSlowNoticeSeconds + 1f);
                }),
                new Shot("Result_failed.png", 2560, 1440, 720f, p =>
                {
                    var session = Online();
                    InterpretationPoller.ApplyFailure(session, InterpretationFailure.ConnectionLost);
                    p.PresentSession(session);
                }),
                new Shot("Result_offline.png", 2560, 1440, 720f, p =>
                {
                    var session = Online();
                    InterpretationPoller.ApplyOffline(session);
                    p.PresentSession(session);
                }),
            };
        }

        private static ReadingSessionSnapshot Offline(int cardCount)
        {
            var draws = cardCount == 10
                ? LocalReadingSimulator.CreatePlaceholderDraws(10, CelticNames, null)
                : LocalReadingSimulator.CreatePlaceholderDraws(cardCount);
            var spreadName = cardCount == 10 ? "凯尔特十字" : cardCount == 1 ? "单张牌" : "牌阵";
            return LocalReadingSimulator.CreateSession(2, spreadName, "我这段关系的整体走向？", "general", draws);
        }

        private static ReadingSessionSnapshot Online()
        {
            var session = ReadingSessionMapper.FromBackendStart(
                new PredictionResponse
                {
                    id = 67,
                    spread_type_id = 2,
                    question = "我接下来最该把力气放在哪里？",
                    question_type = "general",
                },
                LocalReadingSimulator.CreatePlaceholderDraws(3));
            session.spreadName = "过去现在未来";
            return session;
        }

        private static ReadingSessionSnapshot Ready()
        {
            var session = Online();
            ReadingSessionMapper.ApplyInterpretation(session, new InterpretationResponse
            {
                id = 1,
                summary = "旧的节奏正在松动，新的方向需要你亲手确认。",
                overall_interpretation = "过去的积累给了你底气，眼下的犹豫来自选择太多。把注意力收回到一件真正重要的事上，" +
                    "局面会比想象中更快清晰。接下来的几周适合收拢精力，先完成手头最关键的一步，再决定要不要扩展。",
                card_analysis = "过去：愚者（正位）— 敢于开始的勇气仍在，旧的节奏正在松动。\n" +
                    "现在：魔术师（正位）— 资源齐备，关键在于把注意力收回到一件事上。\n" +
                    "建议：女祭司（逆位）— 别只听外界的声音，给直觉留一点安静。",
                advice = "这一周只定一个目标，每天为它做一件小事。",
                warning = "解读仅供参考，重要决定请结合现实情况。",
                model_used = "deepseek-chat",
            });
            return session;
        }

        private static void Capture(string outFolder, Shot shot)
        {
            EditorSceneManager.OpenScene(ResultScenePath);
            var canvasObject = GameObject.Find("ResultCanvas");
            var presenter = canvasObject != null ? canvasObject.GetComponent<ResultPanelPresenter>() : null;
            var fit = canvasObject != null ? canvasObject.GetComponent<ResultCanvasAspectFit>() : null;
            var scaler = canvasObject != null ? canvasObject.GetComponent<CanvasScaler>() : null;
            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (presenter == null || fit == null || scaler == null || camera == null)
            {
                throw new InvalidOperationException("Result presenter, aspect fit, canvas scaler or camera not found.");
            }

            var canvasRect = (RectTransform)canvasObject.transform;
            var scroll = (RectTransform)canvasObject.transform.Find("ResultReadingScroll");
            var rt = new RenderTexture(shot.Width, shot.Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(shot.Width, shot.Height, TextureFormat.RGBA32, false);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var prevAspect = camera.aspect;
            var states = PrepareCanvases(camera);

            try
            {
                camera.aspect = (float)shot.Width / shot.Height;
                camera.targetTexture = rt;
                RenderTexture.active = rt;

                // Size the canvas for this shot, then lay the reading out for that size.
                fit.Apply(shot.Width, shot.Height);
                scaler.enabled = false;
                scaler.enabled = true;
                Canvas.ForceUpdateCanvases();

                shot.Present(presenter);

                var canvasSize = canvasRect.rect.size;
                if (Mathf.Abs(canvasSize.y - shot.ExpectedCanvasHeight) > 1f)
                {
                    throw new InvalidOperationException(
                        $"{shot.File}: canvas is {canvasSize.x}x{canvasSize.y}, expected height {shot.ExpectedCanvasHeight}.");
                }

                fit.Repin(canvasSize.y);
                presenter.ApplyLayout(canvasSize);
                RebuildReading(canvasObject);
                Canvas.ForceUpdateCanvases();

                CaptureRig.RenderConverged(camera);
                tex.ReadPixels(new Rect(0, 0, shot.Width, shot.Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, shot.File), tex.EncodeToPNG());

                var pixelsPerUnit = shot.Height / canvasSize.y;
                var outside = CountTextOutsideFrame(tex, camera, scroll, pixelsPerUnit);
                Debug.Log($"Phase 67 capture {shot.File}: canvas={canvasSize.x:0}x{canvasSize.y:0} " +
                    $"readingHeight={scroll.rect.height:0.0} outsideFrameTextPixels={outside}");
                if (outside > 0)
                {
                    throw new InvalidOperationException($"{shot.File}: {outside} reading-text pixels outside the frame's inner gold line.");
                }

                if (shot.File == ControlShotFile)
                {
                    // Control: with the viewport mask off the reading runs past the frame, so the check
                    // must see it. A zero here means the check itself is blind.
                    var mask = scroll.Find("Viewport").GetComponent<RectMask2D>();
                    mask.enabled = false;
                    Canvas.ForceUpdateCanvases();
                    CaptureRig.RenderConverged(camera);
                    tex.ReadPixels(new Rect(0, 0, shot.Width, shot.Height), 0, 0);
                    tex.Apply();
                    mask.enabled = true;
                    var unmasked = CountTextOutsideFrame(tex, camera, scroll, pixelsPerUnit);
                    Debug.Log($"Phase 67 capture control {shot.File}: unmaskedOutsideFrameTextPixels={unmasked}");
                    if (unmasked == 0)
                    {
                        throw new InvalidOperationException(
                            $"{shot.File}: control failed - with the viewport mask off the outside-frame check still counts 0.");
                    }
                }
            }
            finally
            {
                RestoreCanvases(states);
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        private static void RebuildReading(GameObject canvasObject)
        {
            var content = canvasObject.transform.Find("ResultReadingScroll/Viewport/Content") as RectTransform;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }

            // RectMask2D caches its clip rect; toggle it so the capture clips to the layout just
            // applied (Phase 60 lesson).
            foreach (var mask in UnityEngine.Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mask.enabled = false;
                mask.enabled = true;
            }
        }

        // Reading text must never show outside the frame's inner gold line. The rows checked run from
        // that line (FrameInnerLineUnits above the panel's bottom edge, 6 units below the viewport clip)
        // to BelowFrameUnits under the panel, so both a viewport inset shrunk past the gold line and a
        // mask spilling beyond the panel are caught. The band is fixed to the frame's intended inner
        // edge, not to the viewport's actual rect, so it does not move with an inset regression.
        private static int CountTextOutsideFrame(Texture2D tex, Camera camera, RectTransform frame, float pixelsPerUnit)
        {
            var corners = new Vector3[4];
            frame.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
            var bottom = Mathf.RoundToInt(bottomLeft.y);
            var left = Mathf.RoundToInt(bottomLeft.x) + CornerMargin;
            var right = Mathf.RoundToInt(bottomRight.x) - CornerMargin;
            var lowest = bottom - Mathf.RoundToInt(BelowFrameUnits * pixelsPerUnit);
            var highest = bottom + Mathf.RoundToInt(FrameInnerLineUnits * pixelsPerUnit);

            var count = 0;
            for (var y = Mathf.Max(0, lowest); y <= Mathf.Min(tex.height - 1, highest); y++)
            {
                for (var x = Mathf.Max(0, left); x <= Mathf.Min(tex.width - 1, right); x++)
                {
                    if (IsTextInk(tex.GetPixel(x, y)))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool IsTextInk(Color pixel)
        {
            foreach (var ink in LightTextInks)
            {
                if (IsWithin(pixel, ink, LightInkTolerance))
                {
                    return true;
                }
            }

            return IsWithin(pixel, NoticeTextInk, NoticeInkTolerance);
        }

        private static bool IsWithin(Color pixel, Color ink, float tolerance)
        {
            var dr = pixel.r - ink.r;
            var dg = pixel.g - ink.g;
            var db = pixel.b - ink.b;
            return dr * dr + dg * dg + db * db < tolerance * tolerance;
        }

        private static (Canvas c, RenderMode m, Camera cam, float d, bool p)[] PrepareCanvases(Camera camera)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var states = new (Canvas, RenderMode, Camera, float, bool)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                states[i] = (c, c.renderMode, c.worldCamera, c.planeDistance, c.pixelPerfect);
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = 1f;
                c.pixelPerfect = false;
            }

            return states;
        }

        private static void RestoreCanvases((Canvas c, RenderMode m, Camera cam, float d, bool p)[] states)
        {
            foreach (var s in states)
            {
                if (s.c != null)
                {
                    s.c.renderMode = s.m;
                    s.c.worldCamera = s.cam;
                    s.c.planeDistance = s.d;
                    s.c.pixelPerfect = s.p;
                }
            }
        }
    }
}
