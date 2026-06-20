using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltMovementService
    {
        private readonly IBeltPath beltPath;
        private readonly BeltFollowerRegistry followerRegistry;
        private readonly bool hasCollectionPointOverride;
        private readonly Vector3 collectionPointWorldPosition;
        private float beltSpeed;

        public BeltMovementService(
            IBeltPath beltPath,
            BeltFollowerRegistry followerRegistry,
            Vector3 collectionPointWorldPosition,
            bool hasCollectionPointOverride)
        {
            this.beltPath = beltPath;
            this.followerRegistry = followerRegistry;
            this.collectionPointWorldPosition = collectionPointWorldPosition;
            this.hasCollectionPointOverride = hasCollectionPointOverride;
        }

        public void SetSpeed(float speed)
        {
            beltSpeed = speed;
        }

        public void Tick(float deltaTime)
        {
            if (beltPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            float deltaDistance = beltSpeed * deltaTime;

            for (int i = 0; i < followerRegistry.GetFollowers().Count; i++)
            {
                IBeltFollower follower = followerRegistry.GetFollowers()[i];

                if (!follower.IsActiveOnBelt)
                {
                    continue;
                }

                float nextDistance = follower.BeltDistance + deltaDistance;
                follower.SetBeltDistance(nextDistance);
            }
        }

        public bool IsNearCollectionPoint(float beltDistance)
        {
            if (!hasCollectionPointOverride)
            {
                return BeltCollectionUtility.IsNearCollectionPoint(
                    beltDistance,
                    beltPath.TotalLength,
                    beltPath.NormalizeDistance(beltDistance));
            }

            float collectionPointDistance = beltPath.GetClosestDistance(collectionPointWorldPosition);
            return Mathf.Abs(Mathf.DeltaAngle(
                beltPath.NormalizeDistance(beltDistance) * 360f,
                beltPath.NormalizeDistance(collectionPointDistance) * 360f))
                <= (GameConstants.CollectionDistanceThreshold / Mathf.Max(beltPath.TotalLength, Mathf.Epsilon)) * 360f;
        }
    }
}
