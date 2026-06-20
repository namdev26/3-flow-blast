using FlowBlast.Core.Enums;

namespace FlowBlast.Core.Events
{
    public readonly struct BlockCollectedEvent
    {
        public int BoxId { get; }
        public int BlockId { get; }
        public BlockColor Color { get; }
        public int FilledAmount { get; }
        public int Capacity { get; }

        public BlockCollectedEvent(
            int boxId,
            int blockId,
            BlockColor color,
            int filledAmount,
            int capacity)
        {
            BoxId = boxId;
            BlockId = blockId;
            Color = color;
            FilledAmount = filledAmount;
            Capacity = capacity;
        }
    }

    public readonly struct BoxSentToBeltEvent
    {
        public int BoxId { get; }
        public BlockColor Color { get; }

        public BoxSentToBeltEvent(int boxId, BlockColor color)
        {
            BoxId = boxId;
            Color = color;
        }
    }

    public readonly struct BoxSentToConveyorEvent
    {
        public int BoxId { get; }
        public BlockColor Color { get; }
        public int SlotIndex { get; }

        public BoxSentToConveyorEvent(int boxId, BlockColor color, int slotIndex)
        {
            BoxId = boxId;
            Color = color;
            SlotIndex = slotIndex;
        }
    }

    public readonly struct BoxBlastedEvent
    {
        public int BoxId { get; }
        public BlockColor Color { get; }

        public BoxBlastedEvent(int boxId, BlockColor color)
        {
            BoxId = boxId;
            Color = color;
        }
    }

    public readonly struct BoxFrozenUnlockedEvent
    {
        public int BoxId { get; }
        public int RemainingClears { get; }

        public BoxFrozenUnlockedEvent(int boxId, int remainingClears)
        {
            BoxId = boxId;
            RemainingClears = remainingClears;
        }
    }

    public readonly struct LevelWonEvent
    {
        public string LevelId { get; }

        public LevelWonEvent(string levelId)
        {
            LevelId = levelId;
        }
    }

    public readonly struct LevelLostEvent
    {
        public string LevelId { get; }
        public string Reason { get; }
        public bool CanRevive { get; }
        public int ReviveCountdownSeconds { get; }

        public LevelLostEvent(string levelId, string reason, bool canRevive = false, int reviveCountdownSeconds = 0)
        {
            LevelId = levelId;
            Reason = reason;
            CanRevive = canRevive;
            ReviveCountdownSeconds = reviveCountdownSeconds;
        }
    }
}
