using FlowBlast.Core.Enums;
using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Services.Belt;

namespace FlowBlast.Services.Box
{
    public sealed class BoxCollectionService
    {
        private readonly BeltSlotService beltSlotService;
        private readonly BoxConveyorSlotService boxConveyorSlotService;
        private readonly BeltMovementService beltMovementService;
        private readonly BoxConveyorMovementService boxConveyorMovementService;
        private readonly IGameEventBus eventBus;

        public BoxCollectionService(
            BeltSlotService beltSlotService,
            BoxConveyorSlotService boxConveyorSlotService,
            BeltMovementService beltMovementService,
            BoxConveyorMovementService boxConveyorMovementService,
            IGameEventBus eventBus)
        {
            this.beltSlotService = beltSlotService;
            this.boxConveyorSlotService = boxConveyorSlotService;
            this.beltMovementService = beltMovementService;
            this.boxConveyorMovementService = boxConveyorMovementService;
            this.eventBus = eventBus;
        }

        public bool TryCollect(BlockModel block, float blockBeltDistance, out BoxModel collectedBox)
        {
            collectedBox = null;

            if (!beltMovementService.IsNearCollectionPoint(blockBeltDistance))
            {
                return false;
            }

            if (TryCollectFromConveyor(block, out collectedBox))
            {
                PublishCollectedEvent(block, collectedBox);
                return true;
            }

            if (TryCollectFromMainBelt(block, out collectedBox))
            {
                PublishCollectedEvent(block, collectedBox);
                return true;
            }

            return false;
        }

        private bool TryCollectFromConveyor(BlockModel block, out BoxModel collectedBox)
        {
            collectedBox = null;

            for (int slotIndex = 0; slotIndex < boxConveyorSlotService.MaxSlots; slotIndex++)
            {
                BoxModel box = boxConveyorSlotService.GetBoxAtSlot(slotIndex);

                if (box == null)
                {
                    continue;
                }

                if (box.IsFull())
                {
                    continue;
                }

                if (!box.IsReadyForConveyorCollection)
                {
                    continue;
                }

                float boxDistance = boxConveyorMovementService.GetSlotDistance(slotIndex);

                if (!boxConveyorMovementService.IsNearCollectionPoint(boxDistance))
                {
                    continue;
                }

                if (!box.TryCollect(block.Color))
                {
                    continue;
                }

                collectedBox = box;
                return true;
            }

            return false;
        }

        private bool TryCollectFromMainBelt(BlockModel block, out BoxModel collectedBox)
        {
            collectedBox = null;

            for (int i = 0; i < beltSlotService.GetActiveBoxes().Count; i++)
            {
                BoxModel box = beltSlotService.GetActiveBoxes()[i];

                if (box.IsFull())
                {
                    continue;
                }

                if (!box.TryCollect(block.Color))
                {
                    continue;
                }

                collectedBox = box;
                return true;
            }

            return false;
        }

        private void PublishCollectedEvent(BlockModel block, BoxModel box)
        {
            eventBus.Publish(new BlockCollectedEvent(
                box.Id,
                block.Id,
                block.Color,
                box.FilledAmount,
                box.Capacity));
        }
    }
}
