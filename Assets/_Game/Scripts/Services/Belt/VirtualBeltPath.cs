using System.Collections.Generic;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class VirtualBeltPath : IBeltPath
    {
        private readonly CatmullRomPathSampler sampler = new CatmullRomPathSampler();

        public float TotalLength => sampler.TotalLength;

        public VirtualBeltPath(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            float curveStrength,
            Transform parentTransform)
        {
            List<Vector3> worldPositions = new List<Vector3>(localPositions.Count);

            for (int i = 0; i < localPositions.Count; i++)
            {
                Vector3 worldPos = parentTransform != null
                    ? parentTransform.TransformPoint(localPositions[i])
                    : localPositions[i];
                worldPositions.Add(worldPos);
            }

            sampler.Rebuild(worldPositions, closedLoop, curveStrength);
        }

        public Vector3 GetPositionAtDistance(float distance)
        {
            return sampler.GetPositionAtDistance(distance);
        }

        public Quaternion GetRotationAtDistance(float distance)
        {
            return sampler.GetRotationAtDistance(distance);
        }

        public float NormalizeDistance(float distance)
        {
            return sampler.NormalizeDistance(distance);
        }

        public float GetClosestDistance(Vector3 worldPosition)
        {
            return sampler.GetClosestDistance(worldPosition);
        }
    }
}
