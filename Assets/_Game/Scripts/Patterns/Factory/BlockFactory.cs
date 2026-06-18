using FlowBlast.Core.Enums;
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

        public BlockModel CreateModel(BoxVisualProfile visualProfile)
        {
            nextId++;
            BlockColor color = visualProfile != null ? visualProfile.BlockColor : BlockColor.None;
            return new BlockModel(nextId, color, visualProfile);
        }

        public bool TryCreateView(BlockModel model, out BlockView view)
        {
            if (!pool.TryGet(out view))
            {
                return false;
            }

            view.Configure(beltPath);
            view.Bind(model, colorPalette);
            return true;
        }

        public void ReleaseView(BlockView view)
        {
            pool.Release(view);
        }

        public void ConsumeView(BlockView view)
        {
            pool.Consume(view);
        }

        public void RecyclePool()
        {
            pool.RecycleAll();
        }
    }
}
