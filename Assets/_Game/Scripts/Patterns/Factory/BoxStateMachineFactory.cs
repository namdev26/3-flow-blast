using System.Collections.Generic;
using FlowBlast.Patterns.State;

namespace FlowBlast.Patterns.Factory
{
    public static class BoxStateMachineFactory
    {
        public static BoxStateMachine Create()
        {
            IBoxState[] states =
            {
                new WaitingBoxState(),
                new OnBeltBoxState(),
                new FillingBoxState(),
                new BlastingBoxState(),
                new CompletedBoxState()
            };

            return new BoxStateMachine(states);
        }
    }
}
