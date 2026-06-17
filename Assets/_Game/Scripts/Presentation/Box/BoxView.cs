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
        private Transform beltParent;
        private float beltDistance;
        private bool isActiveOnBelt;
        private Quaternion smoothedRotation;
        private Vector3 defaultFillScale;

        public BoxModel Model => model;
        public float BeltDistance => beltDistance;
        public bool IsActiveOnBelt => isActiveOnBelt;

        private void Awake()
        {
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
            isActiveOnBelt = true;
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
            UpdateTransform(true);
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

            UpdateTransform(false);
        }

        public void HideCompleted()
        {
            isActiveOnBelt = false;
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

        private void UpdateTransform(bool snapRotation)
        {
            BeltFollowerTransformUtility.ApplyPathTransform(
                transform,
                beltPath,
                beltDistance,
                ref smoothedRotation,
                Time.deltaTime,
                snapRotation);
        }
    }
}
