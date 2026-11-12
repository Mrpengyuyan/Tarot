using TarotUnity.Gameplay;
using UnityEngine;

namespace TarotUnity.Presentation
{
    /// <summary>A real 3D destination; only opted-in cards use the rectangular paper surface.</summary>
    public sealed class RitualSpreadSlot : MonoBehaviour
    {
        public const float PaperWidth = RitualPaperSurface.PaperWidth;
        private Camera view;
        private ReadingFlowController flow;

        public void Configure(Camera camera, ReadingFlowController readingFlow) { view = camera; flow = readingFlow; }

        public Vector3 ScaleFor(Transform moving)
        {
            var parent = moving.parent != null ? moving.parent.lossyScale : Vector3.one;
            var scale = transform.lossyScale;
            return new Vector3(scale.x / parent.x, scale.y / parent.y, scale.z / parent.z);
        }

        public void Dock(CardView value)
        {
            if (value == null) return;
            value.transform.SetParent(transform, false);
            value.transform.localPosition = Vector3.zero;
            value.transform.localRotation = Quaternion.identity;
            value.transform.localScale = Vector3.one;
            if (value.FaceArtwork != null) value.FaceArtwork.transform.SetParent(value.transform, false);
            RitualPaperSurface.Attach(value).Back.sortingOrder = 0;
            if (view != null && flow != null) RitualPaperFinish.Attach(value,view,flow);
        }
    }
}
