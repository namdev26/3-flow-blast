using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public interface IBoxState
    {
        BoxStateId StateId { get; }
        bool CanSendToBelt(BoxStateContext context);
        bool TryCollect(BoxStateContext context, BlockModel block);
    }
}
