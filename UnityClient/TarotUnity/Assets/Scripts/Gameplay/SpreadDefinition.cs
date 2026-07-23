using System;
using UnityEngine;

namespace TarotUnity.Gameplay
{
    /// <summary>One card's resting place on the table: a local position and yaw.</summary>
    [Serializable]
    public struct SlotPlacement
    {
        public Vector3 localPosition;
        public float yawDegrees;
    }

    /// <summary>
    /// Phase 63: a spread is data now, not a hardcoded 1/3 branch. One asset per
    /// spread carries everything the runtime needs - how many cards, where they sit
    /// on the table, what each position is called, and how the camera frames them -
    /// so adding a spread (e.g. the Celtic Cross) is authoring an asset, not editing
    /// switches in the layout, camera, socket-glow and reading systems.
    /// </summary>
    [CreateAssetMenu(menuName = "Tarot Unity/Spread Definition", fileName = "SpreadDefinition")]
    public sealed class SpreadDefinition : ScriptableObject
    {
        public int spreadId;
        public int cardCount;
        public string displayName;

        [Tooltip("Table slots in reading order; length should equal cardCount.")]
        public SlotPlacement[] slots = Array.Empty<SlotPlacement>();

        [Tooltip("Position names in reading order, e.g. 现状 / 挑战 / …")]
        public string[] positionNames = Array.Empty<string>();

        [Tooltip("Optional per-position meaning for the offline reading text.")]
        public string[] positionMeanings = Array.Empty<string>();

        [Header("Camera framing")]
        public Vector3 cameraPosition;
        public Vector3 cameraEuler;
        public float cameraFov = 60f;

        public string PositionName(int index)
        {
            return positionNames != null && index >= 0 && index < positionNames.Length && !string.IsNullOrEmpty(positionNames[index])
                ? positionNames[index]
                : $"第 {index + 1} 位";
        }

        public string PositionMeaning(int index)
        {
            return positionMeanings != null && index >= 0 && index < positionMeanings.Length
                ? positionMeanings[index]
                : string.Empty;
        }
    }
}
