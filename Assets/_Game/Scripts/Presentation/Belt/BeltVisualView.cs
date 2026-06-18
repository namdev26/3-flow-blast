using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation.Belt
{
    public sealed class BeltVisualView : MonoBehaviour
    {
        private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BeltPath beltPath;
        [SerializeField] private LevelData levelData;
        [SerializeField] private bool syncWithBeltSpeed = true;
        [SerializeField] private float beltSpeed = GameConstants.DefaultBeltSpeed;
        [SerializeField] private float uvRepeatsPerLoop = GameConstants.DefaultBeltUvRepeatsPerLoop;
        [SerializeField] private float manualScrollSpeed = GameConstants.DefaultBeltManualScrollSpeed;
        [SerializeField] private bool isScrollingEnabled = true;
        [SerializeField] private bool autoResolveSceneReferences = true;

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
        }

        private void Start()
        {
            if (autoResolveSceneReferences && beltPath == null)
            {
                Transform pathRoot = TransformHierarchyUtility.FindChildRecursive(
                    transform.root,
                    GameplayZoneNames.BoxConveyorPath);

                if (pathRoot != null)
                {
                    beltPath = pathRoot.GetComponent<BeltPath>();
                }

                if (beltPath == null)
                {
                    beltPath = FindFirstObjectByType<BeltPath>();
                }
            }

            ApplyScrollProperties();
        }

        private void Update()
        {
            if (!isScrollingEnabled || meshRenderer == null)
            {
                return;
            }

            ApplyScrollProperties();
        }

        public void SetScrollingEnabled(bool enabled)
        {
            isScrollingEnabled = enabled;
            ApplyScrollProperties();
        }

        public void SetBeltSpeed(float speed)
        {
            beltSpeed = speed;
            ApplyScrollProperties();
        }

        private void ApplyScrollProperties()
        {
            float scrollSpeed = isScrollingEnabled ? ResolveScrollSpeed() : 0f;

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(ScrollSpeedId, scrollSpeed);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private float ResolveScrollSpeed()
        {
            if (!syncWithBeltSpeed)
            {
                return manualScrollSpeed;
            }

            float activeBeltSpeed = levelData != null ? levelData.BeltSpeed : beltSpeed;

            if (beltPath == null || beltPath.TotalLength <= Mathf.Epsilon)
            {
                return manualScrollSpeed;
            }

            return activeBeltSpeed / beltPath.TotalLength * uvRepeatsPerLoop;
        }
    }
}
