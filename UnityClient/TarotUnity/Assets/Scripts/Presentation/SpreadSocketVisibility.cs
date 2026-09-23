using System;
using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Phase 68: only the selected spread's card sockets are on the table. Phase 63 added the
    /// ten Celtic sockets beside the four original ones and nothing hid either set, so all
    /// fourteen outlines were always drawn on top of each other.
    /// Sockets are grouped by card count, the way RitualStepIndicator groups their glows.
    /// Only each socket's own active flag is switched: MP_CardSockets and MP_CelticSockets
    /// stay active, because tests find the Celtic group with GameObject.Find, which skips
    /// inactive objects.
    /// </summary>
    public sealed class SpreadSocketVisibility : MonoBehaviour
    {
        [Serializable]
        public sealed class SpreadSocketSet
        {
            public int cardCount;
            public GameObject[] sockets = Array.Empty<GameObject>();
        }

        [SerializeField] private ReadingFlowController flowController;
        [SerializeField] private SpreadSocketSet[] socketSets = Array.Empty<SpreadSocketSet>();

        private void OnEnable()
        {
            if (flowController == null)
            {
                return;
            }

            flowController.SpreadSelected += Apply;
            Apply(flowController.SelectedSpreadCardCount);
        }

        private void OnDisable()
        {
            if (flowController != null)
            {
                flowController.SpreadSelected -= Apply;
            }
        }

        /// <summary>Shows the sockets of the spread with this many cards and hides every other set.</summary>
        public void Apply(int cardCount)
        {
            foreach (var set in socketSets)
            {
                if (set == null)
                {
                    continue;
                }

                var show = set.cardCount == cardCount;
                foreach (var socket in set.sockets)
                {
                    if (socket != null && socket.activeSelf != show)
                    {
                        socket.SetActive(show);
                    }
                }
            }
        }
    }
}
