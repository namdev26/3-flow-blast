using FlowBlast.Core.Constants;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation.Belt
{
    public static class BeltFollowerTransformUtility
    {
        public static Quaternion SmoothRotation(Quaternion current, Quaternion target, float deltaTime)
        {
            float blend = 1f - Mathf.Exp(-GameConstants.BeltPathRotationSmoothSpeed * deltaTime);
            return Quaternion.Slerp(current, target, blend);
        }

        public static void ApplyPathTransform(
            Transform targetTransform,
            IBeltPath beltPath,
            float beltDistance,
            ref Quaternion smoothedRotation,
            float deltaTime,
            bool snapRotation)
        {
            if (beltPath == null)
            {
                return;
            }

            Vector3 position = beltPath.GetPositionAtDistance(beltDistance);
            Quaternion targetRotation = beltPath.GetRotationAtDistance(beltDistance);

            if (snapRotation || smoothedRotation == default)
            {
                smoothedRotation = targetRotation;
            }
            else
            {
                smoothedRotation = SmoothRotation(smoothedRotation, targetRotation, deltaTime);
            }

            targetTransform.SetPositionAndRotation(position, smoothedRotation);
        }

        public static void ApplyLanePathTransform(
            Transform targetTransform,
            IBeltPath beltPath,
            float beltDistance,
            ref Quaternion smoothedRotation,
            int laneIndex,
            int laneCount,
            float laneSpacing,
            float deltaTime,
            bool snapRotation)
        {
            if (beltPath == null)
            {
                return;
            }

            Vector3 centerPosition = beltPath.GetPositionAtDistance(beltDistance);
            Quaternion targetRotation = beltPath.GetRotationAtDistance(beltDistance);

            if (snapRotation || smoothedRotation == default)
            {
                smoothedRotation = targetRotation;
            }
            else
            {
                smoothedRotation = SmoothRotation(smoothedRotation, targetRotation, deltaTime);
            }

            Vector3 lanePosition = BeltLaneLayout.GetLanePosition(
                centerPosition,
                smoothedRotation,
                laneIndex,
                laneCount,
                laneSpacing);

            targetTransform.SetPositionAndRotation(lanePosition, smoothedRotation);
        }

        public static void ApplyPathPositionOnly(
            Transform targetTransform,
            IBeltPath beltPath,
            float beltDistance)
        {
            if (beltPath == null)
            {
                return;
            }

            targetTransform.position = beltPath.GetPositionAtDistance(beltDistance);
        }
    }
}
