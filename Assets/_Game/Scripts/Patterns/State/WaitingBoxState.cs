using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class WaitingBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.Waiting;

        public bool CanSendToBelt(BoxStateContext context)
        {
            if (context.Box.IsFrozen)
            {
                return false;
            }

            return true;
        }

        public bool TryCollect(BoxStateContext context, BlockModel block)
        {
            return false;
        }
    }
}
