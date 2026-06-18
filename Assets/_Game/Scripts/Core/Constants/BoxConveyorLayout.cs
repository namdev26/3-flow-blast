using UnityEngine;

namespace FlowBlast.Core.Constants
{
    public static class BoxConveyorLayout
    {
        public const float DefaultWaypointSurfaceHeight = 0.3f;
        public const float DefaultSlotStartOffset = 0f;

        /// <summary>
        /// Local positions on BoxConveyorPath (child of Convenyor_Box_Visual).
        /// Scaled oval matching the main belt stadium shape.
        /// </summary>
        public static readonly Vector3[] DefaultOvalWaypointLocalPositions =
        {
            new Vector3(-2.8f, DefaultWaypointSurfaceHeight, 0.4f),
            new Vector3(-1.8f, DefaultWaypointSurfaceHeight, 2.6f),
            new Vector3(0f, DefaultWaypointSurfaceHeight, 2f),
            new Vector3(1.8f, DefaultWaypointSurfaceHeight, 2.6f),
            new Vector3(3f, DefaultWaypointSurfaceHeight, 0.2f),
            new Vector3(1.4f, DefaultWaypointSurfaceHeight, -2.2f),
            new Vector3(0f, DefaultWaypointSurfaceHeight, -3f),
            new Vector3(-1.4f, DefaultWaypointSurfaceHeight, -2f)
        };

        public static float GetSlotSpacing(float pathLength, int maxSlots)
        {
            if (maxSlots <= 0 || pathLength <= Mathf.Epsilon)
            {
                return 0f;
            }

            return pathLength / maxSlots;
        }

        public static float GetSlotBaseOffset(
            int slotIndex,
            float pathLength,
            int maxSlots,
            float startOffset = DefaultSlotStartOffset)
        {
            if (slotIndex < 0)
            {
                return startOffset;
            }

            return startOffset + (slotIndex + 0.5f) * GetSlotSpacing(pathLength, maxSlots);
        }

        public static float GetSlotDistance(
            int slotIndex,
            float pathLength,
            int maxSlots,
            float conveyorPhase = 0f,
            float startOffset = DefaultSlotStartOffset)
        {
            return conveyorPhase + GetSlotBaseOffset(slotIndex, pathLength, maxSlots, startOffset);
        }
    }
}
