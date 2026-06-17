using System;
using FlowBlast.Core.Enums;

namespace FlowBlast.Data
{
    [Serializable]
    public class BoxDefinition
    {
        public BlockColor Color = BlockColor.Green;
        public int Capacity = 3;
        public bool IsHidden;
        public int FrozenClearsRequired;
    }
}
