using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    /// <summary>
    /// Marks a <see cref="Text"/> as an accent element (for example a reading
    /// section header) so <see cref="TarotUiTheme"/> paints it with the gold
    /// accent colour at runtime instead of the size-based body/muted colour.
    /// Without this the theme's Awake pass would flatten a deliberate gold
    /// hierarchy back to plain ivory.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class TarotUiAccentText : MonoBehaviour
    {
    }
}
