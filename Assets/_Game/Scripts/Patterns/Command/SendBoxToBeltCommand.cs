using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Box;
using FlowBlast.Services.Belt;

namespace FlowBlast.Patterns.Command
{
    public sealed class SendBoxToBeltCommand : ICommand
    {
        private readonly BoxQueueService boxQueueService;
        private readonly BeltSlotService beltSlotService;
        private readonly IGameEventBus eventBus;

        public SendBoxToBeltCommand(
            BoxQueueService boxQueueService,
            BeltSlotService beltSlotService,
            IGameEventBus eventBus)
        {
            this.boxQueueService = boxQueueService;
            this.beltSlotService = beltSlotService;
            this.eventBus = eventBus;
        }

        public bool CanExecute()
        {
            if (!beltSlotService.HasAvailableSlot())
            {
                return false;
            }

            BoxModel frontBox = boxQueueService.PeekFrontBox();

            if (frontBox == null)
            {
                return false;
            }

            return frontBox.CanSendToBelt();
        }

        public void Execute()
        {
            if (!CanExecute())
            {
                return;
            }

            BoxModel box = boxQueueService.DequeueFrontBox();

            if (box == null)
            {
                return;
            }

            beltSlotService.OccupySlot(box);
            box.MarkOnBelt();
            box.RevealColor();

            eventBus.Publish(new BoxSentToBeltEvent(box.Id, box.Color));
        }
    }
}
