using FlowBlast.Core.Enums;

namespace FlowBlast.Patterns.State
{
    public interface IBoxState
    {
        BoxStateId StateId { get; }
        bool CanSendToBelt(BoxStateContext context);
        bool TryCollect(BoxStateContext context, BlockColor blockColor);
    }
}
