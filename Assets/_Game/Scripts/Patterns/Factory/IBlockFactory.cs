using FlowBlast.Core.Enums;
using FlowBlast.Domain;
using FlowBlast.Patterns.Pool;
using FlowBlast.Presentation.Block;

namespace FlowBlast.Patterns.Factory
{
    public interface IBlockFactory
    {
        BlockView CreateView(BlockModel model);
        void ReleaseView(BlockView view);
    }
}
