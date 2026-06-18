using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Belt;

namespace FlowBlast.Patterns.Command
{
    public sealed class SendBoardBoxToConveyorCommand
    {
        private readonly BoxConveyorSlotService boxConveyorSlotService;
        private readonly IGameEventBus eventBus;

        public SendBoardBoxToConveyorCommand(
            BoxConveyorSlotService boxConveyorSlotService,
            IGameEventBus eventBus)
        {
            this.boxConveyorSlotService = boxConveyorSlotService;
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

            box.MarkOnBelt();
            box.RevealColor();
            eventBus.Publish(new BoxSentToConveyorEvent(box.Id, box.Color, slotIndex));
            return true;
        }
    }
}
