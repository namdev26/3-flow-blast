using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class FillingBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.Filling;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockModel block)
        {
            if (context.Box.IsFull() || block == null)
            {
                return false;
            }

            if (context.Box.VisualProfile != null || block.VisualProfile != null)
            {
                return context.Box.VisualProfile == block.VisualProfile;
            }

            return context.Box.Color == block.Color;
        }
    }
}
