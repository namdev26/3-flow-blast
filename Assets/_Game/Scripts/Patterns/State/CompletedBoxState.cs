using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class CompletedBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.Completed;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockModel block)
        {
            return false;
        }
    }
}
