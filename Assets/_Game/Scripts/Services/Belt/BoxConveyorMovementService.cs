using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Presentation.Box;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BoxConveyorMovementService
    {
        private readonly IBeltPath boxConveyorPath;
        private readonly BeltFollowerRegistry followerRegistry;
        private readonly int maxSlots;
        private float beltSpeed;
        private float conveyorPhase;

        public BoxConveyorMovementService(
            IBeltPath boxConveyorPath,
            BeltFollowerRegistry followerRegistry,
            int maxSlots)
        {
            this.boxConveyorPath = boxConveyorPath;
            this.followerRegistry = followerRegistry;
            this.maxSlots = maxSlots;
        }

        public void SetSpeed(float speed)
        {
            beltSpeed = speed;
        }

        public void Reset()
        {
            conveyorPhase = 0f;
        }

        public float GetSlotDistance(int slotIndex)
        {
            if (boxConveyorPath == null || slotIndex < 0)
            {
                return 0f;
            }

            return BoxConveyorLayout.GetSlotDistance(
                slotIndex,
                boxConveyorPath.TotalLength,
                maxSlots,
                conveyorPhase);
        }

        public void Tick(float deltaTime)
        {
            if (boxConveyorPath == null || boxConveyorPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            conveyorPhase += beltSpeed * deltaTime;
            WrapPhase();

            IReadOnlyList<IBeltFollower> followers = followerRegistry.GetFollowers();

            for (int i = 0; i < followers.Count; i++)
            {
                if (followers[i] is not BoxView boxView || !boxView.IsActiveOnBoxConveyor)
                {
                    continue;
                }

                boxView.SetBeltDistance(GetSlotDistance(boxView.ConveyorSlotIndex));
            }
        }

        private void WrapPhase()
        {
            float pathLength = boxConveyorPath.TotalLength;
            conveyorPhase %= pathLength;

            if (conveyorPhase < 0f)
            {
                conveyorPhase += pathLength;
            }
        }
    }
}
