using UnityEngine;

namespace TarotUnity.UI
{
    /// <summary>
    /// Marks a text element (for example a reading section header) as an accent
    /// so <see cref="TarotUiTheme"/> paints it with the gold accent colour at
    /// runtime instead of the size-based body/muted colour. Without this the
    /// theme's Awake pass would flatten a deliberate gold hierarchy back to
    /// plain ivory.
    ///
    /// Deliberately not [RequireComponent]-constrained to a text type: screens
    /// migrate from legacy Text to TextMeshPro one at a time (Phase 43), and this
    /// marker has to ride along on either.
    /// </summary>
    public sealed class TarotUiAccentText : MonoBehaviour
    {
    }
}
