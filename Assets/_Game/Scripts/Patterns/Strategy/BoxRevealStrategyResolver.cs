using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public sealed class BoxRevealStrategyResolver
    {
        private readonly VisibleBoxRevealStrategy visibleStrategy = new VisibleBoxRevealStrategy();
        private readonly HiddenBoxRevealStrategy hiddenStrategy = new HiddenBoxRevealStrategy();

        public IBoxRevealStrategy Resolve(BoxModel box)
        {
            if (box.IsHidden)
            {
                return hiddenStrategy;
            }

            return visibleStrategy;
        }
    }
}
