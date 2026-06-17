using FlowBlast.Core.Enums;
using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public sealed class HiddenBoxRevealStrategy : IBoxRevealStrategy
    {
        public BlockColor GetDisplayColor(BoxModel box)
        {
            if (box.IsRevealed)
            {
                return box.Color;
            }

            return BlockColor.None;
        }

        public bool ShouldRevealOnBelt(BoxModel box)
        {
            return box.IsHidden && !box.IsRevealed;
        }
    }
}
