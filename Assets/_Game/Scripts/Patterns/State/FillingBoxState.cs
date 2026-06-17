using FlowBlast.Core.Enums;

namespace FlowBlast.Patterns.State
{
    public sealed class FillingBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.Filling;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockColor blockColor)
        {
            return context.Box.Color == blockColor;
        }
    }
}
