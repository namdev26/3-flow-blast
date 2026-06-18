using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Patterns.Strategy;
using FlowBlast.Services.Belt;

namespace FlowBlast.Services.Box
{
    public sealed class BoxBlastService
    {
        private readonly BeltSlotService beltSlotService;
        private readonly BeltSlotService boxConveyorSlotService;
        private readonly BoxRegistryService boxRegistryService;
        private readonly FrozenUnlockStrategy frozenUnlockStrategy;
        private readonly IGameEventBus eventBus;

        public BoxBlastService(
            BeltSlotService beltSlotService,
            BeltSlotService boxConveyorSlotService,
            BoxRegistryService boxRegistryService,
            FrozenUnlockStrategy frozenUnlockStrategy,
            IGameEventBus eventBus)
        {
            this.beltSlotService = beltSlotService;
            this.boxConveyorSlotService = boxConveyorSlotService;
            this.boxRegistryService = boxRegistryService;
            this.frozenUnlockStrategy = frozenUnlockStrategy;
            this.eventBus = eventBus;
        }

        public bool TryBlast(BoxModel box)
        {
            if (!box.IsFull())
            {
                return false;
            }

            box.MarkBlasting();
            box.MarkCompleted();
            beltSlotService.ReleaseSlot(box);
            boxConveyorSlotService.ReleaseSlot(box);

            eventBus.Publish(new BoxBlastedEvent(box.Id, box.Color));
            NotifyFrozenBoxes(box);

            return true;
        }

        private void NotifyFrozenBoxes(BoxModel blastedBox)
        {
            for (int i = 0; i < boxRegistryService.GetAllBoxes().Count; i++)
            {
                frozenUnlockStrategy.OnBoxBlasted(blastedBox, boxRegistryService.GetAllBoxes()[i]);
            }
        }
    }
}
