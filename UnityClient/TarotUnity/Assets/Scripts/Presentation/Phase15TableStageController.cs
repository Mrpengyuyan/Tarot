using UnityEngine;

namespace TarotUnity.Presentation
{
    public sealed class Phase15TableStageController : MonoBehaviour
    {
        [SerializeField] private GameObject tableRoot;
        [SerializeField] private Light warmKeyLight;
        [SerializeField] private Light coolRimLight;

        public void SetStageVisible(bool visible)
        {
            if (tableRoot != null)
            {
                tableRoot.SetActive(visible);
            }

            if (warmKeyLight != null)
            {
                warmKeyLight.enabled = visible;
            }

            if (coolRimLight != null)
            {
                coolRimLight.enabled = visible;
            }
        }
    }
}
