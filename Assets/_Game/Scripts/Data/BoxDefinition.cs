using System;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Data
{
    [Serializable]
    public class BoxDefinition
    {
        public BlockColor Color = BlockColor.Green;
        public BoxVisualProfile VisualProfile;
        public int Capacity = GameConstants.DefaultBoxCapacity;
        public bool IsHidden;
        public int FrozenClearsRequired;
    }
}
