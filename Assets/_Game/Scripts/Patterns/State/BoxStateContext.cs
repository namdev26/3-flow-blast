using FlowBlast.Domain;

namespace FlowBlast.Patterns.State
{
    public sealed class BoxStateContext
    {
        public BoxModel Box { get; private set; }

        public void Bind(BoxModel box)
        {
            Box = box;
        }
    }
}
