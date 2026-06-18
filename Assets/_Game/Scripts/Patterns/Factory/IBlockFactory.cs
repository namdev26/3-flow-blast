using FlowBlast.Core.Enums;
using FlowBlast.Domain;
using FlowBlast.Patterns.Pool;
using FlowBlast.Presentation.Block;

namespace FlowBlast.Patterns.Factory
{
    public interface IBlockFactory
    {
        bool TryCreateView(BlockModel model, out BlockView view);
        void ReleaseView(BlockView view);
    }
}
