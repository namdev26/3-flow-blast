using System;
using System.Collections;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Strategy;
using FlowBlast.Presentation.Belt;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Box;
using UnityEngine;
using QuickOutline = Outline;

namespace FlowBlast.Presentation.Box
{
    public sealed class BoxView : MonoBehaviour, IBeltFollower
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform fillIndicator;
        [SerializeField] private BlockColorPalette colorPalette;
        [SerializeField] private QuickOutline boxOutline;

        [Header("Box Data")]
        [SerializeField] private BlockColor boxColor = BlockColor.Green;
        [SerializeField] private int capacity = GameConstants.DefaultBoxCapacity;
        [SerializeField] private bool isHidden;
        [SerializeField] private int frozenClearsRequired;

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
        private MaterialPropertyBlock propertyBlock;
        private BoxRegistryService boxRegistryService;

        public BoxModel Model => model;
        public float BeltDistance => beltDistance;
        public int ConveyorSlotIndex => conveyorSlotIndex;
        public bool IsActiveOnBelt => isActiveOnMainBelt;
        public bool IsActiveOnBoxConveyor => isActiveOnBoxConveyor;
        public bool IsFlyingToConveyor => isFlyingToConveyor;
        public bool CanReceiveBoardClick =>
            model != null
            && boxRegistryService != null
            && !isActiveOnMainBelt
            && !isActiveOnBoxConveyor
            && !isFlyingToConveyor
            && model.CanSendToBelt()
            && boxRegistryService.CanSelect(model);

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();

            if (meshRenderer != null)
            {
                boxVisualTransform = meshRenderer.transform;
                defaultBoxVisualScale = boxVisualTransform.localScale;
            }

            if (boxOutline == null)
            {
                boxOutline = GetComponentInChildren<QuickOutline>(true);
            }

            if (fillIndicator != null)
            {
                defaultFillScale = fillIndicator.localScale;
            }
        }

        public void Configure(
            IBeltPath path,
            Transform beltParentTransform,
            BlockColorPalette palette,
            BoxRegistryService registryService = null)
        {
            beltPath = path;
            beltParent = beltParentTransform;
            colorPalette = palette;
            boxRegistryService = registryService;
        }

        public void ConfigureBoxConveyor(IBeltPath path, Transform parent)
        {
            boxConveyorPath = path;
            boxConveyorParent = parent;
        }

        public BoxDefinition CreateDefinition()
        {
            return new BoxDefinition
            {
                Color = boxColor,
                Capacity = capacity,
                IsHidden = isHidden,
                FrozenClearsRequired = frozenClearsRequired
            };
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
            Color fallbackColor = displayColor == BlockColor.None
                ? Color.gray
                : colorPalette.GetUnityColor(displayColor);

            if (model.VisualProfile != null && displayColor != BlockColor.None)
            {
                ApplyVisualProfile(model.VisualProfile, fallbackColor);
            }
            else
            {
                ApplyColor(fallbackColor);
            }

            UpdateFillIndicator();
            UpdateBoardAvailabilityOutline(CanReceiveBoardClick);
        }

        public void UpdateBoardAvailabilityOutline(bool isVisible)
        {
            if (boxOutline == null)
            {
                return;
            }

            boxOutline.enabled = isVisible;
        }

        public void MoveToBelt()
        {
            isFlyingToConveyor = false;
            isActiveOnBoxConveyor = false;
            isActiveOnMainBelt = true;
            conveyorSlotIndex = -1;
            beltDistance = 0f;

            AttachToRuntimeParent(beltParent);

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

            AttachToRuntimeParent(boxConveyorParent);

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

            isActiveOnMainBelt = false;
            isActiveOnBoxConveyor = false;
            conveyorSlotIndex = -1;
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
            ReparentToConveyorForFlight();

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.one;
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
                transform.localScale = Vector3.Lerp(startScale, targetScale, smoothTime);
                yield return null;
            }

            Vector3 landingTarget = getTargetPosition();
            transform.SetPositionAndRotation(landingTarget, startRotation);
            transform.localScale = targetScale;
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

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyVisualProfile(BoxVisualProfile visualProfile, Color fallbackColor)
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            Color tintColor = visualProfile != null ? visualProfile.TintColor : fallbackColor;
            Texture2D baseTexture = visualProfile != null ? visualProfile.BaseTexture : null;
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, tintColor);
            propertyBlock.SetColor(ColorId, tintColor);
            propertyBlock.SetTexture(BaseMapId, baseTexture);
            propertyBlock.SetTexture(MainTexId, baseTexture);
            meshRenderer.SetPropertyBlock(propertyBlock);
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

        private void AttachToRuntimeParent(Transform targetParent)
        {
            if (targetParent == null)
            {
                return;
            }

            transform.SetParent(targetParent, true);
            transform.localScale = Vector3.one;
        }

        private void ReparentToConveyorForFlight()
        {
            if (boxConveyorParent == null)
            {
                return;
            }

            Vector3 worldPosition = transform.position;
            Quaternion worldRotation = transform.rotation;
            Vector3 worldScale = transform.lossyScale;
            transform.SetParent(boxConveyorParent, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = DivideVector3(worldScale, boxConveyorParent.lossyScale);
        }

        private static Vector3 DivideVector3(Vector3 numerator, Vector3 denominator)
        {
            return new Vector3(
                SafeDivide(numerator.x, denominator.x),
                SafeDivide(numerator.y, denominator.y),
                SafeDivide(numerator.z, denominator.z));
        }

        private static float SafeDivide(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) > Mathf.Epsilon
                ? numerator / denominator
                : numerator;
        }

    }
}
