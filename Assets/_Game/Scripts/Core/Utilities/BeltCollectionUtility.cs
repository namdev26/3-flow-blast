using FlowBlast.Core.Constants;
using UnityEngine;

namespace FlowBlast.Core.Utilities
{
    public static class BeltCollectionUtility
    {
        public static bool IsNearCollectionPoint(float beltDistance, float pathLength, float normalizeDistance)
        {
            if (pathLength <= Mathf.Epsilon)
            {
                return false;
            }

            float normalized = normalizeDistance;
            float collectionNormalized = GameConstants.CollectionZoneNormalized;
            float delta = Mathf.Abs(normalized - collectionNormalized);

            if (delta > 0.5f)
            {
                delta = 1f - delta;
            }

            float threshold = GameConstants.CollectionDistanceThreshold / pathLength;
            return delta <= threshold;
        }
    }
}
