using FlowBlast.Core.Enums;

namespace FlowBlast.Patterns.State
{
    public sealed class OnBeltBoxState : IBoxState
    {
        public BoxStateId StateId => BoxStateId.OnBelt;

        public bool CanSendToBelt(BoxStateContext context)
        {
            return false;
        }

        public bool TryCollect(BoxStateContext context, BlockColor blockColor)
        {
            if (context.Box.Color != blockColor)
            {
                return false;
            }

            context.Box.MarkFilling();
            return true;
        }
    }
}
