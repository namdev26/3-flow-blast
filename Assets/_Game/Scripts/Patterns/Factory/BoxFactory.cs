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
        private readonly Transform beltParent;
        private int nextId;

        public BoxFactory(BoxView prefab, Transform beltParent)
        {
            this.prefab = prefab;
            this.beltParent = beltParent;
        }

        public BoxModel CreateModel(BoxDefinition definition)
        {
            nextId++;
            return CreateModel(nextId, definition);
        }

        public BoxModel CreateModel(int id, BoxDefinition definition)
        {
            nextId = Mathf.Max(nextId, id);
            BoxStateMachine stateMachine = BoxStateMachineFactory.Create();
            return new BoxModel(id, definition, stateMachine);
        }

        public BoxView CreateView(BoxModel model)
        {
            return CreateView(model, beltParent, Vector3.zero);
        }

        public BoxView CreateView(BoxModel model, Transform parent, Vector3 localPosition)
        {
            Transform spawnParent = parent != null ? parent : beltParent;
            BoxView view = Object.Instantiate(prefab, spawnParent);
            view.transform.localPosition = localPosition;
            view.Bind(model);
            return view;
        }

        public Transform BeltParent => beltParent;
    }
}
