using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Pool;
using FlowBlast.Presentation.Block;
using FlowBlast.Services.Belt;

namespace FlowBlast.Patterns.Factory
{
    public sealed class BlockFactory : IBlockFactory
    {
        private readonly BlockViewPool pool;
        private readonly BlockColorPalette colorPalette;
        private readonly IBeltPath beltPath;
        private int nextId;

        public BlockFactory(BlockViewPool pool, BlockColorPalette colorPalette, IBeltPath beltPath)
        {
            this.pool = pool;
            this.colorPalette = colorPalette;
            this.beltPath = beltPath;
        }

        public BlockModel CreateModel(Core.Enums.BlockColor color)
        {
            nextId++;
            return new BlockModel(nextId, color);
        }

        public BlockView CreateView(BlockModel model)
        {
            BlockView view = pool.Get();
            view.Configure(beltPath);
            view.Bind(model, colorPalette);
            return view;
        }

        public void ReleaseView(BlockView view)
        {
            pool.Release(view);
        }
    }
}
