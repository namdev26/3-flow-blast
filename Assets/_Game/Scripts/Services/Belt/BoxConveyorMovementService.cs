using System.Collections.Generic;
using FlowBlast.Presentation.Box;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BoxConveyorMovementService
    {
        private readonly IBeltPath boxConveyorPath;
        private readonly BeltFollowerRegistry followerRegistry;
        private float beltSpeed;

        public BoxConveyorMovementService(IBeltPath boxConveyorPath, BeltFollowerRegistry followerRegistry)
        {
            this.boxConveyorPath = boxConveyorPath;
            this.followerRegistry = followerRegistry;
        }

        public void SetSpeed(float speed)
        {
            beltSpeed = speed;
        }

        public void Tick(float deltaTime)
        {
            if (boxConveyorPath == null || boxConveyorPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            float deltaDistance = beltSpeed * deltaTime;
            IReadOnlyList<IBeltFollower> followers = followerRegistry.GetFollowers();

            for (int i = 0; i < followers.Count; i++)
            {
                if (followers[i] is not BoxView boxView || !boxView.IsActiveOnBoxConveyor)
                {
                    continue;
                }

                float nextDistance = boxView.BeltDistance + deltaDistance;
                boxView.SetBeltDistance(nextDistance);
            }
        }
    }
}
