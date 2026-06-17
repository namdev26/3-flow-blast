using UnityEngine;

namespace FlowBlast.Core.Constants
{
    public static class BeltLaneLayout
    {
        public static Vector3 GetLanePosition(
            Vector3 centerPosition,
            Quaternion pathRotation,
            int laneIndex,
            int laneCount,
            float laneSpacing)
        {
            if (laneCount <= 1)
            {
                return centerPosition;
            }

            float centerLane = (laneCount - 1) * 0.5f;
            float lateralOffset = (laneIndex - centerLane) * laneSpacing;
            return centerPosition + pathRotation * Vector3.right * lateralOffset;
        }

        public static int GetTotalBlockCapacity(float pathLength, float rowSpacing, int laneCount)
        {
            if (pathLength <= Mathf.Epsilon || rowSpacing <= Mathf.Epsilon || laneCount <= 0)
            {
                return 0;
            }

            int rowCapacity = Mathf.FloorToInt(pathLength / rowSpacing);
            return rowCapacity * laneCount;
        }
    }
}
