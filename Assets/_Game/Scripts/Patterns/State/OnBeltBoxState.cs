using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class OnBeltBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.OnBelt;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockModel block)
        {
            if (block == null)
            {
                return false;
            }

            bool isMatchingProfile = context.Box.VisualProfile != null || block.VisualProfile != null
                ? context.Box.VisualProfile == block.VisualProfile
                : context.Box.Color == block.Color;

            if (!isMatchingProfile)
            {
                return false;
            }

            context.Box.MarkFilling();
            return true;
        }
    }
}
