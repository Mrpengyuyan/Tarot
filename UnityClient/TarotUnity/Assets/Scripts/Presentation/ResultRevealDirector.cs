using System.Collections;
using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class ResultRevealDirector : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] revealGroups;
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

            foreach (var group in revealGroups)
            {
                if (group == null)
                {
                    continue;
                }

                yield return FadeGroup(group, 0f, 1f);

                if (groupInterval > 0f)
                {
                    yield return new WaitForSeconds(groupInterval);
                }
            }

            IsRevealComplete = true;
            activeReveal = null;
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to)
        {
            var duration = Mathf.Max(0.01f, fadeDuration);
            group.alpha = from;
            group.interactable = false;
            group.blocksRaycasts = false;

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            group.alpha = to;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private void SetAllVisibleInstant(bool visible)
        {
            IsRevealComplete = visible;
            if (revealGroups == null)
            {
                return;
            }

            foreach (var group in revealGroups)
            {
                if (group == null)
                {
                    continue;
                }

                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
        }
    }
}
