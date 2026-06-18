using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Patterns.State;

namespace FlowBlast.Domain
{
    public sealed class BoxModel
    {
        private readonly BoxStateMachine stateMachine;
        private readonly int frozenClearsRequired;

        public int Id { get; }
        public BlockColor Color { get; }
        public BoxVisualProfile VisualProfile { get; }
        public int Capacity { get; }
        public int FilledAmount { get; private set; }
        public bool IsHidden { get; }
        public bool IsFrozen => remainingFrozenClears > 0;
        public bool IsRevealed { get; private set; }
        public bool IsOnBoxConveyor { get; private set; }
        public bool IsReadyForConveyorCollection { get; private set; }
        public BoxStateId CurrentState => stateMachine.CurrentStateId;

        private int remainingFrozenClears;

        public BoxModel(int id, BoxDefinition definition, BoxStateMachine stateMachine)
        {
            Id = id;
            Color = definition.Color;
            VisualProfile = definition.VisualProfile;
            Capacity = definition.Capacity;
            IsHidden = definition.IsHidden;
            frozenClearsRequired = definition.FrozenClearsRequired;
            remainingFrozenClears = definition.FrozenClearsRequired;
            IsRevealed = !definition.IsHidden;
            this.stateMachine = stateMachine;
            stateMachine.Bind(this);
        }

        public bool CanSendToBelt()
        {
            return stateMachine.CanSendToBelt();
        }

        public bool TryCollect(BlockModel block)
        {
            if (block == null)
            {
                return false;
            }

            if (!stateMachine.TryCollect(block))
            {
                return false;
            }

            FilledAmount++;
            return true;
        }

        public bool IsFull()
        {
            return FilledAmount >= Capacity;
        }

        public void MarkOnBelt()
        {
            stateMachine.TransitionTo(BoxStateId.OnBelt);
        }

        public void MarkOnBoxConveyor()
        {
            IsOnBoxConveyor = true;
            IsReadyForConveyorCollection = false;
        }

        public void MarkReadyForConveyorCollection()
        {
            if (!IsOnBoxConveyor)
            {
                return;
            }

            IsReadyForConveyorCollection = true;
        }

        public void MarkFilling()
        {
            stateMachine.TransitionTo(BoxStateId.Filling);
        }

        public void MarkBlasting()
        {
            stateMachine.TransitionTo(BoxStateId.Blasting);
        }

        public void MarkCompleted()
        {
            IsOnBoxConveyor = false;
            IsReadyForConveyorCollection = false;
            stateMachine.TransitionTo(BoxStateId.Completed);
        }

        public void RevealColor()
        {
            IsRevealed = true;
        }

        public bool TryReduceFrozenCounter()
        {
            if (remainingFrozenClears <= 0)
            {
                return false;
            }

            remainingFrozenClears--;
            return true;
        }

        public int GetRemainingFrozenClears()
        {
            return remainingFrozenClears;
        }

        public bool HasFrozenRequirement()
        {
            return frozenClearsRequired > 0;
        }
    }
}
