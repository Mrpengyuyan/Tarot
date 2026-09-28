using System.Collections;
using NUnit.Framework;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Tests.PlayMode
{
    /// <summary>
    /// Phase 72: the draw waits for the player to pick from the fan. Tests that press
    /// 洗牌抽取 call this to pick for the player - always the middle-most remaining card -
    /// until the fan stops accepting picks.
    /// </summary>
    public static class DrawRitualTestDriver
    {
        public static IEnumerator PickAll(float timeoutSeconds = 30f)
        {
            var fan = Object.FindFirstObjectByType<SpreadFanController>();
            Assert.That(fan, Is.Not.Null, "the reading room has a fan");
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!fan.AcceptingPicks)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the fan never opened for picks");
                yield return null;
            }

            while (fan.AcceptingPicks)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "picking did not finish");
                if (fan.FanCards.Count > 0)
                {
                    fan.RequestPick(fan.FanCards[fan.FanCards.Count / 2]);
                }

                yield return null;
            }
        }
    }
}
