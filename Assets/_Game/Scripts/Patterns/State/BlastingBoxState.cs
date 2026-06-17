using FlowBlast.Core.Enums;

namespace FlowBlast.Patterns.State
{
    public sealed class BlastingBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.Blasting;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockColor blockColor)
        {
            return false;
        }
    }
}
