using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Presentation.Belt;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation.Block
{
    public sealed class BlockView : MonoBehaviour, IBeltFollower
    {
        [SerializeField] private MeshRenderer meshRenderer;

        private BlockModel model;
        private IBeltPath beltPath;
        private float beltDistance;
        private int laneIndex;
        private int laneCount = 1;
        private float laneSpacing;
        private bool isActiveOnBelt;
        private Quaternion smoothedRotation;

        public BlockModel Model => model;
        public float BeltDistance => beltDistance;
        public bool IsActiveOnBelt => isActiveOnBelt;

        public void Configure(IBeltPath path)
        {
            beltPath = path;
        }

        public void Bind(BlockModel blockModel, BlockColorPalette palette)
        {
            model = blockModel;
            ApplyColor(palette.GetUnityColor(blockModel.Color));
        }

        public void ActivateOnBelt(float startDistance, int lane, int totalLanes, float lateralSpacing)
        {
            laneIndex = lane;
            laneCount = totalLanes;
            laneSpacing = lateralSpacing;
            isActiveOnBelt = true;
            beltDistance = startDistance;
            smoothedRotation = beltPath.GetRotationAtDistance(beltDistance);
            ApplyBeltTransform(true);
        }

        public void SetBeltDistance(float distance)
        {
            beltDistance = distance;
        }

        private void LateUpdate()
        {
            if (!isActiveOnBelt)
            {
                return;
            }

            ApplyBeltTransform(false);
        }

        public void ResetView()
        {
            model = null;
            beltDistance = 0f;
            laneIndex = 0;
            laneCount = 1;
            laneSpacing = 0f;
            isActiveOnBelt = false;
            smoothedRotation = default;
        }

        private void ApplyColor(Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            meshRenderer.material.color = color;
        }

        private void ApplyBeltTransform(bool snapRotation)
        {
            BeltFollowerTransformUtility.ApplyLanePathTransform(
                transform,
                beltPath,
                beltDistance,
                ref smoothedRotation,
                laneIndex,
                laneCount,
                laneSpacing,
                Time.deltaTime,
                snapRotation);
        }
    }
}
