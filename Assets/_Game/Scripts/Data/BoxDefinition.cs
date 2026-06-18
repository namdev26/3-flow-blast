using System;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;

namespace FlowBlast.Data
{
    [Serializable]
    public class BoxDefinition
    {
        public BlockColor Color = BlockColor.Green;
        public int Capacity = GameConstants.DefaultBoxCapacity;
        public bool IsHidden;
        public int FrozenClearsRequired;
    }
}
