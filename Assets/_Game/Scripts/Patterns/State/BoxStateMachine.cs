using System.Collections.Generic;
using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class BoxStateMachine
    {
        private readonly Dictionary<BoxStateId, IBoxState> states;
        private readonly BoxStateContext context;
        private IBoxState currentState;

        public BoxStateId CurrentStateId => currentState.StateId;

        public BoxStateMachine(IEnumerable<IBoxState> stateHandlers)
        {
            context = new BoxStateContext();
            states = new Dictionary<BoxStateId, IBoxState>();

            foreach (IBoxState state in stateHandlers)
            {
                states[state.StateId] = state;
            }

            currentState = states[BoxStateId.Waiting];
        }

        public void Bind(BoxModel box)
        {
            context.Bind(box);
        }

        public void TransitionTo(BoxStateId stateId)
        {
            if (!states.TryGetValue(stateId, out IBoxState nextState))
            {
                return;
            }

            currentState = nextState;
        }

        public bool CanSendToBelt()
        {
            return currentState.CanSendToBelt(context);
        }

        public bool TryCollect(BlockColor blockColor)
        {
            return currentState.TryCollect(context, blockColor);
        }
    }
}
