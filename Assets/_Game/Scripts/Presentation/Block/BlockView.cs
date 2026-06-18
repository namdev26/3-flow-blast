using System;
using System.Collections;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Presentation.Belt;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation.Block
{
    public sealed class BlockView : MonoBehaviour, IBeltFollower
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private MeshRenderer meshRenderer;

        private BlockModel model;
        private IBeltPath beltPath;
        private float beltDistance;
        private int laneIndex;
        private int laneCount = 1;
        private float laneSpacing;
        private bool isActiveOnBelt;
        private bool isFlyingToBox;
        private Coroutine flyCoroutine;
        private Quaternion smoothedRotation;
        private MaterialPropertyBlock propertyBlock;

        public BlockModel Model => model;
        public float BeltDistance => beltDistance;
        public bool IsActiveOnBelt => isActiveOnBelt;
        public bool IsFlyingToBox => isFlyingToBox;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        public void Configure(IBeltPath path)
        {
            beltPath = path;
        }

        public void Bind(BlockModel blockModel, BlockColorPalette palette)
        {
            model = blockModel;

            if (blockModel?.VisualProfile != null)
            {
                ApplyVisualProfile(blockModel.VisualProfile);
                return;
            }

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

        public void BeginCollectFly(Func<Vector3> getTargetPosition, Action onComplete)
        {
            if (flyCoroutine != null)
            {
                StopCoroutine(flyCoroutine);
            }

            isActiveOnBelt = false;
            flyCoroutine = StartCoroutine(FlyToBoxRoutine(
                getTargetPosition,
                GameConstants.BlockFlyToBoxDuration,
                GameConstants.BlockFlyToBoxArcHeight,
                onComplete));
        }

        private IEnumerator FlyToBoxRoutine(
            Func<Vector3> getTargetPosition,
            float duration,
            float arcHeight,
            Action onComplete)
        {
            isFlyingToBox = true;
            Vector3 startPosition = transform.position;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
                float smoothTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
                Vector3 currentTarget = getTargetPosition();
                Vector3 flatPosition = Vector3.Lerp(startPosition, currentTarget, smoothTime);
                float arcOffset = 4f * arcHeight * smoothTime * (1f - smoothTime);
                transform.position = flatPosition + Vector3.up * arcOffset;
                yield return null;
            }

            transform.position = getTargetPosition();
            isFlyingToBox = false;
            flyCoroutine = null;
            onComplete?.Invoke();
        }

        private void LateUpdate()
        {
            if (!isActiveOnBelt || isFlyingToBox)
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
            isFlyingToBox = false;
            smoothedRotation = default;
        }

        private void ApplyColor(Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetTexture(BaseMapId, null);
            propertyBlock.SetTexture(MainTexId, null);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyVisualProfile(BoxVisualProfile visualProfile)
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, visualProfile.TintColor);
            propertyBlock.SetColor(ColorId, visualProfile.TintColor);
            propertyBlock.SetTexture(BaseMapId, visualProfile.BaseTexture);
            propertyBlock.SetTexture(MainTexId, visualProfile.BaseTexture);
            meshRenderer.SetPropertyBlock(propertyBlock);
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
