using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Phase 69: records the type scale already applied under this object, so the restyle
    /// bootstrapper scales by (target / applied) and running it again changes nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTypeScale : MonoBehaviour
    {
        [SerializeField] private float appliedScale = 1f;

        public float AppliedScale
        {
            get => appliedScale;
            set => appliedScale = value;
        }
    }
}
