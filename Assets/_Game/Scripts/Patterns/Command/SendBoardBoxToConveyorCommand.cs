using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Belt;

namespace FlowBlast.Patterns.Command
{
    public sealed class SendBoardBoxToConveyorCommand
    {
        private readonly BeltSlotService boxConveyorSlotService;
        private readonly IGameEventBus eventBus;

        public SendBoardBoxToConveyorCommand(
            BeltSlotService boxConveyorSlotService,
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

            int slotIndex = boxConveyorSlotService.ActiveCount;
            boxConveyorSlotService.OccupySlot(box);
            box.MarkOnBelt();
            box.RevealColor();
            eventBus.Publish(new BoxSentToConveyorEvent(box.Id, box.Color, slotIndex));
            return true;
        }
    }
}
