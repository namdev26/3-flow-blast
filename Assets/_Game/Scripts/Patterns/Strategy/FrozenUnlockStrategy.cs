using FlowBlast.Core.Events;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public sealed class FrozenUnlockStrategy : IFrozenUnlockStrategy
    {
        private readonly IGameEventBus eventBus;

        public FrozenUnlockStrategy(IGameEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        public bool CanSendToBelt(BoxModel box)
        {
            return !box.IsFrozen;
        }

        public void OnBoxBlasted(BoxModel blastedBox, BoxModel targetBox)
        {
            if (!targetBox.HasFrozenRequirement() || !targetBox.IsFrozen)
            {
                return;
            }

            if (!targetBox.TryReduceFrozenCounter())
            {
                return;
            }

            eventBus.Publish(new BoxFrozenUnlockedEvent(
                targetBox.Id,
                targetBox.GetRemainingFrozenClears()));
        }
    }
}
