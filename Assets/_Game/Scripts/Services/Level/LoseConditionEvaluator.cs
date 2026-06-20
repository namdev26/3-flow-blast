using FlowBlast.Services.Block;

namespace FlowBlast.Services.Level
{
    public sealed class LoseConditionEvaluator
    {
        private readonly BlockSpawnService blockSpawnService;
        private readonly int maxBacklogBlocks;

        public LoseConditionEvaluator(BlockSpawnService blockSpawnService, int maxBacklogBlocks)
        {
            this.blockSpawnService = blockSpawnService;
            this.maxBacklogBlocks = maxBacklogBlocks;
        }

        public bool IsLose(out string reason)
        {
            reason = string.Empty;
            return false;
        }
    }
}
