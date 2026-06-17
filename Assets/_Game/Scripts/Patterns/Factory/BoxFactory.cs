using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.State;
using FlowBlast.Presentation.Box;
using UnityEngine;

namespace FlowBlast.Patterns.Factory
{
    public sealed class BoxFactory : IBoxFactory
    {
        private readonly BoxView prefab;
        private readonly Transform queueParent;
        private readonly Transform beltParent;
        private int nextId;

        public BoxFactory(BoxView prefab, Transform queueParent, Transform beltParent)
        {
            this.prefab = prefab;
            this.queueParent = queueParent;
            this.beltParent = beltParent;
        }

        public BoxModel CreateModel(BoxDefinition definition)
        {
            nextId++;
            BoxStateMachine stateMachine = BoxStateMachineFactory.Create();
            return new BoxModel(nextId, definition, stateMachine);
        }

        public BoxView CreateView(BoxModel model)
        {
            BoxView view = Object.Instantiate(prefab, queueParent);
            view.Bind(model);
            return view;
        }

        public Transform BeltParent => beltParent;
    }
}
