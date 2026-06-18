using System;
using System.Collections;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Strategy;
using FlowBlast.Presentation.Belt;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation.Box
{
    public sealed class BoxView : MonoBehaviour, IBeltFollower
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform fillIndicator;
        [SerializeField] private BlockColorPalette colorPalette;

        private readonly BoxRevealStrategyResolver revealStrategyResolver = new BoxRevealStrategyResolver();

        private BoxModel model;
        private IBeltPath beltPath;
        private IBeltPath boxConveyorPath;
        private Transform beltParent;
        private Transform boxConveyorParent;
        private float beltDistance;
        private int conveyorSlotIndex = -1;
        private bool isActiveOnMainBelt;
        private bool isActiveOnBoxConveyor;
        private bool isFlyingToConveyor;
        private Coroutine flyCoroutine;
        private Quaternion smoothedRotation;
        private Vector3 defaultFillScale;
        private Transform boxVisualTransform;
        private Vector3 defaultBoxVisualScale;

        public BoxModel Model => model;
        public float BeltDistance => beltDistance;
        public int ConveyorSlotIndex => conveyorSlotIndex;
        public bool IsActiveOnBelt => isActiveOnMainBelt;
        public bool IsActiveOnBoxConveyor => isActiveOnBoxConveyor;
        public bool IsFlyingToConveyor => isFlyingToConveyor;
        public bool CanReceiveBoardClick =>
            model != null
            && !isActiveOnMainBelt
            && !isActiveOnBoxConveyor
            && !isFlyingToConveyor
            && model.CanSendToBelt();

        private void Awake()
        {
            if (meshRenderer != null)
            {
                boxVisualTransform = meshRenderer.transform;
                defaultBoxVisualScale = boxVisualTransform.localScale;
            }

            if (fillIndicator != null)
            {
                defaultFillScale = fillIndicator.localScale;
            }
        }

        public void Configure(IBeltPath path, Transform beltParentTransform, BlockColorPalette palette)
        {
            beltPath = path;
            beltParent = beltParentTransform;
            colorPalette = palette;
        }

        public void ConfigureBoxConveyor(IBeltPath path, Transform parent)
        {
            boxConveyorPath = path;
            boxConveyorParent = parent;
        }

        public void Bind(BoxModel boxModel)
        {
            model = boxModel;
            RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            if (model == null)
            {
                return;
            }

            IBoxRevealStrategy revealStrategy = revealStrategyResolver.Resolve(model);
            BlockColor displayColor = revealStrategy.GetDisplayColor(model);
            ApplyColor(displayColor == BlockColor.None
                ? Color.gray
                : colorPalette.GetUnityColor(displayColor));

            UpdateFillIndicator();
        }

        public void MoveToBelt()
        {
            isFlyingToConveyor = false;
            isActiveOnBoxConveyor = false;
            isActiveOnMainBelt = true;
            conveyorSlotIndex = -1;
            beltDistance = 0f;

            if (beltParent != null)
            {
                transform.SetParent(beltParent, true);
            }

            if (model != null && model.IsHidden)
            {
                model.RevealColor();
            }

            RefreshPresentation();
            smoothedRotation = beltPath.GetRotationAtDistance(beltDistance);
            UpdateMainBeltTransform(true);
        }

        public void BoardOnBoxConveyor(int slotIndex, float slotDistance)
        {
            isFlyingToConveyor = false;
            isActiveOnMainBelt = false;
            isActiveOnBoxConveyor = true;
            conveyorSlotIndex = slotIndex;
            beltDistance = slotDistance;

            if (boxConveyorParent != null)
            {
                transform.SetParent(boxConveyorParent, true);
            }

            RefreshPresentation();
            UpdateBoxConveyorTransform();
        }

        public void SetBeltDistance(float distance)
        {
            beltDistance = distance;
        }

        public void FlyToConveyorTarget(Func<Vector3> getTargetPosition, Action onComplete)
        {
            if (flyCoroutine != null)
            {
                StopCoroutine(flyCoroutine);
            }

            flyCoroutine = StartCoroutine(FlyToConveyorRoutine(
                getTargetPosition,
                GameConstants.BoxFlyToConveyorDuration,
                GameConstants.BoxFlyToConveyorArcHeight,
                onComplete));
        }

        private IEnumerator FlyToConveyorRoutine(
            Func<Vector3> getTargetPosition,
            float duration,
            float arcHeight,
            Action onComplete)
        {
            isFlyingToConveyor = true;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startVisualScale = GetBoardVisualScale();
            Vector3 targetVisualScale = GetConveyorVisualScale();
            ApplyBoxVisualScale(startVisualScale);

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
                float smoothTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
                Vector3 currentTarget = getTargetPosition();
                Vector3 flatPosition = Vector3.Lerp(startPosition, currentTarget, smoothTime);
                float arcOffset = 4f * arcHeight * smoothTime * (1f - smoothTime);
                transform.SetPositionAndRotation(
                    flatPosition + Vector3.up * arcOffset,
                    startRotation);
                ApplyBoxVisualScale(Vector3.Lerp(startVisualScale, targetVisualScale, smoothTime));
                yield return null;
            }

            Vector3 landingTarget = getTargetPosition();
            transform.SetPositionAndRotation(landingTarget, startRotation);
            ApplyBoxVisualScale(targetVisualScale);
            isFlyingToConveyor = false;
            flyCoroutine = null;
            onComplete?.Invoke();
        }

        private void LateUpdate()
        {
            if (isActiveOnBoxConveyor)
            {
                UpdateBoxConveyorTransform();
                return;
            }

            if (!isActiveOnMainBelt)
            {
                return;
            }

            UpdateMainBeltTransform(false);
        }

        public void HideCompleted()
        {
            if (flyCoroutine != null)
            {
                StopCoroutine(flyCoroutine);
                flyCoroutine = null;
            }

            isFlyingToConveyor = false;
            isActiveOnMainBelt = false;
            isActiveOnBoxConveyor = false;
            conveyorSlotIndex = -1;
            gameObject.SetActive(false);
        }

        private void ApplyColor(Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            meshRenderer.material.color = color;
        }

        private void UpdateFillIndicator()
        {
            if (fillIndicator == null || model == null || model.Capacity <= 0)
            {
                return;
            }

            float fillRatio = (float)model.FilledAmount / model.Capacity;
            fillIndicator.localScale = new Vector3(
                defaultFillScale.x,
                defaultFillScale.y * fillRatio,
                defaultFillScale.z);
        }

        private void UpdateMainBeltTransform(bool snapRotation)
        {
            BeltFollowerTransformUtility.ApplyPathTransform(
                transform,
                beltPath,
                beltDistance,
                ref smoothedRotation,
                Time.deltaTime,
                snapRotation);
        }

        private void UpdateBoxConveyorTransform()
        {
            BeltFollowerTransformUtility.ApplyPathPositionOnly(
                transform,
                boxConveyorPath,
                beltDistance);
        }

        private Vector3 GetBoardVisualScale()
        {
            if (defaultBoxVisualScale != Vector3.zero)
            {
                return defaultBoxVisualScale;
            }

            float boardScale = GameConstants.BoxBoardVisualScale;
            return new Vector3(boardScale, boardScale, boardScale);
        }

        private static Vector3 GetConveyorVisualScale()
        {
            float conveyorScale = GameConstants.BoxConveyorVisualScale;
            return new Vector3(conveyorScale, conveyorScale, conveyorScale);
        }

        private void ApplyBoxVisualScale(Vector3 scale)
        {
            if (boxVisualTransform == null)
            {
                return;
            }

            boxVisualTransform.localScale = scale;
        }
    }
}
