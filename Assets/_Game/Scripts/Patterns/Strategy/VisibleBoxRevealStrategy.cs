using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public sealed class VisibleBoxRevealStrategy : IBoxRevealStrategy
    {
        public BlockColor GetDisplayColor(BoxModel box)
        {
            return box.Color;
        }

        public bool ShouldRevealOnBelt(BoxModel box)
        {
            return true;
        }
    }
}
