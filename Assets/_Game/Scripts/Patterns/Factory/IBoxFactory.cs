using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Presentation.Box;

namespace FlowBlast.Patterns.Factory
{
    public interface IBoxFactory
    {
        BoxView CreateView(BoxModel model);
    }
}
