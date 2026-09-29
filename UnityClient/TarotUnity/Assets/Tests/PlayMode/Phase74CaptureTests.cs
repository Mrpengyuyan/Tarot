using System.Collections;
using System.IO;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 74 review frames from a real one-card round - the shuffle, the pick's flight with
    /// the camera following, and the flip - written to $PHASE74_CAPTURE_DIR. Ignored when the
    /// variable is unset, so it never runs in the normal suite and nothing goes into the repo.
    /// </summary>
    public sealed class Phase74CaptureTests
    {
        private const int W = 1600;
        private const int H = 900;

        [UnityTest]
        public IEnumerator CaptureAOneCardRound()
        {
            var dir = System.Environment.GetEnvironmentVariable("PHASE74_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                Assert.Ignore("Set PHASE74_CAPTURE_DIR to write the Phase 74 review frames.");
            }

            Directory.CreateDirectory(dir);
            ReadingSessionStore.Clear();
            SceneManager.LoadScene("ReadingRoom");
            yield return null;
            while (SceneManager.GetActiveScene().name != "ReadingRoom")
            {
                yield return null;
            }

            yield return null;
            var flow = Object.FindFirstObjectByType<ReadingFlowController>();
            var deck = Object.FindFirstObjectByType<DeckController>();
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            GameObject.Find("ReadingRoomCanvas").transform.Find("DrawButton").GetComponent<Button>().onClick.Invoke();

            var start = Time.time;
            var next = 0f;
            var shot = 0;
            while (!fan.AcceptingPicks)
            {
                if (Time.time - start >= next)
                {
                    Capture(dir, $"A_shuffle_{shot++:00}_{Time.time - start:0.00}s");
                    next += 0.2f;
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
            Capture(dir, "B_fan");
            fan.RequestPick(fan.FanCards[fan.FanCards.Count - 1]);
            start = Time.time;
            next = 0f;
            shot = 0;
            while (flow.State != ReadingFlowState.WaitingForFlip)
            {
                if (Time.time - start >= next)
                {
                    Capture(dir, $"C_pick_{shot++:00}_{Time.time - start:0.00}s");
                    next += 0.15f;
                }

                yield return null;
            }

            yield return new WaitForSeconds(1.2f);
            Capture(dir, "D_ready");
            var card = deck.ActiveCards[0];
            card.GetComponent<CardClickHandler>().OnPointerClick(
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            var flipper = card.GetComponent<CardFlipController>();
            start = Time.time;
            next = 0f;
            shot = 0;
            while (Time.time - start < 2.2f)
            {
                if (Time.time - start >= next)
                {
                    Capture(dir, $"E_flip_{shot++:00}_{Time.time - start:0.00}s");
                    next += 0.08f;
                }

                yield return null;
            }

            Assert.That(flipper.IsFlipping, Is.False, "control: the flip finished");
        }

        private static Camera SceneCamera()
        {
            Camera best = null;
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c.isActiveAndEnabled && c.targetTexture == null && (best == null || c.depth > best.depth))
                {
                    best = c;
                }
            }

            return best;
        }

        private static void Capture(string dir, string name)
        {
            var camera = SceneCamera();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.aspect = (float)W / H;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = previous;
            camera.ResetAspect();
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
