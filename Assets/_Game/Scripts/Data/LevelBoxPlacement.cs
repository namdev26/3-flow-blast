using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Data
{
    [System.Serializable]
    public sealed class LevelBoxPlacement
    {
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private BlockColor color = BlockColor.Green;
        [SerializeField] private BoxVisualProfile visualProfile;
        [SerializeField] private int capacity = GameConstants.DefaultBoxCapacity;
        [SerializeField] private bool isHidden;
        [SerializeField] private int frozenClearsRequired;

        public Vector3 LocalPosition
        {
            get => localPosition;
            set => localPosition = value;
        }

        public BlockColor Color
        {
            get => color;
            set => color = value;
        }

        public BoxVisualProfile VisualProfile
        {
            get => visualProfile;
            set => visualProfile = value;
        }

        public int Capacity
        {
            get => capacity;
            set => capacity = Mathf.Max(1, value);
        }

        public bool IsHidden
        {
            get => isHidden;
            set => isHidden = value;
        }

        public int FrozenClearsRequired
        {
            get => frozenClearsRequired;
            set => frozenClearsRequired = Mathf.Max(0, value);
        }

        public BoxDefinition CreateDefinition()
        {
            return new BoxDefinition
            {
                Color = GetResolvedColor(),
                VisualProfile = visualProfile,
                Capacity = Mathf.Max(1, capacity),
                IsHidden = isHidden,
                FrozenClearsRequired = Mathf.Max(0, frozenClearsRequired)
            };
        }

        public void ApplyDefinition(BoxDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Color = definition.VisualProfile != null
                ? definition.VisualProfile.BlockColor
                : definition.Color;
            VisualProfile = definition.VisualProfile;
            Capacity = definition.Capacity;
            IsHidden = definition.IsHidden;
            FrozenClearsRequired = definition.FrozenClearsRequired;
        }

        private BlockColor GetResolvedColor()
        {
            return visualProfile != null ? visualProfile.BlockColor : color;
        }
    }
}
