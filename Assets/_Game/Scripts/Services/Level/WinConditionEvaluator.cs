using FlowBlast.Core.Enums;
using FlowBlast.Services.Block;
using FlowBlast.Services.Box;
using FlowBlast.Services.Belt;

namespace FlowBlast.Services.Level
{
    public sealed class WinConditionEvaluator
    {
        private readonly BoxQueueService boxQueueService;
        private readonly BlockSpawnService blockSpawnService;
        private readonly BeltSlotService beltSlotService;

        public WinConditionEvaluator(
            BoxQueueService boxQueueService,
            BlockSpawnService blockSpawnService,
            BeltSlotService beltSlotService)
        {
            this.boxQueueService = boxQueueService;
            this.blockSpawnService = blockSpawnService;
            this.beltSlotService = beltSlotService;
        }

        public bool IsWin(int completedBoxCount, int requiredBoxCount)
        {
            if (completedBoxCount < requiredBoxCount)
            {
                return false;
            }

            if (boxQueueService.HasWaitingBoxes())
            {
                return false;
            }

            if (beltSlotService.GetActiveBoxes().Count > 0)
            {
                return false;
            }

            if (blockSpawnService.HasActiveBlocks())
            {
                return false;
            }

            return true;
        }
    }
}
