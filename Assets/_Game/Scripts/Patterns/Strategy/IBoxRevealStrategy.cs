using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public interface IBoxRevealStrategy
    {
        BlockColor GetDisplayColor(BoxModel box);
        bool ShouldRevealOnBelt(BoxModel box);
    }
}
