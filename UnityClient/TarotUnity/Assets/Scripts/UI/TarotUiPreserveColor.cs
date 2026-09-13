using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Marks a text whose authored colour carries meaning - for example the Phase 67 offline
    /// notice's dark-gold ink - so <see cref="TarotUiTheme"/> keeps that colour while still
    /// applying the role font. Like <see cref="TarotUiAccentText"/>, it is not constrained to a
    /// text type.
    /// </summary>
    public sealed class TarotUiPreserveColor : MonoBehaviour
    {
    }
}
