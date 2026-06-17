using FlowBlast.Core.Constants;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltMovementService
    {
        private readonly IBeltPath beltPath;
        private readonly BeltFollowerRegistry followerRegistry;
        private float beltSpeed;

        public BeltMovementService(IBeltPath beltPath, BeltFollowerRegistry followerRegistry)
        {
            this.beltPath = beltPath;
            this.followerRegistry = followerRegistry;
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
            float normalized = beltPath.NormalizeDistance(beltDistance);
            float collectionNormalized = 0.5f;
            float delta = Mathf.Abs(normalized - collectionNormalized);

            if (delta > 0.5f)
            {
                delta = 1f - delta;
            }

            float threshold = GameConstants.CollectionDistanceThreshold / beltPath.TotalLength;
            return delta <= threshold;
        }
    }
}
