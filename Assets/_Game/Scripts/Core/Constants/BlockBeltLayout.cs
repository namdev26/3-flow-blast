using FlowBlast.Presentation.Block;
using UnityEngine;

namespace FlowBlast.Core.Constants
{
    public static class BlockBeltLayout
    {
        public const float PackRatio = 0.98f;

        public static float CalculateSpacing(BlockView blockPrefab)
        {
            if (blockPrefab == null)
            {
                return GameConstants.FallbackBlockSpacing;
            }

            Vector3 scale = blockPrefab.transform.localScale;
            float footprint = Mathf.Max(scale.x, scale.z);
            return footprint * PackRatio;
        }
    }
}
