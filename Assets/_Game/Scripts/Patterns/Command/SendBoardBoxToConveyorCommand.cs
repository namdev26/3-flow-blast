using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Box;

namespace FlowBlast.Patterns.Command
{
    public sealed class SendBoardBoxToConveyorCommand
    {
        private readonly BoxConveyorSlotService boxConveyorSlotService;
        private readonly BoxRegistryService boxRegistryService;
        private readonly IGameEventBus eventBus;

        public SendBoardBoxToConveyorCommand(
            BoxConveyorSlotService boxConveyorSlotService,
            BoxRegistryService boxRegistryService,
            IGameEventBus eventBus)
        {
            this.boxConveyorSlotService = boxConveyorSlotService;
            this.boxRegistryService = boxRegistryService;
            this.eventBus = eventBus;
        }

        public bool CanExecute(BoxModel box)
        {
            if (box == null)
            {
                return false;
            }

            if (!boxConveyorSlotService.HasAvailableSlot())
            {
                return false;
            }

            if (!boxRegistryService.CanSelect(box))
            {
                return false;
            }

            return box.CanSendToBelt();
        }

        public bool Execute(BoxModel box)
        {
            if (!CanExecute(box))
            {
                return false;
            }

            if (!boxConveyorSlotService.TryAcquireSlot(box, out int slotIndex))
            {
                return false;
            }

            boxRegistryService.MarkSentToConveyor(box);
            box.MarkOnBelt();
            box.MarkOnBoxConveyor();
            box.RevealColor();
            eventBus.Publish(new BoxSentToConveyorEvent(box.Id, box.Color, slotIndex));
            return true;
        }
    }
}
