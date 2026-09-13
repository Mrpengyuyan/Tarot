using System;
using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ResultRevealDirector : MonoBehaviour
    {
        // Phase 67: an element that fades in together with revealGroups[groupIndex], so the
        // section headings, the mode label and the spread band join the reveal without making
        // it longer.
        [Serializable]
        public struct RevealCompanion
        {
            public int groupIndex;
            public CanvasGroup group;
        }

        [SerializeField] private CanvasGroup[] revealGroups;
        [SerializeField] private RevealCompanion[] companions = Array.Empty<RevealCompanion>();
        [SerializeField] private float firstDelay = 0.18f;
        [SerializeField] private float groupInterval = 0.16f;
        [SerializeField] private float fadeDuration = 0.28f;

        private Coroutine activeReveal;

        public bool IsRevealComplete { get; private set; }

        private void Awake()
        {
            SetAllVisibleInstant(false);
        }

        public void PlayReveal()
        {
            if (!gameObject.activeInHierarchy)
            {
                SetAllVisibleInstant(true);
                return;
            }

            if (activeReveal != null)
            {
                StopCoroutine(activeReveal);
            }

            activeReveal = StartCoroutine(RevealRoutine());
        }

        public IEnumerator RevealRoutine()
        {
            IsRevealComplete = false;
            SetAllVisibleInstant(false);

            if (firstDelay > 0f)
            {
                yield return new WaitForSeconds(firstDelay);
            }

            if (revealGroups != null)
            {
                for (var i = 0; i < revealGroups.Length; i++)
                {
                    if (revealGroups[i] == null)
                    {
                        continue;
                    }

                    yield return FadeGroup(i, 0f, 1f);

                    if (groupInterval > 0f)
                    {
                        yield return new WaitForSeconds(groupInterval);
                    }
                }
            }

            IsRevealComplete = true;
            activeReveal = null;
        }

        private IEnumerator FadeGroup(int index, float from, float to)
        {
            var duration = Mathf.Max(0.01f, fadeDuration);
            SetGroupState(index, from, false);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                SetGroupAlpha(index, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetGroupState(index, to, true);
        }

        private void SetGroupAlpha(int index, float alpha)
        {
            revealGroups[index].alpha = alpha;
            if (companions == null)
            {
                return;
            }

            foreach (var companion in companions)
            {
                if (companion.groupIndex == index && companion.group != null)
                {
                    companion.group.alpha = alpha;
                }
            }
        }

        private void SetGroupState(int index, float alpha, bool interactive)
        {
            Apply(revealGroups[index], alpha, interactive);
            if (companions == null)
            {
                return;
            }

            foreach (var companion in companions)
            {
                if (companion.groupIndex == index)
                {
                    Apply(companion.group, alpha, interactive);
                }
            }
        }

        private void SetAllVisibleInstant(bool visible)
        {
            IsRevealComplete = visible;
            var alpha = visible ? 1f : 0f;
            if (revealGroups != null)
            {
                foreach (var group in revealGroups)
                {
                    Apply(group, alpha, visible);
                }
            }

            if (companions != null)
            {
                foreach (var companion in companions)
                {
                    Apply(companion.group, alpha, visible);
                }
            }
        }

        private static void Apply(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
