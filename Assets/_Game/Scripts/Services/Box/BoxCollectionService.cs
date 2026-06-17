using FlowBlast.Core.Enums;
using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Belt;

namespace FlowBlast.Services.Box
{
    public sealed class BoxCollectionService
    {
        private readonly BeltSlotService beltSlotService;
        private readonly BeltMovementService beltMovementService;
        private readonly IGameEventBus eventBus;

        public BoxCollectionService(
            BeltSlotService beltSlotService,
            BeltMovementService beltMovementService,
            IGameEventBus eventBus)
        {
            this.beltSlotService = beltSlotService;
            this.beltMovementService = beltMovementService;
            this.eventBus = eventBus;
        }

        public bool TryCollect(BlockModel block, float blockBeltDistance)
        {
            if (!beltMovementService.IsNearCollectionPoint(blockBeltDistance))
            {
                return false;
            }

            for (int i = 0; i < beltSlotService.GetActiveBoxes().Count; i++)
            {
                BoxModel box = beltSlotService.GetActiveBoxes()[i];

                if (!box.TryCollect(block.Color))
                {
                    continue;
                }

                eventBus.Publish(new BlockCollectedEvent(
                    box.Id,
                    block.Color,
                    box.FilledAmount,
                    box.Capacity));

                return true;
            }

            return false;
        }
    }
}
